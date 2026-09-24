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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Specifies atomic prepared-cell mutation of logical windows.</summary>
public sealed class CursesBulkCellWriteTests {
	[Fact]
	public void RowWritePreservesCursorAndDoesNotRetainSource() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow window = screen.StandardWindow;
		window.Move( 1, 1 );
		CursesCell[] cells = [ new( "a" ), new( "b" ) ];

		window.WriteCells( 0, 1, cells );
		cells[ 0 ] = new CursesCell( "changed" );

		Assert.Equal( "a", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( "b", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 1, window.CursorColumn );
	}

	[Fact]
	public void InvalidSourceCannotPartiallyMutateDestination() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow window = screen.StandardWindow;
		window.WriteCells( 0, 0, [ new CursesCell( "old" ) ] );
		screen.VirtualScreen.MarkClean();

		ArgumentException error = Assert.Throws<ArgumentException>(
			() => window.WriteCells(
				0, 0, 2, 2,
				[ new CursesCell( "a" ), new CursesCell( "b" ),
					new CursesCell( "c" ), CursesCell.Continuation() ],
				2
			)
		);

		Assert.Equal( "cells", error.ParamName );
		Assert.Equal( "old", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public void RectangularWriteIgnoresSourcePaddingAndClearsOldMetadata() {
		CursesScreen screen = new( 5, 3 );
		CursesWindow window = screen.StandardWindow;
		window.SetMetadata( 1, 1, new CursesCellMetadata(
			new CursesHyperlink( "https://example.org/" )
		) );
		CursesCell[] source = [
			new( "a" ), new( "b" ), new( "padding" ),
			new( "c" ), new( "d" )
		];

		window.WriteCells( 1, 1, 2, 2, source, 3 );

		Assert.Equal( "a", screen.VirtualScreen[ 1, 1 ].Content );
		Assert.Equal( "b", screen.VirtualScreen[ 1, 2 ].Content );
		Assert.Equal( "c", screen.VirtualScreen[ 2, 1 ].Content );
		Assert.Equal( "d", screen.VirtualScreen[ 2, 2 ].Content );
		Assert.Null( window.GetMetadata( 1, 1 ) );
	}

	[Fact]
	public void BoundaryReplacementRepairsExistingWideFootprint() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "界" );
		window.WriteCells( 0, 1, [ new CursesCell( "x" ) ] );

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( "x", screen.VirtualScreen[ 0, 1 ].Content );
	}

	[Fact]
	public void IdenticalSecondWriteDoesNotDamageCleanCells() {
		CursesScreen screen = new( 3, 1 );
		CursesCell[] source = [ new( "a" ), new( "b" ) ];
		screen.StandardWindow.WriteCells( 0, 0, source );
		screen.VirtualScreen.MarkClean();

		screen.StandardWindow.WriteCells( 0, 0, source );

		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public void EmptyWriteValidatesArgumentsWithoutMutation() {
		CursesScreen screen = new( 3, 2 );
		CursesWindow window = screen.StandardWindow;
		screen.VirtualScreen.MarkClean();

		window.WriteCells( 2, 3, 0, 0, [], 0 );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.WriteCells( 3, 0, 0, 0, [], 0 )
		);
		Assert.Throws<ArgumentException>(
			() => window.WriteCells( 0, 0, 2, 2, [ new CursesCell( "a" ) ], 2 )
		);
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public void WideSourceMustHaveMatchingContinuationWithinItsRow() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesCell wide = new( "界", CursesStyle.Default, 2 );
		CursesCell continuation = CursesCell.Continuation();

		window.WriteCells( 0, 1, [ wide, continuation ] );
		Assert.Equal( wide, screen.VirtualScreen[ 0, 1 ] );
		Assert.Equal( continuation, screen.VirtualScreen[ 0, 2 ] );
		Assert.Equal( "cells", Assert.Throws<ArgumentException>(
			() => window.WriteCells( 0, 0, [ wide ] )
		).ParamName );
		Assert.Equal( "cells", Assert.Throws<ArgumentException>(
			() => window.WriteCells( 0, 0, [ continuation ] )
		).ParamName );
	}

	[Fact]
	public void InvalidBlockPreservesExistingRasterAndValidBlockRemovesIt() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow window = screen.StandardWindow;
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		window.SetRasterCell( 0, 1, raster );
		screen.VirtualScreen.MarkClean();

		Assert.Throws<ArgumentException>(
			() => window.WriteCells( 0, 0, 1, 2,
				[ new CursesCell( "a" ), CursesCell.Continuation() ], 2 )
		);
		Assert.True( window.GetRasterCell( 0, 1 ).HasValue );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );

		window.WriteCells( 0, 0, 1, 2,
			[ new CursesCell( "a" ), new CursesCell( "b" ) ], 2 );
		Assert.Null( window.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public void CompleteFrameIsOneCallAndIdenticalFrameProducesNoNewDamage() {
		CursesScreen screen = new( 80, 24 );
		CursesCell[] frame = new CursesCell[ 80 * 24 ];
		Array.Fill( frame, new CursesCell( "." ) );
		screen.VirtualScreen.MarkClean();

		screen.StandardWindow.WriteCells( 0, 0, 24, 80, frame, 80 );
		Assert.Equal( 80 * 24, screen.VirtualScreen.DirtyCellCount );
		Assert.Equal( ".", screen.VirtualScreen[ 23, 79 ].Content );
		screen.VirtualScreen.MarkClean();
		screen.StandardWindow.WriteCells( 0, 0, 24, 80, frame, 80 );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}
}
