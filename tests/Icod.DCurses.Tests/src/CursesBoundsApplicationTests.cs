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

/// <summary>Verifies 1.3 rectangle application conveniences.</summary>
public sealed class CursesBoundsApplicationTests {
	[Fact]
	public void ScreenAndStandardWindowExposeScreenBounds() {
		CursesScreen screen = new( 11, 7 );
		CursesRectangle expected = new(
			0,
			0,
			7,
			11
		);

		Assert.Equal( expected, screen.Bounds );
		Assert.Equal( expected, screen.StandardWindow.Bounds );

		screen.Resize(
			8,
			5
		);

		CursesRectangle resized = new(
			0,
			0,
			5,
			8
		);
		Assert.Equal( resized, screen.Bounds );
		Assert.Equal( resized, screen.StandardWindow.Bounds );
	}

	[Fact]
	public void NonStandardWindowBoundsAreRelativeToImmediateParent() {
		CursesScreen screen = new( 12, 8 );
		CursesWindow parent = screen.CreateWindow(
			2,
			3,
			5,
			7
		);
		CursesWindow child = parent.CreateSubwindow(
			1,
			2,
			2,
			3
		);

		Assert.Equal(
			new CursesRectangle(
				2,
				3,
				5,
				7
			),
			parent.Bounds
		);
		Assert.Equal(
			new CursesRectangle(
				1,
				2,
				2,
				3
			),
			child.Bounds
		);
	}

	[Fact]
	public void WindowSetBoundsValidatesFinalRectangleAtomically() {
		CursesScreen screen = new( 8, 6 );
		CursesWindow window = screen.CreateWindow(
			0,
			0,
			2,
			4
		);
		window.Move(
			1,
			3
		);

		window.SetBounds(
			new CursesRectangle(
				3,
				5,
				2,
				3
			)
		);

		Assert.Equal( 3, window.OriginRow );
		Assert.Equal( 5, window.OriginColumn );
		Assert.Equal( 2, window.Rows );
		Assert.Equal( 3, window.Columns );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void WindowSetBoundsClampsCursorWhenFinalDimensionsShrink() {
		CursesScreen screen = new( 10, 6 );
		CursesWindow window = screen.CreateWindow(
			1,
			1,
			4,
			6
		);
		window.Move(
			3,
			5
		);

		window.SetBounds(
			new CursesRectangle(
				2,
				4,
				2,
				3
			)
		);

		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void InvalidWindowSetBoundsLeavesGeometryAndCursorUnchanged() {
		CursesScreen screen = new( 8, 5 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			2,
			3
		);
		window.Move(
			1,
			2
		);
		CursesRectangle before = window.Bounds;

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.SetBounds(
				new CursesRectangle(
					4,
					6,
					2,
					3
				)
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.SetBounds(
				new CursesRectangle(
					1,
					2,
					0,
					3
				)
			)
		);

		Assert.Equal( before, window.Bounds );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void StandardWindowRejectsSetBounds() {
		CursesScreen screen = new( 8, 4 );

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => screen.StandardWindow.SetBounds( screen.Bounds )
		);

		Assert.Contains(
			"standard window",
			exception.Message,
			StringComparison.OrdinalIgnoreCase
		);
	}

	[Fact]
	public void PanelSetBoundsValidatesFinalRectangleAtomically() {
		CursesScreen screen = new( 8, 6 );
		CursesPanel panel = screen.CreatePanel(
			0,
			0,
			2,
			4
		);
		panel.ContentWindow.Write( "ABCD" );

		panel.SetBounds(
			new CursesRectangle(
				3,
				5,
				2,
				3
			)
		);

		Assert.Equal(
			new CursesRectangle(
				3,
				5,
				2,
				3
			),
			panel.Bounds
		);
		Assert.Equal( "A", panel.ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "B", panel.ContentWindow.GetCell( 0, 1 ).Content );
		Assert.Equal( "C", panel.ContentWindow.GetCell( 0, 2 ).Content );
	}

	[Fact]
	public void InvalidPanelSetBoundsLeavesRetainedStateAndGeometryUnchanged() {
		CursesScreen screen = new( 8, 5 );
		CursesPanel panel = screen.CreatePanel(
			1,
			2,
			2,
			3
		);
		panel.ContentWindow.Write( "ABC" );
		CursesRectangle before = panel.Bounds;
		CursesWindow content = panel.ContentWindow;

		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.SetBounds(
				new CursesRectangle(
					4,
					6,
					2,
					3
				)
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.SetBounds(
				new CursesRectangle(
					1,
					2,
					2,
					0
				)
			)
		);

		Assert.Equal( before, panel.Bounds );
		Assert.Same( content, panel.ContentWindow );
		Assert.Equal( "ABC", ReadText( content, 0, 0, 3 ) );
	}

	[Fact]
	public void PanelSetBoundsRejectsDisposedPanel() {
		CursesScreen screen = new( 8, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		CursesRectangle bounds = panel.Bounds;
		panel.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => panel.SetBounds( bounds )
		);
	}

	private static string ReadText(
		CursesWindow window,
		int row,
		int column,
		int count
	) {
		ArgumentNullException.ThrowIfNull( window );
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > count ) {
			throw new ArgumentOutOfRangeException( nameof( count ) );
		}

		char[] characters = new char[ count ];
		for ( int index = 0; index < count; index++ ) {
			CursesCell cell = window.GetCell(
				row,
				column + index
			);
			characters[ index ] = cell.IsBlank
				? ' '
				: cell.Content[ 0 ]
			;
		}
		return new string( characters );
	}
}
