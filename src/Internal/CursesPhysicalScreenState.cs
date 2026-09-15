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

	internal CursesPhysicalScreenState(int columns, int rows) {
		if (columns <= 0) { throw new ArgumentOutOfRangeException(nameof(columns)); }
		if (rows <= 0) { throw new ArgumentOutOfRangeException(nameof(rows)); }
		long cellCount = (long)columns * rows;
		if (int.MaxValue < cellCount) { throw new ArgumentOutOfRangeException(nameof(rows)); }
		Columns = columns;
		Rows = rows;
		cells = new CursesCell[(int)cellCount];
		knownCells = new bool[(int)cellCount];
	}

	internal int Columns { get; }
	internal int Rows { get; }
	internal int SemanticMetadataCount => semanticMetadata?.Count ?? 0;
	internal int RasterCellCount => retainedRaster?.Count ?? 0;

	internal bool TryGetCell(int row, int column, out CursesCell cell) {
		int offset = GetOffset(row, column);
		if (!knownCells[offset]) { cell = default; return false; }
		cell = cells[offset];
		return true;
	}

	internal CursesCellMetadata? GetMetadata(int row, int column) {
		int offset = GetOffset(row, column);
		if (!knownCells[offset]) { throw new InvalidOperationException("Semantic metadata cannot be read for an unknown physical coordinate."); }
		return semanticMetadata?.Get(row, column);
	}

	internal CursesRasterCell? GetRasterCell(int row, int column) {
		int offset = GetOffset(row, column);
		if (!knownCells[offset]) { throw new InvalidOperationException("Raster state cannot be read for an unknown physical coordinate."); }
		return retainedRaster?.Get(row, column)?.Cell;
	}

	internal void SetCell(int row, int column, CursesCell cell) {
		SetCell(row, column, cell, metadata: null, rasterCell: null);
	}

	internal void SetCell(int row, int column, CursesCell cell, CursesCellMetadata? metadata) {
		SetCell(row, column, cell, metadata, rasterCell: null);
	}

	internal void SetCell(int row, int column, CursesCell cell, CursesCellMetadata? metadata, CursesRasterCell? rasterCell) {
		int offset = GetOffset(row, column);
		if (rasterCell.HasValue && !rasterCell.Value.IsValid) {
			throw new ArgumentException("The default CursesRasterCell value cannot be retained.", nameof(rasterCell));
		}
		cells[offset] = cell;
		knownCells[offset] = true;
		SetMetadata(row, column, metadata);
		SetRasterCell(row, column, rasterCell);
	}

	internal void Invalidate() {
		Array.Clear(knownCells);
		semanticMetadata = null;
		retainedRaster = null;
	}

	private void SetMetadata(int row, int column, CursesCellMetadata? metadata) {
		if (metadata is null) {
			if (semanticMetadata is null) { return; }
			semanticMetadata.Set(row, column, null);
			if (semanticMetadata.IsEmpty) { semanticMetadata = null; }
			return;
		}
		semanticMetadata ??= new CursesSparseCellPlane<CursesCellMetadata>(Columns, Rows);
		semanticMetadata.Set(row, column, metadata);
	}

	private void SetRasterCell(int row, int column, CursesRasterCell? rasterCell) {
		if (!rasterCell.HasValue) {
			if (retainedRaster is null) { return; }
			retainedRaster.Set(row, column, null);
			if (retainedRaster.IsEmpty) { retainedRaster = null; }
			return;
		}
		retainedRaster ??= new CursesSparseCellPlane<CursesRasterCellReference>(Columns, Rows);
		retainedRaster.Set(row, column, new CursesRasterCellReference(rasterCell.Value));
	}

	private int GetOffset(int row, int column) {
		if (row < 0 || row >= Rows) { throw new ArgumentOutOfRangeException(nameof(row)); }
		if (column < 0 || column >= Columns) { throw new ArgumentOutOfRangeException(nameof(column)); }
		return (row * Columns) + column;
	}
}
