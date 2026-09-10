namespace Icod.DCurses.Internal;

/// <summary>Retains one screen's composed frame and independently observed producer revisions.</summary>
internal sealed class CursesPanelCompositionState {
	private readonly Dictionary<CursesPanel, PanelSnapshot> panelSnapshots = new(
		ReferenceEqualityComparer.Instance
	);
	private readonly List<int> damagedOffsets = new();
	private CursesVirtualScreen? sourceBaseScreen;
	private CursesVirtualScreen? composedScreen;
	private CursesPanel[] previousOrder = Array.Empty<CursesPanel>();
	private bool[]? damagedCells;
	private ulong baseRevision;

	/// <summary>Composes the current screen and panel state into a retained logical frame.</summary>
	/// <param name="screen">The screen whose logical layers should be composed.</param>
	/// <returns>The retained composed logical frame.</returns>
	internal CursesVirtualScreen Compose(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );

		CursesVirtualScreen currentBaseScreen = screen.VirtualScreen;
		CursesPanel[] currentOrder = screen.SnapshotPanelsBottomToTop();
		if ( composedScreen is null
			|| sourceBaseScreen is null
			|| !ReferenceEquals(
				sourceBaseScreen,
				currentBaseScreen
			)
			|| composedScreen.Columns != currentBaseScreen.Columns
			|| composedScreen.Rows != currentBaseScreen.Rows ) {
			return Initialize(
				currentBaseScreen,
				currentOrder
			);
		}

		currentBaseScreen.EnableChangeTracking();
		foreach ( CursesPanel panel in currentOrder ) {
			panel.VirtualScreen.EnableChangeTracking();
		}

