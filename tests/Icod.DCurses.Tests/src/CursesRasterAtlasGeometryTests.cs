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

public sealed class CursesRasterAtlasGeometryTests {
	[Fact]
	public void GeometryRetainsExactCellAndPixelDimensions() {
		CursesRasterAtlasGeometry geometry = new( 11, 13, 9, 17 );

		Assert.Equal( 11, geometry.Rows );
		Assert.Equal( 13, geometry.Columns );
		Assert.Equal( 9, geometry.TilePixelWidth );
		Assert.Equal( 17, geometry.TilePixelHeight );
		Assert.Equal( 117, geometry.PixelWidth );
		Assert.Equal( 187, geometry.PixelHeight );
	}

	[Fact]
	public void MaximumCellAxesRemainAccepted() {
		CursesRasterAtlasGeometry geometry = new( 256, 256, 1, 1 );
		Assert.Equal( 256, geometry.PixelWidth );
		Assert.Equal( 256, geometry.PixelHeight );
	}
}
