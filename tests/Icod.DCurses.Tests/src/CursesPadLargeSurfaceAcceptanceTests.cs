using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises large pads, geometry churn, edge clipping, and repeated panning.</summary>
public sealed class CursesPadLargeSurfaceAcceptanceTests {
	[Fact]
	public void DestinationScreenShrinkFailsCleanlyAndGrowthRestoresPresentation() {
		CursesPad pad = new(
			6,
			3
		);
		pad.ContentWindow.FillRectangle( 0, 0, 1, 6, new CursesCell( "A" ) );
		pad.ContentWindow.FillRectangle( 1, 0, 1, 6, new CursesCell( "B" ) );
		CursesScreen screen = new(
			4,
			2
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			1,
			2,
			4,
			0,
			0
		);
		viewport.Present();

		screen.Resize(
			3,
			1
		);
		Assert.Throws<ArgumentOutOfRangeException>( viewport.Present );
		Assert.Equal( 6, pad.Columns );
		Assert.Equal( 3, pad.Rows );
		Assert.Equal( "A", pad.ContentWindow.GetCell( 0, 0 ).Content );

		screen.Resize(
			5,
			3
		);
		viewport.Present();
		AssertRowPrefix( screen.StandardWindow, 0, "AAAA" );
		AssertRowPrefix( screen.StandardWindow, 1, "BBBB" );
	}

	[Fact]
	public void RepositionedDestinationWindowReceivesViewportAtNewProjection() {
		CursesPad pad = new(
			6,
			2
		);
		pad.ContentWindow.Write( "ABCDEF" );
		CursesScreen screen = new(
			10,
			6
		);
		CursesWindow destination = screen.CreateWindow(
			1,
			1,
			2,
			4
		);
		CursesPadViewport viewport = pad.CreateViewport(
			destination,
			0,
			1,
			1,
			3,
			0,
			0
		);
		viewport.Present();
		Assert.Equal( "B", screen.VirtualScreen[ 1, 1 ].Content );

		destination.Reposition(
			3,
			5
		);
		viewport.Present();

		Assert.Equal( "B", screen.VirtualScreen[ 3, 5 ].Content );
		Assert.Equal( "C", screen.VirtualScreen[ 3, 6 ].Content );
		Assert.Equal( "D", screen.VirtualScreen[ 3, 7 ].Content );
	}

	[Theory]
	[InlineData( 0, 0 )]
	[InlineData( 0, 2 )]
	[InlineData( 2, 0 )]
	[InlineData( 2, 2 )]
	public void ViewportCanPresentEveryPadCorner(
		int padRow,
		int padColumn
	) {
		CursesPad pad = new(
			5,
			4
		);
		for ( int row = 0; row < pad.Rows; row++ ) {
			for ( int column = 0; column < pad.Columns; column++ ) {
				pad.ContentWindow.FillRectangle(
					row,
					column,
					1,
					1,
					new CursesCell( ((char)( 'A' + ( row * pad.Columns ) + column )).ToString() )
				);
			}
		}

		CursesScreen screen = new(
			3,
			2
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			padRow,
			padColumn,
			2,
			3,
			0,
			0
		);
		viewport.Present();

		for ( int rowOffset = 0; rowOffset < 2; rowOffset++ ) {
			for ( int columnOffset = 0; columnOffset < 3; columnOffset++ ) {
				Assert.Equal(
					pad.ContentWindow.GetCell(
						padRow + rowOffset,
						padColumn + columnOffset
					),
					screen.StandardWindow.GetCell(
						rowOffset,
						columnOffset
					)
				);
			}
		}
	}

	[Fact]
	public void ViewportNormalizesWideGlyphsAtBothSourceEdges() {
		CursesPad pad = new(
			6,
			1
		);
		pad.ContentWindow.Write( "A\u754CBC" );
		CursesScreen screen = new(
			2,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);

		viewport.Present();
		Assert.Equal( "A", screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.True( screen.StandardWindow.GetCell( 0, 1 ).IsBlank );
		CursesCellFootprint.Validate( screen.VirtualScreen );

		viewport.SetSource(
			0,
			2
		);
		viewport.Present();
		Assert.True( screen.StandardWindow.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "B", screen.StandardWindow.GetCell( 0, 1 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void ViewportRepairsWideGlyphAcrossDestinationBoundary() {
		CursesPad pad = new(
			2,
			1
		);
		pad.ContentWindow.Write( "XY" );
		CursesScreen screen = new(
			4,
			1
		);
		screen.StandardWindow.Write( "A\u754CB" );
		CursesWindow destination = screen.CreateWindow(
			0,
			2,
			1,
			2
		);

		pad.PresentTo(
			destination,
			0,
			0,
			1,
			2,
			0,
			0
		);

		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( "Y", screen.VirtualScreen[ 0, 3 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void VeryWideAndVeryTallPadsPresentFarEdges() {
		CursesPad wide = new(
			20_000,
			3
		);
		wide.ContentWindow.FillRectangle(
			2,
			19_997,
			1,
			3,
			new CursesCell( "W" )
		);
		CursesScreen wideDestination = new(
			3,
			1
		);
		wide.PresentTo(
			wideDestination.StandardWindow,
			2,
			19_997,
			1,
			3,
			0,
			0
		);
		AssertRowPrefix( wideDestination.StandardWindow, 0, "WWW" );

		CursesPad tall = new(
			3,
			20_000
		);
		tall.ContentWindow.FillRectangle(
			19_999,
			0,
			1,
			3,
			new CursesCell( "T" )
		);
		CursesScreen tallDestination = new(
			3,
			1
		);
		tall.PresentTo(
			tallDestination.StandardWindow,
			19_999,
			0,
			1,
			3,
			0,
			0
		);
		AssertRowPrefix( tallDestination.StandardWindow, 0, "TTT" );
	}

	[Fact]
	public void RepeatedPanningAfterEditorStyleMutationsPreservesDestinationFootprints() {
		CursesPad pad = new(
			40,
			30
		);
		for ( int row = 0; row < pad.Rows; row++ ) {
			pad.ContentWindow.FillRectangle(
				row,
				0,
				1,
				pad.Columns,
				new CursesCell( ((char)( 'A' + ( row % 26 ) )).ToString() )
			);
		}

		CursesWindow editor = pad.ContentWindow.CreateSubwindow(
			5,
			5,
			10,
			20
		);
		editor.Move(
			2,
			3
		);
		editor.Write( "A\u754CB" );
		editor.Move(
			2,
			4
		);
		editor.InsertCells( 2 );
		editor.DeleteCells();
		editor.Move(
			4,
			0
		);
		editor.InsertLines();
		editor.DeleteLines();

		CursesScreen screen = new(
			10,
			5
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			5,
			10,
			0,
			0
		);

		for ( int iteration = 0; iteration < 80; iteration++ ) {
			viewport.SetSource(
				( iteration * 3 ) % ( pad.Rows - viewport.Rows + 1 ),
				( iteration * 5 ) % ( pad.Columns - viewport.Columns + 1 )
			);
			viewport.Present();
			CursesCellFootprint.Validate( screen.VirtualScreen );
		}
	}

	private static void AssertRowPrefix(
		CursesWindow window,
		int row,
		string expected
	) {
		ArgumentNullException.ThrowIfNull( window );
		ArgumentNullException.ThrowIfNull( expected );
		string actual = string.Concat(
			Enumerable.Range( 0, expected.Length )
				.Select( column => window.GetCell( row, column ).Content )
		);
		Assert.Equal( expected, actual );
	}
}
