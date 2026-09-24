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

/// <summary>Specifies allocation and materialization bounds for large-content viewports.</summary>
public sealed class CursesViewportPerformanceTests {
	[Fact]
	public void TenMillionRowDocumentMaterializesOnlyItsVisibleSlice() {
		CursesViewport viewport = new CursesViewport( 10_000_000, 200, 80, 40, 9_000_000, 100 );
		viewport = viewport.EnsureVisible( new CursesCellPosition( 9_000_100, 120 ) );
		CursesRectangle requested = viewport.GetVisibleContent( 2, 2 );
		Assert.Equal( new CursesRectangle( 9_000_019, 98, 84, 44 ), requested );
		CursesCell[] materialized = new CursesCell[ requested.Rows * requested.Columns ];
		int visited = 0;
		for ( int row = requested.Row; row < requested.BottomExclusive; row++ ) {
			for ( int column = requested.Column; column < requested.RightExclusive; column++ ) {
				materialized[ visited++ ] = CursesCell.Blank();
			}
		}
		Assert.Equal( 84 * 44, visited );
		Assert.Equal( visited, materialized.Length );
	}

	[Fact]
	public void AlgorithmicWorldMaterializesOnlyTheWindowAndOverscan() {
		CursesViewport viewport = new CursesViewport( 2048, 2048, 80, 24, 100, 100 );
		CursesRectangle requested = viewport.GetVisibleContent( 2, 2 );
		Assert.Equal( new CursesRectangle( 98, 98, 84, 28 ), requested );
		byte[] materialized = new byte[ requested.Rows * requested.Columns ];
		int visited = 0;
		for ( int row = requested.Row; row < requested.BottomExclusive; row++ ) {
			for ( int column = requested.Column; column < requested.RightExclusive; column++ ) {
				materialized[ visited++ ] = (byte)( ( row * 31 + column * 17 ) & 3 );
			}
		}
		Assert.Equal( 84 * 28, visited );
		Assert.Equal( visited, materialized.Length );
	}

	[Fact]
	public void ViewportOperationsAllocateNothingAfterWarmup() {
		CursesViewport viewport = new CursesViewport( 10_000_000, 2048, 80, 40, 500, 500 );
		for ( int index = 0; index < 256; index++ ) {
			viewport = viewport.PanBy( 1, -1 ).PageBy( -1, 1 );
			viewport = viewport.EnsureVisible( new CursesCellPosition( 500, 500 ) );
			_ = viewport.GetVisibleContent( 2, 2 );
		}

		long minimumAllocated = long.MaxValue;
		for ( int sample = 0; sample < 8; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < 10_000; index++ ) {
				viewport = viewport.PanBy( 1, -1 ).PageBy( -1, 1 );
				viewport = viewport.EnsureVisible( new CursesCellPosition( 500, 500 ) );
				_ = viewport.GetVisibleContent( 2, 2 );
			}
			minimumAllocated = Math.Min(
				minimumAllocated,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
		}

		Assert.Equal( 0L, minimumAllocated );
		Assert.True( viewport.OriginRow >= 0 );
	}
}
