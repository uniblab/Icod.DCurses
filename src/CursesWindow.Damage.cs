namespace Icod.DCurses;

/// <summary>Window-local damage-range operations.</summary>
public sealed partial class CursesWindow {
	/// <summary>Marks every currently projected cell in one window-local rectangle as dirty.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="rows">The positive rectangle height.</param>
	/// <param name="columns">The positive rectangle width.</param>
	/// <remarks>
	/// Valid local coordinates clipped outside the owning screen by ancestor/screen geometry are ignored until
	/// they project onto the screen again. The cursor is preserved.
	/// </remarks>
	public void TouchRegion(
		int row,
		int column,
		int rows,
		int columns
	) {
		ValidateRegion(
			row,
			column,
			rows,
			columns
		);

		for ( int localRow = row; localRow < row + rows; localRow++ ) {
			for ( int localColumn = column; localColumn < column + columns; localColumn++ ) {
				if ( TryMapToScreen(
					localRow,
					localColumn,
					out int screenRow,
					out int screenColumn
				) ) {
					screen.VirtualScreen.TouchCell(
						screenRow,
						screenColumn
					);
				}
			}
		}
	}

	/// <summary>Gets whether any currently projected cell in one window-local rectangle is dirty.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="rows">The positive rectangle height.</param>
	/// <param name="columns">The positive rectangle width.</param>
	/// <returns>
	/// <see langword="true"/> when at least one currently projected cell in the region requires refresh;
	/// otherwise <see langword="false"/>.
	/// </returns>
	public bool IsRegionTouched(
		int row,
		int column,
		int rows,
		int columns
	) {
		ValidateRegion(
			row,
			column,
			rows,
			columns
		);

		for ( int localRow = row; localRow < row + rows; localRow++ ) {
			for ( int localColumn = column; localColumn < column + columns; localColumn++ ) {
				if ( TryMapToScreen(
					localRow,
					localColumn,
					out int screenRow,
					out int screenColumn
				) && screen.VirtualScreen.IsDirty(
					screenRow,
					screenColumn
				) ) {
					return true;
				}
			}
		}

		return false;
	}
}
