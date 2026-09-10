namespace Icod.DCurses.Internal;

/// <summary>Composes one base logical frame with independent retained panels.</summary>
internal static class CursesPanelCompositor {
	/// <summary>Creates a complete composed logical frame.</summary>
	/// <param name="baseScreen">The destination screen's uncomposed base frame.</param>
	/// <param name="panels">The complete remembered panel order from bottom to top.</param>
	/// <returns>A new logical frame.</returns>
	internal static CursesVirtualScreen Compose(
		CursesVirtualScreen baseScreen,
		CursesPanel[] panels
	) {
		ArgumentNullException.ThrowIfNull( baseScreen );
		ArgumentNullException.ThrowIfNull( panels );

		CursesVirtualScreen result = new(
			baseScreen.Columns,
			baseScreen.Rows
		);
		CopyBaseCells(
			baseScreen,
			result
		);
		CopyBaseMetadata(
			baseScreen,
			result
		);

		foreach ( CursesPanel panel in panels ) {
			if ( panel is null ) {
				throw new ArgumentException(
					"The panel order contains a null panel.",
					nameof( panels )
				);
			}
			if ( !panel.IsVisible ) {
				continue;
			}

			OverlayPanelCells(
				panel,
				result
			);
			OverlayPanelMetadata(
				panel,
				result
			);
		}

		CursesCellFootprint.Repair( result );
		return result;
	}

	private static void CopyBaseCells(
		CursesVirtualScreen source,
		CursesVirtualScreen destination
	) {
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				destination.SetCell(
					row,
					column,
					source.GetCell(
						row,
						column
					)
				);
			}
		}
	}

	private static void CopyBaseMetadata(
		CursesVirtualScreen source,
		CursesVirtualScreen destination
	) {
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				CursesCellMetadata? metadata = source.GetMetadata(
					row,
					column
				);
				if ( metadata is null ) {
					continue;
				}

				destination.SetMetadata(
					row,
					column,
					metadata
				);
			}
		}
	}

	private static void OverlayPanelCells(
		CursesPanel panel,
		CursesVirtualScreen destination
	) {
		GetIntersection(
			panel,
			destination,
			out int sourceRowEnd,
			out int sourceColumnEnd
		);
		if ( 0 >= sourceRowEnd
			|| 0 >= sourceColumnEnd ) {
			return;
		}

		for ( int sourceRow = 0; sourceRow < sourceRowEnd; sourceRow++ ) {
			for ( int sourceColumn = 0; sourceColumn < sourceColumnEnd; sourceColumn++ ) {
				CursesCell cell = panel.VirtualScreen.GetCell(
					sourceRow,
					sourceColumn
				);
				if ( IsTransparent(
					panel,
					cell
				) ) {
					continue;
				}

				destination.SetCell(
					panel.Row + sourceRow,
					panel.Column + sourceColumn,
					cell
				);
			}
		}
	}

	private static void OverlayPanelMetadata(
		CursesPanel panel,
		CursesVirtualScreen destination
	) {
		GetIntersection(
			panel,
			destination,
			out int sourceRowEnd,
			out int sourceColumnEnd
		);
		if ( 0 >= sourceRowEnd
			|| 0 >= sourceColumnEnd ) {
			return;
		}

		for ( int sourceRow = 0; sourceRow < sourceRowEnd; sourceRow++ ) {
			for ( int sourceColumn = 0; sourceColumn < sourceColumnEnd; sourceColumn++ ) {
				CursesCell cell = panel.VirtualScreen.GetCell(
					sourceRow,
					sourceColumn
				);
				if ( IsTransparent(
					panel,
					cell
				) ) {
					continue;
				}

				CursesCellMetadata? metadata = panel.VirtualScreen.GetMetadata(
					sourceRow,
					sourceColumn
				);
				if ( metadata is null ) {
					continue;
				}

				destination.SetMetadata(
					panel.Row + sourceRow,
					panel.Column + sourceColumn,
					metadata
				);
			}
		}
	}

	private static void GetIntersection(
		CursesPanel panel,
		CursesVirtualScreen destination,
		out int sourceRowEnd,
		out int sourceColumnEnd
	) {
		sourceRowEnd = Math.Min(
			panel.Rows,
			destination.Rows - panel.Row
		);
		sourceColumnEnd = Math.Min(
			panel.Columns,
			destination.Columns - panel.Column
		);
	}

	private static bool IsTransparent(
		CursesPanel panel,
		CursesCell cell
	) {
		return CursesPanelTransparency.BlankCellsTransparent == panel.Transparency
			&& cell.IsBlank;
	}
}
