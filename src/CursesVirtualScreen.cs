namespace Icod.DCurses;

/// <summary>
/// Stores the application-requested logical image of a terminal screen.
/// </summary>
/// <remarks>
/// This type does not write to a terminal. Physical-screen knowledge is tracked separately by the
/// refresh layer so application intent cannot be confused with what is believed to be on the terminal.
/// Coordinates are zero-based.
/// </remarks>
public sealed class CursesVirtualScreen {
	private readonly CursesCell[] cells;
	private readonly bool[] dirtyCells;
	private int dirtyCellCount;

	/// <summary>Initializes a blank logical screen.</summary>
	/// <param name="columns">The positive number of columns.</param>
	/// <param name="rows">The positive number of rows.</param>
	public CursesVirtualScreen(
		int columns,
		int rows ) {
		int cellCount = ValidateDimensions(
			columns,
			rows
		);

		Columns = columns;
		Rows = rows;
		cells = new CursesCell[ cellCount ];
		dirtyCells = new bool[ cellCount ];
		Array.Fill(
			dirtyCells,
			true
		);
		dirtyCellCount = cellCount;
	}

	/// <summary>Gets the number of columns.</summary>
	public int Columns {
		get;
	}

	/// <summary>Gets the number of rows.</summary>
	public int Rows {
		get;
	}

	/// <summary>Gets the number of cells in the logical screen.</summary>
	public int CellCount => cells.Length;

	/// <summary>Gets or sets one logical cell using zero-based row and column coordinates.</summary>
	public CursesCell this[
		int row,
		int column ] {
		get => GetCell(
			row,
			column
		);
		set => SetCell(
			row,
			column,
			value
		);
	}

	/// <summary>Gets one logical cell.</summary>
	public CursesCell GetCell(
		int row,
		int column ) {
		return cells[ GetOffset( row, column ) ];
	}

	/// <summary>Sets one logical cell.</summary>
	public void SetCell(
		int row,
		int column,
		CursesCell cell ) {
		int offset = GetOffset(
			row,
			column
		);

		if ( cells[ offset ] == cell ) {
			return;
		}

		cells[ offset ] = cell;
		MarkDirty( offset );
	}

	/// <summary>Clears the logical screen to default blank cells.</summary>
	public void Clear() {
		Fill( default );
	}

	/// <summary>Clears the logical screen to blank cells carrying the supplied style.</summary>
	public void Clear( CursesStyle style ) {
		Fill( CursesCell.Blank( style ) );
	}

	/// <summary>Fills every logical coordinate with the same cell value.</summary>
	public void Fill( CursesCell cell ) {
		for ( int offset = 0; offset < cells.Length; offset++ ) {
			if ( cells[ offset ] == cell ) {
				continue;
			}

			cells[ offset ] = cell;
			MarkDirty( offset );
		}
	}

	internal int DirtyCellCount => dirtyCellCount;

	internal bool IsDirty(
		int row,
		int column ) {
		return dirtyCells[ GetOffset( row, column ) ];
	}

	internal void TouchCell(
		int row,
		int column ) {
		MarkDirty( GetOffset( row, column ) );
	}

	internal ReadOnlySpan<CursesCell> Cells => cells;

	internal void MarkClean() {
		Array.Clear( dirtyCells );
		dirtyCellCount = 0;
	}

	internal void Invalidate() {
		Array.Fill(
			dirtyCells,
			true
		);
		dirtyCellCount = dirtyCells.Length;
	}

	private void MarkDirty( int offset ) {
		if ( dirtyCells[ offset ] ) {
			return;
		}

		dirtyCells[ offset ] = true;
		dirtyCellCount++;
	}

	private int GetOffset(
		int row,
		int column ) {
		if ( row < 0 || row >= Rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				row,
				"The row must be inside the virtual screen."
			);
		}

		if ( column < 0 || column >= Columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( column ),
				column,
				"The column must be inside the virtual screen."
			);
		}

		return ( row * Columns ) + column;
	}

	private static int ValidateDimensions(
		int columns,
		int rows ) {
		if ( columns <= 0 ) {
			throw new ArgumentOutOfRangeException(
				nameof( columns ),
				columns,
				"The virtual-screen column count must be positive."
			);
		}

		if ( rows <= 0 ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The virtual-screen row count must be positive."
			);
		}

		long cellCount = (long)columns * rows;
		if ( int.MaxValue < cellCount ) {
			throw new ArgumentOutOfRangeException(
				nameof( rows ),
				rows,
				"The requested virtual screen contains too many cells."
			);
		}

		return (int)cellCount;
	}
}
