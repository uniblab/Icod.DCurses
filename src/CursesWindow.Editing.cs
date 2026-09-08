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
