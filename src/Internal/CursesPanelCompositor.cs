/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
		CopyBaseRaster(
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
			OverlayPanelRaster(
				panel,
				result
			);
		}

		CursesCellFootprint.Repair( result );
		return result;
	}

	/// <summary>Resolves one destination coordinate against the base frame and visible panel order.</summary>
	/// <param name="baseScreen">The destination screen's uncomposed base frame.</param>
	/// <param name="panels">The complete remembered panel order from bottom to top.</param>
	/// <param name="row">The zero-based destination row.</param>
	/// <param name="column">The zero-based destination column.</param>
	/// <returns>The logical cell, semantic metadata, and retained raster contributed at the coordinate.</returns>
	internal static CursesLogicalCellState ResolveCell(
		CursesVirtualScreen baseScreen,
		CursesPanel[] panels,
		int row,
		int column
	) {
		ArgumentNullException.ThrowIfNull( baseScreen );
		ArgumentNullException.ThrowIfNull( panels );

		CursesLogicalCellState result = new(
			baseScreen.GetCell(
				row,
				column
			),
			baseScreen.GetMetadata(
				row,
				column
			),
			baseScreen.GetRasterCell(
				row,
				column
			)
		);
		foreach ( CursesPanel panel in panels ) {
			if ( panel is null ) {
				throw new ArgumentException(
					"The panel order contains a null panel.",
					nameof( panels )
				);
			}
			if ( !panel.IsVisible
				|| row < panel.Row
				|| column < panel.Column
				|| row >= panel.Row + panel.Rows
				|| column >= panel.Column + panel.Columns ) {
				continue;
			}

			int sourceRow = row - panel.Row;
			int sourceColumn = column - panel.Column;
			CursesVirtualScreen panelScreen = panel.VirtualScreen;
			CursesCell cell = panelScreen.GetCell(
				sourceRow,
				sourceColumn
			);
			CursesRasterCell? raster = panelScreen.GetRasterCell(
				sourceRow,
				sourceColumn
			);
			if ( IsTransparent(
				panel,
				cell,
				raster
			) ) {
				continue;
			}

			result = new CursesLogicalCellState(
				cell,
				panelScreen.GetMetadata(
					sourceRow,
					sourceColumn
				),
				raster
			);
		}
		return result;
	}

	/// <summary>Gets whether one panel coordinate is transparent to lower logical layers.</summary>
	internal static bool IsTransparent(
		CursesPanel panel,
		CursesCell cell,
		CursesRasterCell? raster
	) {
		ArgumentNullException.ThrowIfNull( panel );
		return CursesPanelTransparency.BlankCellsTransparent == panel.Transparency
			&& cell.IsBlank
			&& !raster.HasValue;
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

	private static void CopyBaseRaster(
		CursesVirtualScreen source,
		CursesVirtualScreen destination
	) {
		for ( int row = 0; row < source.Rows; row++ ) {
			for ( int column = 0; column < source.Columns; column++ ) {
				CursesRasterCell? raster = source.GetRasterCell(
					row,
					column
				);
				if ( !raster.HasValue ) {
					continue;
				}

				destination.SetRasterCell(
					row,
					column,
					raster.Value
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
				CursesVirtualScreen source = panel.VirtualScreen;
				CursesCell cell = source.GetCell(
					sourceRow,
					sourceColumn
				);
				CursesRasterCell? raster = source.GetRasterCell(
					sourceRow,
					sourceColumn
				);
				if ( IsTransparent(
					panel,
					cell,
					raster
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
				CursesVirtualScreen source = panel.VirtualScreen;
				CursesCell cell = source.GetCell(
					sourceRow,
					sourceColumn
				);
				CursesRasterCell? raster = source.GetRasterCell(
					sourceRow,
					sourceColumn
				);
				if ( IsTransparent(
					panel,
					cell,
					raster
				) ) {
					continue;
				}

				CursesCellMetadata? metadata = source.GetMetadata(
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

	private static void OverlayPanelRaster(
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
				CursesRasterCell? raster = panel.VirtualScreen.GetRasterCell(
					sourceRow,
					sourceColumn
				);
				if ( !raster.HasValue ) {
					continue;
				}

				destination.SetRasterCell(
					panel.Row + sourceRow,
					panel.Column + sourceColumn,
					raster.Value
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
}
