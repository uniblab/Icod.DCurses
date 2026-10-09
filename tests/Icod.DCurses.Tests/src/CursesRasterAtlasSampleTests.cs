/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

using System.Xml.Linq;
using Icod.DCurses.RasterAtlas.Sample;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises the headless model and public-only raster-atlas sample contract.</summary>
public sealed class CursesRasterAtlasSampleTests {
	[Fact]
	public void RecoverableAtlasSetupFailureIsWrittenWithItsStageAndException() {
		using StringWriter writer = new();
		FormatException exception = new( "Synthetic persistent response failure." );

		RasterAtlasSampleFallback.WriteSetupFailure(
			writer,
			"creating the persistent raster atlas",
			exception
		);

		string diagnostic = writer.ToString();
		Assert.Contains(
			"Raster atlas setup failed while creating the persistent raster atlas:",
			diagnostic,
			StringComparison.Ordinal
		);
		Assert.Contains(
			"System.FormatException: Synthetic persistent response failure.",
			diagnostic,
			StringComparison.Ordinal
		);
	}

	[Fact]
	public void MalformedPersistentCreationResponseSelectsTextFallback() {
		Assert.True(
			RasterAtlasSampleFallback.IsRecoverableSetupException(
				new FormatException( "Synthetic terminal-assigned image id failure." )
			)
		);
	}

	[Fact]
	public void CancellationAndProgrammingErrorsDoNotSelectTextFallback() {
		Assert.False(
			RasterAtlasSampleFallback.IsRecoverableSetupException(
				new OperationCanceledException()
			)
		);
		Assert.False(
			RasterAtlasSampleFallback.IsRecoverableSetupException(
				new ArgumentException( "Synthetic caller error." )
			)
		);
	}

	[Fact]
	public void PresentationTimeoutSelectsTextFallback() {
		Assert.True(
			RasterAtlasSampleFallback.IsRecoverablePresentationException(
				new TimeoutException( "Synthetic terminal query deadline." )
			)
		);
	}

	[Fact]
	public void StableCameraMovementProducesOnlyOldAndNewTileUpdates() {
		RasterAtlasSampleState state = new( 10, 10 );
		CursesRasterAtlasGeometry geometry = new( 10, 10, 2, 3 );

		RasterAtlasMoveResult move = state.Move( 0, 1 );
		CursesRasterAtlasTileUpdate[] updates = state.CreateUpdates( move, geometry );

		Assert.True( move.Moved );
		Assert.False( move.ViewportChanged );
		Assert.Equal( 2, updates.Length );
		Assert.Equal( ( 5, 5 ), ( updates[ 0 ].Row, updates[ 0 ].Column ) );
		Assert.Equal( ( 5, 6 ), ( updates[ 1 ].Row, updates[ 1 ].Column ) );
		Assert.All( updates, update => {
			Assert.Equal( 2, update.Image.Width );
			Assert.Equal( 3, update.Image.Height );
		} );
	}

	[Fact]
	public void CameraShiftProducesOneBoundedViewportUpdatePerCell() {
		RasterAtlasSampleState state = new( 4, 4 );
		CursesRasterAtlasGeometry geometry = new( 4, 4, 1, 1 );
		RasterAtlasMoveResult move = default;
		for ( int index = 0; index < 4; index++ ) {
			move = state.Move( 0, 1 );
		}

		Assert.True( move.ViewportChanged );
		CursesRasterAtlasTileUpdate[] updates = state.CreateUpdates( move, geometry );
		Assert.Equal( 16, updates.Length );
		Assert.Equal( 16, updates.Select( update => ( update.Row, update.Column ) ).Distinct().Count() );
	}

	[Fact]
	public void ResizeRequiresMatchingRecreatedGeometryAndUsesNoWorldSizedStorage() {
		RasterAtlasSampleState state = new( 4, 5 );
		Assert.True( state.ResizeViewport( 6, 7 ) );
		Assert.Throws<ArgumentException>(
			() => state.CreateInitialImage( new CursesRasterAtlasGeometry( 4, 5, 1, 1 ) )
		);
		TerminalRasterImage image = state.CreateInitialImage(
			new CursesRasterAtlasGeometry( 6, 7, 2, 3 )
		);
		Assert.Equal( 14, image.Width );
		Assert.Equal( 18, image.Height );
		Assert.Equal( 42, state.CreateTextFrame().Length );
	}

