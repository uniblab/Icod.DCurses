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

/// <summary>Prepared-cell bulk mutation for logical windows.</summary>
public sealed partial class CursesWindow {
	/// <summary>Writes one complete in-bounds row slice without changing the cursor.</summary>
	public void WriteCells(
		int row,
		int column,
		ReadOnlySpan<CursesCell> cells
	) {
		WriteCells(
			row,
			column,
			1,
			cells.Length,
			cells,
			cells.Length
		);
	}

	/// <summary>Writes one complete in-bounds rectangular prepared-cell block without changing the cursor.</summary>
	public void WriteCells(
		int row,
		int column,
		int rows,
		int columns,
		ReadOnlySpan<CursesCell> cells,
		int sourceStride
	) {
		if ( 0 > row || (long)row + rows > Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column || (long)column + columns > Columns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 > columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( sourceStride < columns ) {
			throw new ArgumentOutOfRangeException( nameof( sourceStride ) );
		}

		long requiredLength = 0 == rows || 0 == columns
			? 0
			: (long)( rows - 1 ) * sourceStride + columns
		;
		if ( requiredLength > int.MaxValue
			|| requiredLength > cells.Length ) {
			throw new ArgumentException(
				"The source span does not contain the complete prepared-cell block.",
				nameof( cells )
			);
		}

		// Validate every footprint before changing any retained value.
		for ( int sourceRow = 0; sourceRow < rows && 0 < columns; sourceRow++ ) {
			ReadOnlySpan<CursesCell> source = cells.Slice(
				sourceRow * sourceStride,
				columns
			);
			for ( int sourceColumn = 0; sourceColumn < columns; sourceColumn++ ) {
				CursesCell cell = source[ sourceColumn ];
				if ( cell.IsContinuation ) {
					throw new ArgumentException(
						"A source row cannot contain an orphan continuation cell.",
						nameof( cells )
					);
				}
				if ( 2 == cell.DisplayWidth ) {
					if ( sourceColumn + 1 >= columns
						|| !source[ sourceColumn + 1 ].IsContinuation
						|| source[ sourceColumn + 1 ].Style != cell.Style ) {
						throw new ArgumentException(
							"A wide source cell requires a matching continuation within its row.",
							nameof( cells )
						);
					}
					sourceColumn++;
				}
			}
		}

		for ( int targetRow = 0; targetRow < rows && 0 < columns; targetRow++ ) {
			ReadOnlySpan<CursesCell> source = cells.Slice(
				targetRow * sourceStride,
				columns
			);
			for ( int targetColumn = 0; targetColumn < columns; targetColumn++ ) {
				SetCellIfVisible(
					row + targetRow,
					column + targetColumn,
					source[ targetColumn ]
				);
			}
		}
	}
}
