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

/// <summary>Describes the outcome of one attempted refresh.</summary>
public enum CursesRefreshOutcome {
	/// <summary>The attempt committed its logical and physical work.</summary>
	Succeeded,
	/// <summary>The attempt was cancelled before logical publication.</summary>
	Cancelled,
	/// <summary>The attempt failed before logical publication.</summary>
	Failed
}

/// <summary>Classifies semantic output prepared by one refresh attempt.</summary>
[Flags]
public enum CursesRefreshOperationKinds {
	/// <summary>No semantic output was prepared.</summary>
	None = 0,
	/// <summary>Text content was prepared.</summary>
	Text = 1,
	/// <summary>Cursor movement or visibility was prepared.</summary>
	Cursor = 2,
	/// <summary>Rendition changes were prepared.</summary>
	Rendition = 4,
	/// <summary>Erasure was prepared.</summary>
	Erase = 8,
	/// <summary>Character insertion or deletion was prepared.</summary>
	CharacterShift = 16,
	/// <summary>Line insertion or deletion was prepared.</summary>
	LineShift = 32,
	/// <summary>Hyperlink presentation was prepared.</summary>
	Hyperlink = 64,
	/// <summary>Raster placeholders were prepared.</summary>
	Raster = 128,
	/// <summary>Synchronized-output framing was prepared.</summary>
	Synchronization = 256
}

/// <summary>Immutable DCurses logical and semantic work observed for one refresh attempt.</summary>
public sealed class CursesRefreshDiagnosticsSnapshot {
	internal CursesRefreshDiagnosticsSnapshot(
		long sequence,
		CursesRefreshOutcome outcome,
		bool isFullRepaint,
		bool physicalStateInvalidated,
		bool logicalStatePublished,
		int logicalCellsExamined,
		int logicalCellsChanged,
		int damagedRows,
		int damagedRegions,
		int preparedOutputItemCount,
		int applicationPayloadCount,
		int rasterPlaceholderCellCount,
		CursesRefreshOperationKinds operationKinds
	) {
		Sequence = sequence;
		Outcome = outcome;
		IsFullRepaint = isFullRepaint;
		PhysicalStateInvalidated = physicalStateInvalidated;
		LogicalStatePublished = logicalStatePublished;
		LogicalCellsExamined = logicalCellsExamined;
		LogicalCellsChanged = logicalCellsChanged;
		DamagedRows = damagedRows;
		DamagedRegions = damagedRegions;
		PreparedOutputItemCount = preparedOutputItemCount;
		ApplicationPayloadCount = applicationPayloadCount;
		RasterPlaceholderCellCount = rasterPlaceholderCellCount;
		OperationKinds = operationKinds;
	}

	/// <summary>Gets the one-based refresh-attempt sequence.</summary>
	public long Sequence { get; }

	/// <summary>Gets whether the attempt succeeded, was cancelled, or failed.</summary>
	public CursesRefreshOutcome Outcome { get; }

	/// <summary>Gets whether the attempt required a full physical repaint.</summary>
	public bool IsFullRepaint { get; }

	/// <summary>Gets whether physical-screen certainty was invalidated.</summary>
	public bool PhysicalStateInvalidated { get; }

	/// <summary>Gets whether the desired logical state was published.</summary>
	public bool LogicalStatePublished { get; }

	/// <summary>Gets the number of logical cells examined.</summary>
	public int LogicalCellsExamined { get; }

	/// <summary>Gets the number of logical cells changed.</summary>
	public int LogicalCellsChanged { get; }

	/// <summary>Gets the number of damaged rows considered.</summary>
	public int DamagedRows { get; }

	/// <summary>Gets the number of damaged regions considered.</summary>
	public int DamagedRegions { get; }

	/// <summary>Gets the number of semantic output items prepared.</summary>
	public int PreparedOutputItemCount { get; }

	/// <summary>Gets the number of application payloads prepared.</summary>
	public int ApplicationPayloadCount { get; }

	/// <summary>Gets the number of raster placeholder cells prepared.</summary>
	public int RasterPlaceholderCellCount { get; }

	/// <summary>Gets the union of semantic operation categories prepared.</summary>
	public CursesRefreshOperationKinds OperationKinds { get; }
}
