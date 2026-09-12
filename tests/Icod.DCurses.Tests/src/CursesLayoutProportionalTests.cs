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

public sealed class CursesLayoutProportionalTests {
	[Fact]
	public void EqualRowWeightsAssignOddRemainderToSecondRegion() {
		CursesRectangle bounds = new( 2, 3, 5, 8 );

		CursesLayout.SplitRowsProportional(
			bounds,
			1,
			1,
			out CursesRectangle first,
			out CursesRectangle second
		);

		Assert.Equal( new CursesRectangle( 2, 3, 2, 8 ), first );
		Assert.Equal( new CursesRectangle( 4, 3, 3, 8 ), second );
	}

	[Fact]
	public void UnequalRowWeightsCoverBoundsExactly() {
		CursesRectangle bounds = new( 4, 5, 10, 7 );

		CursesLayout.SplitRowsProportional(
			bounds,
			1,
			3,
			out CursesRectangle first,
			out CursesRectangle second
		);

		Assert.Equal( new CursesRectangle( 4, 5, 2, 7 ), first );
		Assert.Equal( new CursesRectangle( 6, 5, 8, 7 ), second );
		Assert.Equal( bounds.Rows, first.Rows + second.Rows );
		Assert.Equal( first.BottomExclusive, second.Row );
	}

	[Fact]
	public void EqualColumnWeightsAssignOddRemainderToSecondRegion() {
		CursesRectangle bounds = new( 2, 3, 6, 5 );

		CursesLayout.SplitColumnsProportional(
			bounds,
			1,
			1,
			out CursesRectangle first,
			out CursesRectangle second
		);

		Assert.Equal( new CursesRectangle( 2, 3, 6, 2 ), first );
		Assert.Equal( new CursesRectangle( 2, 5, 6, 3 ), second );
	}

	[Fact]
	public void UnequalColumnWeightsCoverBoundsExactly() {
		CursesRectangle bounds = new( 4, 5, 7, 10 );

		CursesLayout.SplitColumnsProportional(
			bounds,
			3,
			1,
			out CursesRectangle first,
			out CursesRectangle second
		);

		Assert.Equal( new CursesRectangle( 4, 5, 7, 7 ), first );
		Assert.Equal( new CursesRectangle( 4, 12, 7, 3 ), second );
		Assert.Equal( bounds.Columns, first.Columns + second.Columns );
		Assert.Equal( first.RightExclusive, second.Column );
	}

	[Fact]
	public void TinyBoundsRemainDeterministic() {
		CursesRectangle rowBounds = new( 3, 4, 1, 6 );
		CursesRectangle columnBounds = new( 3, 4, 6, 1 );

		CursesLayout.SplitRowsProportional(
			rowBounds,
			1,
			1,
			out CursesRectangle firstRow,
			out CursesRectangle secondRow
		);
		CursesLayout.SplitColumnsProportional(
			columnBounds,
			1,
			1,
			out CursesRectangle firstColumn,
			out CursesRectangle secondColumn
		);

		Assert.Equal( new CursesRectangle( 3, 4, 0, 6 ), firstRow );
		Assert.Equal( new CursesRectangle( 3, 4, 1, 6 ), secondRow );
		Assert.Equal( new CursesRectangle( 3, 4, 6, 0 ), firstColumn );
		Assert.Equal( new CursesRectangle( 3, 4, 6, 1 ), secondColumn );
	}

	[Fact]
	public void EmptyBoundsPreserveDeterministicOrigins() {
		CursesRectangle rowEmpty = new( 7, 8, 0, 5 );
		CursesRectangle columnEmpty = new( 7, 8, 5, 0 );

		CursesLayout.SplitRowsProportional(
			rowEmpty,
			2,
			3,
			out CursesRectangle firstRow,
			out CursesRectangle secondRow
		);
		CursesLayout.SplitColumnsProportional(
			columnEmpty,
			2,
			3,
			out CursesRectangle firstColumn,
			out CursesRectangle secondColumn
		);

		Assert.Equal( rowEmpty, firstRow );
		Assert.Equal( rowEmpty, secondRow );
		Assert.Equal( columnEmpty, firstColumn );
		Assert.Equal( columnEmpty, secondColumn );
	}

	[Fact]
	public void LargeWeightsDoNotOverflowAllocationArithmetic() {
		CursesRectangle bounds = new(
			0,
			0,
			int.MaxValue,
			int.MaxValue
		);

		CursesLayout.SplitRowsProportional(
			bounds,
			int.MaxValue,
			int.MaxValue,
			out CursesRectangle firstRow,
			out CursesRectangle secondRow
		);
		CursesLayout.SplitColumnsProportional(
			bounds,
			int.MaxValue,
			int.MaxValue,
			out CursesRectangle firstColumn,
			out CursesRectangle secondColumn
		);

		Assert.Equal( int.MaxValue / 2, firstRow.Rows );
		Assert.Equal( int.MaxValue - firstRow.Rows, secondRow.Rows );
		Assert.Equal( int.MaxValue / 2, firstColumn.Columns );
		Assert.Equal( int.MaxValue - firstColumn.Columns, secondColumn.Columns );
	}

	[Fact]
	public void ProportionalSplitsRejectNonPositiveWeights() {
		CursesRectangle bounds = new( 1, 2, 3, 4 );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitRowsProportional(
				bounds,
				0,
				1,
				out _,
				out _
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitRowsProportional(
				bounds,
				1,
				-1,
				out _,
				out _
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitColumnsProportional(
				bounds,
				-1,
				1,
				out _,
				out _
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesLayout.SplitColumnsProportional(
				bounds,
				1,
				0,
				out _,
				out _
			)
		);
	}
}
