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

public sealed class CursesLayoutDockTests {
	private static readonly CursesRectangle Bounds = new(
		row: 2,
		column: 3,
		rows: 6,
		columns: 8
	);

	[Fact]
	public void DockTopAllocatesRequestedRowsAndRemainder() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Top,
			2,
			out CursesRectangle docked,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 2, 3, 2, 8 ), docked );
		Assert.Equal( new CursesRectangle( 4, 3, 4, 8 ), remaining );
	}

	[Fact]
	public void DockBottomAllocatesRequestedRowsAndRemainder() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Bottom,
			2,
			out CursesRectangle docked,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 6, 3, 2, 8 ), docked );
		Assert.Equal( new CursesRectangle( 2, 3, 4, 8 ), remaining );
	}

	[Fact]
	public void DockLeftAllocatesRequestedColumnsAndRemainder() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Left,
			3,
			out CursesRectangle docked,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 2, 3, 6, 3 ), docked );
		Assert.Equal( new CursesRectangle( 2, 6, 6, 5 ), remaining );
	}

	[Fact]
	public void DockRightAllocatesRequestedColumnsAndRemainder() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Right,
			3,
			out CursesRectangle docked,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 2, 8, 6, 3 ), docked );
		Assert.Equal( new CursesRectangle( 2, 3, 6, 5 ), remaining );
	}

	[Fact]
	public void OversizedDockRequestsClipToAvailableExtent() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Top,
			99,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Bottom,
			99,
			out CursesRectangle bottom,
			out CursesRectangle above
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Left,
			99,
			out CursesRectangle left,
			out CursesRectangle rightOfLeft
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Right,
			99,
			out CursesRectangle right,
			out CursesRectangle leftOfRight
		);

		Assert.Equal( Bounds, top );
		Assert.Equal( new CursesRectangle( 8, 3, 0, 8 ), below );
		Assert.Equal( Bounds, bottom );
		Assert.Equal( new CursesRectangle( 2, 3, 0, 8 ), above );
		Assert.Equal( Bounds, left );
		Assert.Equal( new CursesRectangle( 2, 11, 6, 0 ), rightOfLeft );
		Assert.Equal( Bounds, right );
		Assert.Equal( new CursesRectangle( 2, 3, 6, 0 ), leftOfRight );
	}

	[Fact]
	public void ZeroDockRequestsYieldEdgeEmptyAllocationAndOriginalRemainder() {
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Top,
			0,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Bottom,
			0,
			out CursesRectangle bottom,
			out CursesRectangle above
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Left,
			0,
			out CursesRectangle left,
			out CursesRectangle rightOfLeft
		);
		CursesLayout.Dock(
			Bounds,
			CursesDockEdge.Right,
			0,
			out CursesRectangle right,
			out CursesRectangle leftOfRight
		);

		Assert.Equal( new CursesRectangle( 2, 3, 0, 8 ), top );
		Assert.Equal( Bounds, below );
		Assert.Equal( new CursesRectangle( 8, 3, 0, 8 ), bottom );
		Assert.Equal( Bounds, above );
		Assert.Equal( new CursesRectangle( 2, 3, 6, 0 ), left );
		Assert.Equal( Bounds, rightOfLeft );
		Assert.Equal( new CursesRectangle( 2, 11, 6, 0 ), right );
		Assert.Equal( Bounds, leftOfRight );
	}

	[Fact]
	public void DockPreservesEmptyInputGeometry() {
		CursesRectangle rowEmpty = new( 4, 5, 0, 7 );
		CursesRectangle columnEmpty = new( 4, 5, 7, 0 );

		CursesLayout.Dock(
			rowEmpty,
			CursesDockEdge.Top,
			3,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.Dock(
			rowEmpty,
			CursesDockEdge.Bottom,
			3,
			out CursesRectangle bottom,
			out CursesRectangle above
		);
		CursesLayout.Dock(
			columnEmpty,
			CursesDockEdge.Left,
			3,
			out CursesRectangle left,
			out CursesRectangle rightOfLeft
		);
		CursesLayout.Dock(
			columnEmpty,
			CursesDockEdge.Right,
			3,
			out CursesRectangle right,
			out CursesRectangle leftOfRight
		);

		Assert.Equal( rowEmpty, top );
		Assert.Equal( rowEmpty, below );
		Assert.Equal( rowEmpty, bottom );
		Assert.Equal( rowEmpty, above );
		Assert.Equal( columnEmpty, left );
		Assert.Equal( columnEmpty, rightOfLeft );
		Assert.Equal( columnEmpty, right );
		Assert.Equal( columnEmpty, leftOfRight );
	}

	[Fact]
	public void DockRejectsNegativeSize() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.Dock(
				Bounds,
				CursesDockEdge.Top,
				-1,
				out _,
				out _
			)
		);

		Assert.Equal( "size", exception.ParamName );
	}

	[Fact]
	public void DockRejectsInvalidEdge() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.Dock(
				Bounds,
				(CursesDockEdge)( -1 ),
				1,
				out _,
				out _
			)
		);

		Assert.Equal( "edge", exception.ParamName );
	}
}
