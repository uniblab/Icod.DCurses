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

/// <summary>Identifies the semantic outcome of one interaction-routing operation.</summary>
public enum CursesInteractionResultKind {
	/// <summary>The input was not routed to a region or command.</summary>
	Unrouted = 0,
	/// <summary>The input was routed to an interaction region without matching a command.</summary>
	Targeted = 1,
	/// <summary>The input matched a local or router-global command identity.</summary>
	Command = 2
}
