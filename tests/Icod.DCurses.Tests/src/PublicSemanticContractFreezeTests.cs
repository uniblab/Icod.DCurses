using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Pins compatibility-sensitive geometry, cell, and text semantics for the 0.9 freeze.</summary>
public sealed class PublicSemanticContractFreezeTests {
	[Fact]
	public void WindowAndSubwindowCoordinatesAreZeroBasedAndParentLocal() {
		CursesScreen screen = new(
			columns: 8,
			rows: 4
		);
		CursesWindow window = screen.CreateWindow(
			row: 1,
			column: 2,
			rows: 2,
			columns: 4
		);
		CursesWindow subwindow = window.CreateSubwindow(
			row: 1,
			column: 1,
			rows: 1,
			columns: 2
		);

		subwindow.Move(
			0,
			0
		);
		subwindow.Write( "Z" );

		Assert.Equal( 1, window.OriginRow );
		Assert.Equal( 2, window.OriginColumn );
		Assert.Equal( 1, subwindow.OriginRow );
		Assert.Equal( 1, subwindow.OriginColumn );
		Assert.Equal( "Z", subwindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "Z", window.GetCell( 1, 1 ).Content );
		Assert.Equal( "Z", screen.VirtualScreen.GetCell( 2, 3 ).Content );
	}

	[Fact]
	public void InvalidGeometryRetainsArgumentOutOfRangeContract() {
		ArgumentOutOfRangeException columns = Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesScreen(
				columns: 0,
				rows: 1
			)
		);
		Assert.Equal( "columns", columns.ParamName );

		CursesScreen screen = new(
			columns: 4,
			rows: 2
		);
		ArgumentOutOfRangeException row = Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreateWindow(
				row: -1,
				column: 0,
				rows: 1,
				columns: 1
			)
		);
		Assert.Equal( "row", row.ParamName );

		ArgumentOutOfRangeException moveRow = Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.StandardWindow.Move(
				row: 2,
				column: 0
			)
		);
		Assert.Equal( "row", moveRow.ParamName );
	}

	[Fact]
	public void ColumnHelpersUseHalfOpenIntervalsAndNeverSplitWideElements() {
		const string text = "A\u754CB";

		Assert.Equal( 4, CursesText.MeasureColumns( text ) );
		Assert.Equal( "A", CursesText.TruncateToColumns( text, 2 ) );
		Assert.Equal( "A\u754C", CursesText.TruncateToColumns( text, 3 ) );
		Assert.Equal( "\u754C", CursesText.SliceByColumns( text, 1, 2 ) );
		Assert.Equal( string.Empty, CursesText.SliceByColumns( text, 2, 1 ) );
		Assert.Equal( "B", CursesText.SliceByColumns( text, 3, 1 ) );
	}

	[Fact]
	public void AmbiguousWidthPolicyDefaultsNarrowAndSupportsExplicitWide() {
		const string ambiguous = "\u00B7";

		Assert.Equal(
			CursesAmbiguousWidthPolicy.Narrow,
			UnicodeCursesTextWidthProvider.Instance.AmbiguousWidthPolicy
		);
		Assert.Equal(
			CursesAmbiguousWidthPolicy.Wide,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.AmbiguousWidthPolicy
		);
		Assert.Equal(
			1,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( ambiguous )
		);
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.GetWidth( ambiguous )
		);
	}

	[Fact]
	public void SemanticLineCellsRemainDistinctFromOrdinaryUnicodeText() {
		CursesCell semantic = CursesCell.Line( CursesLineGlyph.Horizontal );
		CursesCell ordinary = new( semantic.Content );

		Assert.True( semantic.IsLineGlyph );
		Assert.Equal( CursesLineGlyph.Horizontal, semantic.LineGlyph );
		Assert.False( ordinary.IsLineGlyph );
		Assert.Null( ordinary.LineGlyph );
		Assert.NotEqual( semantic, ordinary );
	}

	[Fact]
	public void OverwritingWideContinuationRepairsTheWholeExistingFootprint() {
		CursesScreen screen = new(
			columns: 4,
			rows: 1
		);
		CursesWindow window = screen.StandardWindow;
		window.Write( "\u754C" );

		Assert.Equal( 2, window.GetCell( 0, 0 ).DisplayWidth );
		Assert.True( window.GetCell( 0, 1 ).IsContinuation );

		window.Move(
			0,
			1
		);
		window.Write( "X" );

		Assert.True( window.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "X", window.GetCell( 0, 1 ).Content );
		Assert.False( window.GetCell( 0, 1 ).IsContinuation );
	}

	[Fact]
	public void ScreenResizeRepairsWideFootprintClippedByNewBoundary() {
		CursesScreen screen = new(
			columns: 3,
			rows: 1
		);
		screen.StandardWindow.Write( "A\u754C" );

		screen.Resize(
			columns: 2,
			rows: 1,
			preserveContents: true
		);

		Assert.Equal( "A", screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.True( screen.StandardWindow.GetCell( 0, 1 ).IsBlank );
	}
}