	[Fact]
	public void AtlasTileUpdatesUseSuppliedArtworkAndOpaquePlayer() {
		RasterAtlasSampleState state = new( 10, 10 );
		CursesRasterAtlasGeometry geometry = new( 10, 10, 16, 16 );

		CursesRasterAtlasTileUpdate[] updates = state.CreateFullUpdates( geometry );

		TerminalRasterImage water = updates.Single( update => update.Row == 0 && update.Column == 0 ).Image;
		TerminalRasterImage grass = updates.Single( update => update.Row == 0 && update.Column == 1 ).Image;
		TerminalRasterImage forest = updates.Single( update => update.Row == 0 && update.Column == 4 ).Image;
		TerminalRasterImage road = updates.Single( update => update.Row == 0 && update.Column == 9 ).Image;
		TerminalRasterImage player = updates.Single( update => update.Row == 5 && update.Column == 5 ).Image;

		Assert.Equal( new TerminalRasterColor( 85, 85, 255 ), water.GetPixelColor( 5, 0 ) );
		Assert.Equal( new TerminalRasterColor( 85, 255, 85 ), grass.GetPixelColor( 1, 3 ) );
		Assert.Equal( new TerminalRasterColor( 170, 0, 0 ), forest.GetPixelColor( 5, 0 ) );
		Assert.Equal( new TerminalRasterColor( 255, 255, 255 ), road.GetPixelColor( 0, 0 ) );
		Assert.Equal( new TerminalRasterColor( 0, 0, 0 ), player.GetPixelColor( 0, 0 ) );
		Assert.Equal( new TerminalRasterColor( 170, 85, 0 ), player.GetPixelColor( 6, 2 ) );
	}

	[Fact]
	public void AtlasTileUpdatesScaleArtworkWithNearestNeighborSampling() {
		RasterAtlasSampleState state = new( 10, 10 );
		CursesRasterAtlasGeometry geometry = new( 10, 10, 8, 8 );

		CursesRasterAtlasTileUpdate[] updates = state.CreateFullUpdates( geometry );
		TerminalRasterImage water = updates.Single( update => update.Row == 0 && update.Column == 0 ).Image;
		TerminalRasterImage player = updates.Single( update => update.Row == 5 && update.Column == 5 ).Image;

		Assert.Equal( new TerminalRasterColor( 85, 85, 255 ), water.GetPixelColor( 3, 0 ) );
		Assert.Equal( new TerminalRasterColor( 0, 0, 0 ), player.GetPixelColor( 0, 0 ) );
		Assert.Equal( new TerminalRasterColor( 170, 85, 0 ), player.GetPixelColor( 3, 1 ) );
	}

	[Fact]
	public void AtlasSetupUsesArtworkWhileFrameAndTextRemainGenerated() {
		RasterAtlasSampleState state = new( 10, 10 );
		CursesRasterAtlasGeometry geometry = new( 10, 10, 16, 16 );

		TerminalRasterImage frame = state.CreateInitialImage( geometry );
		Assert.Equal( new TerminalRasterColor( 28, 92, 160 ), frame.GetPixelColor( 5, 0 ) );
		Assert.Equal( new TerminalRasterColor( 245, 220, 80 ), frame.GetPixelColor( 5 * 16, 5 * 16 ) );
		Assert.Equal( "~", state.CreateTextFrame()[ 0 ].Content );
		Assert.Equal( "@", state.CreateTextFrame()[ 5 * 10 + 5 ].Content );

		string program = File.ReadAllText( Path.Combine(
			FindRepositoryRoot(),
			"samples",
			"Icod.DCurses.RasterAtlas.Sample",
			"Program.cs"
		) );
		Assert.Contains( "state.CreateAtlasImage( geometry )", program, StringComparison.Ordinal );
		Assert.Contains( "RefreshRasterAsync( state.CreateInitialImage( geometry )", program, StringComparison.Ordinal );
	}

