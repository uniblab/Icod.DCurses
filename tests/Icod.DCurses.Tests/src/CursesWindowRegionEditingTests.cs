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

using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies window-local inspection, fill, and erase-completion behavior.</summary>
public sealed class CursesWindowRegionEditingTests {
	[Fact]
	public void GetCellUsesWindowLocalCoordinatesAfterReposition() {
		CursesScreen screen = new( 10, 6 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			3,
			4
		);
		window.Reposition(
			2,
			4
		);
		window.Move(
			1,
			2
		);
		window.Write( "G" );

		CursesCell cell = window.GetCell(
			1,
			2
		);

		Assert.Equal( "G", cell.Content );
		Assert.Equal( cell, screen.VirtualScreen[ 3, 6 ] );
	}

	[Fact]
	public void FillRectangleUsesLocalCoordinatesAndPreservesCursor() {
		CursesScreen screen = new( 9, 5 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			3,
			5
		);
		window.Move(
			2,
			4
		);
		CursesCell fill = new( "#" );

		window.FillRectangle(
			0,
			1,
			2,
			3,
			fill
		);

		Assert.Equal( 2, window.CursorRow );
		Assert.Equal( 4, window.CursorColumn );
		for ( int row = 1; row <= 2; row++ ) {
			for ( int column = 3; column <= 5; column++ ) {
				Assert.Equal( fill, screen.VirtualScreen[ row, column ] );
			}
		}
		Assert.True( screen.VirtualScreen[ 1, 2 ].IsBlank );
		Assert.True( screen.VirtualScreen[ 1, 6 ].IsBlank );
	}

	[Fact]
	public void FillRectangleRepairsWideFootprintAcrossWindowBoundary() {
		CursesScreen screen = new( 8, 2 );
		CursesWindow standard = screen.StandardWindow;
		standard.Move(
			0,
			2
		);
		standard.Write( "\u754C" );
		CursesWindow rightHalf = screen.CreateWindow(
			0,
			3,
			1,
			2
		);

		rightHalf.FillRectangle(
			0,
			0,
			1,
			1,
			new CursesCell( "X" )
		);

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 3 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void FillRectangleRejectsContinuationAndWideLeaderCells() {
		CursesScreen screen = new( 8, 2 );
		CursesWindow window = screen.CreateWindow(
			0,
			0,
			1,
			4
		);

		Assert.Throws<ArgumentException>(
			() => window.FillRectangle(
				0,
				0,
				1,
				1,
				CursesCell.Continuation()
			)
		);

		screen.StandardWindow.Move(
			1,
			0
		);
		screen.StandardWindow.Write( "\u754C" );
		CursesCell wideLeader = screen.VirtualScreen[ 1, 0 ];
		Assert.Equal( 2, wideLeader.DisplayWidth );
		Assert.Throws<ArgumentException>(
			() => window.FillRectangle(
				0,
				0,
				1,
				1,
				wideLeader
			)
		);
	}

	[Fact]
	public void ClearToBeginningOfLineUsesBackgroundAndPreservesCursor() {
		CursesScreen screen = new( 7, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCDEFG" );
		window.BackgroundCell = new CursesCell( "." );
		window.Move(
			0,
			3
		);

		window.ClearToBeginningOfLine();

		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 3, window.CursorColumn );
		for ( int column = 0; column <= 3; column++ ) {
			Assert.Equal( ".", screen.VirtualScreen[ 0, column ].Content );
		}
		Assert.Equal( "E", screen.VirtualScreen[ 0, 4 ].Content );
		Assert.Equal( "F", screen.VirtualScreen[ 0, 5 ].Content );
		Assert.Equal( "G", screen.VirtualScreen[ 0, 6 ].Content );
	}

	[Theory]
	[InlineData( -1, 0, 1, 1 )]
	[InlineData( 0, -1, 1, 1 )]
	[InlineData( 0, 0, 0, 1 )]
	[InlineData( 0, 0, 1, 0 )]
	[InlineData( 1, 0, 2, 1 )]
	[InlineData( 0, 2, 1, 2 )]
	public void FillRectangleRejectsInvalidLocalRectangle(
		int row,
		int column,
		int rows,
		int columns
	) {
		CursesScreen screen = new( 6, 4 );
		CursesWindow window = screen.CreateWindow(
			0,
			0,
			2,
			3
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.FillRectangle(
				row,
				column,
				rows,
				columns,
				new CursesCell( "X" )
			)
		);
	}
}
