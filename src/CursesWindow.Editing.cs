namespace Icod.DCurses;

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
	/// footprint, the owning screen repairs the other half before storing the replacement cells.
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
	/// content, and never retains half of a two-column footprint at an edit boundary.
	/// </remarks>
	public void InsertCells( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesCell[] source = SnapshotRow( cursorRow );
		CursesCell[] result = (CursesCell[])source.Clone();
		int shift = Math.Min(
			count,
			Columns - cursorColumn
		);

		for ( int column = cursorColumn; column < Columns; column++ ) {
			result[ column ] = editingBackground;
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
	/// with <see cref="BackgroundCell"/>, and never retains half of a two-column footprint.
	/// </remarks>
	public void DeleteCells( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesCell[] source = SnapshotRow( cursorRow );
		CursesCell[] result = (CursesCell[])source.Clone();
		int shift = Math.Min(
			count,
			Columns - cursorColumn
		);

		for ( int column = cursorColumn; column < Columns; column++ ) {
			result[ column ] = editingBackground;
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
	/// <remarks>The operation preserves the cursor and discards rows shifted below the window.</remarks>
	public void InsertLines( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesCell[][] source = SnapshotRows();
		CursesCell[][] result = CloneRows( source );
		int shift = Math.Min(
			count,
			Rows - cursorRow
		);

		for ( int row = cursorRow; row < Rows; row++ ) {
			Array.Fill(
				result[ row ],
				editingBackground
			);
		}
		for ( int row = cursorRow; row < Rows - shift; row++ ) {
			result[ row + shift ] = (CursesCell[])source[ row ].Clone();
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
	/// The operation preserves the cursor and fills the vacated rows at the bottom with
	/// <see cref="BackgroundCell"/>.
	/// </remarks>
	public void DeleteLines( int count = 1 ) {
		ValidateEditCount( count );
		CursesCell editingBackground = GetEditingBackgroundCell();
		CursesCell[][] source = SnapshotRows();
		CursesCell[][] result = CloneRows( source );
		int shift = Math.Min(
			count,
			Rows - cursorRow
		);

		for ( int row = cursorRow; row < Rows; row++ ) {
			Array.Fill(
				result[ row ],
				editingBackground
			);
		}
		for ( int row = cursorRow; row < Rows - shift; row++ ) {
			result[ row ] = (CursesCell[])source[ row + shift ].Clone();
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

	private CursesCell[] SnapshotRow( int row ) {
		if ( 0 > row || row >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}

		CursesCell[] result = new CursesCell[ Columns ];
		for ( int column = 0; column < Columns; column++ ) {
			result[ column ] = GetCellOrBackground(
				row,
				column
			);
		}
		return result;
	}

	private CursesCell[][] SnapshotRows() {
		CursesCell[][] result = new CursesCell[ Rows ][];
		for ( int row = 0; row < Rows; row++ ) {
			result[ row ] = SnapshotRow( row );
		}
		return result;
	}

	private static CursesCell[][] CloneRows( CursesCell[][] source ) {
		ArgumentNullException.ThrowIfNull( source );
		CursesCell[][] result = new CursesCell[ source.Length ][];
		for ( int row = 0; row < source.Length; row++ ) {
			ArgumentNullException.ThrowIfNull( source[ row ] );
			result[ row ] = (CursesCell[])source[ row ].Clone();
		}
		return result;
	}

	private void CommitEditingRows(
		CursesCell[][] rows,
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
		CursesCell[] cells,
		CursesCell editingBackground
	) {
		ArgumentNullException.ThrowIfNull( cells );
		if ( cells.Length != Columns ) {
			throw new ArgumentException(
				"The editing snapshot width must match the window.",
				nameof( cells )
			);
		}

		for ( int column = 0; column < Columns; column++ ) {
			SetCellIfVisible(
				row,
				column,
				editingBackground
			);
		}
		for ( int column = 0; column < Columns; column++ ) {
			if ( cells[ column ] == editingBackground ) {
				continue;
			}
			SetCellIfVisible(
				row,
				column,
				cells[ column ]
			);
		}
	}

	private static void NormalizeEditingRow(
		CursesCell[] cells,
		CursesCell editingBackground
	) {
		ArgumentNullException.ThrowIfNull( cells );
		for ( int column = 0; column < cells.Length; column++ ) {
			CursesCell cell = cells[ column ];
			if ( cell.IsContinuation ) {
				cells[ column ] = editingBackground;
				continue;
			}
			if ( 2 != cell.DisplayWidth ) {
				continue;
			}
			if ( column + 1 >= cells.Length
				|| !cells[ column + 1 ].IsContinuation ) {
				cells[ column ] = editingBackground;
				continue;
			}

			cells[ column + 1 ] = CursesCell.Continuation( cell.Style );
			column++;
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
