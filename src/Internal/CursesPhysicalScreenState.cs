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

/// <summary>
/// Tracks the last physical-screen image known by the refresh layer.
/// </summary>
/// <remarks>
/// This state is intentionally separate from <see cref="CursesVirtualScreen"/>. Unknown physical cells
/// must never be mistaken for application-requested blank cells.
/// </remarks>
internal sealed class CursesPhysicalScreenState {
	private readonly CursesCell[] cells;
	private readonly bool[] knownCells;
	private CursesSparseCellPlane<CursesCellMetadata>? semanticMetadata;
	private CursesSparseCellPlane<CursesRasterCellReference>? retainedRaster;

	/// <summary>Initializes physical-screen state with every cell initially unknown.</summary>
	/// <param name="columns">The positive terminal column count.</param>
	/// <param name="rows">The positive terminal row count.</param>
	internal CursesPhysicalScreenState(
		int columns,
		int rows
	) {
		if ( columns <= 0 ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		if ( rows <= 0 ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		long cellCount = (long)columns * rows;
		if ( int.MaxValue < cellCount ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}

		Columns = columns;
		Rows = rows;
		cells = new CursesCell[ (int)cellCount ];
		knownCells = new bool[ (int)cellCount ];
	}

	private CursesPhysicalScreenState(
		CursesPhysicalScreenState source
	) {
		ArgumentNullException.ThrowIfNull( source );

		Columns = source.Columns;
		Rows = source.Rows;
		cells = (CursesCell[])source.cells.Clone();
		knownCells = (bool[])source.knownCells.Clone();
		if ( source.semanticMetadata is not null ) {
			semanticMetadata = new CursesSparseCellPlane<CursesCellMetadata>(
				Columns,
				Rows
			);
			for ( int row = 0; row < Rows; row++ ) {
				semanticMetadata.ReplaceRow(
					row,
					source.semanticMetadata.SnapshotRow( row )
				);
			}
		}
		if ( source.retainedRaster is not null ) {
			retainedRaster = new CursesSparseCellPlane<CursesRasterCellReference>(
				Columns,
				Rows
			);
			for ( int row = 0; row < Rows; row++ ) {
				retainedRaster.ReplaceRow(
					row,
					source.retainedRaster.SnapshotRow( row )
				);
			}
		}
	}

	/// <summary>Gets the physical-screen column count.</summary>
	internal int Columns {
		get;
	}

	/// <summary>Gets the physical-screen row count.</summary>
	internal int Rows {
		get;
	}

	/// <summary>Gets the number of retained coordinates carrying semantic metadata.</summary>
	internal int SemanticMetadataCount => semanticMetadata?.Count ?? 0;

	/// <summary>Gets the number of retained coordinates carrying raster state.</summary>
	internal int RasterCellCount => retainedRaster?.Count ?? 0;

	/// <summary>Creates a detached copy preserving known and unknown coordinates.</summary>
	internal CursesPhysicalScreenState Clone() {
		return new CursesPhysicalScreenState( this );
	}

	/// <summary>Gets a known physical cell when one has been recorded.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">Receives the known physical cell when available.</param>
	/// <returns><see langword="true"/> when the physical cell is known.</returns>
	internal bool TryGetCell(
		int row,
		int column,
		out CursesCell cell
	) {
		int offset = GetOffset(
			row,
			column
		);

		if ( !knownCells[ offset ] ) {
			cell = default;
			return false;
		}

		cell = cells[ offset ];
		return true;
	}

	/// <summary>Gets retained physical semantic metadata for one known coordinate.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns>The retained semantic metadata, or <see langword="null"/> when the known coordinate is unlinked.</returns>
	internal CursesCellMetadata? GetMetadata(
		int row,
		int column
	) {
		int offset = GetOffset(
			row,
			column
		);
		if ( !knownCells[ offset ] ) {
			throw new InvalidOperationException(
				"Semantic metadata cannot be read for an unknown physical coordinate."
			);
		}

		return semanticMetadata?.Get(
			row,
			column
		);
	}

	/// <summary>Gets retained physical raster state for one known coordinate.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <returns>The retained raster cell, or <see langword="null"/> when none is known at the coordinate.</returns>
	internal CursesRasterCell? GetRasterCell(
		int row,
		int column
	) {
		int offset = GetOffset(
			row,
			column
		);
		if ( !knownCells[ offset ] ) {
			throw new InvalidOperationException(
				"Raster state cannot be read for an unknown physical coordinate."
			);
		}

		return retainedRaster?.Get(
			row,
			column
		)?.Cell;
	}

	/// <summary>Records one physical cell as known and without retained semantic or raster state.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">The physical cell value.</param>
	internal void SetCell(
		int row,
		int column,
		CursesCell cell
	) {
		SetCell(
			row,
			column,
			cell,
			metadata: null,
			rasterCell: null
		);
	}

	/// <summary>Records one physical cell and semantic value as known without retained raster state.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">The physical cell value.</param>
	/// <param name="metadata">The semantic metadata, or <see langword="null"/> for unlinked content.</param>
	internal void SetCell(
		int row,
		int column,
		CursesCell cell,
		CursesCellMetadata? metadata
	) {
		SetCell(
			row,
			column,
			cell,
			metadata,
			rasterCell: null
		);
	}

	/// <summary>Records one physical cell, semantic value, and retained raster value as known.</summary>
	/// <param name="row">The zero-based row.</param>
	/// <param name="column">The zero-based column.</param>
	/// <param name="cell">The physical cell value.</param>
	/// <param name="metadata">The semantic metadata, or <see langword="null"/> for unlinked content.</param>
	/// <param name="rasterCell">The retained raster cell, or <see langword="null"/> when none is physically represented.</param>
	internal void SetCell(
		int row,
		int column,
		CursesCell cell,
		CursesCellMetadata? metadata,
		CursesRasterCell? rasterCell
	) {
		int offset = GetOffset(
			row,
			column
		);
		if ( rasterCell.HasValue
			&& !rasterCell.Value.IsValid ) {
			throw new ArgumentException(
				"The default CursesRasterCell value cannot be retained.",
				nameof( rasterCell )
			);
		}

		cells[ offset ] = cell;
		knownCells[ offset ] = true;
		SetMetadata(
			row,
			column,
			metadata
		);
		SetRasterCell(
			row,
			column,
			rasterCell
		);
	}

	/// <summary>Marks every retained physical cell, semantic value, and raster value unknown.</summary>
	internal void Invalidate() {
		Array.Clear( knownCells );
		semanticMetadata = null;
		retainedRaster = null;
	}

	private void SetMetadata(
		int row,
		int column,
		CursesCellMetadata? metadata
	) {
		if ( metadata is null ) {
			if ( semanticMetadata is null ) {
				return;
			}
			semanticMetadata.Set(
				row,
				column,
				null
			);
			if ( semanticMetadata.IsEmpty ) {
				semanticMetadata = null;
			}
			return;
		}

		semanticMetadata ??= new CursesSparseCellPlane<CursesCellMetadata>(
			Columns,
			Rows
		);
		semanticMetadata.Set(
			row,
			column,
			metadata
		);
	}

	private void SetRasterCell(
		int row,
		int column,
		CursesRasterCell? rasterCell
	) {
		if ( !rasterCell.HasValue ) {
			if ( retainedRaster is null ) {
				return;
			}
			retainedRaster.Set(
				row,
				column,
				null
			);
			if ( retainedRaster.IsEmpty ) {
				retainedRaster = null;
			}
			return;
		}

		retainedRaster ??= new CursesSparseCellPlane<CursesRasterCellReference>(
			Columns,
			Rows
		);
		retainedRaster.Set(
			row,
			column,
			new CursesRasterCellReference( rasterCell.Value )
		);
	}

	private int GetOffset(
		int row,
		int column
	) {
		if ( row < 0 || row >= Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 || column >= Columns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}

		return ( row * Columns ) + column;
	}
}