		CollectBaseDamage( currentBaseScreen );
		CollectPanelDamage( currentOrder );
		RecomposeDamage(
			currentBaseScreen,
			currentOrder,
			composedScreen
		);
		CaptureState(
			currentBaseScreen,
			currentOrder
		);
		return composedScreen;
	}

	private CursesVirtualScreen Initialize(
		CursesVirtualScreen currentBaseScreen,
		CursesPanel[] currentOrder
	) {
		ArgumentNullException.ThrowIfNull( currentBaseScreen );
		ArgumentNullException.ThrowIfNull( currentOrder );

		currentBaseScreen.EnableChangeTracking();
		foreach ( CursesPanel panel in currentOrder ) {
			panel.VirtualScreen.EnableChangeTracking();
		}

		CursesVirtualScreen result = CursesPanelCompositor.Compose(
			currentBaseScreen,
			currentOrder
		);
		sourceBaseScreen = currentBaseScreen;
		composedScreen = result;
		damagedCells = new bool[ result.CellCount ];
		damagedOffsets.Clear();
		CaptureState(
			currentBaseScreen,
			currentOrder
		);
		return result;
	}

	private void CollectBaseDamage(
		CursesVirtualScreen currentBaseScreen
	) {
		ArgumentNullException.ThrowIfNull( currentBaseScreen );
		if ( currentBaseScreen.ChangeRevision == baseRevision ) {
			return;
		}

		for ( int row = 0; row < currentBaseScreen.Rows; row++ ) {
			for ( int column = 0; column < currentBaseScreen.Columns; column++ ) {
				if ( currentBaseScreen.GetCellChangeRevision(
					row,
					column
				) <= baseRevision ) {
					continue;
				}

				AddDamageCell(
					row,
					column,
					currentBaseScreen.Rows,
					currentBaseScreen.Columns
				);
			}
		}
	}

	private void CollectPanelDamage(
		CursesPanel[] currentOrder
	) {
		ArgumentNullException.ThrowIfNull( currentOrder );
		CursesVirtualScreen destination = composedScreen
			?? throw new InvalidOperationException(
				"The composed panel frame has not been initialized."
			);

		if ( OrderChanged( currentOrder ) ) {
			foreach ( CursesPanel panel in previousOrder ) {
				if ( panelSnapshots.TryGetValue(
					panel,
					out PanelSnapshot previous
				)
					&& previous.IsVisible ) {
					AddDamageRectangle(
						previous.Row,
						previous.Column,
						previous.Rows,
						previous.Columns,
						destination.Rows,
						destination.Columns
					);
				}
			}
			foreach ( CursesPanel panel in currentOrder ) {
				if ( panel.IsVisible ) {
					AddDamageRectangle(
						panel.Row,
						panel.Column,
						panel.Rows,
						panel.Columns,
						destination.Rows,
						destination.Columns
					);
				}
			}
		}

		foreach ( CursesPanel panel in previousOrder ) {
			if ( ContainsReference(
				currentOrder,
				panel
			) ) {
				continue;
			}
			if ( panelSnapshots.TryGetValue(
				panel,
				out PanelSnapshot removed
			)
				&& removed.IsVisible ) {
				AddDamageRectangle(
					removed.Row,
					removed.Column,
					removed.Rows,
					removed.Columns,
					destination.Rows,
					destination.Columns
				);
			}
		}

		foreach ( CursesPanel panel in currentOrder ) {
			if ( !panelSnapshots.TryGetValue(
				panel,
				out PanelSnapshot previous
			) ) {
				if ( panel.IsVisible ) {
					AddDamageRectangle(
						panel.Row,
						panel.Column,
						panel.Rows,
						panel.Columns,
						destination.Rows,
						destination.Columns
					);
				}
				continue;
			}

			bool positionChanged = previous.Row != panel.Row
				|| previous.Column != panel.Column;
			bool visibilityChanged = previous.IsVisible != panel.IsVisible;
			bool transparencyChanged = previous.Transparency != panel.Transparency;
			if ( positionChanged
				|| visibilityChanged
				|| transparencyChanged ) {
				if ( previous.IsVisible ) {
					AddDamageRectangle(
						previous.Row,
						previous.Column,
						previous.Rows,
						previous.Columns,
						destination.Rows,
						destination.Columns
					);
				}
				if ( panel.IsVisible ) {
					AddDamageRectangle(
						panel.Row,
						panel.Column,
						panel.Rows,
						panel.Columns,
						destination.Rows,
						destination.Columns
					);
				}
				continue;
			}

			ulong currentRevision = panel.VirtualScreen.ChangeRevision;
			if ( !panel.IsVisible
				|| currentRevision == previous.ContentRevision ) {
				continue;
			}

			CollectVisiblePanelCellDamage(
				panel,
				previous.ContentRevision,
				destination.Rows,
				destination.Columns
			);
		}
	}

	private void CollectVisiblePanelCellDamage(
		CursesPanel panel,
		ulong previousRevision,
		int destinationRows,
		int destinationColumns
	) {
		ArgumentNullException.ThrowIfNull( panel );
		CursesVirtualScreen source = panel.VirtualScreen;
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				if ( source.GetCellChangeRevision(
					row,
					column
				) <= previousRevision ) {
					continue;
				}

				AddDamageCell(
					panel.Row + row,
					panel.Column + column,
					destinationRows,
					destinationColumns
				);
			}
		}
	}

	private void RecomposeDamage(
		CursesVirtualScreen currentBaseScreen,
		CursesPanel[] currentOrder,
		CursesVirtualScreen destination
	) {
		ArgumentNullException.ThrowIfNull( currentBaseScreen );
		ArgumentNullException.ThrowIfNull( currentOrder );
		ArgumentNullException.ThrowIfNull( destination );

		try {
			foreach ( int offset in damagedOffsets ) {
				int row = offset / destination.Columns;
				int column = offset % destination.Columns;
				CursesLogicalCellState desired = CursesPanelCompositor.ResolveCell(
					currentBaseScreen,
					currentOrder,
					row,
					column
				);
				ApplyLogicalCellState(
					destination,
					row,
					column,
					desired
				);
			}

			CursesCellFootprint.Repair( destination );
		} finally {
			ClearDamage();
		}
	}

	private static void ApplyLogicalCellState(
		CursesVirtualScreen destination,
		int row,
		int column,
		CursesLogicalCellState desired
	) {
		ArgumentNullException.ThrowIfNull( destination );
		CursesCell currentCell = destination.GetCell(
			row,
			column
		);
		CursesCellMetadata? currentMetadata = destination.GetMetadata(
			row,
			column
		);
		if ( currentCell == desired.Cell
			&& Equals(
				currentMetadata,
				desired.Metadata
			) ) {
			return;
		}

		if ( currentCell != desired.Cell ) {
			destination.SetCell(
				row,
				column,
				desired.Cell
			);
			if ( desired.Metadata is not null ) {
				destination.SetMetadata(
					row,
					column,
					desired.Metadata
				);
			}
			return;
		}

		destination.SetMetadata(
			row,
			column,
			desired.Metadata
		);
	}

	private void CaptureState(
		CursesVirtualScreen currentBaseScreen,
		CursesPanel[] currentOrder
	) {
		ArgumentNullException.ThrowIfNull( currentBaseScreen );
		ArgumentNullException.ThrowIfNull( currentOrder );

		baseRevision = currentBaseScreen.ChangeRevision;
		panelSnapshots.Clear();
		foreach ( CursesPanel panel in currentOrder ) {
			panelSnapshots.Add(
				panel,
				PanelSnapshot.Capture( panel )
			);
		}
		previousOrder = currentOrder;
	}

	private bool OrderChanged(
		CursesPanel[] currentOrder
	) {
		ArgumentNullException.ThrowIfNull( currentOrder );
		if ( previousOrder.Length != currentOrder.Length ) {
			return true;
		}

		for ( int index = 0; index < currentOrder.Length; index++ ) {
			if ( !ReferenceEquals(
				previousOrder[ index ],
				currentOrder[ index ]
			) ) {
				return true;
			}
		}
		return false;
	}

	private static bool ContainsReference(
		CursesPanel[] panels,
		CursesPanel candidate
	) {
		ArgumentNullException.ThrowIfNull( panels );
		ArgumentNullException.ThrowIfNull( candidate );

		foreach ( CursesPanel panel in panels ) {
			if ( ReferenceEquals(
				panel,
				candidate
			) ) {
				return true;
			}
		}
		return false;
	}

	private void AddDamageRectangle(
		int row,
		int column,
		int rows,
		int columns,
		int destinationRows,
		int destinationColumns
	) {
		if ( 0 >= rows
			|| 0 >= columns
			|| 0 >= destinationRows
			|| 0 >= destinationColumns ) {
			return;
		}

		int rowStart = Math.Max(
			0,
			row
		);
		int rowEnd = Math.Min(
			destinationRows,
			row + rows
		);
		int columnStart = Math.Max(
			0,
			column
		);
		int columnEnd = Math.Min(
			destinationColumns,
			column + columns
		);
		for ( int currentRow = rowStart; currentRow < rowEnd; currentRow++ ) {
			for ( int currentColumn = columnStart; currentColumn < columnEnd; currentColumn++ ) {
				AddDamageCell(
					currentRow,
					currentColumn,
					destinationRows,
					destinationColumns
				);
			}
		}
	}

	private void AddDamageCell(
		int row,
		int column,
		int destinationRows,
		int destinationColumns
	) {
		if ( 0 > row
			|| row >= destinationRows ) {
			return;
		}

		for ( int haloColumn = column - 1; haloColumn <= column + 1; haloColumn++ ) {
			if ( 0 > haloColumn
				|| haloColumn >= destinationColumns ) {
				continue;
			}

			AddDamageOffset(
				( row * destinationColumns ) + haloColumn
			);
		}
	}

	private void AddDamageOffset(
		int offset
	) {
		bool[] flags = damagedCells
			?? throw new InvalidOperationException(
				"The composed panel damage map has not been initialized."
			);
		if ( flags[ offset ] ) {
			return;
		}

		flags[ offset ] = true;
		damagedOffsets.Add( offset );
	}

	private void ClearDamage() {
		bool[]? flags = damagedCells;
		if ( flags is not null ) {
			foreach ( int offset in damagedOffsets ) {
				flags[ offset ] = false;
			}
		}
		damagedOffsets.Clear();
	}

	private readonly record struct PanelSnapshot(
		int Row,
		int Column,
		int Rows,
		int Columns,
		bool IsVisible,
		CursesPanelTransparency Transparency,
		ulong ContentRevision
	) {
		internal static PanelSnapshot Capture(
			CursesPanel panel
		) {
			ArgumentNullException.ThrowIfNull( panel );
			return new PanelSnapshot(
				panel.Row,
				panel.Column,
				panel.Rows,
				panel.Columns,
				panel.IsVisible,
				panel.Transparency,
				panel.VirtualScreen.ChangeRevision
			);
		}
	}
}
