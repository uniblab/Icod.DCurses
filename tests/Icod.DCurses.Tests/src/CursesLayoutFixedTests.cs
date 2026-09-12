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

public sealed class CursesLayoutFixedTests {
	private static readonly CursesRectangle Bounds = new(
		row: 2,
		column: 3,
		rows: 6,
		columns: 8
	);

	[Fact]
	public void SplitTopAllocatesRequestedRowsAndRemainder() {
		CursesLayout.SplitTop(
			Bounds,
			2,
			out CursesRectangle first,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 2, 3, 2, 8 ), first );
		Assert.Equal( new CursesRectangle( 4, 3, 4, 8 ), remaining );
	}

	[Fact]
	public void SplitBottomAllocatesRequestedRowsAndRemainder() {
		CursesLayout.SplitBottom(
			Bounds,
			2,
			out CursesRectangle remaining,
			out CursesRectangle last
		);

		Assert.Equal( new CursesRectangle( 2, 3, 4, 8 ), remaining );
		Assert.Equal( new CursesRectangle( 6, 3, 2, 8 ), last );
	}

	[Fact]
	public void SplitLeftAllocatesRequestedColumnsAndRemainder() {
		CursesLayout.SplitLeft(
			Bounds,
			3,
			out CursesRectangle first,
			out CursesRectangle remaining
		);

		Assert.Equal( new CursesRectangle( 2, 3, 6, 3 ), first );
		Assert.Equal( new CursesRectangle( 2, 6, 6, 5 ), remaining );
	}

	[Fact]
	public void SplitRightAllocatesRequestedColumnsAndRemainder() {
		CursesLayout.SplitRight(
			Bounds,
			3,
			out CursesRectangle remaining,
			out CursesRectangle last
		);

		Assert.Equal( new CursesRectangle( 2, 3, 6, 5 ), remaining );
		Assert.Equal( new CursesRectangle( 2, 8, 6, 3 ), last );
	}

	[Fact]
	public void OversizedFixedRequestsClipToAvailableExtent() {
		CursesLayout.SplitTop(
			Bounds,
			99,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.SplitBottom(
			Bounds,
			99,
			out CursesRectangle above,
			out CursesRectangle bottom
		);
		CursesLayout.SplitLeft(
			Bounds,
			99,
			out CursesRectangle left,
			out CursesRectangle rightOfLeft
		);
		CursesLayout.SplitRight(
			Bounds,
			99,
			out CursesRectangle leftOfRight,
			out CursesRectangle right
		);

		Assert.Equal( Bounds, top );
		Assert.Equal( new CursesRectangle( 8, 3, 0, 8 ), below );
		Assert.Equal( new CursesRectangle( 2, 3, 0, 8 ), above );
		Assert.Equal( Bounds, bottom );
		Assert.Equal( Bounds, left );
		Assert.Equal( new CursesRectangle( 2, 11, 6, 0 ), rightOfLeft );
		Assert.Equal( new CursesRectangle( 2, 3, 6, 0 ), leftOfRight );
		Assert.Equal( Bounds, right );
	}

	[Fact]
	public void ZeroFixedRequestsYieldBoundaryEmptyAllocationAndOriginalRemainder() {
		CursesLayout.SplitTop(
			Bounds,
			0,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.SplitBottom(
			Bounds,
			0,
			out CursesRectangle above,
			out CursesRectangle bottom
		);
		CursesLayout.SplitLeft(
			Bounds,
			0,
			out CursesRectangle left,
			out CursesRectangle rightOfLeft
		);
		CursesLayout.SplitRight(
			Bounds,
			0,
			out CursesRectangle leftOfRight,
			out CursesRectangle right
		);

		Assert.Equal( new CursesRectangle( 2, 3, 0, 8 ), top );
		Assert.Equal( Bounds, below );
		Assert.Equal( Bounds, above );
		Assert.Equal( new CursesRectangle( 8, 3, 0, 8 ), bottom );
		Assert.Equal( new CursesRectangle( 2, 3, 6, 0 ), left );
		Assert.Equal( Bounds, rightOfLeft );
		Assert.Equal( Bounds, leftOfRight );
		Assert.Equal( new CursesRectangle( 2, 11, 6, 0 ), right );
	}

	[Fact]
	public void FixedSplitsPreserveEmptyInputGeometry() {
		CursesRectangle rowEmpty = new( 4, 5, 0, 7 );
		CursesRectangle columnEmpty = new( 4, 5, 7, 0 );

		CursesLayout.SplitTop(
			rowEmpty,
			3,
			out CursesRectangle top,
			out CursesRectangle below
		);
		CursesLayout.SplitRight(
			columnEmpty,
			3,
			out CursesRectangle left,
			out CursesRectangle right
		);

		Assert.Equal( rowEmpty, top );
		Assert.Equal( rowEmpty, below );
		Assert.Equal( columnEmpty, left );
		Assert.Equal( columnEmpty, right );
	}

	[Fact]
	public void FixedSplitsRejectNegativeRequests() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitTop( Bounds, -1, out _, out _ )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitBottom( Bounds, -1, out _, out _ )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitLeft( Bounds, -1, out _, out _ )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitRight( Bounds, -1, out _, out _ )
		);
	}

	[Fact]
	public void ClipReturnsRectangleIntersection() {
		CursesRectangle rectangle = new( 1, 1, 5, 7 );
		CursesRectangle container = new( 3, 4, 5, 5 );

		Assert.Equal(
			new CursesRectangle( 3, 4, 3, 4 ),
			CursesLayout.Clip( rectangle, container )
		);
		Assert.Equal(
			new CursesRectangle( 20, 20, 0, 0 ),
			CursesLayout.Clip(
				rectangle,
				new CursesRectangle( 20, 20, 2, 2 )
			)
		);
	}
}
