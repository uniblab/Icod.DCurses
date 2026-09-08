namespace Icod.DCurses.Internal;

/// <summary>
/// Validates and repairs leader/continuation invariants in logical screen storage.
/// </summary>
internal static class CursesCellFootprint {
	/// <summary>
	/// Repairs invalid wide-cell footprints by preserving valid complete footprints and blanking clipped
	/// or orphaned cells. Intended for structural operations such as preserved screen resize.
	/// </summary>
	internal static void Repair(
		CursesVirtualScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );

		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				CursesCell cell = screen[ row, column ];
				if ( cell.IsContinuation ) {
					if ( HasValidLeader(
						screen,
						row,
						column
					) ) {
						continue;
					}

					screen[ row, column ] = CursesCell.Blank( cell.Style );
					continue;
				}

				if ( 2 != cell.DisplayWidth ) {
					continue;
				}

				if ( column + 1 >= screen.Columns
					|| !screen[ row, column + 1 ].IsContinuation ) {
					screen[ row, column ] = CursesCell.Blank( cell.Style );
					continue;
				}

				CursesCell continuation = screen[ row, column + 1 ];
				if ( continuation.Style != cell.Style ) {
					screen[ row, column + 1 ] = CursesCell.Continuation( cell.Style );
				}
			}
		}
	}

	/// <summary>Throws when the logical screen contains an invalid wide-cell footprint.</summary>
	internal static void Validate(
		CursesVirtualScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );

		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				CursesCell cell = screen[ row, column ];
				if ( cell.IsContinuation ) {
					if ( !HasValidLeader(
						screen,
						row,
						column
					) ) {
						throw new InvalidOperationException(
							$"Orphaned continuation cell at ({row},{column})."
						);
					}
					continue;
				}

				if ( 2 == cell.DisplayWidth
					&& ( column + 1 >= screen.Columns
						|| !screen[ row, column + 1 ].IsContinuation ) ) {
					throw new InvalidOperationException(
						$"Two-column leader at ({row},{column}) has no continuation."
					);
				}
			}
		}
	}

	private static bool HasValidLeader(
		CursesVirtualScreen screen,
		int row,
		int column
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( 0 >= column ) {
			return false;
		}

		CursesCell leader = screen[ row, column - 1 ];
		return !leader.IsContinuation
			&& 2 == leader.DisplayWidth;
	}
}
