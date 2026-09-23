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

/// <summary>Verifies regional retained-state eligibility for terminal editing operations.</summary>
public sealed class CursesEditingRegionSafetyTests {
	[Fact]
	public void KnownTextOnlyRectangleIsSafe() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();

		Assert.True(
			IsTestRegionFree( desired, physical )
		);
	}

	[Fact]
	public void DesiredMetadataInsideRectangleIsUnsafe() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();
		desired.SetMetadata( 1, 1, CreateMetadata() );

		Assert.False( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void PhysicalMetadataInsideRectangleIsUnsafe() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();
		physical.SetCell( 1, 1, desired[ 1, 1 ], CreateMetadata() );

		Assert.False( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void DesiredRasterInsideRectangleIsUnsafe() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();
		desired.SetRasterCell(
			1,
			1,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.False( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void PhysicalRasterInsideRectangleIsUnsafe() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();
		physical.SetCell(
			1,
			1,
			desired[ 1, 1 ],
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.False( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void RetainedStateImmediatelyOutsideRectangleDoesNotBlockIt() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();
		desired.SetMetadata( 1, 0, CreateMetadata() );
		desired.SetRasterCell(
			0,
			1,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);
		physical.SetCell( 1, 3, desired[ 1, 3 ], CreateMetadata() );
		physical.SetCell(
			2,
			2,
			desired[ 2, 2 ],
			metadata: null,
			rasterCell: CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.True( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void UnknownPhysicalCoordinateInsideRectangleIsUnsafe() {
		CursesVirtualScreen desired = new( 4, 3 );
		CursesPhysicalScreenState physical = new( 4, 3 );
		for ( int row = 0; row < desired.Rows; row++ ) {
			for ( int column = 0; column < desired.Columns; column++ ) {
				if ( 1 != row || 1 != column ) {
					physical.SetCell( row, column, desired[ row, column ] );
				}
			}
		}

		Assert.False( IsTestRegionFree( desired, physical ) );
	}

	[Fact]
	public void NullScreensAndMismatchedDimensionsAreRejected() {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();

		Assert.Throws<ArgumentNullException>(
			() => CursesEditingRegionSafety.IsRetainedStateFree(
				null!,
				physical,
				1,
				2,
				1,
				3
			)
		);
		Assert.Throws<ArgumentNullException>(
			() => CursesEditingRegionSafety.IsRetainedStateFree(
				desired,
				null!,
				1,
				2,
				1,
				3
			)
		);
		ArgumentException mismatch = Assert.Throws<ArgumentException>(
			() => CursesEditingRegionSafety.IsRetainedStateFree(
				desired,
				new CursesPhysicalScreenState( 5, 3 ),
				1,
				2,
				1,
				3
			)
		);
		Assert.Equal( "physical", mismatch.ParamName );
	}

	[Theory]
	[InlineData( -1, 2, "topRow" )]
	[InlineData( 3, 3, "topRow" )]
	[InlineData( 1, 1, "bottomRowExclusive" )]
	[InlineData( 1, 4, "bottomRowExclusive" )]
	public void InvalidRowRangeIsRejected(
		int topRow,
		int bottomRowExclusive,
		string expectedParameter
	) {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();

		ArgumentOutOfRangeException exception =
			Assert.Throws<ArgumentOutOfRangeException>(
				() => CursesEditingRegionSafety.IsRetainedStateFree(
					desired,
					physical,
					topRow,
					bottomRowExclusive,
					1,
					3
				)
			);
		Assert.Equal( expectedParameter, exception.ParamName );
	}

	[Theory]
	[InlineData( -1, 3, "startColumn" )]
	[InlineData( 4, 4, "startColumn" )]
	[InlineData( 1, 1, "endColumnExclusive" )]
	[InlineData( 1, 5, "endColumnExclusive" )]
	public void InvalidColumnRangeIsRejected(
		int startColumn,
		int endColumnExclusive,
		string expectedParameter
	) {
		( CursesVirtualScreen desired, CursesPhysicalScreenState physical ) =
			CreateKnownScreens();

		ArgumentOutOfRangeException exception =
			Assert.Throws<ArgumentOutOfRangeException>(
				() => CursesEditingRegionSafety.IsRetainedStateFree(
					desired,
					physical,
					1,
					2,
					startColumn,
					endColumnExclusive
				)
			);
		Assert.Equal( expectedParameter, exception.ParamName );
	}

	private static bool IsTestRegionFree(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical
	) {
		return CursesEditingRegionSafety.IsRetainedStateFree(
			desired,
			physical,
			topRow: 1,
			bottomRowExclusive: 2,
			startColumn: 1,
			endColumnExclusive: 3
		);
	}

	private static (
		CursesVirtualScreen Desired,
		CursesPhysicalScreenState Physical
	) CreateKnownScreens() {
		CursesVirtualScreen desired = new( 4, 3 );
		CursesPhysicalScreenState physical = new( 4, 3 );
		for ( int row = 0; row < desired.Rows; row++ ) {
			for ( int column = 0; column < desired.Columns; column++ ) {
				physical.SetCell( row, column, desired[ row, column ] );
			}
		}
		return ( desired, physical );
	}

	private static CursesCellMetadata CreateMetadata() {
		return new CursesCellMetadata(
			new CursesHyperlink( "https://example.invalid/t2004" )
		);
	}
}
