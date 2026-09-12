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

	/// <summary>Splits a rectangle's rows according to two positive proportional weights.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="firstWeight">The positive weight for the first region.</param>
	/// <param name="secondWeight">The positive weight for the second region.</param>
	/// <param name="first">The first region, beginning at the source top edge.</param>
	/// <param name="second">The second region, receiving any integer remainder.</param>
	public static void SplitRowsProportional(
		CursesRectangle bounds,
		int firstWeight,
		int secondWeight,
		out CursesRectangle first,
		out CursesRectangle second
	) {
		if ( 0 >= firstWeight ) {
			throw new ArgumentOutOfRangeException( nameof( firstWeight ) );
		}
		if ( 0 >= secondWeight ) {
			throw new ArgumentOutOfRangeException( nameof( secondWeight ) );
		}

		long totalWeight = (long)firstWeight + secondWeight;
		int firstRows = (int)( (long)bounds.Rows * firstWeight / totalWeight );
		first = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			firstRows,
			bounds.Columns
		);
		second = new CursesRectangle(
			bounds.Row + firstRows,
			bounds.Column,
			bounds.Rows - firstRows,
			bounds.Columns
		);
	}

	/// <summary>Splits a rectangle's columns according to two positive proportional weights.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="firstWeight">The positive weight for the first region.</param>
	/// <param name="secondWeight">The positive weight for the second region.</param>
	/// <param name="first">The first region, beginning at the source left edge.</param>
	/// <param name="second">The second region, receiving any integer remainder.</param>
	public static void SplitColumnsProportional(
		CursesRectangle bounds,
		int firstWeight,
		int secondWeight,
		out CursesRectangle first,
		out CursesRectangle second
	) {
		if ( 0 >= firstWeight ) {
			throw new ArgumentOutOfRangeException( nameof( firstWeight ) );
		}
		if ( 0 >= secondWeight ) {
			throw new ArgumentOutOfRangeException( nameof( secondWeight ) );
		}

		long totalWeight = (long)firstWeight + secondWeight;
		int firstColumns = (int)( (long)bounds.Columns * firstWeight / totalWeight );
		first = new CursesRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows,
			firstColumns
		);
		second = new CursesRectangle(
			bounds.Row,
			bounds.Column + firstColumns,
			bounds.Rows,
			bounds.Columns - firstColumns
		);
	}

	/// <summary>Allocates a fixed extent from one edge of a rectangle.</summary>
	/// <param name="bounds">The source rectangle.</param>
	/// <param name="edge">The edge from which to allocate.</param>
	/// <param name="size">The non-negative requested extent.</param>
	/// <param name="docked">The allocated edge rectangle.</param>
	/// <param name="remaining">The rectangle remaining after allocation.</param>
	public static void Dock(
		CursesRectangle bounds,
		CursesDockEdge edge,
		int size,
		out CursesRectangle docked,
		out CursesRectangle remaining
	) {
		if ( !Enum.IsDefined( edge ) ) {
			throw new ArgumentOutOfRangeException( nameof( edge ) );
		}
		if ( 0 > size ) {
			throw new ArgumentOutOfRangeException( nameof( size ) );
		}

		switch ( edge ) {
			case CursesDockEdge.Top:
				SplitTop(
					bounds,
					size,
					out docked,
					out remaining
				);
				break;
			case CursesDockEdge.Right:
				SplitRight(
					bounds,
					size,
					out remaining,
					out docked
				);
				break;
			case CursesDockEdge.Bottom:
				SplitBottom(
					bounds,
					size,
					out remaining,
					out docked
				);
				break;
			case CursesDockEdge.Left:
				SplitLeft(
					bounds,
					size,
					out docked,
					out remaining
				);
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( edge ) );
		}
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
