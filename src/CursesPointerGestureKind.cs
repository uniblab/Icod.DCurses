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

/// <summary>Identifies one deterministic clock-free pointer gesture phase.</summary>
public enum CursesPointerGestureKind {
	/// <summary>A concrete mouse button was pressed.</summary>
	Press = 0,

	/// <summary>A concrete mouse button was released without completing a click or drag.</summary>
	Release = 1,

	/// <summary>The pointer moved without beginning or continuing a drag.</summary>
	Move = 2,

	/// <summary>A same-target press/release completed without intervening cell movement.</summary>
	Click = 3,

	/// <summary>The first routed cell movement after a targeted concrete-button press.</summary>
	DragStart = 4,

	/// <summary>A later routed movement in an active drag.</summary>
	DragMove = 5,

	/// <summary>The concrete button ending an active drag was released.</summary>
	DragEnd = 6,

	/// <summary>The mouse wheel moved upward.</summary>
	WheelUp = 7,

	/// <summary>The mouse wheel moved downward.</summary>
	WheelDown = 8,

	/// <summary>The mouse wheel moved left.</summary>
	WheelLeft = 9,

	/// <summary>The mouse wheel moved right.</summary>
	WheelRight = 10
}
