namespace Icod.DCurses;

/// <summary>Window-to-window rectangular composition operations.</summary>
public sealed partial class CursesWindow {
	/// <summary>Copies a source rectangle into a destination window, including blank cells.</summary>
	/// <param name="destination">The destination window.</param>
	/// <param name="sourceRow">The zero-based source row.</param>
	/// <param name="sourceColumn">The zero-based source column.</param>
	/// <param name="rows">The positive rectangle height.</param>
	/// <param name="columns">The positive rectangle width.</param>
	/// <param name="destinationRow">The zero-based destination row.</param>
	/// <param name="destinationColumn">The zero-based destination column.</param>
	/// <remarks>
	/// Source cells are snapshotted before any destination mutation, so overlapping source/destination
	/// rectangles are deterministic. Source and destination cursor positions are preserved.
	/// </remarks>
	public void CopyRectangleTo(
		CursesWindow destination,
		int sourceRow,
		int sourceColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn
	) {
		TransferRectangleTo(
			destination,
			sourceRow,
			sourceColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn,
			transparentBlanks: false
		);
	}

	/// <summary>Overlays a source rectangle onto a destination window without replacing destination cells with source blanks.</summary>
	/// <param name="destination">The destination window.</param>
	/// <param name="sourceRow">The zero-based source row.</param>
	/// <param name="sourceColumn">The zero-based source column.</param>
	/// <param name="rows">The positive rectangle height.</param>
	/// <param name="columns">The positive rectangle width.</param>
	/// <param name="destinationRow">The zero-based destination row.</param>
	/// <param name="destinationColumn">The zero-based destination column.</param>
	/// <remarks>
	/// Ordinary source blank cells are transparent. Two-column continuation cells remain structural parts
	/// of their leading cell. Source cells are snapshotted before destination mutation, and both cursors are preserved.
	/// </remarks>
	public void OverlayRectangleTo(
		CursesWindow destination,
		int sourceRow,
		int sourceColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn
	) {
		TransferRectangleTo(
			destination,
			sourceRow,
			sourceColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn,
			transparentBlanks: true
		);
	}

	private void TransferRectangleTo(
		CursesWindow destination,
		int sourceRow,
		int sourceColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn,
		bool transparentBlanks
	) {
		ArgumentNullException.ThrowIfNull( destination );
		ValidateRegion(
			sourceRow,
			sourceColumn,
			rows,
			columns
		);
		destination.ValidateRegion(
			destinationRow,
			destinationColumn,
			rows,
			columns
		);

		CursesCell boundaryBlank = CursesCell.Blank( backgroundCell.Style );
		CursesCell[][] snapshot = SnapshotCompositionRectangle(
			sourceRow,
			sourceColumn,
			rows,
			columns,
			boundaryBlank
		);
		CursesCell destinationRepairBlank = CursesCell.Blank( destination.backgroundCell.Style );

		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			CursesCell[] sourceCells = snapshot[ rowOffset ];
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				CursesCell sourceCell = sourceCells[ columnOffset ];
				if ( sourceCell.IsContinuation ) {
					continue;
				}
				if ( transparentBlanks && sourceCell.IsBlank ) {
					continue;
				}

				int targetRow = destinationRow + rowOffset;
				int targetColumn = destinationColumn + columnOffset;
				if ( 2 == sourceCell.DisplayWidth ) {
					destination.SetCellIfVisible(
						targetRow,
						targetColumn,
						destinationRepairBlank
					);
					destination.SetCellIfVisible(
						targetRow,
						targetColumn + 1,
						destinationRepairBlank
					);
					destination.SetCellIfVisible(
						targetRow,
						targetColumn,
						sourceCell
					);
					destination.SetCellIfVisible(
						targetRow,
						targetColumn + 1,
						CursesCell.Continuation( sourceCell.Style )
					);
					columnOffset++;
					continue;
				}

				destination.SetCellIfVisible(
					targetRow,
					targetColumn,
					sourceCell
				);
			}
		}
	}

	private CursesCell[][] SnapshotCompositionRectangle(
		int row,
		int column,
		int rows,
		int columns,
		CursesCell boundaryBlank
	) {
		CursesCell[][] result = new CursesCell[ rows ][];
		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			CursesCell[] sourceCells = new CursesCell[ columns ];
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				sourceCells[ columnOffset ] = GetCellOrBackground(
					row + rowOffset,
					column + columnOffset
				);
			}
			NormalizeEditingRow(
				sourceCells,
				boundaryBlank
			);
			result[ rowOffset ] = sourceCells;
		}
		return result;
	}
}
