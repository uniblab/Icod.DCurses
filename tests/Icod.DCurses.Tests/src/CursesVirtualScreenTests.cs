using Icod.DCurses;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesVirtualScreenTests {
	[Fact]
	public void DirectCellConstructionRejectsNullContentDeterministically() {
		Assert.Throws<ArgumentNullException>(
			() => _ = new CursesCell( null! )
		);
	}

	[Fact]
	public void StyledVirtualFrameCanBeConstructedWithoutTerminalBackend() {
		CursesVirtualScreen screen = new( 5, 2 );
		CursesStyle heading = new(
			CursesColor.Indexed( 14 ),
			CursesColor.Default,
			CursesTextAttributes.Bold | CursesTextAttributes.Underline
		);
		CursesStyle wide = new(
			CursesColor.Rgb( 200, 180, 40 ),
			CursesColor.Indexed( 0 ),
			CursesTextAttributes.Reverse
		);

		screen[ 0, 0 ] = new CursesCell( "T", heading );
		screen[ 0, 1 ] = new CursesCell( "O", heading );
		screen[ 0, 2 ] = new CursesCell( "P", heading );
		screen[ 1, 0 ] = new CursesCell( "界", wide );
		screen[ 1, 1 ] = CursesCell.Continuation( wide );

		Assert.Equal( "TOP", string.Concat(
			screen[ 0, 0 ].Content,
			screen[ 0, 1 ].Content,
			screen[ 0, 2 ].Content
		) );
		Assert.Equal( heading, screen[ 0, 1 ].Style );
		Assert.Equal( "界", screen[ 1, 0 ].Content );
		Assert.True( screen[ 1, 1 ].IsContinuation );
	}

	[Fact]
	public void NewScreenStartsDirtyBecausePhysicalStateIsUnknown() {
		CursesVirtualScreen screen = new( 3, 2 );

		Assert.Equal( 6, screen.DirtyCellCount );
		Assert.True( screen.IsDirty( 0, 0 ) );
		Assert.True( screen.IsDirty( 1, 2 ) );
	}

	[Fact]
	public void ChangingOneCleanCellMarksOnlyThatCellDirty() {
		CursesVirtualScreen screen = new( 3, 2 );
		screen.MarkClean();

		screen[ 1, 1 ] = new CursesCell( "x" );

		Assert.Equal( 1, screen.DirtyCellCount );
		Assert.True( screen.IsDirty( 1, 1 ) );
		Assert.False( screen.IsDirty( 0, 0 ) );
	}

	[Fact]
	public void WritingSameValueDoesNotDirtyCleanCell() {
		CursesVirtualScreen screen = new( 2, 1 );
		screen[ 0, 0 ] = new CursesCell( "x" );
		screen.MarkClean();

		screen[ 0, 0 ] = new CursesCell( "x" );

		Assert.Equal( 0, screen.DirtyCellCount );
		Assert.False( screen.IsDirty( 0, 0 ) );
	}

	[Fact]
	public void ClearCanApplyStyledBlankCells() {
		CursesVirtualScreen screen = new( 2, 2 );
		CursesStyle background = new(
			CursesColor.Default,
			CursesColor.Indexed( 4 ),
			CursesTextAttributes.None
		);

		screen.Clear( background );

		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				Assert.True( screen[ row, column ].IsBlank );
				Assert.Equal( background, screen[ row, column ].Style );
			}
		}
	}

	[Fact]
	public void DesiredAndPhysicalScreenStateRemainIndependent() {
		CursesVirtualScreen desired = new( 2, 1 );
		CursesPhysicalScreenState physical = new( 2, 1 );
		CursesCell oldCell = new( "o" );
		CursesCell newCell = new( "n" );

		physical.SetCell( 0, 0, oldCell );
		desired[ 0, 0 ] = newCell;

		Assert.Equal( newCell, desired[ 0, 0 ] );
		Assert.True( physical.TryGetCell( 0, 0, out CursesCell knownCell ) );
		Assert.Equal( oldCell, knownCell );

		physical.Invalidate();
		Assert.False( physical.TryGetCell( 0, 0, out _ ) );
		Assert.Equal( newCell, desired[ 0, 0 ] );
	}

	[Fact]
	public void InvalidateMarksEveryDesiredCellDirtyAgain() {
		CursesVirtualScreen screen = new( 4, 3 );
		screen.MarkClean();

		screen.Invalidate();

		Assert.Equal( 12, screen.DirtyCellCount );
		Assert.True( screen.IsDirty( 0, 0 ) );
		Assert.True( screen.IsDirty( 2, 3 ) );
	}

	[Fact]
	public void DimensionsAndCoordinatesAreValidated() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesVirtualScreen( 0, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesVirtualScreen( 1, 0 )
		);

		CursesVirtualScreen screen = new( 2, 2 );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => _ = screen[ -1, 0 ]
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => _ = screen[ 0, 2 ]
		);
	}
}
