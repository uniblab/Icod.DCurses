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

/// <summary>Verifies exact cursor-style restoration through the DCurses session facade.</summary>
public sealed class CursesCursorStyleIntegrationTests {
	[Fact]
	public async Task NestedLeasesRestoreExactObservedBaselineInLifoOrder() {
		CursorStyleTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		Task<CursesCursorStyleLease> outerAcquire = session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBlock,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );
		transport.PublishCursorStyle( 3 );
		CursesCursorStyleLease outer = await outerAcquire;
		CursesCursorStyleLease inner = await session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBar,
			TimeSpan.FromSeconds( 5 )
		);

		Assert.Equal( CursesCursorStyle.SteadyBlock, outer.Style );
		Assert.Equal( CursesCursorStyle.SteadyBar, inner.Style );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[2 q" ), transport.GetWrite( 1 ) );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[6 q" ), transport.GetWrite( 2 ) );

		await inner.DisposeAsync();
		await outer.DisposeAsync();

		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[2 q" ), transport.GetWrite( 3 ) );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[3 q" ), transport.GetWrite( 4 ) );
	}

	[Fact]
	public async Task UnsupportedObservationDoesNotMutateStyle() {
		CursorStyleTransport transport = new();
		await using CursesSession session = await OpenCursesAsync( transport );

		Task<CursesCursorStyleLease> acquire = session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBar,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );
		transport.Publish( Encoding.ASCII.GetBytes( "\u001bP0$r\u001b\\" ) );

		await Assert.ThrowsAsync<NotSupportedException>( () => acquire );
		Assert.Equal( 0, transport.CursorStyleMutationCount );
	}

	[Fact]
	public async Task TimeoutAndCancellationDoNotGuessAReset() {
		CursorStyleTransport timeoutTransport = new();
		await using CursesSession timeoutSession = await OpenCursesAsync( timeoutTransport );
		await Assert.ThrowsAsync<TimeoutException>( () => timeoutSession.AcquireCursorStyleAsync(
			CursesCursorStyle.BlinkingUnderline,
			TimeSpan.Zero
		).AsTask() );
		Assert.Equal( 0, timeoutTransport.CursorStyleMutationCount );

		CursorStyleTransport cancelTransport = new();
		await using CursesSession cancelSession = await OpenCursesAsync( cancelTransport );
		using CancellationTokenSource cancellation = new();
		Task<CursesCursorStyleLease> acquire = cancelSession.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyUnderline,
			TimeSpan.FromSeconds( 5 ),
			cancellation.Token
		).AsTask();
		await cancelTransport.WaitForWriteCountAsync( 1 );
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>( () => acquire );
		Assert.Equal( 0, cancelTransport.CursorStyleMutationCount );
	}

	[Fact]
	public async Task CursorStyleLeaseDoesNotChangeHiddenVisibility() {
		CursorStyleTransport transport = new();
		CursesSession session = await OpenCursesAsync( transport, hideCursor: true );

		Assert.Contains( transport.Writes, write =>
			Encoding.Latin1.GetString( write ) == "<hide>" );
		Task<CursesCursorStyleLease> acquire = session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBar,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 2 );
		transport.PublishCursorStyle( 1 );
		CursesCursorStyleLease lease = await acquire;
		await lease.DisposeAsync();

		Assert.DoesNotContain( transport.Writes, write =>
			Encoding.Latin1.GetString( write ) == "<show>" );

		await session.DisposeAsync();
		Assert.Single( transport.Writes, write =>
			Encoding.Latin1.GetString( write ) == "<show>" );
	}

	[Fact]
	public async Task SessionCloseAndLeaseDisposalRestoreBaselineOnce() {
		CursorStyleTransport transport = new();
		CursesSession session = await OpenCursesAsync( transport );
		Task<CursesCursorStyleLease> acquire = session.AcquireCursorStyleAsync(
			CursesCursorStyle.BlinkingBar,
			TimeSpan.FromSeconds( 5 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );
		transport.PublishCursorStyle( 4 );
		CursesCursorStyleLease lease = await acquire;

		await Task.WhenAll(
			session.DisposeAsync().AsTask(),
			lease.DisposeAsync().AsTask()
		);
		await lease.DisposeAsync();

		Assert.Single( transport.Writes, write =>
			write.AsSpan().SequenceEqual( Encoding.ASCII.GetBytes( "\u001b[4 q" ) ) );
	}

	[Fact]
	public async Task SessionCloseCancelsPendingBaselineObservationAsDisposed() {
		CursorStyleTransport transport = new();
		CursesSession session = await OpenCursesAsync( transport );
		Task<CursesCursorStyleLease> acquire = session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBlock,
			TimeSpan.FromSeconds( 30 )
		).AsTask();
		await transport.WaitForWriteCountAsync( 1 );

		Task disposal = session.DisposeAsync().AsTask();
		await Assert.ThrowsAsync<ObjectDisposedException>( () => acquire );
		await disposal;
		Assert.Equal( 0, transport.CursorStyleMutationCount );
	}

	private static async ValueTask<CursesSession> OpenCursesAsync(
		CursorStyleTransport transport,
		bool hideCursor = false
	) {
		TerminalSession terminal = await TerminalSession.OpenAsync(
			new CursorStyleControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			transport,
			transport,
			new TerminalSessionOptions {
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false,
				InputDecoderOptions = new TerminalInputDecoderOptions {
					EscapeSequenceTimeout = TimeSpan.Zero
				}
			}
		);
		return await CursesSession.OpenAsync(
			terminal,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = hideCursor
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "cursor-style-test" )
			.SetString( StringCapability.CursorInvisible, "<hide>" )
			.SetString( StringCapability.CursorNormal, "<show>" )
			.Build();
	}

	private sealed class CursorStyleTransport : ITerminalInput, ITerminalOutput {
		private readonly object sync = new();
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly List<byte[]> writes = [];
		private readonly SemaphoreSlim writeSignal = new( 0 );

		internal IReadOnlyList<byte[]> Writes {
			get {
				lock ( this.sync ) {
					return this.writes.Select( bytes => bytes.ToArray() ).ToArray();
				}
			}
		}

		internal int CursorStyleMutationCount {
			get {
				lock ( this.sync ) {
					return this.writes.Count( static bytes =>
						5 == bytes.Length
						&& 0x1B == bytes[ 0 ]
						&& (byte)'[' == bytes[ 1 ]
						&& (byte)' ' == bytes[ 3 ]
						&& (byte)'q' == bytes[ 4 ]
					);
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

		internal void PublishCursorStyle( int parameter ) {
			this.Publish( Encoding.ASCII.GetBytes( $"\u001bP1$r{parameter} q\u001b\\" ) );
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
			byte[] bytes = await this.input.Reader.ReadAsync( cancellationToken );
			bytes.AsSpan().CopyTo( buffer.Span );
			return bytes.Length;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
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

	private sealed class CursorStyleControlProvider : ITerminalControlProvider {
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
