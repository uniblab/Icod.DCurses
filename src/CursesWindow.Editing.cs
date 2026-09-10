namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>Window-local inspection and region-editing operations.</summary>
public sealed partial class CursesWindow {
	/// <summary>Gets one logical cell using window-local coordinates.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <returns>
	/// The projected logical cell, or <see cref="BackgroundCell"/> when the valid local coordinate is
	/// temporarily clipped outside the owning screen by ancestor/screen geometry.
	/// </returns>
	public CursesCell GetCell(
		int row,
		int column
	) {
		ValidateCoordinate(
			row,
			column
		);
		return GetCellOrBackground(
			row,
			column
		);
	}

	/// <summary>Fills one window-local rectangle with a single-column logical cell.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="rows">The positive rectangle height.</param>
	/// <param name="columns">The positive rectangle width.</param>
	/// <param name="cell">The one-column non-continuation cell copied into the rectangle.</param>
	/// <remarks>
	/// The cursor is preserved. If the selected rectangle intersects either half of an existing two-column
	/// footprint, the owning screen repairs the other half before storing the replacement cells. Existing
	/// semantic metadata in the filled coordinates is removed with the replaced content.
	/// </remarks>
	public void FillRectangle(
		int row,
		int column,
		int rows,
		int columns,
		CursesCell cell
	) {
		ValidateRegion(
			row,
			column,
			rows,
			columns
		);
		ValidateFillCell( cell );

		FillRegion(
			row,
			column,
			rows,
			columns,
			cell
		);
	}

	/// <summary>Erases from the beginning of the current line through the cursor column.</summary>
	/// <remarks>The cursor is preserved.</remarks>
	public void ClearToBeginningOfLine() {
		FillRegion(
			cursorRow,
			0,
			1,
			cursorColumn + 1,
			backgroundCell
		);
		TouchLine( cursorRow );
	}

	/// <summary>Inserts background cells at the cursor and shifts the remaining row content right.</summary>
	/// <param name="count">The positive number of terminal columns to insert.</param>
	/// <remarks>
	/// The operation is confined to the current window row, preserves the cursor, discards shifted-out
	/// content, moves semantic metadata with surviving content, and never retains half of a two-column
	/// footprint at an edit boundary.
	/// </remarks>
	public void InsertCells( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);
		CursesLogicalCellState[] source = SnapshotEditingRow( cursorRow );
		CursesLogicalCellState[] result = (CursesLogicalCellState[])source.Clone();
		int shift = Math.Min(
			count,
			Columns - cursorColumn
		);

		for ( int column = cursorColumn; column < Columns; column++ ) {
			result[ column ] = backgroundState;
		}
		for ( int column = cursorColumn; column < Columns - shift; column++ ) {
			result[ column + shift ] = source[ column ];
		}

