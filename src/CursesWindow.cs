namespace Icod.DCurses;

using System.Globalization;
using System.Text;

/// <summary>Controls horizontal boundary behavior when writing through a <see cref="CursesWindow"/>.</summary>
public enum CursesWrapMode {
	/// <summary>Stop a text write when it reaches the right edge of the window.</summary>
	Clip,

	/// <summary>Continue a text write at column zero of the following row.</summary>
	Wrap
}

/// <summary>
/// Represents one rectangular logical view projected into a shared <see cref="CursesVirtualScreen"/>.
/// </summary>
/// <remarks>
/// Windows are views rather than independent compositing layers. Writes made through overlapping windows
/// therefore update the same logical cells. Coordinates supplied to window methods are zero-based and local
/// to that window.
/// </remarks>
public sealed class CursesWindow {
	private const int TabWidth = 8;

	private readonly CursesScreen screen;
	private readonly CursesWindow? parent;
	private readonly bool isStandardWindow;
	private int originRow;
	private int originColumn;
	private int rows;
	private int columns;
	private int cursorRow;
	private int cursorColumn;
	private CursesCell backgroundCell;
	private CursesWrapMode wrapMode;

	private CursesWindow(
		CursesScreen screen,
		CursesWindow? parent,
		int row,
		int column,
		int rows,
		int columns,
		bool isStandardWindow ) {
		ArgumentNullException.ThrowIfNull( screen );

		this.screen = screen;
		this.parent = parent;
		originRow = row;
		originColumn = column;
		this.rows = rows;
		this.columns = columns;
		this.isStandardWindow = isStandardWindow;
		CurrentStyle = CursesStyle.Default;
		backgroundCell = CursesCell.Blank();
		WrapMode = CursesWrapMode.Wrap;
	}

	/// <summary>Gets whether this is the standard window associated with its logical screen.</summary>
	public bool IsStandardWindow => isStandardWindow;

	/// <summary>Gets the window origin row relative to its parent, or zero for the standard window.</summary>
	public int OriginRow => isStandardWindow
		? 0
		: originRow
	;

	/// <summary>Gets the window origin column relative to its parent, or zero for the standard window.</summary>
	public int OriginColumn => isStandardWindow
		? 0
		: originColumn
	;

	/// <summary>Gets the logical height of this window.</summary>
	public int Rows => isStandardWindow
		? screen.Rows
		: rows
	;

	/// <summary>Gets the logical width of this window.</summary>
	public int Columns => isStandardWindow
		? screen.Columns
		: columns
	;

	/// <summary>Gets the current zero-based cursor row.</summary>
	public int CursorRow => cursorRow;

	/// <summary>Gets the current zero-based cursor column.</summary>
	public int CursorColumn => cursorColumn;

	/// <summary>Gets or sets the style applied by unstyled text writes.</summary>
	public CursesStyle CurrentStyle {
		get;
		set;
	}

	/// <summary>Gets or sets the cell used by erase, clear, and scrolling operations.</summary>
	public CursesCell BackgroundCell {
		get => backgroundCell;
		set {
			if ( value.IsContinuation ) {
				throw new ArgumentException(
					"A continuation cell cannot be used as a window background.",
					nameof( value )
				);
			}

			backgroundCell = value;
		}
	}

	/// <summary>Gets or sets horizontal wrapping behavior. The default is <see cref="CursesWrapMode.Wrap"/>.</summary>
	public CursesWrapMode WrapMode {
		get => wrapMode;
		set {
			if ( !Enum.IsDefined( value ) ) {
				throw new ArgumentOutOfRangeException( nameof( value ) );
			}

			wrapMode = value;
		}
	}

	/// <summary>Gets or sets whether advancing below the bottom row scrolls this window upward.</summary>
	public bool ScrollingEnabled {
		get;
		set;
	}

	/// <summary>Creates a subwindow whose coordinates are relative to this window.</summary>
	public CursesWindow CreateSubwindow(
		int row,
		int column,
		int rows,
		int columns ) {
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			Rows,
			Columns
		);

