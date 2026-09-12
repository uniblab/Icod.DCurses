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

public sealed partial class CursesWindow {
	/// <summary>Gets this window's rectangle relative to its immediate containing surface.</summary>
	public CursesRectangle Bounds => new(
		OriginRow,
		OriginColumn,
		Rows,
		Columns
	);

	/// <summary>Atomically assigns a non-standard window rectangle relative to its containing surface.</summary>
	/// <param name="bounds">The positive-sized final rectangle.</param>
	public void SetBounds( CursesRectangle bounds ) {
		if ( isStandardWindow ) {
			throw new InvalidOperationException(
				"The standard window is anchored to its owning CursesScreen."
			);
		}

		int containingRows = parent?.Rows ?? screen.Rows;
		int containingColumns = parent?.Columns ?? screen.Columns;
		CursesScreen.ValidateWindowRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows,
			bounds.Columns,
			containingRows,
			containingColumns
		);

		originRow = bounds.Row;
		originColumn = bounds.Column;
		rows = bounds.Rows;
		columns = bounds.Columns;
		cursorRow = Math.Min(
			cursorRow,
			rows - 1
		);
		cursorColumn = Math.Min(
			cursorColumn,
			columns - 1
		);
	}
}
