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

/// <summary>Identifies one command-sequence processing outcome.</summary>
public enum CursesCommandSequenceResultKind {
	/// <summary>The input was processed through ordinary interaction routing.</summary>
	Fallback = 0,

	/// <summary>The input extended an incomplete command sequence.</summary>
	Pending = 1,

	/// <summary>The input completed one command sequence.</summary>
	Completed = 2,

	/// <summary>The input did not extend the pending sequence and was routed once as fallback.</summary>
	Mismatch = 3
}
