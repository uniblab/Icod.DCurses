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

/// <summary>Prepared-cell bulk mutation for logical windows.</summary>
public sealed partial class CursesWindow {
	/// <summary>Writes one complete in-bounds row slice without changing the cursor.</summary>
	public void WriteCells(
		int row,
		int column,
		ReadOnlySpan<CursesCell> cells
	) {
		throw new NotImplementedException();
	}

	/// <summary>Writes one complete in-bounds rectangular prepared-cell block without changing the cursor.</summary>
	public void WriteCells(
		int row,
		int column,
		int rows,
		int columns,
		ReadOnlySpan<CursesCell> cells,
		int sourceStride
	) {
		throw new NotImplementedException();
	}
}
