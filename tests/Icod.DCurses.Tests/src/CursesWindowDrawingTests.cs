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

/// <summary>Verifies geometric line and border drawing.</summary>
public sealed class CursesWindowDrawingTests {
	[Fact]
	public void HorizontalAndVerticalLinesUseWindowLocalCoordinatesAndPreserveCursor() {
		CursesScreen screen = new( 9, 6 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			4,
			5
		);
		window.Move(
			2,
			3
		);

		window.DrawHorizontalLine(
			0,
			1,
			3,
			new CursesCell( "-" )
		);
		window.DrawVerticalLine(
			1,
			0,
			3,
			new CursesCell( "|" )
		);

		Assert.Equal( 2, window.CursorRow );
		Assert.Equal( 3, window.CursorColumn );
		Assert.Equal( "-", screen.VirtualScreen[ 1, 3 ].Content );
		Assert.Equal( "-", screen.VirtualScreen[ 1, 4 ].Content );
		Assert.Equal( "-", screen.VirtualScreen[ 1, 5 ].Content );
		Assert.Equal( "|", screen.VirtualScreen[ 2, 2 ].Content );
		Assert.Equal( "|", screen.VirtualScreen[ 3, 2 ].Content );
		Assert.Equal( "|", screen.VirtualScreen[ 4, 2 ].Content );
	}

	[Fact]
	public void DrawingThroughContinuationRepairsExistingWideFootprint() {
		CursesScreen screen = new( 6, 2 );
		CursesWindow root = screen.StandardWindow;
		root.Write( "A\u754CB" );
		CursesWindow continuationOnly = screen.CreateWindow(
			0,
			2,
			1,
			2
		);

		continuationOnly.DrawHorizontalLine(
			0,
			0,
			2,
			new CursesCell( "-" )
		);

		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( "-", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( "-", screen.VirtualScreen[ 0, 3 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void BorderDrawsDistinctEdgesAndCorners() {
		CursesScreen screen = new( 8, 5 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			3,
			4
		);
		window.Move(
			1,
			1
		);

		window.DrawBorder(
			new CursesCell( "-" ),
			new CursesCell( "|" ),
			new CursesCell( "1" ),
			new CursesCell( "2" ),
			new CursesCell( "3" ),
			new CursesCell( "4" )
		);

		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 1, window.CursorColumn );
		AssertRowContent( window, 0, "1--2" );
		Assert.Equal( "|", window.GetCell( 1, 0 ).Content );
		Assert.True( window.GetCell( 1, 1 ).IsBlank );
		Assert.True( window.GetCell( 1, 2 ).IsBlank );
		Assert.Equal( "|", window.GetCell( 1, 3 ).Content );
		AssertRowContent( window, 2, "3--4" );
	}

	[Fact]
	public void BorderSupportsMinimalTwoByTwoWindow() {
		CursesScreen screen = new( 2, 2 );
		CursesWindow window = screen.StandardWindow;

		window.DrawBorder(
			new CursesCell( "-" ),
			new CursesCell( "|" ),
			new CursesCell( "A" ),
			new CursesCell( "B" ),
			new CursesCell( "C" ),
			new CursesCell( "D" )
		);

		AssertRowContent( window, 0, "AB" );
		AssertRowContent( window, 1, "CD" );
	}

	[Fact]
	public void BorderRejectsWindowTooSmallForUnambiguousCorners() {
		CursesScreen screen = new( 1, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesCell cell = new( "#" );

		Assert.Throws<InvalidOperationException>(
			() => window.DrawBorder(
				cell,
				cell,
				cell,
				cell,
				cell,
				cell
			)
		);
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( -1 )]
	public void LineDrawingRejectsNonPositiveLengths( int length ) {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesCell cell = new( "#" );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.DrawHorizontalLine(
				0,
				0,
				length,
				cell
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.DrawVerticalLine(
				0,
				0,
				length,
				cell
			)
		);
	}

	[Fact]
	public void DrawingRejectsContinuationCells() {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.StandardWindow;

		Assert.Throws<ArgumentException>(
			() => window.DrawHorizontalLine(
				0,
				0,
				2,
				CursesCell.Continuation()
			)
		);
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
