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

/// <summary>Verifies semantic drawing overloads preserve line-junction identity.</summary>
public sealed class CursesSemanticDrawingTests {
	[Fact]
	public void SemanticHorizontalAndVerticalLinesUseLineCells() {
		CursesScreen screen = new( 5, 4 );
		CursesWindow window = screen.StandardWindow;

		window.DrawHorizontalLine( 1, 1, 3 );
		window.DrawVerticalLine( 0, 4, 4 );

		for ( int column = 1; column <= 3; column++ ) {
			CursesCell cell = window.GetCell( 1, column );
			Assert.Equal( CursesLineGlyph.Horizontal, cell.LineGlyph );
			Assert.Equal( "─", cell.Content );
		}
		for ( int row = 0; row < 4; row++ ) {
			CursesCell cell = window.GetCell( row, 4 );
			Assert.Equal( CursesLineGlyph.Vertical, cell.LineGlyph );
			Assert.Equal( "│", cell.Content );
		}
	}

	[Fact]
	public void SemanticBorderUsesDistinctCornerAndEdgeIdentities() {
		CursesScreen screen = new( 5, 4 );
		CursesWindow window = screen.StandardWindow;

		window.DrawBorder();

		Assert.Equal( CursesLineGlyph.UpperLeftCorner, window.GetCell( 0, 0 ).LineGlyph );
		Assert.Equal( CursesLineGlyph.UpperRightCorner, window.GetCell( 0, 4 ).LineGlyph );
		Assert.Equal( CursesLineGlyph.LowerLeftCorner, window.GetCell( 3, 0 ).LineGlyph );
		Assert.Equal( CursesLineGlyph.LowerRightCorner, window.GetCell( 3, 4 ).LineGlyph );
		Assert.Equal( CursesLineGlyph.Horizontal, window.GetCell( 0, 2 ).LineGlyph );
		Assert.Equal( CursesLineGlyph.Vertical, window.GetCell( 2, 0 ).LineGlyph );
	}

	[Fact]
	public void SemanticBorderAppliesOneStyleToEveryBorderCell() {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesStyle style = new(
			CursesColor.Indexed( 6 ),
			CursesColor.Default,
			CursesTextAttributes.Bold | CursesTextAttributes.Underline
		);

		window.DrawBorder( style );

		for ( int row = 0; row < window.Rows; row++ ) {
			for ( int column = 0; column < window.Columns; column++ ) {
				if ( 0 != row
					&& row != window.Rows - 1
					&& 0 != column
					&& column != window.Columns - 1 ) {
					continue;
				}

				Assert.Equal( style, window.GetCell( row, column ).Style );
			}
		}
	}
}
