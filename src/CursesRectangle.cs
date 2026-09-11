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

/// <summary>Represents one immutable zero-based terminal-cell rectangle.</summary>
public readonly record struct CursesRectangle {
	/// <summary>Initializes one immutable terminal-cell rectangle.</summary>
	/// <param name="row">The non-negative zero-based row.</param>
	/// <param name="column">The non-negative zero-based column.</param>
	/// <param name="rows">The non-negative height.</param>
	/// <param name="columns">The non-negative width.</param>
	public CursesRectangle(
		int row,
		int column,
		int rows,
		int columns
	) {
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 > columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( row > int.MaxValue - rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( column > int.MaxValue - columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		Row = row;
		Column = column;
		Rows = rows;
		Columns = columns;
	}

	/// <summary>Gets the zero-based top row.</summary>
	public int Row {
		get;
	}

	/// <summary>Gets the zero-based left column.</summary>
	public int Column {
		get;
	}

	/// <summary>Gets the rectangle height.</summary>
	public int Rows {
		get;
	}

	/// <summary>Gets the rectangle width.</summary>
	public int Columns {
		get;
	}

	/// <summary>Gets the first row after this rectangle.</summary>
	public int BottomExclusive => Row + Rows;

	/// <summary>Gets the first column after this rectangle.</summary>
	public int RightExclusive => Column + Columns;

	/// <summary>Gets whether this rectangle contains no terminal cells.</summary>
	public bool IsEmpty => 0 == Rows || 0 == Columns;

	/// <summary>Returns whether one coordinate lies inside this rectangle.</summary>
	/// <param name="row">The zero-based row to test.</param>
	/// <param name="column">The zero-based column to test.</param>
	public bool Contains(
		int row,
		int column
	) {
		if ( IsEmpty ) {
			return false;
		}

		return row >= Row
			&& row < BottomExclusive
			&& column >= Column
			&& column < RightExclusive;
	}

	/// <summary>Returns whether another rectangle lies entirely within this rectangle.</summary>
	/// <param name="rectangle">The rectangle to test.</param>
	public bool Contains( CursesRectangle rectangle ) {
		return rectangle.Row >= Row
			&& rectangle.Column >= Column
			&& rectangle.BottomExclusive <= BottomExclusive
			&& rectangle.RightExclusive <= RightExclusive;
	}

	/// <summary>Returns the intersection with another rectangle.</summary>
	/// <param name="rectangle">The rectangle to intersect.</param>
	public CursesRectangle Intersect( CursesRectangle rectangle ) {
		int row = Math.Max(
			Row,
			rectangle.Row
		);
		int column = Math.Max(
			Column,
			rectangle.Column
		);
		int bottom = Math.Min(
			BottomExclusive,
			rectangle.BottomExclusive
		);
		int right = Math.Min(
			RightExclusive,
			rectangle.RightExclusive
		);

		return new CursesRectangle(
			row,
			column,
			Math.Max(
				0,
				bottom - row
			),
			Math.Max(
				0,
				right - column
			)
		);
	}

	/// <summary>Returns this rectangle after applying terminal-cell insets.</summary>
	/// <param name="insets">The insets to apply.</param>
	public CursesRectangle Inset( CursesInsets insets ) {
		int rowOffset = Math.Min(
			Rows,
			insets.Top
		);
		int columnOffset = Math.Min(
			Columns,
			insets.Left
		);

		return new CursesRectangle(
			Row + rowOffset,
			Column + columnOffset,
			Math.Max(
				0,
				Rows - insets.Vertical
			),
			Math.Max(
				0,
				Columns - insets.Horizontal
			)
		);
	}
}
