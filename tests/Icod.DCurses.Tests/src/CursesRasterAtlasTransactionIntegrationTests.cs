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

using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises the complete atlas transaction against a scripted Terminal session.</summary>
[Collection( TerminalProtocolNegotiationCollection.Name )]
public sealed partial class CursesRasterAtlasTransactionIntegrationTests {
	[Fact]
	public async Task CreationProjectionAndPresentationUseDeterministicOrder() {
		AtlasTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );
		TerminalRasterImage initial = Rgb24( 2, 2, 0 );
		CursesRasterAtlas atlas = await CreateAtlasAsync( session, transport, initial );
		await using ( atlas ) {
			Assert.Equal( 2, atlas.Rows );
			Assert.Equal( 2, atlas.Columns );
			Assert.Equal( 1, atlas.TilePixelWidth );
			Assert.Equal( 1, atlas.TilePixelHeight );
			Assert.Equal( CursesRasterOwnershipStatus.Current, atlas.OwnershipState.Status );
			Assert.False( atlas.RequiresRecreation );
			Assert.DoesNotContain(
				typeof( CursesRasterAtlas ).GetFields(
					BindingFlags.Instance | BindingFlags.NonPublic
				),
				field => typeof( TerminalRasterImage ).IsAssignableFrom( field.FieldType )
			);

			CursesWindow window = session.StandardScreen;
			int cursorRow = window.CursorRow;
			int cursorColumn = window.CursorColumn;
			window.WriteRasterAtlas(
				1,
				1,
				atlas,
				new CursesRectangle( 0, 0, 2, 2 )
			);
			Assert.Equal( cursorRow, window.CursorRow );
			Assert.Equal( cursorColumn, window.CursorColumn );
			for ( int row = 0; row < 2; row++ ) {
				for ( int column = 0; column < 2; column++ ) {
					CursesRasterCell expected = atlas.GetCell( row, column );
					CursesRasterCell actual = Assert.IsType<CursesRasterCell>(
						window.GetRasterCell( row + 1, column + 1 )
					);
					Assert.True( expected.Equals( actual ) );
				}
			}

			int presentationStart = transport.WriteCount;
			Task<CursesRasterAtlasPresentationResult> presentation = atlas.PresentAsync(
				[
					new CursesRasterAtlasTileUpdate( 1, 1, Rgb24( 1, 1, 4 ) ),
					new CursesRasterAtlasTileUpdate( 0, 0, Rgb24( 1, 1, 1 ) )
				]
			).AsTask();
			await transport.WaitForWriteCountAsync( presentationStart + 1 );
			string expectedComposition =
				"\u001b_Ga=c,i=77,r=1,c=2,w=2,h=2,X=0,Y=0,x=0,y=0,C=1\u001b\\";
			string actualComposition = transport.GetAsciiWrite( presentationStart );
			Assert.True(
				string.Equals( expectedComposition, actualComposition, StringComparison.Ordinal ),
				$"Expected composition bytes: {Convert.ToHexString( Encoding.ASCII.GetBytes( expectedComposition ) )}; actual: {Convert.ToHexString( Encoding.ASCII.GetBytes( actualComposition ) )}."
			);
			transport.PublishOk();

			await transport.WaitForWriteCountAsync( presentationStart + 2 );
			string expectedFirstUpdate =
				"\u001b_Ga=f,f=24,s=1,v=1,t=d,i=77,r=2,x=0,y=0,X=1,m=0;AQID\u001b\\";
			string actualFirstUpdate = transport.GetAsciiWrite( presentationStart + 1 );
			Assert.True(
				string.Equals( expectedFirstUpdate, actualFirstUpdate, StringComparison.Ordinal ),
				$"Expected first-update bytes: {Convert.ToHexString( Encoding.ASCII.GetBytes( expectedFirstUpdate ) )}; actual: {Convert.ToHexString( Encoding.ASCII.GetBytes( actualFirstUpdate ) )}."
			);
			transport.PublishOk();

			await transport.WaitForWriteCountAsync( presentationStart + 3 );
			Assert.Equal(
				"\u001b_Ga=f,f=24,s=1,v=1,t=d,i=77,r=2,x=1,y=1,X=1,m=0;BAUG\u001b\\",
				transport.GetAsciiWrite( presentationStart + 2 )
			);
			transport.PublishOk();

			await transport.WaitForWriteCountAsync( presentationStart + 4 );
			Assert.Equal(
				"\u001b_Ga=a,i=77,c=2\u001b\\",
				transport.GetAsciiWrite( presentationStart + 3 )
			);
			transport.PublishOk();

			CursesRasterAtlasPresentationResult result = await presentation;
			Assert.Equal( CursesRasterAtlasPresentationStatus.Presented, result.Status );
			Assert.Equal( 2, result.RequestedUpdateCount );
			Assert.Equal( 2, result.CompletedUpdateCount );
			Assert.True( result.FrameSelected );
		}
	}

	[Fact]
	public async Task AmbiguousCompositionFailureRequiresExplicitRecreation() {
		AtlasTransport transport = new();
		await using CursesSession session = await OpenSessionAsync( transport );
		CursesRasterAtlas atlas = await CreateAtlasAsync(
			session,
			transport,
			Rgb24( 1, 1, 0 )
		);
		await using ( atlas ) {
			transport.FailNextFlush = true;
			await Assert.ThrowsAsync<IOException>(
				() => atlas.PresentAsync(
					[ new CursesRasterAtlasTileUpdate( 0, 0, Rgb24( 1, 1, 7 ) ) ]
				).AsTask()
			);

			Assert.True( atlas.RequiresRecreation );
			Assert.Throws<InvalidOperationException>( () => atlas.GetCell( 0, 0 ) );
			Assert.Throws<InvalidOperationException>(
				() => atlas.PresentAsync(
					[ new CursesRasterAtlasTileUpdate( 0, 0, Rgb24( 1, 1, 7 ) ) ]
				)
			);
		}
	}

	private static async Task<CursesRasterAtlas> CreateAtlasAsync(
		CursesSession session,
		AtlasTransport transport,
		TerminalRasterImage initial
	) {
		int creationStart = transport.WriteCount;
		Task<TerminalControlResult<CursesRasterAtlas>> creation = session
			.CreateRasterAtlasAsync(
				initial,
				initial.Height,
				initial.Width
			).AsTask();

		await transport.WaitForWriteCountAsync( creationStart + 1 );
		transport.Publish(
			Encoding.ASCII.GetBytes( "\u001b_Gi=77,I=1;OK\u001b\\" )
		);
		await transport.WaitForWriteCountAsync( creationStart + 2 );
		transport.Publish(
			Encoding.ASCII.GetBytes( "\u001b_Gi=77,p=1;OK\u001b\\" )
		);
		await transport.WaitForWriteCountAsync( creationStart + 3 );
		transport.PublishOk();

		TerminalControlResult<CursesRasterAtlas> result = await creation;
		Assert.Equal( TerminalControlStatus.Available, result.Status );
		return Assert.IsType<CursesRasterAtlas>( result.Value );
	}

	private static TerminalRasterImage Rgb24(
		int width,
		int height,
		byte first
	) {
		byte[] pixels = new byte[ checked( width * height * 3 ) ];
		for ( int index = 0; index < pixels.Length; index++ ) {
			pixels[ index ] = unchecked( (byte)( first + index ) );
		}
		return TerminalRasterImage.CreateRgb24( width, height, pixels );
	}

	private static TerminalRasterImage Rgba32(
		int width,
		int height,
		byte first
	) {
		byte[] pixels = new byte[ checked( width * height * 4 ) ];
		for ( int index = 0; index < pixels.Length; index++ ) {
			pixels[ index ] = unchecked( (byte)( first + index ) );
		}
		return TerminalRasterImage.CreateRgba32( width, height, pixels );
	}

	private static async ValueTask<CursesSession> OpenSessionAsync(
		AtlasTransport transport
	) {
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new AtlasTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			transport,
			transport,
			new TerminalSessionOptions {
				TerminalOverride = TerminalProfiles.Dumb,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false,
				RequireInteractiveOutput = false
			}
		);
		try {
			Task<TerminalCapabilityStatus> verification = terminalSession
				.VerifyCapabilityAsync(
					TerminalCapability.PersistentRasterGraphics
				).AsTask();
			await transport.WaitForWriteCountAsync( 1 );
			string genericProbe = transport.GetAsciiWrite( 0 );
			int imageMarker = genericProbe.IndexOf( "i=", StringComparison.Ordinal );
			Assert.True( 0 <= imageMarker );
			int imageEnd = genericProbe.IndexOfAny( [ ',', ';' ], imageMarker + 2 );
			Assert.True( imageMarker + 2 < imageEnd );
			string imageId = genericProbe.Substring(
				imageMarker + 2,
				imageEnd - imageMarker - 2
			);
			transport.Publish(
				Encoding.ASCII.GetBytes(
					$"\u001b_Gi={imageId};OK\u001b\\\u001b[?64;1c"
				)
			);
			await transport.WaitForWriteCountAsync( 2 );
			string persistentProbe = transport.GetAsciiWrite( 1 );
			int numberMarker = persistentProbe.IndexOf( "I=", StringComparison.Ordinal );
			Assert.True( 0 <= numberMarker );
			int numberEnd = persistentProbe.IndexOfAny( [ ',', ';' ], numberMarker + 2 );
			Assert.True( numberMarker + 2 < numberEnd );
			string imageNumber = persistentProbe.Substring(
				numberMarker + 2,
				numberEnd - numberMarker - 2
			);
			transport.Publish(
				Encoding.ASCII.GetBytes(
					$"\u001b_Gi=77,I={imageNumber};OK\u001b\\"
				)
			);
			await transport.WaitForWriteCountAsync( 3 );
			Assert.Equal(
				"\u001b_Ga=d,d=I,i=77,q=2\u001b\\",
				transport.GetAsciiWrite( 2 )
			);
			Assert.True( ( await verification ).IsUsable );

			return await CursesSession.OpenAsync(
				terminalSession,
				new CursesSessionOptions {
					UseAlternateScreen = false,
					EnableKeypad = false,
					HideCursor = false,
					UseSynchronizedOutput = false
				}
			);
		} catch {
			await terminalSession.DisposeAsync();
			throw;
		}
	}

	private sealed class AtlasTerminalControlProvider : ITerminalControlProvider {
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
			return TerminalControlResult<TerminalSize>.Available(
				new TerminalSize( 120, 40 )
			);
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

	private sealed class AtlasTransport : ITerminalInput, ITerminalOutput {
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>(
			new UnboundedChannelOptions {
				SingleReader = true,
				SingleWriter = false,
				AllowSynchronousContinuations = false
			}
		);
		private readonly object sync = new();
		private readonly List<byte[]> writes = [];
		private readonly SemaphoreSlim writeSignal = new( 0 );

		internal bool FailNextFlush {
			get;
			set;
		}

		internal int WriteCount {
			get {
				lock ( sync ) {
					return writes.Count;
				}
			}
		}

		internal string GetAsciiWrite(
			int index
		) {
			lock ( sync ) {
				return Encoding.ASCII.GetString( writes[ index ] );
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

		internal void PublishOk() {
			Publish( Encoding.ASCII.GetBytes( "\u001b_Gi=77;OK\u001b\\" ) );
		}

		internal async Task WaitForWriteCountAsync(
			int expected
		) {
			using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 15 ) );
			while ( true ) {
				lock ( sync ) {
					if ( expected <= writes.Count ) {
						return;
					}
				}
				await writeSignal.WaitAsync( timeout.Token ).ConfigureAwait( false );
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
			if ( FailNextFlush ) {
				FailNextFlush = false;
				throw new IOException( "Synthetic atlas flush failure." );
			}
			return ValueTask.CompletedTask;
		}
	}
}
