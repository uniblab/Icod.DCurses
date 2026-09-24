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

/// <summary>Exercises independent row and column layouts for application-shaped regions.</summary>
public sealed class CursesTrackLayoutApplicationTests {
	[Fact]
	public void TransposingBoundsPreservesTrackExtentsAndOffsets() {
		CursesTrack[] tracks = [
			CursesTrack.Fixed( 2 ),
			CursesTrack.Weighted( minimum: 2, maximum: 5 ),
			CursesTrack.Weighted( 2, minimum: 2, maximum: 7 ),
			CursesTrack.Fixed( 3 )
		];
		CursesRectangle rowBounds = new CursesRectangle( 7, 11, 31, 17 );
		CursesRectangle columnBounds = new CursesRectangle( 11, 7, 17, 31 );
		CursesRectangle[] rows = CursesLayout.ArrangeRows( rowBounds, tracks, 1, CursesTrackDistribution.SpaceEvenly );
		CursesRectangle[] columns = CursesLayout.ArrangeColumns( columnBounds, tracks, 1, CursesTrackDistribution.SpaceEvenly );
		Assert.Equal( tracks.Length, rows.Length );
		for ( int index = 0; index < tracks.Length; index++ ) {
			Assert.Equal( rows[ index ].Row, columns[ index ].Column );
			Assert.Equal( rows[ index ].Rows, columns[ index ].Columns );
			Assert.Equal( rows[ index ].Column, columns[ index ].Row );
			Assert.Equal( rows[ index ].Columns, columns[ index ].Rows );
			Assert.True( rowBounds.Contains( rows[ index ] ) );
			Assert.True( columnBounds.Contains( columns[ index ] ) );
		}
	}

	[Theory]
	[InlineData( 2, 0 )]
	[InlineData( 3, 1 )]
	[InlineData( 10, 8 )]
	[InlineData( 25, 23 )]
	public void EditorDocumentStatusAndPromptRecomputeOnResize( int height, int documentHeight ) {
		CursesRectangle bounds = new CursesRectangle( 0, 0, height, 80 );
		CursesTrack[] tracks = [ CursesTrack.Weighted(), CursesTrack.Fixed( 1 ), CursesTrack.Fixed( 1 ) ];
		CursesRectangle[] regions = CursesLayout.ArrangeRows( bounds, tracks );
		Assert.Equal( documentHeight, regions[ 0 ].Rows );
		Assert.Equal( new CursesRectangle( documentHeight, 0, 1, 80 ), regions[ 1 ] );
		Assert.Equal( new CursesRectangle( height - 1, 0, 1, 80 ), regions[ 2 ] );
		Assert.Equal( regions, CursesLayout.ArrangeRows( bounds, tracks ) );
		AssertPartition( bounds, regions );
	}

	[Theory]
	[InlineData( 6, 4, 1, 5, 1 )]
	[InlineData( 20, 10, 1, 19, 7 )]
	[InlineData( 80, 25, 60, 20, 22 )]
	public void RoguelikeMapSidebarAndMessagesStayDisjoint(
		int width, int height, int mapWidth, int sidebarWidth, int mapHeight
	) {
		CursesRectangle bounds = new CursesRectangle( 0, 0, height, width );
		CursesRectangle[] vertical = CursesLayout.ArrangeRows(
			bounds,
			[ CursesTrack.Weighted( minimum: 1 ), CursesTrack.Fixed( 3 ) ]
		);
		CursesRectangle[] horizontal = CursesLayout.ArrangeColumns(
			vertical[ 0 ],
			[ CursesTrack.Weighted( minimum: 1 ), CursesTrack.Fixed( 20, minimum: 5 ) ]
		);
		Assert.Equal( mapHeight, horizontal[ 0 ].Rows );
		Assert.Equal( mapWidth, horizontal[ 0 ].Columns );
		Assert.Equal( sidebarWidth, horizontal[ 1 ].Columns );
		Assert.Equal( 3, vertical[ 1 ].Rows );
		AssertPartition( bounds, [ horizontal[ 0 ], horizontal[ 1 ], vertical[ 1 ] ] );
	}

	private static void AssertPartition( CursesRectangle bounds, CursesRectangle[] regions ) {
		foreach ( CursesRectangle region in regions ) {
			Assert.True( bounds.Contains( region ) );
		}
		for ( int first = 0; first < regions.Length; first++ ) {
			for ( int second = first + 1; second < regions.Length; second++ ) {
				Assert.True( regions[ first ].Intersect( regions[ second ] ).IsEmpty );
			}
		}
	}
}
