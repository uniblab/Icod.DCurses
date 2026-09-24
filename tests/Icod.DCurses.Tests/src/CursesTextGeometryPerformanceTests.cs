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

/// <summary>Qualifies indexed text-geometry lookup and allocation costs.</summary>
[Collection( AllocationMeasurementCollection.Name )]
public sealed class CursesTextGeometryPerformanceTests {
	private const int AllocationSamples = 8;
	private const int LargeLineLength = 65_536;
	private const int LookupIterations = 64;
	private const int SelectionIterations = 64;
	private const long SelectionAllocationCeiling = 8L * 1024L;
	private const int WarmupIterations = 8;

	[Fact]
	public void RepeatedLocalNavigationAndPointMappingAllocateNothing() {
		CursesTextLayout layout = CreateLargeLayout();
		int middle = LargeLineLength / 2;
		int checksum = 0;
		Action operation = () => {
			CursesTextPosition position = new( middle );
			for ( int index = 0; index < LookupIterations; index++ ) {
				position = layout.GetNextPosition( position );
				position = layout.GetPreviousPosition( position );
				CursesTextVisualPosition visual = layout.GetVisualPosition(
					position
				);
				CursesTextHitTestResult hit = layout.HitTest(
					visual.Line,
					visual.Column
				);
				CursesTextVisualPosition vertical = layout.MoveVertically(
					visual,
					0,
					visual.Column
				);
				checksum ^= hit.Position.Offset ^ vertical.Column;
			}
			if ( middle != position.Offset ) {
				throw new InvalidOperationException(
					"Local caret movement changed its representative origin."
				);
			}
		};

		Assert.Equal( 0, MeasureMinimumAllocatedBytes( operation ) );
		GC.KeepAlive( checksum );
	}

	[Fact]
	public void SelectionGeometryAllocatesOnlyItsExactResultArrays() {
		CursesTextLayout layout = CreateLargeLayout();
		CursesTextSelection selection = new(
			new CursesTextPosition( 1_024 ),
			new CursesTextPosition( LargeLineLength - 1_024 )
		);
		CursesRectangle[]? last = null;
		Action operation = () => {
			for ( int index = 0; index < SelectionIterations; index++ ) {
				last = layout.GetSelectionRectangles( selection, 0, 1 );
				if ( 1 != last.Length
					|| 1_024 != last[ 0 ].Column
					|| LargeLineLength - 2_048 != last[ 0 ].Columns ) {
					throw new InvalidOperationException(
						"Representative selection geometry changed during allocation measurement."
					);
				}
			}
		};

		long allocated = MeasureMinimumAllocatedBytes( operation );
		Assert.InRange( allocated, 1, SelectionAllocationCeiling );
		GC.KeepAlive( last );
	}

	private static CursesTextLayout CreateLargeLayout() {
		return CursesTextLayout.Create(
			new string( 'a', LargeLineLength ),
			new CursesTextLayoutOptions( LargeLineLength )
		);
	}

	private static long MeasureMinimumAllocatedBytes( Action operation ) {
		for ( int index = 0; index < WarmupIterations; index++ ) {
			operation();
		}

		long minimum = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			int thread = Environment.CurrentManagedThreadId;
			long before = GC.GetAllocatedBytesForCurrentThread();
			operation();
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.Equal( thread, Environment.CurrentManagedThreadId );
			minimum = Math.Min( minimum, allocated );
		}
		return minimum;
	}
}
