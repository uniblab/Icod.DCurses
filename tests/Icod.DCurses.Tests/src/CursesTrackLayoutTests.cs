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

	[Fact]
	public void LimitsSaturateAndRedistributeToRemainingWeightedTracks() {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, 20 ),
			[ CursesTrack.Fixed( 4, maximum: 3 ), CursesTrack.Weighted( minimum: 2, maximum: 4 ), CursesTrack.Weighted() ]
		);
		Assert.Equal( new[] { 3, 4, 13 }, Array.ConvertAll( result, static rectangle => rectangle.Columns ) );
		Assert.Equal( new[] { 0, 3, 7 }, Array.ConvertAll( result, static rectangle => rectangle.Column ) );
	}

	[Fact]
	public void FixedPreferencesYieldInOrderWhenOnlyMinimumsFit() {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, 10 ),
			[ CursesTrack.Fixed( 10, minimum: 2 ), CursesTrack.Fixed( 10, minimum: 3 ), CursesTrack.Weighted( minimum: 1 ) ]
		);
		Assert.Equal( new[] { 6, 3, 1 }, Array.ConvertAll( result, static rectangle => rectangle.Columns ) );
	}

	[Fact]
	public void WeightedRemainderGoesToLowIndexAndHonorsSaturatedCaps() {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, 13 ),
			[ CursesTrack.Weighted( maximum: 2 ), CursesTrack.Weighted( maximum: 5 ), CursesTrack.Weighted() ]
		);
		Assert.Equal( new[] { 2, 5, 6 }, Array.ConvertAll( result, static rectangle => rectangle.Columns ) );
		CursesRectangle[] remainder = CursesLayout.ArrangeRows(
			new CursesRectangle( 5, 2, 9, 1 ),
			[ CursesTrack.Weighted(), CursesTrack.Weighted(), CursesTrack.Weighted() ],
			gap: 1
		);
		Assert.Equal( new[] { 3, 2, 2 }, Array.ConvertAll( remainder, static rectangle => rectangle.Rows ) );
		Assert.Equal( new[] { 5, 9, 12 }, Array.ConvertAll( remainder, static rectangle => rectangle.Row ) );
	}

	[Theory]
	[InlineData( CursesTrackDistribution.Start, 0, 3 )]
	[InlineData( CursesTrackDistribution.Center, 5, 8 )]
	[InlineData( CursesTrackDistribution.End, 11, 14 )]
	[InlineData( CursesTrackDistribution.SpaceBetween, 0, 14 )]
	[InlineData( CursesTrackDistribution.SpaceAround, 3, 12 )]
	[InlineData( CursesTrackDistribution.SpaceEvenly, 4, 11 )]
	public void SurplusUsesTheRequestedDistribution( CursesTrackDistribution distribution, int first, int second ) {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, 16 ),
			[ CursesTrack.Fixed( 2 ), CursesTrack.Fixed( 2 ) ],
			gap: 1,
			distribution: distribution
		);
		Assert.Equal( first, result[ 0 ].Column );
		Assert.Equal( second, result[ 1 ].Column );
		Assert.Equal( 2, result[ 0 ].Columns );
		Assert.Equal( 2, result[ 1 ].Columns );
	}

	[Fact]
	public void OneTrackDistributionAndOddCenterHaveDefinedEdges() {
		CursesRectangle bounds = new CursesRectangle( 0, 10, 1, 9 );
		Assert.Equal( 10, CursesLayout.ArrangeColumns( bounds, [ CursesTrack.Fixed( 2 ) ], distribution: CursesTrackDistribution.SpaceBetween )[ 0 ].Column );
		Assert.Equal( 13, CursesLayout.ArrangeColumns( bounds, [ CursesTrack.Fixed( 2 ) ], distribution: CursesTrackDistribution.Center )[ 0 ].Column );
		Assert.Equal( 14, CursesLayout.ArrangeColumns( bounds, [ CursesTrack.Fixed( 2 ) ], distribution: CursesTrackDistribution.SpaceAround )[ 0 ].Column );
	}

	[Fact]
	public void FactoryLimitsAndLayoutArgumentsAreValidated() {
		Assert.Equal( "size", Assert.Throws<ArgumentOutOfRangeException>( () => CursesTrack.Fixed( -1 ) ).ParamName );
		Assert.Equal( "weight", Assert.Throws<ArgumentOutOfRangeException>( () => CursesTrack.Weighted( 0 ) ).ParamName );
		Assert.Equal( "minimum", Assert.Throws<ArgumentOutOfRangeException>( () => CursesTrack.Fixed( 1, minimum: -1 ) ).ParamName );
		Assert.Equal( "maximum", Assert.Throws<ArgumentOutOfRangeException>( () => CursesTrack.Weighted( maximum: -1 ) ).ParamName );
		CursesRectangle bounds = new CursesRectangle( 0, 0, 1, 5 );
		Assert.Equal( "tracks", Assert.Throws<ArgumentException>( () => CursesLayout.ArrangeColumns( bounds, [ CursesTrack.Fixed( 3, minimum: 3 ), CursesTrack.Weighted( minimum: 3 ) ] ) ).ParamName );
		Assert.Equal( "gap", Assert.Throws<ArgumentOutOfRangeException>( () => CursesLayout.ArrangeColumns( bounds, [ CursesTrack.Fixed( 1 ), CursesTrack.Fixed( 1 ), CursesTrack.Fixed( 1 ) ], gap: int.MaxValue ) ).ParamName );
		Assert.Equal( "distribution", Assert.Throws<ArgumentOutOfRangeException>( () => CursesLayout.ArrangeColumns( bounds, [], distribution: (CursesTrackDistribution)99 ) ).ParamName );
		Assert.Equal( 4096, CursesLayout.ArrangeRows( new CursesRectangle( 0, 0, 0, 1 ), new CursesTrack[ 4096 ] ).Length );
		Assert.Equal( "tracks", Assert.Throws<ArgumentOutOfRangeException>( () => CursesLayout.ArrangeRows( bounds, new CursesTrack[ 4097 ] ) ).ParamName );
	}

	[Fact]
	public void LargeWeightsAndExtentsDoNotOverflow() {
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, int.MaxValue ),
			[ CursesTrack.Weighted( int.MaxValue ), CursesTrack.Weighted( int.MaxValue ) ],
			gap: 1
		);
		Assert.Equal( 1_073_741_823, result[ 0 ].Columns );
		Assert.Equal( 1_073_741_824, result[ 1 ].Column );
		Assert.Equal( int.MaxValue, result[ 1 ].RightExclusive );
	}
}
