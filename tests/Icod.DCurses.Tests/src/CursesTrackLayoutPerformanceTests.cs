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

/// <summary>Bounds track scratch allocation independently of coordinate extent.</summary>
public sealed class CursesTrackLayoutPerformanceTests {
	[Fact]
	public void MaximumTrackCountUsesOneResultArrayAndBoundedScratch() {
		CursesTrack[] tracks = new CursesTrack[ 4096 ];
		Array.Fill( tracks, CursesTrack.Weighted( maximum: 1 ) );
		CursesRectangle bounds = new CursesRectangle( 0, 0, 1, int.MaxValue );
		_ = CursesLayout.ArrangeColumns( bounds, tracks );
		long minimumAllocated = long.MaxValue;
		CursesRectangle[]? latest = null;
		for ( int sample = 0; sample < 8; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			latest = CursesLayout.ArrangeColumns( bounds, tracks );
			minimumAllocated = Math.Min(
				minimumAllocated,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
		}
		Assert.NotNull( latest );
		Assert.Equal( tracks.Length, latest!.Length );
		Assert.All( latest, static rectangle => Assert.Equal( 1, rectangle.Columns ) );
		Assert.InRange( minimumAllocated, 65_536L, 131_072L );
		Assert.NotSame( latest, CursesLayout.ArrangeColumns( bounds, tracks ) );
	}

	[Fact]
	public void LargeExtentsDoNotChangeTheSmallTrackAllocationBound() {
		CursesTrack[] tracks = [ CursesTrack.Fixed( 2 ), CursesTrack.Weighted( maximum: 3 ) ];
		CursesRectangle[] result = CursesLayout.ArrangeColumns(
			new CursesRectangle( 0, 0, 1, int.MaxValue ), tracks
		);
		long minimumAllocated = long.MaxValue;
		for ( int sample = 0; sample < 8; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			result = CursesLayout.ArrangeColumns(
				new CursesRectangle( 0, 0, 1, int.MaxValue ), tracks
			);
			minimumAllocated = Math.Min(
				minimumAllocated,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
		}
		Assert.Equal( 2, result.Length );
		Assert.Equal( 3, result[ 1 ].Columns );
		Assert.InRange( minimumAllocated, 32L, 4_096L );
	}

	[Fact]
	public void ExistingDockAndSplitOperationsRemainAllocationFree() {
		CursesRectangle bounds = new CursesRectangle( 0, 0, 80, 40 );
		for ( int index = 0; index < 256; index++ ) {
			CursesLayout.Dock( bounds, CursesDockEdge.Top, 1, out _, out _ );
			CursesLayout.SplitLeft( bounds, 2, out _, out _ );
		}
		long minimumAllocated = long.MaxValue;
		CursesRectangle result = default;
		for ( int sample = 0; sample < 8; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < 1000; index++ ) {
				CursesLayout.Dock( bounds, CursesDockEdge.Top, 1, out _, out result );
				CursesLayout.SplitLeft( result, 2, out _, out result );
			}
			minimumAllocated = Math.Min(
				minimumAllocated,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
		}
		Assert.Equal( 0L, minimumAllocated );
		Assert.Equal( 38, result.Columns );
	}
}
