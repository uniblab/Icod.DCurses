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

/// <summary>Specifies stateless fixed and weighted track arrangement.</summary>
public sealed class CursesTrackLayoutTests {
	[Fact]
	public void ArrangeColumnsAssignsFixedThenWeightedSpace() {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 2, 3, 10, 20 ),
			[ CursesTrack.Fixed( 4 ), CursesTrack.Weighted(), CursesTrack.Weighted( 2 ) ],
			gap: 1
		);

		Assert.Equal( new CursesRectangle( 2, 3, 10, 4 ), result[ 0 ] );
		Assert.Equal( new CursesRectangle( 2, 8, 10, 5 ), result[ 1 ] );
		Assert.Equal( new CursesRectangle( 2, 14, 10, 9 ), result[ 2 ] );
	}

	[Fact]
	public void ZeroTracksAndFixedOnlyTracksRemainWithinBounds() {
		CursesRectangle bounds = new CursesRectangle( 5, 7, 10, 20 );
		Assert.Empty( CursesLayout.ArrangeRows( bounds, [] ) );
		Assert.Equal(
			new[] { new CursesRectangle( 5, 7, 4, 20 ), new CursesRectangle( 10, 7, 2, 20 ) },
			CursesLayout.ArrangeRows( bounds, [ CursesTrack.Fixed( 4 ), CursesTrack.Fixed( 2 ) ], gap: 1 )
		);
		Assert.Equal( CursesTrack.Fixed( 4 ), CursesTrack.Fixed( 4 ) );
	}
}
