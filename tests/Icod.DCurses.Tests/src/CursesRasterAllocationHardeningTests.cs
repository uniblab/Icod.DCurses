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

/// <summary>Hardens sparse retained-raster allocation and churn behavior.</summary>
public sealed class CursesRasterAllocationHardeningTests {
	private const int AllocationIterations = 10000;
	private const long AllocationMeasurementNoiseAllowance = 1024;
	private const int AllocationSamples = 8;
	private const int WarmupIterations = 4096;

	[Fact]
	public void RepeatedSparseRowChurnReleasesAllRasterStorage() {
		CursesVirtualScreen screen = new(
			64,
			128
		);
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();

		for ( int cycle = 0; cycle < 64; cycle++ ) {
			for ( int index = 0; index < 16; index++ ) {
				int row = ( cycle + ( index * 7 ) ) % screen.Rows;
				int column = ( cycle * 3 + index * 5 ) % screen.Columns;
				screen.SetRasterCell(
					row,
					column,
					token
				);
			}

			Assert.True( screen.RasterStorageAllocated );
			Assert.InRange(
				screen.RasterAllocatedRowCount,
				1,
				16
			);

			for ( int row = 0; row < screen.Rows; row++ ) {
				for ( int column = 0; column < screen.Columns; column++ ) {
					if ( screen.GetRasterCell( row, column ).HasValue ) {
						screen.SetRasterCell(
							row,
							column,
							null
						);
					}
				}
			}

			Assert.Equal( 0, screen.RasterCellCount );
			Assert.Equal( 0, screen.RasterAllocatedRowCount );
			Assert.False( screen.RasterStorageAllocated );
		}
	}

	[Fact]
	public void LargeSparseSurfaceMaterializesOnlyTouchedRowsAndReturnsToZero() {
		CursesVirtualScreen screen = new(
			256,
			2048
		);
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		int[] rows = [ 0, 197, 394, 591, 788, 985, 1182, 1379, 1576, 2047 ];

		for ( int index = 0; index < rows.Length; index++ ) {
			screen.SetRasterCell(
				rows[ index ],
				index,
				token
			);
		}

		Assert.Equal( rows.Length, screen.RasterCellCount );
		Assert.Equal( rows.Length, screen.RasterAllocatedRowCount );

		for ( int index = 0; index < rows.Length; index++ ) {
			screen.SetRasterCell(
				rows[ index ],
				index,
				null
			);
		}

		Assert.Equal( 0, screen.RasterCellCount );
		Assert.Equal( 0, screen.RasterAllocatedRowCount );
		Assert.False( screen.RasterStorageAllocated );
	}

	[Fact]
	public void OrdinaryTextMutationDoesNotMaterializeRasterStorageOrAllocatePerWrite() {
		CursesVirtualScreen screen = new(
			80,
			24
		);
		CursesCell first = new( "A" );
		CursesCell second = new( "B" );
		bool toggle = false;
		Action operation = () => {
			screen.SetCell(
				7,
				11,
				toggle ? first : second
			);
			toggle = !toggle;
		};

		long minimumAllocated = MeasureMinimumAllocatedBytes( operation );

		Assert.InRange(
			minimumAllocated,
			0,
			AllocationMeasurementNoiseAllowance
		);
		Assert.Equal( 0, screen.RasterCellCount );
		Assert.Equal( 0, screen.RasterAllocatedRowCount );
		Assert.False( screen.RasterStorageAllocated );
	}

	[Fact]
	public void MaterializedSparseRasterReplacementStaysWithinOneSmallWrapperPerMutation() {
		CursesVirtualScreen screen = new(
			80,
			24
		);
		CursesRasterCell first = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		CursesRasterCell second = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.SetRasterCell(
			7,
			11,
			first
		);
		bool toggle = false;
		Action operation = () => {
			screen.SetRasterCell(
				7,
				11,
				toggle ? first : second
			);
			toggle = !toggle;
		};

		long minimumAllocated = MeasureMinimumAllocatedBytes( operation );

		Assert.InRange(
			minimumAllocated,
			0,
			( 128L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
		Assert.Equal( 1, screen.RasterCellCount );
		Assert.Equal( 1, screen.RasterAllocatedRowCount );
		Assert.True( screen.RasterStorageAllocated );
	}

	private static long MeasureMinimumAllocatedBytes( Action operation ) {
		ArgumentNullException.ThrowIfNull( operation );

		for ( int index = 0; index < WarmupIterations; index++ ) {
			operation();
		}

		long minimumAllocated = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < AllocationIterations; index++ ) {
				operation();
			}
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			minimumAllocated = Math.Min(
				minimumAllocated,
				allocated
			);
		}
		return minimumAllocated;
	}
}
