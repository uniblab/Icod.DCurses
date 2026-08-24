namespace Icod.DCurses.Internal;

/// <summary>
/// Tracks the last physical-screen image known by the refresh layer.
/// </summary>
/// <remarks>
/// This state is intentionally separate from <see cref="CursesVirtualScreen"/>. Unknown physical cells
/// must never be mistaken for application-requested blank cells.
/// </remarks>
internal sealed class CursesPhysicalScreenState {
	private readonly CursesCell[] cells;
	private readonly bool[] knownCells;

	/// <summary>Initializes physical-screen state with every cell initially unknown.</summary>
	/// <param name="columns">The positive terminal column count.</param>
	/// <param name="rows">The positive terminal row count.</param>
	internal CursesPhysicalScreenState(
		int columns,
		int rows ) {
		if ( columns <= 0 ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( rows <= 0 ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		long cellCount = (long)columns * rows;
		if ( int.MaxValue < cellCount ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		Columns = columns;
		Rows = rows;
		cells = new CursesCell[ (int)cellCount ];
		knownCells = new bool[ (int)cellCount ];
	}

	/// <summary>Gets the physical-screen column count.</summary>
	internal int Columns {
		get;
	}

	/// <summary>Gets the physical-screen row count.</summary>
	internal int Rows {
		get;
	}

	/// <summary>Gets a known physical cell when one has been recorded.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">Receives the known physical cell when available.</param>
	/// <returns><see langword="true"/> when the physical cell is known.</returns>
	internal bool TryGetCell(
		int row,
		int column,
		out CursesCell cell ) {
		int offset = GetOffset(
			row,
			column
		);

		if ( !knownCells[ offset ] ) {
			cell = default;
			return false;
		}

		cell = cells[ offset ];
		return true;
	}

	/// <summary>Records one physical cell as known.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">The physical cell value.</param>
	internal void SetCell(
		int row,
		int column,
		CursesCell cell ) {
		int offset = GetOffset(
			row,
			column
		);

		cells[ offset ] = cell;
		knownCells[ offset ] = true;
	}

	/// <summary>Marks every retained physical cell unknown.</summary>
	internal void Invalidate() {
		Array.Clear( knownCells );
	}

	private int GetOffset(
		int row,
		int column ) {
		if ( row < 0 || row >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 || column >= Columns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}

		return ( row * Columns ) + column;
	}
}
