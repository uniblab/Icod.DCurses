/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

/// <summary>Describes an exact terminal-cell-aligned raster-atlas geometry.</summary>
public readonly record struct CursesRasterAtlasGeometry {
	/// <summary>Initializes one bounded raster-atlas geometry.</summary>
	/// <param name="rows">Atlas height in terminal cells, from 1 through 256.</param>
	/// <param name="columns">Atlas width in terminal cells, from 1 through 256.</param>
	/// <param name="tilePixelWidth">Positive width of one terminal cell in pixels.</param>
	/// <param name="tilePixelHeight">Positive height of one terminal cell in pixels.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// An axis is outside its bound, a tile pixel dimension is not positive, or a
	/// derived atlas pixel dimension does not fit in <see cref="int"/>.
	/// </exception>
	public CursesRasterAtlasGeometry(
		int rows,
		int columns,
		int tilePixelWidth,
		int tilePixelHeight
	) {
		ValidateCellAxis( rows, nameof( rows ) );
		ValidateCellAxis( columns, nameof( columns ) );
		if ( 0 >= tilePixelWidth ) {
			throw new ArgumentOutOfRangeException( nameof( tilePixelWidth ) );
		}
		if ( 0 >= tilePixelHeight ) {
			throw new ArgumentOutOfRangeException( nameof( tilePixelHeight ) );
		}

		int pixelWidth;
		int pixelHeight;
		try {
			pixelWidth = checked( columns * tilePixelWidth );
			pixelHeight = checked( rows * tilePixelHeight );
		} catch ( OverflowException ) {
			throw new ArgumentOutOfRangeException(
				nameof( tilePixelWidth ),
				"The atlas pixel dimensions must fit in Int32."
			);
		}

		Rows = rows;
		Columns = columns;
		TilePixelWidth = tilePixelWidth;
		TilePixelHeight = tilePixelHeight;
		PixelWidth = pixelWidth;
		PixelHeight = pixelHeight;
	}

	/// <summary>Gets the atlas height in terminal cells.</summary>
	public int Rows { get; }

	/// <summary>Gets the atlas width in terminal cells.</summary>
	public int Columns { get; }

	/// <summary>Gets one tile's width in pixels.</summary>
	public int TilePixelWidth { get; }

	/// <summary>Gets one tile's height in pixels.</summary>
	public int TilePixelHeight { get; }

	/// <summary>Gets the complete atlas width in pixels.</summary>
	public int PixelWidth { get; }

	/// <summary>Gets the complete atlas height in pixels.</summary>
	public int PixelHeight { get; }

	internal static void ValidateCellAxis(
		int value,
		string parameterName
	) {
		if ( value < 1 || 256 < value ) {
			throw new ArgumentOutOfRangeException(
				parameterName,
				value,
				"A raster-atlas cell axis must be between 1 and 256."
			);
		}
	}
}