		return new CursesWindow(
			screen,
			this,
			row,
			column,
			rows,
			columns,
			isStandardWindow: false
		);
	}

	/// <summary>Moves the logical cursor to one local coordinate.</summary>
	public void Move(
		int row,
		int column ) {
		ValidateCoordinate(
			row,
			column
		);
		cursorRow = row;
		cursorColumn = column;
	}

	/// <summary>Changes the dimensions of a non-standard window without changing its origin.</summary>
	public void Resize(
		int rows,
		int columns ) {
		if ( isStandardWindow ) {
			throw new InvalidOperationException(
				"Resize the owning CursesScreen to resize the standard window."
			);
		}

		int containingRows = parent?.Rows ?? screen.Rows;
		int containingColumns = parent?.Columns ?? screen.Columns;
		CursesScreen.ValidateWindowRectangle(
			originRow,
			originColumn,
			rows,
			columns,
			containingRows,
			containingColumns
		);

		this.rows = rows;
		this.columns = columns;
		cursorRow = Math.Min(
			cursorRow,
			rows - 1
		);
		cursorColumn = Math.Min(
			cursorColumn,
			columns - 1
		);
	}

	/// <summary>Writes terminal-independent text using <see cref="CurrentStyle"/>.</summary>
	public void Write( string text ) {
		Write(
			text,
			CurrentStyle
		);
	}

	/// <summary>Writes terminal-independent text using an explicit style.</summary>
	public void Write(
		string text,
		CursesStyle style ) {
		ArgumentNullException.ThrowIfNull( text );
		ValidateText( text );

		TextElementEnumerator elements = StringInfo.GetTextElementEnumerator( text );
		while ( elements.MoveNext() ) {
			string textElement = (string)elements.Current;
			if ( !WriteTextElementCore(
				textElement,
				style
			) ) {
				break;
			}
		}
	}

	/// <summary>Writes one Unicode scalar value using <see cref="CurrentStyle"/>.</summary>
	public void Write( Rune rune ) {
		_ = WriteRuneCore(
			rune,
			CurrentStyle
		);
	}

	/// <summary>Writes one Unicode scalar value using an explicit style.</summary>
	public void Write(
		Rune rune,
		CursesStyle style ) {
		_ = WriteRuneCore(
			rune,
			style
		);
	}

	/// <summary>Writes one exact logical cell at the current cursor position.</summary>
	public void WriteCell( CursesCell cell ) {
		_ = WriteCellCore( cell );
	}

	/// <summary>Moves to column zero of the following row, scrolling when enabled at the bottom edge.</summary>
	public void NewLine() {
		cursorColumn = 0;
		_ = AdvanceRow();
	}

	/// <summary>Fills the window with its background cell and moves the cursor to the upper-left corner.</summary>
	public void Clear() {
		Erase();
		cursorRow = 0;
		cursorColumn = 0;
	}

	/// <summary>Fills the complete window with its background cell without changing the cursor.</summary>
	public void Erase() {
		FillRegion(
			0,
			0,
			Rows,
			Columns,
			backgroundCell
		);
		Touch();
	}

	/// <summary>Erases from the cursor through the end of the current line.</summary>
	public void ClearToEndOfLine() {
		FillRegion(
			cursorRow,
			cursorColumn,
			1,
			Columns - cursorColumn,
			backgroundCell
		);
		TouchLine( cursorRow );
	}

	/// <summary>Erases from the cursor through the end of the window.</summary>
	public void ClearToEndOfWindow() {
		ClearToEndOfLine();

		for ( int row = cursorRow + 1; row < Rows; row++ ) {
			FillRegion(
				row,
				0,
				1,
				Columns,
				backgroundCell
			);
			TouchLine( row );
		}
	}

	/// <summary>Scrolls the logical contents upward inside this window.</summary>
	/// <param name="lines">The positive number of rows to scroll.</param>
	public void ScrollUp( int lines = 1 ) {
		ValidateScrollCount( lines );

		if ( lines >= Rows ) {
			FillRegion(
				0,
				0,
				Rows,
				Columns,
				backgroundCell
			);
			Touch();
			return;
		}

		for ( int row = 0; row < Rows - lines; row++ ) {
			for ( int column = 0; column < Columns; column++ ) {
				CursesCell source = GetCellOrBackground(
					row + lines,
					column
				);
				SetCellIfVisible(
					row,
					column,
					source
				);
			}
		}

		FillRegion(
			Rows - lines,
			0,
			lines,
			Columns,
			backgroundCell
		);
		Touch();
	}

	/// <summary>Scrolls the logical contents downward inside this window.</summary>
	/// <param name="lines">The positive number of rows to scroll.</param>
	public void ScrollDown( int lines = 1 ) {
		ValidateScrollCount( lines );

		if ( lines >= Rows ) {
			FillRegion(
				0,
				0,
				Rows,
				Columns,
				backgroundCell
			);
			Touch();
			return;
		}

		for ( int row = Rows - 1; row >= lines; row-- ) {
			for ( int column = 0; column < Columns; column++ ) {
				CursesCell source = GetCellOrBackground(
					row - lines,
					column
				);
				SetCellIfVisible(
					row,
					column,
					source
				);
			}
		}

		FillRegion(
			0,
			0,
			lines,
			Columns,
			backgroundCell
		);
		Touch();
	}

	/// <summary>Marks every currently visible cell of this window as changed.</summary>
	public void Touch() {
		for ( int row = 0; row < Rows; row++ ) {
			TouchLine( row );
		}
	}

	/// <summary>Marks one logical row as changed.</summary>
	public void TouchLine( int row ) {
		if ( 0 > row || row >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}

		for ( int column = 0; column < Columns; column++ ) {
			if ( TryMapToScreen(
				row,
				column,
				out int screenRow,
				out int screenColumn ) ) {
				screen.VirtualScreen.TouchCell(
					screenRow,
					screenColumn
				);
			}
		}
	}

	/// <summary>Invalidates the visible window image so a later refresh repaints it.</summary>
	public void Invalidate() {
		Touch();
	}

	internal void HandleScreenResize() {
		if ( !isStandardWindow ) {
			return;
		}

		cursorRow = Math.Min(
			cursorRow,
			Rows - 1
		);
		cursorColumn = Math.Min(
			cursorColumn,
			Columns - 1
		);
	}

	internal static CursesWindow CreateStandard( CursesScreen screen ) {
		ArgumentNullException.ThrowIfNull( screen );
		return new CursesWindow(
			screen,
			parent: null,
			row: 0,
			column: 0,
			rows: 1,
			columns: 1,
			isStandardWindow: true
		);
	}

	internal static CursesWindow CreateRootView(
		CursesScreen screen,
		int row,
		int column,
		int rows,
		int columns ) {
		ArgumentNullException.ThrowIfNull( screen );
		return new CursesWindow(
			screen,
			parent: null,
			row,
			column,
			rows,
			columns,
			isStandardWindow: false
		);
	}

	private bool WriteRuneCore(
		Rune rune,
		CursesStyle style ) {
		return WriteTextElementCore(
			rune.ToString(),
			style
		);
	}

	private bool WriteTextElementCore(
		string textElement,
		CursesStyle style ) {
		ArgumentException.ThrowIfNullOrEmpty( textElement );

		if ( 1 == textElement.Length ) {
			switch ( textElement[ 0 ] ) {
				case '\r':
					cursorColumn = 0;
					return true;

				case '\n':
					cursorColumn = 0;
					return AdvanceRow();

				case '\t':
					return WriteTab( style );
			}
		}

		string normalized = NormalizeMalformedUtf16( textElement );
		int width = screen.TextWidthProvider.GetWidth( normalized );
		if ( width < 0 || width > 2 ) {
			throw new InvalidOperationException(
				"The configured curses text-width provider returned a width outside the supported range."
			);
		}

		if ( 0 == width ) {
			return AppendZeroWidthText( normalized );
		}

		return WriteDisplayCellCore(
			new CursesCell(
				normalized,
				style,
				width
			),
			width
		);
	}

	private bool WriteTab( CursesStyle style ) {
		int spaces = TabWidth - ( cursorColumn % TabWidth );
		for ( int index = 0; index < spaces; index++ ) {
			if ( !WriteCellCore( new CursesCell( " ", style ) ) ) {
				return false;
			}
		}
		return true;
	}

	private bool WriteCellCore( CursesCell cell ) {
		if ( cell.IsContinuation ) {
			RepairCellFootprint(
				cursorRow,
				cursorColumn
			);
			SetCellIfVisible(
				cursorRow,
				cursorColumn,
				cell
			);
			return AdvanceColumns( 1 );
		}

		return WriteDisplayCellCore(
			cell,
			cell.DisplayWidth
		);
	}

	private bool WriteDisplayCellCore(
		CursesCell cell,
		int width ) {
		if ( width < 1 || width > 2 ) {
			throw new ArgumentOutOfRangeException( nameof( width ) );
		}

		if ( 2 == width
			&& cursorColumn + 1 >= Columns ) {
			if ( CursesWrapMode.Clip == WrapMode ) {
				return false;
			}

			cursorColumn = 0;
			if ( !AdvanceRow() ) {
				cursorColumn = Columns - 1;
				return false;
			}
		}

		RepairCellFootprint(
			cursorRow,
			cursorColumn
		);
		if ( 2 == width ) {
			RepairCellFootprint(
				cursorRow,
				cursorColumn + 1
			);
		}

		SetCellIfVisible(
			cursorRow,
			cursorColumn,
			cell
		);
		if ( 2 == width ) {
			SetCellIfVisible(
				cursorRow,
				cursorColumn + 1,
				CursesCell.Continuation( cell.Style )
			);
		}

		return AdvanceColumns( width );
	}

	private bool AdvanceColumns( int width ) {
		int next = cursorColumn + width;
		if ( next < Columns ) {
			cursorColumn = next;
			return true;
		}

		if ( next == Columns
			&& CursesWrapMode.Clip == WrapMode ) {
			cursorColumn = Columns - 1;
			return false;
		}

		if ( CursesWrapMode.Clip == WrapMode ) {
			cursorColumn = Columns - 1;
			return false;
		}

		cursorColumn = 0;
		if ( AdvanceRow() ) {
			return true;
		}

		cursorColumn = Columns - 1;
		return false;
	}

	private bool AppendZeroWidthText( string text ) {
		if ( 0 == cursorColumn ) {
			return true;
		}

		int targetColumn = cursorColumn - 1;
		CursesCell target = GetCellOrBackground(
			cursorRow,
			targetColumn
		);
		while ( target.IsContinuation && 0 < targetColumn ) {
			targetColumn--;
			target = GetCellOrBackground(
				cursorRow,
				targetColumn
			);
		}

		if ( target.IsBlank || target.IsContinuation ) {
			return true;
		}

		SetCellIfVisible(
			cursorRow,
			targetColumn,
			new CursesCell(
				target.Content + text,
				target.Style,
				target.DisplayWidth
			)
		);
		return true;
	}

	private void RepairCellFootprint(
		int row,
		int column ) {
		CursesCell existing = GetCellOrBackground(
			row,
			column
		);

		if ( existing.IsContinuation ) {
			int leader = column - 1;
			while ( 0 <= leader ) {
				CursesCell candidate = GetCellOrBackground(
					row,
					leader
				);
				if ( !candidate.IsContinuation ) {
					SetCellIfVisible(
						row,
						leader,
						backgroundCell
					);
					break;
				}
				leader--;
			}
		}

		if ( existing.DisplayWidth > 1 ) {
			for ( int offset = 1; offset < existing.DisplayWidth; offset++ ) {
				SetCellIfVisible(
					row,
					column + offset,
					backgroundCell
				);
			}
		}

		SetCellIfVisible(
			row,
			column,
			backgroundCell
		);
	}

	private static string NormalizeMalformedUtf16( string text ) {
		ArgumentNullException.ThrowIfNull( text );

		StringBuilder normalized = new();
		ReadOnlySpan<char> remaining = text.AsSpan();
		while ( !remaining.IsEmpty ) {
			OperationStatus status = Rune.DecodeFromUtf16(
				remaining,
				out Rune rune,
				out int consumed
			);
			if ( OperationStatus.Done == status ) {
				normalized.Append( rune.ToString() );
				remaining = remaining[ consumed.. ];
				continue;
			}

			normalized.Append( Rune.ReplacementChar.ToString() );
			remaining = remaining[ 1.. ];
		}

		return normalized.ToString();
	}

	private bool AdvanceRow() {
		if ( cursorRow + 1 < Rows ) {
			cursorRow++;
			return true;
		}

		if ( ScrollingEnabled ) {
			ScrollUp();
			cursorRow = Rows - 1;
			return true;
		}

		cursorRow = Rows - 1;
		return false;
	}

	private void FillRegion(
		int row,
		int column,
		int rows,
		int columns,
		CursesCell cell ) {
		for ( int localRow = row; localRow < row + rows; localRow++ ) {
			for ( int localColumn = column; localColumn < column + columns; localColumn++ ) {
				SetCellIfVisible(
					localRow,
					localColumn,
					cell
				);
			}
		}
	}

	private CursesCell GetCellOrBackground(
		int row,
		int column ) {
		return TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn )
			? screen.VirtualScreen[ screenRow, screenColumn ]
			: backgroundCell
		;
	}

	private void SetCellIfVisible(
		int row,
		int column,
		CursesCell cell ) {
		if ( TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn ) ) {
			screen.VirtualScreen[ screenRow, screenColumn ] = cell;
		}
	}

	private bool TryMapToScreen(
		int row,
		int column,
		out int screenRow,
		out int screenColumn ) {
		screenRow = row;
		screenColumn = column;

		CursesWindow? current = this;
		while ( null != current ) {
			if ( screenRow < 0
				|| screenRow >= current.Rows
				|| screenColumn < 0
				|| screenColumn >= current.Columns ) {
				return false;
			}

			if ( current.isStandardWindow ) {
				break;
			}

			screenRow += current.originRow;
			screenColumn += current.originColumn;
			current = current.parent;
		}

		return 0 <= screenRow
			&& screenRow < screen.Rows
			&& 0 <= screenColumn
			&& screenColumn < screen.Columns
		;
	}

	private void ValidateCoordinate(
		int row,
		int column ) {
		if ( 0 > row || row >= Rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the window."
			);
		}
		if ( 0 > column || column >= Columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( column ),
				column,
				"The column must be inside the window."
			);
		}
	}

	private void ValidateScrollCount( int lines ) {
		if ( 0 >= lines ) {
			throw new ArgumentOutOfRangeException(
				nameof( lines ),
				lines,
				"The scroll count must be positive."
			);
		}
	}

	private static void ValidateText( string text ) {
		ArgumentNullException.ThrowIfNull( text );

		foreach ( Rune rune in text.EnumerateRunes() ) {
			if ( '\r' == rune.Value
				|| '\n' == rune.Value
				|| '\t' == rune.Value ) {
				continue;
			}

			if ( IsControl( rune ) ) {
				throw new ArgumentException(
					"Window text cannot contain terminal control characters other than tab, carriage return, or line feed.",
					nameof( text )
				);
			}
		}
	}

	private static bool IsControl( Rune rune ) {
		return rune.Value <= 0x1F
			|| ( rune.Value >= 0x7F && rune.Value <= 0x9F );
	}
}
