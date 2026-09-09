namespace Icod.DCurses;

using Icod.DCurses.Internal;

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
	/// Source cells and semantic metadata are snapshotted before any destination mutation, so overlapping
	/// source/destination rectangles are deterministic. Source blanks are copied destructively, including
	/// any semantic metadata associated with those coordinates. Source and destination cursor positions
	/// are preserved.
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
	/// Ordinary source blank cells are transparent and therefore do not replace destination cells or
	/// semantic metadata. Two-column continuation cells remain structural parts of their leading cell.
	/// Source cells and semantic metadata are snapshotted before destination mutation, and both cursors
	/// are preserved.
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
		CursesLogicalCellState[][] snapshot = SnapshotCompositionRectangle(
			sourceRow,
			sourceColumn,
			rows,
			columns,
			boundaryBlank
		);
		CursesLogicalCellState destinationRepairState = new(
			CursesCell.Blank( destination.backgroundCell.Style ),
			null
		);

		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			CursesLogicalCellState[] sourceStates = snapshot[ rowOffset ];
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				CursesLogicalCellState sourceState = sourceStates[ columnOffset ];
				CursesCell sourceCell = sourceState.Cell;
				if ( sourceCell.IsContinuation ) {
					continue;
				}
				if ( transparentBlanks && sourceCell.IsBlank ) {
					continue;
				}

				int targetRow = destinationRow + rowOffset;
				int targetColumn = destinationColumn + columnOffset;
				if ( 2 == sourceCell.DisplayWidth ) {
					destination.SetLogicalCellStateIfVisible(
						targetRow,
						targetColumn,
						destinationRepairState
					);
					destination.SetLogicalCellStateIfVisible(
						targetRow,
						targetColumn + 1,
						destinationRepairState
					);
					destination.SetLogicalCellStateIfVisible(
						targetRow,
						targetColumn,
						sourceState
					);
					destination.SetLogicalCellStateIfVisible(
						targetRow,
						targetColumn + 1,
						new CursesLogicalCellState(
							CursesCell.Continuation( sourceCell.Style ),
							sourceState.Metadata
						)
					);
					columnOffset++;
					continue;
				}

				destination.SetLogicalCellStateIfVisible(
					targetRow,
					targetColumn,
					sourceState
				);
			}
		}
	}

	private CursesLogicalCellState[][] SnapshotCompositionRectangle(
		int row,
		int column,
		int rows,
		int columns,
		CursesCell boundaryBlank
	) {
		CursesLogicalCellState[][] result = new CursesLogicalCellState[ rows ][];
		for ( int rowOffset = 0; rowOffset < rows; rowOffset++ ) {
			CursesLogicalCellState[] sourceStates = new CursesLogicalCellState[ columns ];
			for ( int columnOffset = 0; columnOffset < columns; columnOffset++ ) {
				sourceStates[ columnOffset ] = SnapshotLogicalCell(
					row + rowOffset,
					column + columnOffset
				);
			}
			NormalizeEditingRow(
				sourceStates,
				boundaryBlank
			);
			result[ rowOffset ] = sourceStates;
		}
		return result;
	}
}
