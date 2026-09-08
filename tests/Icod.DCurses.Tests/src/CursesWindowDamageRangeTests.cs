using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies range-oriented damage control and representative editing workloads.</summary>
public sealed class CursesWindowDamageRangeTests {
	[Fact]
	public void TouchRegionMarksOnlySelectedProjectedCells() {
		CursesScreen screen = new( 6, 4 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			2,
			3
		);
		screen.VirtualScreen.MarkClean();

		window.TouchRegion(
			0,
			1,
			2,
			2
		);

		Assert.Equal( 4, screen.VirtualScreen.DirtyCellCount );
		Assert.False( screen.VirtualScreen.IsDirty( 1, 2 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 1, 3 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 1, 4 ) );
		Assert.False( screen.VirtualScreen.IsDirty( 2, 2 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 2, 3 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 2, 4 ) );
	}

	[Fact]
	public void IsRegionTouchedUsesAnyDirtyProjectedCellSemantics() {
		CursesScreen screen = new( 5, 3 );
		CursesWindow window = screen.StandardWindow;
		screen.VirtualScreen.MarkClean();
		window.TouchRegion(
			1,
			2,
			1,
			2
		);

		Assert.True( window.IsRegionTouched( 1, 1, 1, 2 ) );
		Assert.True( window.IsRegionTouched( 1, 3, 1, 2 ) );
		Assert.False( window.IsRegionTouched( 0, 0, 1, 5 ) );
		Assert.False( window.IsRegionTouched( 2, 0, 1, 5 ) );
	}

	[Fact]
	public void ClippedLocalCellsAreIgnoredUntilTheyProjectOntoScreen() {
		CursesScreen screen = new( 5, 5 );
		CursesWindow window = screen.CreateWindow(
			2,
			2,
			3,
			3
		);
		screen.Resize(
			3,
			3
		);
		screen.VirtualScreen.MarkClean();

		window.TouchRegion(
			0,
			0,
			3,
			3
		);

		Assert.Equal( 1, screen.VirtualScreen.DirtyCellCount );
		Assert.True( window.IsRegionTouched( 0, 0, 1, 1 ) );
		Assert.False( window.IsRegionTouched( 0, 1, 1, 2 ) );
		Assert.False( window.IsRegionTouched( 1, 0, 2, 3 ) );
	}

	[Fact]
	public void TouchingOnlyContinuationColumnReportsThatSelectedRangeTouched() {
		CursesScreen screen = new( 6, 2 );
		CursesWindow root = screen.StandardWindow;
		root.Write( "A\u754CB" );
		screen.VirtualScreen.MarkClean();
		CursesWindow continuation = screen.CreateWindow(
			0,
			2,
			1,
			1
		);

		continuation.TouchRegion(
			0,
			0,
			1,
			1
		);

		Assert.True( screen.VirtualScreen.IsDirty( 0, 2 ) );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 1 ) );
		Assert.True( continuation.IsRegionTouched( 0, 0, 1, 1 ) );
		Assert.False( root.IsRegionTouched( 0, 1, 1, 1 ) );
	}

	[Fact]
	public void EditorLikeUnicodeWorkloadPreservesFootprintsAndProducesDamage() {
		CursesScreen screen = new( 16, 6 );
		CursesWindow editor = screen.CreateWindow(
			1,
			1,
			4,
			12
		);
		editor.WrapMode = CursesWrapMode.Clip;
		editor.Write( "A\u754CBC\U0001F600D" );
		editor.Move(
			0,
			1
		);
		editor.InsertCells( 2 );
		editor.Move(
			0,
			6
		);
		editor.DeleteCells();
		editor.Move(
			1,
			0
		);
		editor.Write( "row-two" );
		editor.CopyRectangleTo(
			editor,
			1,
			0,
			1,
			7,
			2,
			2
		);
		editor.DrawHorizontalLine(
			3,
			0,
			12,
			new CursesCell( "-" )
		);
		editor.OverlayRectangleTo(
			editor,
			0,
			0,
			2,
			8,
			1,
			1
		);

		CursesCellFootprint.Validate( screen.VirtualScreen );
		Assert.True( editor.IsRegionTouched( 0, 0, 4, 12 ) );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( -1 )]
	public void DamageRegionsRejectNonPositiveDimensions( int dimension ) {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.TouchRegion(
				0,
				0,
				dimension,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.IsRegionTouched(
				0,
				0,
				1,
				dimension
			)
		);
	}
}
