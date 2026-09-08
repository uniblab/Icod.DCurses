using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies logical cell and line insertion/deletion.</summary>
public sealed class CursesWindowInsertDeleteTests {
	[Fact]
	public void InsertCellsShiftsRemainingRowRightAndPreservesCursor() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCDE" );
		window.Move(
			0,
			2
		);

		window.InsertCells( 2 );

		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		AssertRow(
			window,
			"A",
			"B",
			string.Empty,
			string.Empty,
			"C",
			"D",
			"E",
			string.Empty
		);
	}

	[Fact]
	public void DeleteCellsShiftsFollowingRowLeftAndFillsVacatedColumns() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.BackgroundCell = new CursesCell( "." );
		window.Write( "ABCDEFG" );
		window.Move(
			0,
			2
		);

		window.DeleteCells( 2 );

		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		AssertRow(
			window,
			"A",
			"B",
			"E",
			"F",
			"G",
			string.Empty,
			".",
			"."
		);
	}

	[Fact]
	public void InsertCellsMovesCompleteWideFootprintAtomically() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "A\u754CBC" );
		window.Move(
			0,
			0
		);

		window.InsertCells();

		Assert.True( window.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "A", window.GetCell( 0, 1 ).Content );
		Assert.Equal( "\u754C", window.GetCell( 0, 2 ).Content );
		Assert.Equal( 2, window.GetCell( 0, 2 ).DisplayWidth );
		Assert.True( window.GetCell( 0, 3 ).IsContinuation );
		Assert.Equal( "B", window.GetCell( 0, 4 ).Content );
		Assert.Equal( "C", window.GetCell( 0, 5 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void InsertCellsAtContinuationBoundaryDiscardsPartialWideFootprint() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "A\u754CBC" );
		window.Move(
			0,
			2
		);

		window.InsertCells();

		Assert.Equal( "A", window.GetCell( 0, 0 ).Content );
		Assert.True( window.GetCell( 0, 1 ).IsBlank );
		Assert.True( window.GetCell( 0, 2 ).IsBlank );
		Assert.True( window.GetCell( 0, 3 ).IsBlank );
		Assert.Equal( "B", window.GetCell( 0, 4 ).Content );
		Assert.Equal( "C", window.GetCell( 0, 5 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void DeleteCellsThroughWideLeaderDoesNotLeaveContinuation() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "A\u754CBC" );
		window.Move(
			0,
			1
		);

		window.DeleteCells();

		Assert.Equal( "A", window.GetCell( 0, 0 ).Content );
		Assert.True( window.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( "B", window.GetCell( 0, 2 ).Content );
		Assert.Equal( "C", window.GetCell( 0, 3 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void OversizedCellEditCountCollapsesRemainingRowToBackground() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.BackgroundCell = new CursesCell( "." );
		window.Write( "ABCDEF" );
		window.Move(
			0,
			3
		);

		window.InsertCells( 99 );

		AssertRow(
			window,
			"A",
			"B",
			"C",
			".",
			".",
			"."
		);
	}

	[Fact]
	public void InsertLinesIsConfinedToRepositionedWindow() {
		CursesScreen screen = new( 8, 6 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			3,
			4
		);
		window.FillRectangle(
			0,
			0,
			1,
			4,
			new CursesCell( "A" )
		);
		window.FillRectangle(
			1,
			0,
			1,
			4,
			new CursesCell( "B" )
		);
		window.FillRectangle(
			2,
			0,
			1,
			4,
			new CursesCell( "C" )
		);
		window.Reposition(
			2,
			3
		);
		window.Move(
			1,
			2
		);

		window.InsertLines();

		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		AssertRowContent( window, 0, "AAAA" );
		AssertRowContent( window, 1, "" );
		AssertRowContent( window, 2, "BBBB" );
		Assert.True( screen.VirtualScreen[ 1, 3 ].IsBlank );
		Assert.True( screen.VirtualScreen[ 5, 3 ].IsBlank );
	}

	[Fact]
	public void DeleteLinesShiftsRowsUpAndUsesBackgroundAtBottom() {
		CursesScreen screen = new( 7, 5 );
		CursesWindow window = screen.CreateWindow(
			1,
			1,
			3,
			4
		);
		window.BackgroundCell = new CursesCell( "." );
		window.FillRectangle( 0, 0, 1, 4, new CursesCell( "A" ) );
		window.FillRectangle( 1, 0, 1, 4, new CursesCell( "B" ) );
		window.FillRectangle( 2, 0, 1, 4, new CursesCell( "C" ) );
		window.Move(
			1,
			3
		);

		window.DeleteLines();

		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 3, window.CursorColumn );
		AssertRowContent( window, 0, "AAAA" );
		AssertRowContent( window, 1, "CCCC" );
		AssertRowContent( window, 2, "...." );
	}

	[Fact]
	public void LineEditsPreserveWideFootprints() {
		CursesScreen screen = new( 7, 4 );
		CursesWindow window = screen.CreateWindow(
			0,
			1,
			3,
			5
		);
		window.Move(
			0,
			0
		);
		window.Write( "A\u754CB" );
		window.Move(
			0,
			0
		);

		window.InsertLines();

		Assert.True( window.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "A", window.GetCell( 1, 0 ).Content );
		Assert.Equal( "\u754C", window.GetCell( 1, 1 ).Content );
		Assert.True( window.GetCell( 1, 2 ).IsContinuation );
		Assert.Equal( "B", window.GetCell( 1, 3 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( -1 )]
	public void EditingRejectsNonPositiveCounts( int count ) {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;

		Assert.Throws<ArgumentOutOfRangeException>( () => window.InsertCells( count ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => window.DeleteCells( count ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => window.InsertLines( count ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => window.DeleteLines( count ) );
	}

	private static void AssertRow(
		CursesWindow window,
		params string[] expected
	) {
		ArgumentNullException.ThrowIfNull( window );
		ArgumentNullException.ThrowIfNull( expected );
		Assert.Equal( window.Columns, expected.Length );
		for ( int column = 0; column < expected.Length; column++ ) {
			Assert.Equal(
				expected[ column ],
				window.GetCell( 0, column ).Content
			);
		}
	}

	private static void AssertRowContent(
		CursesWindow window,
		int row,
		string expected
	) {
		ArgumentNullException.ThrowIfNull( window );
		ArgumentNullException.ThrowIfNull( expected );
		string actual = string.Concat(
			Enumerable.Range( 0, window.Columns )
				.Select( column => window.GetCell( row, column ).Content )
		);
		Assert.Equal( expected, actual );
	}
}
