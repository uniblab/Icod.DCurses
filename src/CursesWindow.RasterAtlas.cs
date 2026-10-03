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

/// <summary>Retained raster-atlas projection helpers.</summary>
public sealed partial class CursesWindow {
	/// <summary>Writes a rectangular atlas-cell region into this logical window.</summary>
	public void WriteRasterAtlas(
		int row,
		int column,
		CursesRasterAtlas atlas,
		CursesRectangle sourceRectangle
	) {
		ArgumentNullException.ThrowIfNull( atlas );
		throw new NotSupportedException( "Raster-atlas projection is not yet initialized." );
	}
}
