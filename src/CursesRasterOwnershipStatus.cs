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

/// <summary>Identifies DCurses' current view of retained raster ownership.</summary>
public enum CursesRasterOwnershipStatus {
	/// <summary>The wrapped Terminal ownership remains current for its live session generation.</summary>
	Current = 0,

	/// <summary>The terminal-resident ownership certainty has been lost.</summary>
	Stale = 1,

	/// <summary>The ownership relationship has been released by another owner or lifecycle transition.</summary>
	Released = 2,

	/// <summary>The DCurses ownership facade itself has been explicitly disposed.</summary>
	Disposed = 3
}
