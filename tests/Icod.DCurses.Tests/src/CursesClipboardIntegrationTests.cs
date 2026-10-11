/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Tests;

using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Verifies bounded OSC 52 forwarding through the DCurses-owned facade.</summary>
public sealed class CursesClipboardIntegrationTests {
	[Theory]
	[InlineData( CursesClipboardSelection.Clipboard, "c" )]
	[InlineData( CursesClipboardSelection.Primary, "p" )]
	[InlineData( CursesClipboardSelection.Secondary, "q" )]
	[InlineData( CursesClipboardSelection.Select, "s" )]
	public async Task TextWriteMapsSelectionAndStrictUtf8(
		CursesClipboardSelection selection,
		string protocolSelection
	) {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		await session.WriteClipboardAsync( selection, "é" );

		Assert.Equal(
			Encoding.ASCII.GetBytes( $"\u001b]52;{protocolSelection};w6k=\u001b\\" ),
			transport.GetWrite( 0 )
		);
	}

	[Theory]
	[InlineData( CursesClipboardSelection.Clipboard, "c" )]
	[InlineData( CursesClipboardSelection.Primary, "p" )]
	[InlineData( CursesClipboardSelection.Secondary, "q" )]
	[InlineData( CursesClipboardSelection.Select, "s" )]
	public async Task ReadMapsSelectionAndReturnsExactBytes(
		CursesClipboardSelection selection,
		string protocolSelection
	) {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		Task<byte[]> read = session.ReadClipboardAsync(
			selection,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );
		Assert.Equal(
			Encoding.ASCII.GetBytes( $"\u001b]52;{protocolSelection};?\u001b\\" ),
			transport.GetWrite( 0 )
		);
		transport.Publish(
			Encoding.ASCII.GetBytes( $"\u001b]52;{protocolSelection};AP8Q\u001b\\" )
		);

		Assert.Equal( new byte[] { 0x00, 0xFF, 0x10 }, await read );
	}

	[Fact]
	public async Task OmittedOptionsInheritExternalSessionBudgets() {
		ClipboardTransport transport = new();
		TerminalSession terminal = await OpenTerminalAsync(
			transport,
			131_072,
			262_144
		);

		await using CursesSession session = await CursesSession.OpenAsync( terminal );

		Assert.Equal( 131_072, session.Options.ClipboardOptions.MaximumReadBytes );
		Assert.Equal( 262_144, session.Options.ClipboardOptions.MaximumWriteBytes );
	}

	[Fact]
	public async Task ConflictingExternalBudgetsFailBeforeOwnershipTransfer() {
		ClipboardTransport transport = new();
		await using TerminalSession terminal = await OpenTerminalAsync(
			transport,
			131_072,
			262_144
		);

		await Assert.ThrowsAsync<InvalidOperationException>( () => CursesSession.OpenAsync(
			terminal,
			NoPresentationOptions()
		).AsTask() );

		await terminal.WriteClipboardAsync(
			TerminalClipboardSelection.Clipboard,
			new byte[] { 0x41 }
		);
		Assert.Single( transport.Writes );
	}

	[Fact]
	public async Task LargerConfiguredBudgetsAreForwardedWithoutEagerAllocation() {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync(
			transport,
			131_072,
			131_072
		);
		byte[] payload = new byte[ 100_000 ];
		payload[ ^1 ] = 0x5A;

		await session.WriteClipboardAsync(
			CursesClipboardSelection.Clipboard,
			payload
		);

		Assert.True( 100_000 < transport.GetWrite( 0 ).Length );
	}

	[Fact]
	public async Task OpenRefreshAndClosePerformNoAutomaticClipboardIo() {
		ClipboardTransport transport = new();
		CursesSession session = await OpenCursesAsync( transport );

		await session.RefreshAsync();
		await session.DisposeAsync();

		byte[] output = transport.Writes.SelectMany( static bytes => bytes ).ToArray();
		Assert.DoesNotContain(
			"\u001b]52;",
			Encoding.Latin1.GetString( output ),
			StringComparison.Ordinal
		);
	}

