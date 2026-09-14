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

/// <summary>Represents one immutable pointer target with signed region-local coordinates.</summary>
public sealed class CursesPointerTarget {
	internal CursesPointerTarget(
		CursesInteractionRegion region,
		int localRow,
		int localColumn,
		bool isInside
	) {
		ArgumentNullException.ThrowIfNull( region );
		this.Region = region;
		this.LocalRow = localRow;
		this.LocalColumn = localColumn;
		this.IsInside = isInside;
	}

	/// <summary>Gets the interaction region which owns this pointer target.</summary>
	public CursesInteractionRegion Region {
		get;
	}

	/// <summary>Gets the signed row relative to the declared region origin.</summary>
	public int LocalRow {
		get;
	}

	/// <summary>Gets the signed column relative to the declared region origin.</summary>
	public int LocalColumn {
		get;
	}

	/// <summary>Gets whether the pointer lies inside the region's current effective hit area.</summary>
	public bool IsInside {
		get;
	}
}