		NormalizeEditingRow(
			result,
			editingBackground
		);
		CommitEditingRow(
			cursorRow,
			result,
			editingBackground
		);
	}

	/// <summary>Deletes cells at the cursor and shifts following row content left.</summary>
	/// <param name="count">The positive number of terminal columns to delete.</param>
	/// <remarks>
	/// The operation is confined to the current window row, preserves the cursor, fills vacated columns
	/// with <see cref="BackgroundCell"/>, moves semantic metadata with surviving content, and never retains
	/// half of a two-column footprint.
	/// </remarks>
	public void DeleteCells( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);
		CursesLogicalCellState[] source = SnapshotEditingRow( cursorRow );
		CursesLogicalCellState[] result = (CursesLogicalCellState[])source.Clone();
		int shift = Math.Min(
			count,
			Columns - cursorColumn
		);

		for ( int column = cursorColumn; column < Columns; column++ ) {
			result[ column ] = backgroundState;
		}
		for ( int column = cursorColumn; column < Columns - shift; column++ ) {
			result[ column ] = source[ column + shift ];
		}

		NormalizeEditingRow(
			result,
			editingBackground
		);
		CommitEditingRow(
			cursorRow,
			result,
			editingBackground
		);
	}

	/// <summary>Inserts background rows at the cursor and shifts following window rows downward.</summary>
	/// <param name="count">The positive number of rows to insert.</param>
	/// <remarks>
	/// The operation preserves the cursor, discards rows shifted below the window, and moves semantic
	/// metadata with surviving row content.
	/// </remarks>
	public void InsertLines( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);
		CursesLogicalCellState[][] source = SnapshotEditingRows();
		CursesLogicalCellState[][] result = CloneRows( source );
		int shift = Math.Min(
			count,
			Rows - cursorRow
		);

		for ( int row = cursorRow; row < Rows; row++ ) {
			Array.Fill(
				result[ row ],
				backgroundState
			);
		}
		for ( int row = cursorRow; row < Rows - shift; row++ ) {
			result[ row + shift ] = (CursesLogicalCellState[])source[ row ].Clone();
		}

		CommitEditingRows(
			result,
			cursorRow,
			editingBackground
		);
	}

	/// <summary>Deletes rows at the cursor and shifts following window rows upward.</summary>
	/// <param name="count">The positive number of rows to delete.</param>
	/// <remarks>
	/// The operation preserves the cursor, fills vacated rows at the bottom with
	/// <see cref="BackgroundCell"/>, and moves semantic metadata with surviving row content.
	/// </remarks>
	public void DeleteLines( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);
		CursesLogicalCellState[][] source = SnapshotEditingRows();
		CursesLogicalCellState[][] result = CloneRows( source );
		int shift = Math.Min(
			count,
			Rows - cursorRow
		);

		for ( int row = cursorRow; row < Rows; row++ ) {
			Array.Fill(
				result[ row ],
				backgroundState
			);
		}
		for ( int row = cursorRow; row < Rows - shift; row++ ) {
			result[ row ] = (CursesLogicalCellState[])source[ row + shift ].Clone();
		}

		CommitEditingRows(
			result,
			cursorRow,
			editingBackground
		);
	}

	private CursesCell GetEditingBackgroundCell() {
		if ( backgroundCell.IsContinuation
			|| 1 != backgroundCell.DisplayWidth ) {
			throw new InvalidOperationException(
				"Cell and line editing requires a one-column non-continuation BackgroundCell."
			);
		}

		return backgroundCell;
	}

	private CursesLogicalCellState SnapshotLogicalCell(
		int row,
		int column
	) {
		return new CursesLogicalCellState(
			GetCellOrBackground(
				row,
				column
			),
			GetMetadata(
				row,
				column
			)
		);
	}

	private CursesLogicalCellState[] SnapshotEditingRow( int row ) {
		if ( 0 > row || row >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}

		CursesLogicalCellState[] result = new CursesLogicalCellState[ Columns ];
		for ( int column = 0; column < Columns; column++ ) {
			result[ column ] = SnapshotLogicalCell(
				row,
				column
			);
		}
		return result;
	}

	private CursesLogicalCellState[][] SnapshotEditingRows() {
		CursesLogicalCellState[][] result = new CursesLogicalCellState[ Rows ][];
		for ( int row = 0; row < Rows; row++ ) {
			result[ row ] = SnapshotEditingRow( row );
		}
		return result;
	}

	private static CursesLogicalCellState[][] CloneRows(
		CursesLogicalCellState[][] source
	) {
		ArgumentNullException.ThrowIfNull( source );
		CursesLogicalCellState[][] result = new CursesLogicalCellState[ source.Length ][];
		for ( int row = 0; row < source.Length; row++ ) {
			ArgumentNullException.ThrowIfNull( source[ row ] );
			result[ row ] = (CursesLogicalCellState[])source[ row ].Clone();
		}
		return result;
	}

	private void CommitEditingRows(
		CursesLogicalCellState[][] rows,
		int firstRow,
		CursesCell editingBackground
	) {
		ArgumentNullException.ThrowIfNull( rows );
		if ( rows.Length != Rows ) {
			throw new ArgumentException(
				"The editing snapshot height must match the window.",
				nameof( rows )
			);
		}
		if ( 0 > firstRow || firstRow >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( firstRow ) );
		}

		for ( int row = firstRow; row < Rows; row++ ) {
			NormalizeEditingRow(
				rows[ row ],
				editingBackground
			);
			CommitEditingRow(
				row,
				rows[ row ],
				editingBackground
			);
		}
	}

	private void CommitEditingRow(
		int row,
		CursesLogicalCellState[] states,
		CursesCell editingBackground
	) {
		ArgumentNullException.ThrowIfNull( states );
		if ( states.Length != Columns ) {
			throw new ArgumentException(
				"The editing snapshot width must match the window.",
				nameof( states )
			);
		}

		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);
		for ( int column = 0; column < Columns; column++ ) {
			SetLogicalCellStateIfVisible(
				row,
				column,
				backgroundState
			);
		}
		for ( int column = 0; column < Columns; column++ ) {
			CursesLogicalCellState state = states[ column ];
			if ( state == backgroundState ) {
				continue;
			}
			SetLogicalCellStateIfVisible(
				row,
				column,
				state
			);
		}
	}

	private static void NormalizeEditingRow(
		CursesLogicalCellState[] states,
		CursesCell editingBackground
	) {
		ArgumentNullException.ThrowIfNull( states );
		CursesLogicalCellState backgroundState = new(
			editingBackground,
			null
		);

		for ( int column = 0; column < states.Length; column++ ) {
			CursesLogicalCellState state = states[ column ];
			CursesCell cell = state.Cell;
			if ( cell.IsContinuation ) {
				states[ column ] = backgroundState;
				continue;
			}
			if ( 2 != cell.DisplayWidth ) {
				continue;
			}
			if ( column + 1 >= states.Length
				|| !states[ column + 1 ].Cell.IsContinuation ) {
				states[ column ] = backgroundState;
				continue;
			}

			states[ column + 1 ] = new CursesLogicalCellState(
				CursesCell.Continuation( cell.Style ),
				state.Metadata
			);
			column++;
		}
	}

	private void SetLogicalCellStateIfVisible(
		int row,
		int column,
		CursesLogicalCellState state
	) {
		if ( !TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		) ) {
			return;
		}

		screen.VirtualScreen[ screenRow, screenColumn ] = state.Cell;
		if ( state.Metadata is not null ) {
			screen.VirtualScreen.SetMetadata(
				screenRow,
				screenColumn,
				state.Metadata
			);
		}
	}

	private static void ValidateEditCount( int count ) {
		if ( 0 >= count ) {
			throw new ArgumentOutOfRangeException(
				nameof( count ),
				count,
				"The edit count must be positive."
			);
		}
	}

	private void ValidateRegion(
		int row,
		int column,
		int rows,
		int columns
	) {
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			Rows,
			Columns
		);
	}

	private static void ValidateFillCell( CursesCell cell ) {
		if ( cell.IsContinuation
			|| 1 != cell.DisplayWidth ) {
			throw new ArgumentException(
				"A fill cell must be a one-column non-continuation cell.",
				nameof( cell )
			);
		}
	}
}
