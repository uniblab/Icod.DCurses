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

/// <summary>Identifies the outcome of one raster-atlas presentation.</summary>
public enum CursesRasterAtlasPresentationStatus {
	/// <summary>The caller supplied no changes and no terminal I/O occurred.</summary>
	NoChanges = 0,

	/// <summary>The complete update was acknowledged and its frame selected.</summary>
	Presented = 1,

	/// <summary>A required operation is not implemented by the host.</summary>
	Unsupported = 2,

	/// <summary>A required operation is not available for the current endpoint.</summary>
	Unavailable = 3,

	/// <summary>A required operation failed in a controlled manner.</summary>
	Failed = 4
}