	[Fact]
	public async Task InvalidAndOversizedWritesFailBeforeOutput() {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>( () => session.WriteClipboardAsync(
			(CursesClipboardSelection)99,
			new byte[] { 0x41 }
		).AsTask() );
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>( () => session.WriteClipboardAsync(
			CursesClipboardSelection.Clipboard,
			new byte[ 65_537 ]
		).AsTask() );
		await Assert.ThrowsAsync<ArgumentException>( () => session.WriteClipboardAsync(
			CursesClipboardSelection.Clipboard,
			"\uD800"
		).AsTask() );
		Assert.Empty( transport.Writes );
	}

	[Fact]
	public async Task MalformedAndOversizedRepliesKeepTerminalFailures() {
		ClipboardTransport malformedTransport = new();
		await using CursesSession malformed = await OpenCursesAsync( malformedTransport );
		Task<byte[]> malformedRead = malformed.ReadClipboardAsync(
			CursesClipboardSelection.Clipboard,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await malformedTransport.WaitForWriteCountAsync( 1 );
		malformedTransport.Publish( Encoding.ASCII.GetBytes( "\u001b]52;c;Zh==\u001b\\" ) );
		await Assert.ThrowsAsync<FormatException>( () => malformedRead );

		ClipboardTransport oversizedTransport = new();
		await using CursesSession oversized = await OpenCursesAsync(
			oversizedTransport,
			maximumReadBytes: 1,
			maximumWriteBytes: 65_536
		);
		Task<byte[]> oversizedRead = oversized.ReadClipboardAsync(
			CursesClipboardSelection.Clipboard,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await oversizedTransport.WaitForWriteCountAsync( 1 );
		oversizedTransport.Publish( Encoding.ASCII.GetBytes( "\u001b]52;c;YWI=\u001b\\" ) );
		await Assert.ThrowsAsync<FormatException>( () => oversizedRead );
	}

	[Fact]
	public async Task IgnoredQueryReportsTimeoutAndPreservesOrdinaryInput() {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		Task<byte[]> read = session.ReadClipboardAsync(
			CursesClipboardSelection.Clipboard,
			TimeSpan.FromMilliseconds( 100 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );
		await Assert.ThrowsAsync<TimeoutException>( () => read );

		transport.Publish( Encoding.ASCII.GetBytes( "z" ) );
		CursesEvent input = await session.ReadEventAsync( TimeSpan.FromSeconds( 1 ) );
		Assert.Equal( new Rune( 'z' ), input.Input?.Character );
	}

	[Fact]
	public async Task CancellationAndDisposalRemainDistinct() {
		ClipboardTransport cancelTransport = new();
		await using CursesSession cancellable = await OpenCursesAsync( cancelTransport );
		using CancellationTokenSource cancellation = new();
		Task<byte[]> cancelled = cancellable.ReadClipboardAsync(
			CursesClipboardSelection.Clipboard,
			TimeSpan.FromSeconds( 5 ),
			cancellation.Token
		).AsTask();
		await cancelTransport.WaitForWriteCountAsync( 1 );
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>( () => cancelled );

		ClipboardTransport disposeTransport = new();
		CursesSession disposable = await OpenCursesAsync( disposeTransport );
		Task<byte[]> disposed = disposable.ReadClipboardAsync(
			CursesClipboardSelection.Clipboard,
			TimeSpan.FromSeconds( 30 )
		).AsTask();
		await disposeTransport.WaitForWriteCountAsync( 1 );
		Task disposal = disposable.DisposeAsync().AsTask();
		await Assert.ThrowsAsync<ObjectDisposedException>( () => disposed );
		await disposal;
	}

	[Fact]
	public async Task OutputFailureIsPropagated() {
		ClipboardTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );
		transport.WriteFailure = new IOException( "clipboard output failed" );

		IOException failure = await Assert.ThrowsAsync<IOException>( () =>
			session.WriteClipboardAsync(
				CursesClipboardSelection.Clipboard,
				"value"
			).AsTask()
		);

		Assert.Equal( "clipboard output failed", failure.Message );
	}

	private static async ValueTask<CursesSession> OpenCursesAsync(
		ClipboardTransport transport,
		int maximumReadBytes = 65_536,
		int maximumWriteBytes = 65_536
	) {
		TerminalSession terminal = await OpenTerminalAsync(
			transport,
			maximumReadBytes,
			maximumWriteBytes
		);
		return await CursesSession.OpenAsync(
			terminal,
			new CursesSessionOptions {
				ClipboardOptions = new CursesClipboardOptions {
					MaximumReadBytes = maximumReadBytes,
					MaximumWriteBytes = maximumWriteBytes
				},
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
	}

	private static ValueTask<TerminalSession> OpenTerminalAsync(
		ClipboardTransport transport,
		int maximumReadBytes,
		int maximumWriteBytes
	) {
		return TerminalSession.OpenAsync(
			new ClipboardControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			transport,
			transport,
			new TerminalSessionOptions {
				ClipboardOptions = new TerminalClipboardOptions {
					MaximumReadBytes = maximumReadBytes,
					MaximumWriteBytes = maximumWriteBytes
				},
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false,
				InputDecoderOptions = new TerminalInputDecoderOptions {
					EscapeSequenceTimeout = TimeSpan.Zero
				}
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "clipboard-test" )
			.SetString( StringCapability.CursorAddress, "\u001b[%i%p1%d;%p2%dH" )
			.Build();
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private sealed class ClipboardTransport : ITerminalInput, ITerminalOutput {
		private readonly object sync = new();
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly List<byte[]> writes = [];
		private readonly SemaphoreSlim writeSignal = new( 0 );
		private byte[]? pending;
		private int pendingOffset;

		internal Exception? WriteFailure { get; set; }

		internal IReadOnlyList<byte[]> Writes {
			get {
				lock ( this.sync ) {
					return this.writes.Select( bytes => bytes.ToArray() ).ToArray();
				}
			}
		}

		internal byte[] GetWrite( int index ) {
			lock ( this.sync ) {
				return this.writes[ index ].ToArray();
			}
		}

		internal void Publish( byte[] bytes ) {
			if ( !this.input.Writer.TryWrite( bytes.ToArray() ) ) {
				throw new InvalidOperationException( "The input channel is closed." );
			}
		}

		internal async ValueTask WaitForWriteCountAsync( int expected ) {
			using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 5 ) );
			while ( true ) {
				lock ( this.sync ) {
					if ( expected <= this.writes.Count ) {
						return;
					}
				}
				await this.writeSignal.WaitAsync( timeout.Token );
			}
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			if ( this.pending is null ) {
				this.pending = await this.input.Reader.ReadAsync( cancellationToken );
				this.pendingOffset = 0;
			}
			int count = Math.Min( buffer.Length, this.pending.Length - this.pendingOffset );
			this.pending.AsSpan( this.pendingOffset, count ).CopyTo( buffer.Span );
			this.pendingOffset += count;
			if ( this.pendingOffset == this.pending.Length ) {
				this.pending = null;
				this.pendingOffset = 0;
			}
			return count;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( this.WriteFailure is not null ) {
				throw this.WriteFailure;
			}
			lock ( this.sync ) {
				this.writes.Add( buffer.ToArray() );
			}
			this.writeSignal.Release();
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync( CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ClipboardControlProvider : ITerminalControlProvider {
		private readonly TerminalModeSnapshot baseline = TerminalModeSnapshot.CreatePosix(
			0,
			0,
			0,
			0x0002UL,
			new byte[ 32 ],
			0,
			32,
			0,
			new TerminalSpeed( 13, 9600 ),
			new TerminalSpeed( 13, 9600 )
		);

		public TerminalControlResult<TerminalEndpointObservation> Observe(
			TerminalEndpoint endpoint
		) {
			return TerminalControlResult<TerminalEndpointObservation>.Available(
				new TerminalEndpointObservation(
					true,
					null,
					TerminalPlatformKind.PosixTermios,
					TerminalControlCapabilities.Attachment
						| TerminalControlCapabilities.ModeRead
						| TerminalControlCapabilities.ModeWrite
						| TerminalControlCapabilities.LiveSize
				)
			);
		}

		public TerminalControlResult<TerminalSize> GetSize( TerminalEndpoint endpoint ) {
			return TerminalControlResult<TerminalSize>.Available( new TerminalSize( 80, 24 ) );
		}

		public TerminalControlResult<TerminalModeSnapshot> GetMode( TerminalEndpoint endpoint ) {
			return TerminalControlResult<TerminalModeSnapshot>.Available( this.baseline );
		}

		public TerminalControlMutationResult SetMode(
			TerminalEndpoint endpoint,
			TerminalModeSnapshot mode,
			TerminalModeApplyTiming timing
		) {
			return TerminalControlMutationResult.Success();
		}
	}
}
