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

using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Bounds atlas validation allocation by submitted changes, not world extent.</summary>
public sealed class CursesRasterAtlasAllocationTests {
	[Fact]
	public void ValidationAllocationDependsOnSubmittedUpdatesNotAtlasExtent() {
		TerminalRasterImage pixel = TerminalRasterImage.CreateRgb24( 1, 1, new byte[ 3 ] );
		CursesRasterAtlasTileUpdate[] updates = Enumerable.Range( 0, 64 ).Select(
			index => new CursesRasterAtlasTileUpdate( index / 8, index % 8, pixel )
		).ToArray();
		_ = MeasureMinimum( updates, 8, 8 );

		long smallAtlas = MeasureMinimum( updates, 8, 8 );
		long maximumAtlas = MeasureMinimum( updates, 256, 256 );

		Assert.InRange( smallAtlas, 1L, 32_768L );
		Assert.InRange( maximumAtlas, 1L, 32_768L );
		Assert.InRange( Math.Abs( maximumAtlas - smallAtlas ), 0L, 1_024L );
	}

	[Fact]
	public void EmptyValidationUsesOnlyTheDetachedEmptyResult() {
		CursesRasterAtlasTileUpdate[] updates = [];
		long allocated = MeasureMinimum( updates, 256, 256 );
		Assert.InRange( allocated, 0L, 1_024L );
	}

	private static long MeasureMinimum(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
		int rows,
		int columns
	) {
		long minimum = long.MaxValue;
		for ( int sample = 0; sample < 8; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			CursesRasterAtlasTileUpdate[] ordered = CursesRasterAtlas.ValidateAndOrderUpdates(
				updates,
				rows,
				columns,
				1,
				1
			);
			GC.KeepAlive( ordered );
			minimum = Math.Min(
				minimum,
				GC.GetAllocatedBytesForCurrentThread() - before
			);
		}
		return minimum;
	}
}
