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

public sealed class CursesGeometryTests {
	[Fact]
	public void RectangleExposesValidatedBoundsAndContainment() {
		CursesRectangle rectangle = new(
			row: 2,
			column: 3,
			rows: 4,
			columns: 5
		);

		Assert.Equal( 2, rectangle.Row );
		Assert.Equal( 3, rectangle.Column );
		Assert.Equal( 4, rectangle.Rows );
		Assert.Equal( 5, rectangle.Columns );
		Assert.Equal( 6, rectangle.BottomExclusive );
		Assert.Equal( 8, rectangle.RightExclusive );
		Assert.False( rectangle.IsEmpty );
		Assert.True( rectangle.Contains( 2, 3 ) );
		Assert.True( rectangle.Contains( 5, 7 ) );
		Assert.False( rectangle.Contains( 6, 3 ) );
		Assert.False( rectangle.Contains( 2, 8 ) );
	}

	[Fact]
	public void RectangleRejectsNegativeGeometryAndOverflow() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( -1, 0, 1, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( 0, -1, 1, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( 0, 0, -1, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( 0, 0, 1, -1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( int.MaxValue, 0, 1, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRectangle( 0, int.MaxValue, 0, 1 )
		);
	}

	[Fact]
	public void EmptyRectangleContainsNoCoordinates() {
		CursesRectangle rectangle = new(
			row: 4,
			column: 5,
			rows: 0,
			columns: 3
		);

		Assert.True( rectangle.IsEmpty );
		Assert.False( rectangle.Contains( 4, 5 ) );
	}

	[Fact]
	public void RectangleContainmentIncludesEmptyBoundaryRectangle() {
		CursesRectangle outer = new( 2, 3, 4, 5 );
		CursesRectangle inner = new( 3, 4, 2, 2 );
		CursesRectangle emptyAtBoundary = new( 6, 8, 0, 0 );

		Assert.True( outer.Contains( inner ) );
		Assert.True( outer.Contains( emptyAtBoundary ) );
		Assert.False( inner.Contains( outer ) );
	}

	[Fact]
	public void IntersectionReturnsOverlapOrDeterministicEmptyRectangle() {
		CursesRectangle first = new( 1, 2, 4, 5 );
		CursesRectangle second = new( 3, 4, 4, 5 );

		Assert.Equal(
			new CursesRectangle( 3, 4, 2, 3 ),
			first.Intersect( second )
		);
		Assert.Equal(
			new CursesRectangle( 10, 12, 0, 0 ),
			first.Intersect( new CursesRectangle( 10, 12, 2, 2 ) )
		);
	}

	[Fact]
	public void InsetsExposeTotalsAndRejectInvalidValues() {
		CursesInsets insets = new(
			top: 1,
			right: 2,
			bottom: 3,
			left: 4
		);

		Assert.Equal( 1, insets.Top );
		Assert.Equal( 2, insets.Right );
		Assert.Equal( 3, insets.Bottom );
		Assert.Equal( 4, insets.Left );
		Assert.Equal( 6, insets.Horizontal );
		Assert.Equal( 4, insets.Vertical );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( -1, 0, 0, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( 0, -1, 0, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( 0, 0, -1, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( 0, 0, 0, -1 )
		);
	}

	[Fact]
	public void InsetsRejectTotalOverflow() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( int.MaxValue, 0, 1, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesInsets( 0, int.MaxValue, 0, 1 )
		);
	}

	[Fact]
	public void RectangleInsetReturnsInnerRectangle() {
		CursesRectangle rectangle = new( 2, 3, 8, 10 );
		CursesInsets insets = new( 1, 2, 3, 4 );

		Assert.Equal(
			new CursesRectangle( 3, 7, 4, 4 ),
			rectangle.Inset( insets )
		);
	}

	[Fact]
	public void ExcessiveInsetsYieldDeterministicEmptyRectangle() {
		CursesRectangle rectangle = new( 2, 3, 3, 4 );

		Assert.Equal(
			new CursesRectangle( 5, 7, 0, 0 ),
			rectangle.Inset( new CursesInsets( 5, 5, 5, 5 ) )
		);
	}
}
