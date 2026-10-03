/*
	Icod.DCurses.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/

using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

[Collection( TerminalProtocolNegotiationCollection.Name )]
public sealed class CursesCompleteRasterRefreshTests {
	private static TerminalRasterImage Image => TerminalRasterImage.CreateRgb24( 1, 1, [ 1, 2, 3 ] );
	private static CursesRasterAtlasGeometry Geometry => new( 1, 1, 1, 1 );

	[Theory]
	[InlineData( true )]
	[InlineData( false )]
	public async Task FrameOmitsCoveredTextButTextRefreshRestoresIt( bool kitty ) {
		Transport transport = new( kitty );
		await using CursesSession session = await OpenAsync( transport );
		session.StandardScreen.Write( "LSECRET!" );
		session.StandardScreen.Move( 3, 0 );
		session.StandardScreen.Write( "STATUS" );
		TerminalRasterImage image = TerminalRasterImage.CreateRgb24( 6, 1, new byte[18] );
		for ( int frame = 0; frame < 2; frame++ ) {
			transport.Clear();
			await session.RefreshRasterAsync( image, 0, 1, new( 1, 6, 1, 1 ) );
			Assert.DoesNotContain( "SECRET", transport.Text );
			Assert.Contains( "L", transport.Text );
			Assert.Contains( "!", transport.Text );
			Assert.Contains( "STATUS", transport.Text );
			Assert.Equal( 1, transport.FlushCount );
		}
		transport.Clear();
		await session.RefreshAsync();
		Assert.Contains( "LSECRET!", transport.Text );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( 1 )]
	public async Task FrameCannotSplitWideTextAtEitherHorizontalEdge( int column ) {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		session.StandardScreen.Write( "界" );
		await Assert.ThrowsAsync<InvalidOperationException>( () => session.RefreshRasterAsync( Image, 0, column, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
	}

	[Theory]
	[InlineData( true, "\u001b_Ga=T" )]
	[InlineData( false, "\u001bP0;1;0q" )]
	public async Task VerifiedBackendUsesOneTransactionAndRestoresUnknownCursor( bool kitty, string marker ) {
		Transport transport = new( kitty );
		await using CursesSession session = await OpenAsync( transport );
		session.StandardScreen.Move( 3, 0 );
		session.StandardScreen.Write( "STATUS" );
		session.StandardScreen.Move( 0, 0 );
		await session.RefreshRasterAsync( Image, 0, 0, Geometry );
		string output = transport.Text;
		Assert.StartsWith( "\u001b[?2026h", output );
		Assert.Contains( "<clear>", output );
		Assert.Contains( marker, output );
		Assert.True( output.IndexOf( "STATUS", StringComparison.Ordinal ) < output.IndexOf( marker, StringComparison.Ordinal ) );
		Assert.Contains( "<cup:0,0>" + marker, output );
		Assert.EndsWith( "\u001b\\<cup:0,0>\u001b[?2026l", output );
		Assert.Equal( 1, transport.FlushCount );
		Assert.Equal( CursesRefreshOutcome.Succeeded, session.LatestRefreshDiagnostics!.Outcome );
		Assert.True( session.LatestRefreshDiagnostics.OperationKinds.HasFlag( CursesRefreshOperationKinds.Raster ) );
		Assert.Equal( 0, session.LatestRefreshDiagnostics.RasterPlaceholderCellCount );
	}

	[Fact]
	public async Task RepeatedFramesClearAndTextTransitionClearsOnlyOnce() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		await session.RefreshRasterAsync( Image, 0, 0, Geometry );
		transport.Clear();
		await session.RefreshRasterAsync( Image, 1, 1, Geometry );
		Assert.Contains( "<clear>", transport.Text );
		Assert.Contains( "<cup:1,1>\u001b_Ga=T", transport.Text );
		transport.Clear();
		await session.RefreshAsync();
		Assert.Contains( "<clear>", transport.Text );
		Assert.DoesNotContain( "\u001b_G", transport.Text );
		transport.Clear();
		await session.RefreshAsync();
		Assert.Empty( transport.Text );
	}

	[Fact]
	public async Task UnknownRasterRejectsAllPreparedTextBeforeOutput() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport, verify: false );
		session.StandardScreen.Write( "PENDING" );
		await Assert.ThrowsAsync<NotSupportedException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
		Assert.False( session.LatestRefreshDiagnostics!.LogicalStatePublished );
		await session.RefreshAsync();
		Assert.Contains( "PENDING", transport.Text );
	}

	[Theory]
	[InlineData( 3, 0 )]
	[InlineData( 0, 8 )]
	public async Task CurrentBoundsAndFinalRowRejectBeforeOutput( int row, int column ) {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		await Assert.ThrowsAsync<InvalidOperationException>( () => session.RefreshRasterAsync( Image, row, column, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
	}

	[Fact]
	public async Task InvalidGeometryAndCancellationEmitNothing() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		await Assert.ThrowsAsync<ArgumentException>( () => session.RefreshRasterAsync( Image, 0, 0, default ).AsTask() );
		await Assert.ThrowsAsync<ArgumentException>( () => session.RefreshRasterAsync( Image, 0, 0, new( 2, 1, 1, 1 ) ).AsTask() );
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry, cancellation.Token ).AsTask() );
		Assert.Empty( transport.Text );
	}

	[Fact]
	public async Task OverlappingVisiblePanelRejectsButHiddenPanelDoesNot() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		using CursesPanel panel = session.Screen.CreatePanel( 0, 0, 1, 1 );
		await Assert.ThrowsAsync<InvalidOperationException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
		panel.Hide();
		await session.RefreshRasterAsync( Image, 0, 0, Geometry );
		Assert.Contains( "\u001b_Ga=T", transport.Text );
	}

	[Fact]
	public async Task FlushFailureKeepsDamageUntilSuccessfulTextRecovery() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		transport.FailNextFlush = true;
		await Assert.ThrowsAsync<IOException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry ).AsTask() );
		Assert.False( session.LatestRefreshDiagnostics!.LogicalStatePublished );
		transport.Clear();
		await session.RefreshAsync();
		Assert.Contains( "<clear>", transport.Text );
		Assert.DoesNotContain( "\u001b_G", transport.Text );
	}

	[Fact]
	public async Task UnsupportedSixelAlphaRejectsPreparedTextBeforeOutput() {
		Transport transport = new( false );
		await using CursesSession session = await OpenAsync( transport );
		session.StandardScreen.Write( "PENDING" );
		TerminalRasterImage alpha = TerminalRasterImage.CreateRgba32( 1, 1, [ 1, 2, 3, 128 ] );
		await Assert.ThrowsAsync<NotSupportedException>( () => session.RefreshRasterAsync( alpha, 0, 0, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
		await session.RefreshAsync();
		Assert.Contains( "PENDING", transport.Text );
	}

	[Fact]
	public async Task RetainedRasterCellsRejectBeforeOutput() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		session.Screen.VirtualScreen.SetRasterCell( 2, 2,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell( session.HostSession ) );
		await Assert.ThrowsAsync<InvalidOperationException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
		session.Screen.VirtualScreen.SetRasterCell( 2, 2, null );
	}

	[Fact]
	public async Task ResizeAfterImageGenerationRejectsBeforeOutput() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		TerminalRasterImage image = Image;
		transport.Size = new( 1, 1 );
		await Assert.ThrowsAsync<InvalidOperationException>( () => session.RefreshRasterAsync( image, 0, 0, Geometry ).AsTask() );
		Assert.Empty( transport.Text );
	}

	[Fact]
	public async Task RasterWriteFailureDoesNotRetryAndTextRefreshRepairsDamage() {
		Transport transport = new( true );
		await using CursesSession session = await OpenAsync( transport );
		transport.FailRasterWrite = true;
		await Assert.ThrowsAsync<IOException>( () => session.RefreshRasterAsync( Image, 0, 0, Geometry ).AsTask() );
		Assert.Equal( 1, transport.RasterWriteAttempts );
		Assert.False( session.LatestRefreshDiagnostics!.LogicalStatePublished );
		Assert.True( session.LatestRefreshDiagnostics.PhysicalStateInvalidated );
		transport.Clear();
		await session.RefreshAsync();
		Assert.Contains( "<clear>", transport.Text );
		Assert.Equal( 1, transport.RasterWriteAttempts );
	}

	private static async Task<CursesSession> OpenAsync( Transport transport, bool verify = true ) {
		TerminalSession terminal = await TerminalSession.OpenAsync(
			new Provider( transport ), TerminalEndpoint.StandardInput, TerminalEndpoint.StandardOutput,
			transport, transport, new TerminalSessionOptions {
				TerminalOverride = new TerminalDescriptionBuilder( "complete-raster-test" )
					.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
					.SetString( StringCapability.ClearScreen, "<clear>" )
					.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
					.SetString( StringCapability.OriginalColorPair, "<op>" ).Build(),
				ConfigureOutput = false, ObserveLifecycleEvents = false
			}
		);
		try {
			if ( verify ) {
				Assert.True( ( await terminal.VerifyCapabilityAsync( TerminalCapability.RasterGraphics ) ).IsUsable );
			}
			CursesSession session = await CursesSession.OpenAsync( terminal, new CursesSessionOptions {
				UseAlternateScreen = false, EnableKeypad = false, HideCursor = false,
				UseSynchronizedOutput = true, EnableRefreshDiagnostics = true
			} );
			transport.Clear();
			return session;
		} catch {
			await terminal.DisposeAsync();
			throw;
		}
	}

	private sealed class Transport( bool kitty ) : ITerminalInput, ITerminalOutput {
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly StringBuilder output = new();
		internal string Text => output.ToString();
		internal int FlushCount { get; private set; }
		internal bool FailNextFlush { get; set; }
		internal bool FailRasterWrite { get; set; }
		internal int RasterWriteAttempts { get; private set; }
		internal TerminalSize Size { get; set; } = new( 8, 4 );
		internal void Clear() { output.Clear(); FlushCount = 0; }
		public async ValueTask<int> ReadAsync( Memory<byte> buffer, CancellationToken cancellationToken = default ) {
			byte[] bytes = await input.Reader.ReadAsync( cancellationToken );
			bytes.AsSpan().CopyTo( buffer.Span );
			return bytes.Length;
		}
		public ValueTask WriteAsync( ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default ) {
			string text = Encoding.UTF8.GetString( buffer.Span );
			if ( text.Contains( "a=T", StringComparison.Ordinal ) ) {
				RasterWriteAttempts++;
				if ( FailRasterWrite ) { throw new IOException( "Synthetic raster write failure." ); }
			}
			output.Append( text );
			if ( text.Contains( "a=q", StringComparison.Ordinal ) ) {
				int start = text.IndexOf( "i=", StringComparison.Ordinal ) + 2;
				int end = text.IndexOfAny( [ ',', ';' ], start );
				// Any correlated Kitty reply proves protocol recognition. A Sixel-only
				// terminal ignores Kitty APC and replies to the following DA request.
				string response = kitty ? $"\u001b_Gi={text[start..end]};OK\u001b\\\u001b[?64;4c" : "\u001b[?64;4c";
				input.Writer.TryWrite( Encoding.ASCII.GetBytes( response ) );
			}
			return ValueTask.CompletedTask;
		}
		public ValueTask FlushAsync( CancellationToken cancellationToken = default ) {
			FlushCount++;
			if ( FailNextFlush ) { FailNextFlush = false; throw new IOException( "Synthetic flush failure." ); }
			return ValueTask.CompletedTask;
		}
	}

	private sealed class Provider( Transport transport ) : ITerminalControlProvider {
		private readonly TerminalModeSnapshot baseline = TerminalModeSnapshot.CreatePosix(
			0, 0, 0, 0x0002UL, new byte[32], 0, 32, 0, new TerminalSpeed(13,9600), new TerminalSpeed(13,9600) );
		public TerminalControlResult<TerminalEndpointObservation> Observe( TerminalEndpoint endpoint ) =>
			TerminalControlResult<TerminalEndpointObservation>.Available( new( true, null, TerminalPlatformKind.PosixTermios,
				TerminalControlCapabilities.Attachment | TerminalControlCapabilities.LiveSize | TerminalControlCapabilities.ModeRead | TerminalControlCapabilities.ModeWrite ) );
		public TerminalControlResult<TerminalSize> GetSize( TerminalEndpoint endpoint ) => TerminalControlResult<TerminalSize>.Available( transport.Size );
		public TerminalControlResult<TerminalModeSnapshot> GetMode( TerminalEndpoint endpoint ) => TerminalControlResult<TerminalModeSnapshot>.Available( baseline );
		public TerminalControlMutationResult SetMode( TerminalEndpoint endpoint, TerminalModeSnapshot mode, TerminalModeApplyTiming timing ) => TerminalControlMutationResult.Success();
	}
}
