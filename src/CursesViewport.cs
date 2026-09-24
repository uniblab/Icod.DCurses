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

	/// <summary>Moves each origin by the smallest amount needed to reveal a content cell.</summary>
	public CursesViewport EnsureVisible( CursesCellPosition position ) {
		if ( position.Row >= ContentRows || position.Column >= ContentColumns ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		return MoveTo(
			EnsureAxisVisible( OriginRow, Rows, position.Row, 1 ),
			EnsureAxisVisible( OriginColumn, Columns, position.Column, 1 )
		);
	}

	/// <summary>Moves each origin by the smallest amount needed to reveal a content rectangle.</summary>
	/// <remarks>When a rectangle exceeds a viewport axis, its leading edge takes precedence.</remarks>
	public CursesViewport EnsureVisible( CursesRectangle rectangle ) {
		if ( rectangle.BottomExclusive > ContentRows || rectangle.RightExclusive > ContentColumns ) {
			throw new ArgumentOutOfRangeException( nameof( rectangle ) );
		}
		return MoveTo(
			EnsureAxisVisible( OriginRow, Rows, rectangle.Row, rectangle.Rows ),
			EnsureAxisVisible( OriginColumn, Columns, rectangle.Column, rectangle.Columns )
		);
	}

	/// <summary>Returns the visible content rectangle extended by nonnegative overscan and clipped to content.</summary>
	public CursesRectangle GetVisibleContent( int overscanRows = 0, int overscanColumns = 0 ) {
		if ( overscanRows < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( overscanRows ) );
		}
		if ( overscanColumns < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( overscanColumns ) );
		}
		CursesRectangle visible = VisibleContent;
		int row = (int)Math.Max( 0L, (long)visible.Row - overscanRows );
		int column = (int)Math.Max( 0L, (long)visible.Column - overscanColumns );
		int bottom = (int)Math.Min( ContentRows, (long)visible.BottomExclusive + overscanRows );
		int right = (int)Math.Min( ContentColumns, (long)visible.RightExclusive + overscanColumns );
		return new CursesRectangle( row, column, bottom - row, right - column );
	}

	/// <summary>Translates a visible content position to viewport coordinates.</summary>
	public bool TryContentToViewport( CursesCellPosition content, out CursesCellPosition viewport ) {
		if ( !VisibleContent.Contains( content.Row, content.Column ) ) {
			viewport = default;
			return false;
		}
		viewport = new CursesCellPosition( content.Row - OriginRow, content.Column - OriginColumn );
		return true;
	}

	/// <summary>Translates a visible viewport position to content coordinates.</summary>
	public bool TryViewportToContent( CursesCellPosition viewport, out CursesCellPosition content ) {
		CursesRectangle visible = VisibleContent;
		if ( viewport.Row >= visible.Rows || viewport.Column >= visible.Columns || visible.IsEmpty ) {
			content = default;
			return false;
		}
		content = new CursesCellPosition( OriginRow + viewport.Row, OriginColumn + viewport.Column );
		return true;
	}

	private static int EnsureAxisVisible( int origin, int extent, int leading, int length ) {
		if ( extent == 0 ) {
			return origin;
		}
		if ( length > extent || leading < origin ) {
			return leading;
		}
		long trailing = (long)leading + length;
		return trailing > (long)origin + extent ? (int)( trailing - extent ) : origin;
	}

	private static int ClampOrigin( long requested, int contentExtent, int viewportExtent ) {
		return (int)Math.Clamp( requested, 0L, Math.Max( 0L, (long)contentExtent - viewportExtent ) );
	}
}
