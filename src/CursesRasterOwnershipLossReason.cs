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

/// <summary>Explains why retained raster ownership is no longer current.</summary>
public enum CursesRasterOwnershipLossReason {
	/// <summary>No ownership loss has occurred.</summary>
	None = 0,

	/// <summary>The canonical terminal session lost generation/state certainty.</summary>
	SessionStateLost = 1,

	/// <summary>The terminal reported or implied that the underlying resource is missing.</summary>
	ResourceMissing = 2,

	/// <summary>A parent placement relationship was lost.</summary>
	ParentPlacementLost = 3,

	/// <summary>An ancestor ownership relationship was released.</summary>
	AncestorReleased = 4,

	/// <summary>The owning raster resource was released.</summary>
	ResourceReleased = 5,

	/// <summary>The public ownership facade was explicitly disposed.</summary>
	ExplicitDisposal = 6
}
