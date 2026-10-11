/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
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

namespace Icod.DCurses.RasterAtlas.Sample;

using Icod.DCurses;

/// <summary>Computes and applies one complete sample layout.</summary>
internal static class RasterAtlasSampleLayout {
	internal static bool TryArrange(
		CursesScreen screen,
		CursesWindow standard,
		CursesWindow map,
		CursesWindow status,
		CursesPanel help,
		out int mapRows,
		out int mapColumns
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( standard );
		ArgumentNullException.ThrowIfNull( map );
		ArgumentNullException.ThrowIfNull( status );
		ArgumentNullException.ThrowIfNull( help );

		standard.Clear();
		mapRows = 0;
		mapColumns = 0;
		if ( screen.Rows < 6 || screen.Columns < 20 ) {
			return false;
		}

		mapRows = Math.Min( 30, screen.Rows - 2 );
		mapColumns = Math.Min( 80, screen.Columns );
		map.SetBounds( new CursesRectangle( 0, 0, mapRows, mapColumns ) );
		status.SetBounds( new CursesRectangle( screen.Rows - 2, 0, 2, screen.Columns ) );
		int helpRows = Math.Min( 8, screen.Rows - 2 );
		int helpColumns = Math.Min( 54, screen.Columns - 2 );
		help.SetBounds( new CursesRectangle(
			( screen.Rows - helpRows ) / 2,
			( screen.Columns - helpColumns ) / 2,
			helpRows,
			helpColumns
		) );
		return true;
	}
}
