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

	/// <summary>Returns this viewport for a changed content extent, clamping the existing origin.</summary>
	public CursesViewport WithContentExtent( int rows, int columns ) {
		if ( rows < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( columns < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		return new CursesViewport( rows, columns, Rows, Columns, OriginRow, OriginColumn );
	}

	/// <summary>Returns this viewport for a changed visible extent, clamping the existing origin.</summary>
	public CursesViewport WithViewportExtent( int rows, int columns ) {
		return new CursesViewport( ContentRows, ContentColumns, rows, columns, OriginRow, OriginColumn );
	}

	/// <summary>Moves the origin to nonnegative content coordinates, clamped to the valid range.</summary>
	public CursesViewport MoveTo( int row, int column ) {
		if ( row < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		return new CursesViewport( ContentRows, ContentColumns, Rows, Columns, row, column );
	}

	/// <summary>Pans each axis by a signed number of cells without overflowing.</summary>
	public CursesViewport PanBy( int rowDelta, int columnDelta ) {
		return MoveTo(
			ClampOrigin( (long)OriginRow + rowDelta, ContentRows, Rows ),
			ClampOrigin( (long)OriginColumn + columnDelta, ContentColumns, Columns )
		);
	}

	/// <summary>Pans each axis by a signed number of viewport extents without overflowing.</summary>
	public CursesViewport PageBy( int rowPages, int columnPages ) {
		return MoveTo(
			ClampOrigin( OriginRow + (long)rowPages * Rows, ContentRows, Rows ),
			ClampOrigin( OriginColumn + (long)columnPages * Columns, ContentColumns, Columns )
		);
	}

	/// <summary>Moves to the beginning of both content axes.</summary>
	public CursesViewport MoveToStart() {
		return MoveTo( 0, 0 );
	}

	/// <summary>Moves to the last valid origin on both content axes.</summary>
	public CursesViewport MoveToEnd() {
		return MoveTo( Math.Max( 0, ContentRows - Rows ), Math.Max( 0, ContentColumns - Columns ) );
	}

	private static int ClampOrigin( long requested, int contentExtent, int viewportExtent ) {
		return (int)Math.Clamp( requested, 0L, Math.Max( 0L, (long)contentExtent - viewportExtent ) );
	}
}
