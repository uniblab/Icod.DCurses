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

	internal int Columns {
		get;
	}

	internal int Rows {
		get;
	}

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