	[Fact]
	public void RelayoutClearsAbandonedStatusRowsBeforeMovingWindows() {
		CursesScreen screen = new( 40, 8 );
		CursesWindow standard = screen.StandardWindow;
		CursesWindow map = screen.CreateWindow( 0, 0, 1, 1 );
		CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
		using CursesPanel help = screen.CreatePanel( 0, 0, 1, 1 );

		Assert.True( RasterAtlasSampleLayout.TryArrange(
			screen,
			standard,
			map,
			status,
			help,
			out _,
			out _
		) );
		status.Move( 0, 0 );
		status.Write( "OLD STATUS" );
		status.Move( 1, 0 );
		status.Write( "OLD MESSAGE" );

		screen.Resize( 60, 12 );
		Assert.True( RasterAtlasSampleLayout.TryArrange(
			screen,
			standard,
			map,
			status,
			help,
			out _,
			out _
		) );

		for ( int row = 6; row < 8; row++ ) {
			for ( int column = 0; column < 40; column++ ) {
				Assert.True( screen.VirtualScreen.GetCell( row, column ).IsBlank );
			}
		}
		Assert.Equal( new CursesRectangle( 10, 0, 2, 60 ), status.Bounds );
	}

	[Fact]
	public void SampleBuildsInSolutionAndExposesExplicitTextFallback() {
		string root = FindRepositoryRoot();
		string folder = Path.Combine( root, "samples", "Icod.DCurses.RasterAtlas.Sample" );
		string projectPath = Path.Combine( folder, "Icod.DCurses.RasterAtlas.Sample.csproj" );
		XDocument project = XDocument.Load( projectPath );
		Assert.Equal(
			"net8.0;net9.0;net10.0",
			project.Descendants( "TargetFrameworks" ).Single().Value
		);
		Assert.Equal(
			@"..\..\Icod.DCurses.csproj",
			project.Descendants( "ProjectReference" ).Single().Attribute( "Include" )?.Value
		);
		string program = File.ReadAllText( Path.Combine( folder, "Program.cs" ) );
		Assert.Contains( "--text", program, StringComparison.Ordinal );
		Assert.Contains( "VerifyCapabilityAsync", program, StringComparison.Ordinal );
		Assert.Contains( "QueryRasterAtlasGeometryAsync", program, StringComparison.Ordinal );
		Assert.Contains( "CreateRasterAtlasAsync", program, StringComparison.Ordinal );
		Assert.Contains( "WriteRasterAtlas", program, StringComparison.Ordinal );
		Assert.Contains( "PresentAsync", program, StringComparison.Ordinal );
		Assert.Contains(
			@"samples\Icod.DCurses.RasterAtlas.Sample\Icod.DCurses.RasterAtlas.Sample.csproj",
			File.ReadAllText( Path.Combine( root, "Icod.DCurses.sln" ) ),
			StringComparison.Ordinal
		);
	}

	[Fact]
	public void SamplePollsDimensionsWhenLifecycleResizeNotificationIsUnavailable() {
		string root = FindRepositoryRoot();
		string program = File.ReadAllText( Path.Combine(
			root,
			"samples",
			"Icod.DCurses.RasterAtlas.Sample",
			"Program.cs"
		) );

		Assert.Contains( "TimeSpan resizePollInterval", program, StringComparison.Ordinal );
		Assert.Contains( "ReadEventAsync( resizePollInterval )", program, StringComparison.Ordinal );
		Assert.Contains( "CursesEventKind.Timeout", program, StringComparison.Ordinal );
		Assert.Contains( "SynchronizeDimensions()", program, StringComparison.Ordinal );
		Assert.Contains( "screen.Rows != previousRows", program, StringComparison.Ordinal );
		Assert.Contains( "screen.Columns != previousColumns", program, StringComparison.Ordinal );
		Assert.DoesNotContain( "OperatingSystem.", program, StringComparison.Ordinal );
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? current = new( AppContext.BaseDirectory );
		while ( current is not null ) {
			if ( File.Exists( Path.Combine( current.FullName, "Icod.DCurses.sln" ) ) ) {
				return current.FullName;
			}
			current = current.Parent;
		}
		throw new InvalidOperationException( "Repository root not found." );
	}
}
