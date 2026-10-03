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

namespace Icod.DCurses;

/// <summary>Retained raster-atlas projection helpers.</summary>
public sealed partial class CursesWindow {
	/// <summary>Writes a rectangular atlas-cell region into this logical window.</summary>
	public void WriteRasterAtlas(
		int row,
		int column,
		CursesRasterAtlas atlas,
		CursesRectangle sourceRectangle
	) {
		ArgumentNullException.ThrowIfNull( atlas );
		if ( sourceRectangle.BottomExclusive > atlas.Rows
			|| sourceRectangle.RightExclusive > atlas.Columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( sourceRectangle ),
				"The source rectangle must lie wholly inside the raster atlas."
			);
		}
		if ( row < 0 || row > Rows - sourceRectangle.Rows ) {
			throw new ArgumentOutOfRangeException(
				nameof( row ),
				"The destination rectangle must lie wholly inside the window."
			);
		}
		if ( column < 0 || column > Columns - sourceRectangle.Columns ) {
			throw new ArgumentOutOfRangeException(
				nameof( column ),
				"The destination rectangle must lie wholly inside the window."
			);
		}
		CursesSession session = screen.RasterSessionOwner
			?? throw new InvalidOperationException(
				"The window is not bound to a live raster session."
			);
		if ( !atlas.BelongsTo( session ) ) {
			throw new InvalidOperationException(
				"The raster atlas belongs to a different CursesSession."
			);
		}
		if ( CursesRasterOwnershipStatus.Current != atlas.OwnershipState.Status ) {
			throw new InvalidOperationException(
				"The raster atlas is not current and cannot be projected."
			);
		}

		CursesRasterCell[] cells = new CursesRasterCell[
			checked( sourceRectangle.Rows * sourceRectangle.Columns )
		];
		int index = 0;
		for ( int sourceRow = 0; sourceRow < sourceRectangle.Rows; sourceRow++ ) {
			for ( int sourceColumn = 0; sourceColumn < sourceRectangle.Columns; sourceColumn++ ) {
				cells[ index++ ] = atlas.GetCell(
					sourceRectangle.Row + sourceRow,
					sourceRectangle.Column + sourceColumn
				);
			}
		}

		index = 0;
		for ( int targetRow = 0; targetRow < sourceRectangle.Rows; targetRow++ ) {
			for ( int targetColumn = 0; targetColumn < sourceRectangle.Columns; targetColumn++ ) {
				SetRasterCell(
					row + targetRow,
					column + targetColumn,
					cells[ index++ ]
				);
			}
		}
	}
}
