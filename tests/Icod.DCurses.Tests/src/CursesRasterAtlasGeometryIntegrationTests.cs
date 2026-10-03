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

using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesRasterAtlasGeometryIntegrationTests {
	[Fact]
	public async Task DirectCellQueryProducesFreshExactAtlasGeometry() {
		GeometryTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );

		Task<CursesRasterAtlasGeometry> query = session.QueryRasterAtlasGeometryAsync(
			2,
			3,
			TimeSpan.FromSeconds( 30 )
		).AsTask();
		await WaitForWriteCountAsync( transport, 1 );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[16t" ), transport.GetWrite( 0 ) );
		transport.Publish( Encoding.ASCII.GetBytes( "\u001b[6;20;10t" ) );

		CursesRasterAtlasGeometry geometry = await query;
		Assert.Equal( 10, geometry.TilePixelWidth );
		Assert.Equal( 20, geometry.TilePixelHeight );
		Assert.Equal( 30, geometry.PixelWidth );
		Assert.Equal( 40, geometry.PixelHeight );
	}

	[Fact]
	public async Task OnlyDirectTimeoutFallsBackToExactTerminalPixelDerivation() {
		GeometryTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );

		Task<CursesRasterAtlasGeometry> query = session.QueryRasterAtlasGeometryAsync(
			4,
			5,
			TimeSpan.FromMilliseconds( 100 )
		).AsTask();
		await WaitForWriteCountAsync( transport, 1 );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[16t" ), transport.GetWrite( 0 ) );
		await WaitForWriteCountAsync( transport, 2 );
		Assert.Equal( Encoding.ASCII.GetBytes( "\u001b[14t" ), transport.GetWrite( 1 ) );
		transport.Publish( Encoding.ASCII.GetBytes( "\u001b[4;800;1200t" ) );

		CursesRasterAtlasGeometry geometry = await query;
		Assert.Equal( 10, geometry.TilePixelWidth );
		Assert.Equal( 20, geometry.TilePixelHeight );
		Assert.Equal( 50, geometry.PixelWidth );
		Assert.Equal( 80, geometry.PixelHeight );
	}

	[Fact]
	public async Task PreCanceledQueryWritesNoBytes() {
		GeometryTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => session.QueryRasterAtlasGeometryAsync(
				1,
				1,
				TimeSpan.FromSeconds( 30 ),
				cancellation.Token
			).AsTask()
		);
		Assert.Equal( 0, transport.WriteCount );
	}

	private static async ValueTask<CursesSession> OpenSessionAsync(
		GeometryTransport transport
	) {
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new GeometryTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			transport,
			transport,
			new TerminalSessionOptions {
				TerminalOverride = TerminalProfiles.Dumb,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false,
				InputDecoderOptions = new TerminalInputDecoderOptions {
					EscapeSequenceTimeout = TimeSpan.Zero
				}
			}
		);
		try {
			return await CursesSession.OpenAsync(
				terminalSession,
				new CursesSessionOptions {
					UseAlternateScreen = false,
					EnableKeypad = false,
					HideCursor = false
				}
			);
		} catch {
			await terminalSession.DisposeAsync();
			throw;
		}
	}

	private static async Task WaitForWriteCountAsync(
		GeometryTransport transport,
		int expected
	) {
		using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 15 ) );
		await transport.WaitForWriteCountAsync( expected, timeout.Token );
	}

	private sealed class GeometryTerminalControlProvider : ITerminalControlProvider {
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
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalEndpointObservation>.Available(
				new TerminalEndpointObservation(
					true,
					null,
					TerminalPlatformKind.PosixTermios,
					TerminalControlCapabilities.Attachment
						| TerminalControlCapabilities.LiveSize
						| TerminalControlCapabilities.ModeRead
						| TerminalControlCapabilities.ModeWrite
				)
			);
		}

		public TerminalControlResult<TerminalSize> GetSize(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalSize>.Available( new TerminalSize( 120, 40 ) );
		}

		public TerminalControlResult<TerminalModeSnapshot> GetMode(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalModeSnapshot>.Available( baseline );
		}

		public TerminalControlMutationResult SetMode(
			TerminalEndpoint endpoint,
			TerminalModeSnapshot mode,
			TerminalModeApplyTiming timing
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			ArgumentNullException.ThrowIfNull( mode );
			return TerminalControlMutationResult.Success();
		}
	}

	private sealed class GeometryTransport : ITerminalInput, ITerminalOutput {
		private readonly object sync = new();
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly List<byte[]> writes = [];
		private readonly SemaphoreSlim writeSignal = new( 0 );

		internal int WriteCount {
			get {
				lock ( sync ) {
					return writes.Count;
				}
			}
		}

		internal byte[] GetWrite(
			int index
		) {
			lock ( sync ) {
				return writes[ index ].ToArray();
			}
		}

		internal void Publish(
			byte[] bytes
		) {
			ArgumentNullException.ThrowIfNull( bytes );
			if ( !input.Writer.TryWrite( bytes.ToArray() ) ) {
				throw new InvalidOperationException( "The scripted input channel is closed." );
			}
		}

		internal async ValueTask WaitForWriteCountAsync(
			int expected,
			CancellationToken cancellationToken
		) {
			while ( true ) {
				lock ( sync ) {
					if ( expected <= writes.Count ) {
						return;
					}
				}
				await writeSignal.WaitAsync( cancellationToken ).ConfigureAwait( false );
			}
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			byte[] bytes = await input.Reader.ReadAsync( cancellationToken ).ConfigureAwait( false );
			if ( bytes.Length > buffer.Length ) {
				throw new InvalidOperationException( "The scripted input exceeds the read buffer." );
			}
			bytes.AsSpan().CopyTo( buffer.Span );
			return bytes.Length;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			lock ( sync ) {
				writes.Add( buffer.ToArray() );
			}
			writeSignal.Release();
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}
}
