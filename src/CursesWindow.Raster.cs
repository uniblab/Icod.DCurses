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

/// <summary>Retained raster operations for logical windows.</summary>
public sealed partial class CursesWindow {
	/// <summary>Gets one retained raster cell at a window-local coordinate.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <returns>
	/// The retained raster cell, or <see langword="null"/> when none is associated or the valid local
	/// coordinate is temporarily clipped outside the owning screen.
	/// </returns>
	public CursesRasterCell? GetRasterCell(
		int row,
		int column
	) {
		ValidateCoordinate(
			row,
			column
		);
		return TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		)
			? screen.VirtualScreen.GetRasterCell(
				screenRow,
				screenColumn
			)
			: null
		;
	}

	/// <summary>Associates one retained raster cell with a window-local coordinate.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <param name="rasterCell">The raster cell to retain, or <see langword="null"/> to remove it.</param>
	public void SetRasterCell(
		int row,
		int column,
		CursesRasterCell? rasterCell
	) {
		ValidateCoordinate(
			row,
			column
		);
		ValidateRasterCell( rasterCell );
		if ( TryMapToScreen(
			row,
			column,
			out int screenRow,
			out int screenColumn
		) ) {
			screen.VirtualScreen.SetRasterCell(
				screenRow,
				screenColumn,
				rasterCell
			);
		}
	}

	/// <summary>Writes one retained raster cell at the current logical cursor position.</summary>
	/// <param name="rasterCell">The valid raster cell to retain.</param>
	/// <remarks>
	/// The visual/text cell and semantic metadata at the coordinate remain unchanged. Cursor movement
	/// follows the existing one-column wrap/scroll policy.
	/// </remarks>
	public void WriteRasterCell( CursesRasterCell rasterCell ) {
		ValidateRasterCell( rasterCell );
		if ( TryMapToScreen(
			cursorRow,
			cursorColumn,
			out int screenRow,
			out int screenColumn
		) ) {
			screen.VirtualScreen.SetRasterCell(
				screenRow,
				screenColumn,
				rasterCell
			);
		}
		_ = AdvanceColumns( 1 );
	}

	private static void ValidateRasterCell( CursesRasterCell? rasterCell ) {
		if ( rasterCell.HasValue
			&& !rasterCell.Value.IsValid ) {
			throw new ArgumentException(
				"The default CursesRasterCell value cannot be retained.",
				nameof( rasterCell )
			);
		}
	}

	private static void ValidateRasterCell( CursesRasterCell rasterCell ) {
		if ( !rasterCell.IsValid ) {
			throw new ArgumentException(
				"The default CursesRasterCell value cannot be retained.",
				nameof( rasterCell )
			);
		}
	}
}
