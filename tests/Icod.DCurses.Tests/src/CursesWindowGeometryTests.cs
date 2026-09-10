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

/// <summary>Verifies 0.4 window geometry and repositioning semantics.</summary>
public sealed class CursesWindowGeometryTests {
	[Fact]
	public void RootWindowRepositionPreservesLocalCursorAndChangesProjection() {
		CursesScreen screen = new( 10, 6 );
		CursesWindow window = screen.CreateWindow(
			1,
			1,
			2,
			3
		);
		window.Move(
			1,
			2
		);

		window.Reposition(
			3,
			5
		);

		Assert.Equal( 3, window.OriginRow );
		Assert.Equal( 5, window.OriginColumn );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );

		window.Write( "X" );

		Assert.Equal( "X", screen.VirtualScreen[ 4, 7 ].Content );
		Assert.True( screen.VirtualScreen[ 2, 3 ].IsBlank );
	}

	[Fact]
	public void RepositioningAncestorMovesNestedWindowProjection() {
		CursesScreen screen = new( 12, 8 );
		CursesWindow parent = screen.CreateWindow(
			1,
			1,
			5,
			7
		);
		CursesWindow child = parent.CreateSubwindow(
			1,
			2,
			2,
			3
		);

		parent.Reposition(
			2,
			4
		);
		child.Move(
			0,
			1
		);
		child.Write( "N" );

		Assert.Equal( 2, parent.OriginRow );
		Assert.Equal( 4, parent.OriginColumn );
		Assert.Equal( 1, child.OriginRow );
		Assert.Equal( 2, child.OriginColumn );
		Assert.Equal( "N", screen.VirtualScreen[ 3, 7 ].Content );
	}

	[Fact]
	public void SubwindowRepositionIsRelativeToImmediateParent() {
		CursesScreen screen = new( 12, 8 );
		CursesWindow parent = screen.CreateWindow(
			2,
			3,
			4,
			6
		);
		CursesWindow child = parent.CreateSubwindow(
			1,
			1,
			2,
			2
		);

		child.Reposition(
			0,
			3
		);
		child.Move(
			1,
			0
		);
		child.Write( "R" );

		Assert.Equal( 0, child.OriginRow );
		Assert.Equal( 3, child.OriginColumn );
		Assert.Equal( "R", screen.VirtualScreen[ 3, 6 ].Content );
	}

	[Fact]
	public void StandardWindowCannotBeRepositioned() {
		CursesScreen screen = new( 8, 4 );

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => screen.StandardWindow.Reposition(
				0,
				0
			)
		);

		Assert.Contains(
			"standard window",
			exception.Message,
			StringComparison.OrdinalIgnoreCase
		);
	}

	[Fact]
	public void InvalidRepositionLeavesExistingOriginUnchanged() {
		CursesScreen screen = new( 8, 5 );
		CursesWindow window = screen.CreateWindow(
			1,
			2,
			2,
			3
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.Reposition(
				4,
				6
			)
		);

		Assert.Equal( 1, window.OriginRow );
		Assert.Equal( 2, window.OriginColumn );
	}

	[Fact]
	public void ResizeAndRepositionUseCurrentWindowDimensions() {
		CursesScreen screen = new( 12, 8 );
		CursesWindow window = screen.CreateWindow(
			1,
			1,
			2,
			3
		);

		window.Resize(
			4,
			5
		);
		window.Reposition(
			4,
			7
		);

		Assert.Equal( 4, window.Rows );
		Assert.Equal( 5, window.Columns );
		Assert.Equal( 4, window.OriginRow );
		Assert.Equal( 7, window.OriginColumn );
	}
}
