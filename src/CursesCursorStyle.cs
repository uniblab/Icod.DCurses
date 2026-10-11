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

/// <summary>Identifies one semantic terminal text-cursor style.</summary>
public enum CursesCursorStyle {
	/// <summary>A blinking block cursor.</summary>
	BlinkingBlock,

	/// <summary>A steady block cursor.</summary>
	SteadyBlock,

	/// <summary>A blinking underline cursor.</summary>
	BlinkingUnderline,

	/// <summary>A steady underline cursor.</summary>
	SteadyUnderline,

	/// <summary>A blinking vertical bar cursor.</summary>
	BlinkingBar,

	/// <summary>A steady vertical bar cursor.</summary>
	SteadyBar
}
