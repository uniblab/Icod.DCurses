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

/// <summary>Immutable geometry for a visible slice of caller-owned content.</summary>
public readonly record struct CursesViewport {
	/// <summary>Initializes a viewport with its origin clamped to the content extent.</summary>
	/// <param name="contentRows">The nonnegative content height.</param>
	/// <param name="contentColumns">The nonnegative content width.</param>
	/// <param name="rows">The nonnegative viewport height.</param>
	/// <param name="columns">The nonnegative viewport width.</param>
	/// <param name="originRow">The requested nonnegative first visible row.</param>
	/// <param name="originColumn">The requested nonnegative first visible column.</param>
	public CursesViewport(
		int contentRows,
		int contentColumns,
		int rows,
		int columns,
		int originRow = 0,
		int originColumn = 0
	) {
		if ( 0 > contentRows ) {
			throw new ArgumentOutOfRangeException( nameof( contentRows ) );
		}
		if ( 0 > contentColumns ) {
			throw new ArgumentOutOfRangeException( nameof( contentColumns ) );
		}
		if ( 0 > rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 > columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( 0 > originRow ) {
			throw new ArgumentOutOfRangeException( nameof( originRow ) );
		}
		if ( 0 > originColumn ) {
			throw new ArgumentOutOfRangeException( nameof( originColumn ) );
		}

		ContentRows = contentRows;
		ContentColumns = contentColumns;
		Rows = rows;
		Columns = columns;
		OriginRow = Math.Min( originRow, Math.Max( 0, contentRows - rows ) );
		OriginColumn = Math.Min( originColumn, Math.Max( 0, contentColumns - columns ) );
	}

	/// <summary>Gets the height of caller-owned content.</summary>
	public int ContentRows { get; }

	/// <summary>Gets the width of caller-owned content.</summary>
	public int ContentColumns { get; }

	/// <summary>Gets the viewport height.</summary>
	public int Rows { get; }

	/// <summary>Gets the viewport width.</summary>
	public int Columns { get; }

	/// <summary>Gets the clamped first visible content row.</summary>
	public int OriginRow { get; }

	/// <summary>Gets the clamped first visible content column.</summary>
	public int OriginColumn { get; }

	/// <summary>Gets the content rectangle visible within the viewport.</summary>
	public CursesRectangle VisibleContent => new(
		OriginRow,
		OriginColumn,
		Math.Min( Rows, ContentRows - OriginRow ),
		Math.Min( Columns, ContentColumns - OriginColumn )
	);
}
