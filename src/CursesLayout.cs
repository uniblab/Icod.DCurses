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

/// <summary>Provides pure deterministic terminal-cell layout operations.</summary>
public static class CursesLayout {
	/// <summary>Allocates rows from the top of a rectangle and returns the remainder below them.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="rows">The non-negative requested row count.</param>
	/// <param name="first">The allocated top rectangle.</param>
	/// <param name="remaining">The remaining rectangle below the allocation.</param>
	public static void SplitTop(
		CursesRectangle bounds,
		int rows,
		out CursesRectangle first,
		out CursesRectangle remaining
	) {
		if ( 0 > rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		int allocatedRows = Math.Min(
			bounds.Rows,
			rows
		);
		first = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			allocatedRows,
			bounds.Columns
		);
		remaining = new CursesRectangle(
			bounds.Row + allocatedRows,
			bounds.Column,
			bounds.Rows - allocatedRows,
			bounds.Columns
		);
	}

	/// <summary>Allocates rows from the bottom of a rectangle and returns the remainder above them.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="rows">The non-negative requested row count.</param>
	/// <param name="remaining">The remaining rectangle above the allocation.</param>
	/// <param name="last">The allocated bottom rectangle.</param>
	public static void SplitBottom(
		CursesRectangle bounds,
		int rows,
		out CursesRectangle remaining,
		out CursesRectangle last
	) {
		if ( 0 > rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		int allocatedRows = Math.Min(
			bounds.Rows,
			rows
		);
		remaining = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows - allocatedRows,
			bounds.Columns
		);
		last = new CursesRectangle(
			bounds.BottomExclusive - allocatedRows,
			bounds.Column,
			allocatedRows,
			bounds.Columns
		);
	}

	/// <summary>Allocates columns from the left of a rectangle and returns the remainder to their right.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="columns">The non-negative requested column count.</param>
	/// <param name="first">The allocated left rectangle.</param>
	/// <param name="remaining">The remaining rectangle to the right of the allocation.</param>
	public static void SplitLeft(
		CursesRectangle bounds,
		int columns,
		out CursesRectangle first,
		out CursesRectangle remaining
	) {
		if ( 0 > columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		int allocatedColumns = Math.Min(
			bounds.Columns,
			columns
		);
		first = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows,
			allocatedColumns
		);
		remaining = new CursesRectangle(
			bounds.Row,
			bounds.Column + allocatedColumns,
			bounds.Rows,
			bounds.Columns - allocatedColumns
		);
	}

	/// <summary>Allocates columns from the right of a rectangle and returns the remainder to their left.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="columns">The non-negative requested column count.</param>
	/// <param name="remaining">The remaining rectangle to the left of the allocation.</param>
	/// <param name="last">The allocated right rectangle.</param>
	public static void SplitRight(
		CursesRectangle bounds,
		int columns,
		out CursesRectangle remaining,
		out CursesRectangle last
	) {
		if ( 0 > columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		int allocatedColumns = Math.Min(
			bounds.Columns,
			columns
		);
		remaining = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows,
			bounds.Columns - allocatedColumns
		);
		last = new CursesRectangle(
			bounds.Row,
			bounds.RightExclusive - allocatedColumns,
			bounds.Rows,
			allocatedColumns
		);
	}

	/// <summary>Clips one rectangle to a containing rectangle.</summary>
	/// <param name="rectangle">The rectangle to clip.</param>
	/// <param name="container">The clipping container.</param>
	/// <returns>The deterministic rectangle intersection.</returns>
	public static CursesRectangle Clip(
		CursesRectangle rectangle,
		CursesRectangle container
	) {
		return rectangle.Intersect( container );
	}
}
