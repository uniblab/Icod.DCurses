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

/// <summary>Compares prepared-cell blocks with the accepted scalar roguelike workload.</summary>
public sealed class CursesBulkCellWritePerformanceTests {
	[Fact]
	public void FullFrameAndSparseBlockMatchScalarDamageWithBoundedAllocation() {
		CursesScreen scalar = new( 80, 24 );
		CursesScreen bulk = new( 80, 24 );
		scalar.StandardWindow.WrapMode = CursesWrapMode.Clip;
		CursesCell[] frame = PrepareWorldFrame();
		scalar.VirtualScreen.MarkClean();
		bulk.VirtualScreen.MarkClean();

		int scalarCalls = RenderScalarWorldFrame( scalar.StandardWindow );
		bulk.StandardWindow.WriteCells( 0, 0, 24, 80, frame, 80 );

		Assert.Equal( 1_920, scalarCalls );
		Assert.Equal( 1_920, scalar.VirtualScreen.DirtyCellCount );
		Assert.Equal( 1_920, bulk.VirtualScreen.DirtyCellCount );
		AssertSameCells( scalar, bulk );

		long scalarAllocated = MeasureMinimumAllocation(
			() => RenderScalarWorldFrame( scalar.StandardWindow )
		);
		long bulkAllocated = MeasureMinimumAllocation(
			() => bulk.StandardWindow.WriteCells( 0, 0, 24, 80, frame, 80 )
		);
		Assert.True(
			bulkAllocated <= 64 * 1024 && bulkAllocated <= scalarAllocated / 2,
			$"Prepared frame allocated {bulkAllocated} bytes; scalar frame allocated {scalarAllocated} bytes."
		);

		scalar.VirtualScreen.MarkClean();
		bulk.VirtualScreen.MarkClean();
		bulk.StandardWindow.WriteCells( 0, 0, 24, 80, frame, 80 );
		Assert.Equal( 0, bulk.VirtualScreen.DirtyCellCount );

		CursesCell[] nearby = new CursesCell[ 9 ];
		Array.Fill( nearby, new CursesCell( "*" ) );
		for ( int row = 11; row <= 13; row++ ) {
			for ( int column = 39; column <= 41; column++ ) {
				scalar.StandardWindow.Move( row, column );
				scalar.StandardWindow.WriteCell( new CursesCell( "*" ) );
			}
		}
		bulk.StandardWindow.WriteCells( 11, 39, 3, 3, nearby, 3 );

		Assert.Equal( 9, scalar.VirtualScreen.DirtyCellCount );
		Assert.Equal( 9, bulk.VirtualScreen.DirtyCellCount );
		AssertSameCells( scalar, bulk );
	}

	private static CursesCell[] PrepareWorldFrame() {
		CursesCell[] cells = new CursesCell[ 80 * 24 ];
		for ( int row = 0; row < 24; row++ ) {
			for ( int column = 0; column < 80; column++ ) {
				cells[ row * 80 + column ] = new CursesCell(
					GlyphAt( row, column ).ToString()
				);
			}
		}
		return cells;
	}

	private static int RenderScalarWorldFrame( CursesWindow window ) {
		int calls = 0;
		for ( int row = 0; row < 24; row++ ) {
			for ( int column = 0; column < 80; column++ ) {
				window.Move( row, column );
				window.WriteCell( new CursesCell( GlyphAt( row, column ).ToString() ) );
				calls++;
			}
		}
		return calls;
	}

	private static char GlyphAt( int row, int column ) {
		int value = ( ( 1_024 + row ) * 31 ) + ( ( 1_024 + column ) * 17 );
		return 0 == value % 23 ? '#' : '.';
	}

	private static void AssertSameCells( CursesScreen scalar, CursesScreen bulk ) {
		for ( int row = 0; row < 24; row++ ) {
			for ( int column = 0; column < 80; column++ ) {
				Assert.Equal(
					scalar.VirtualScreen[ row, column ],
					bulk.VirtualScreen[ row, column ]
				);
			}
		}
	}

	private static long MeasureMinimumAllocation( Action operation ) {
		for ( int index = 0; index < 8; index++ ) {
			operation();
		}
		long minimum = long.MaxValue;
		for ( int sample = 0; sample < 8; sample++ ) {
			int thread = Environment.CurrentManagedThreadId;
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < 16; index++ ) {
				operation();
			}
			minimum = Math.Min(
				minimum,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
			Assert.Equal( thread, Environment.CurrentManagedThreadId );
		}
		return minimum;
	}
}
