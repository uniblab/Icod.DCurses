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
/// Captures one retained logical cell together with its optional semantic metadata for temporary
/// editing, composition, and structural-copy operations.
/// </summary>
/// <remarks>
/// This is intentionally an internal transient value. The stable public <see cref="CursesCell"/>
/// representation remains independent of surface-owned semantic metadata.
/// </remarks>
internal readonly record struct CursesLogicalCellState(
	CursesCell Cell,
	CursesCellMetadata? Metadata
);
