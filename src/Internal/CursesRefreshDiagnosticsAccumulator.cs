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

/// <summary>Collects bounded semantic observations for one enabled refresh attempt.</summary>
internal sealed class CursesRefreshDiagnosticsAccumulator {
	internal bool IsFullRepaint;
	internal bool PhysicalStateInvalidated;
	internal bool LogicalStatePublished;
	internal int LogicalCellsExamined;
	internal int LogicalCellsChanged;
	internal int DamagedRows;
	internal int DamagedRegions;
	internal int PreparedOutputItemCount;
	internal int ApplicationPayloadCount;
	internal int RasterPlaceholderCellCount;
	internal CursesRefreshOperationKinds OperationKinds;

	internal static int Increment( int value ) => value == int.MaxValue ? value : value + 1;

	internal void RecordItem( CursesRefreshOperationKinds kind ) {
		PreparedOutputItemCount = Increment( PreparedOutputItemCount );
		OperationKinds |= kind;
	}

	internal void RecordPayload( CursesRefreshOperationKinds kind ) {
		this.RecordItem( kind );
		ApplicationPayloadCount = Increment( ApplicationPayloadCount );
	}

	internal void RecordRasterCells( int count ) {
		this.RecordItem( CursesRefreshOperationKinds.Raster );
		RasterPlaceholderCellCount = (int)Math.Min( int.MaxValue,
			(long)RasterPlaceholderCellCount + count );
	}

	internal CursesRefreshDiagnosticsSnapshot Snapshot( long sequence, CursesRefreshOutcome outcome ) =>
		new( sequence, outcome, IsFullRepaint, PhysicalStateInvalidated,
			LogicalStatePublished, LogicalCellsExamined, LogicalCellsChanged,
			DamagedRows, DamagedRegions, PreparedOutputItemCount,
			ApplicationPayloadCount, RasterPlaceholderCellCount, OperationKinds );
}
