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

/// <summary>Identifies one semantic terminal mouse-pointer shape.</summary>
public enum CursesPointerShape {
	/// <summary>The CSS <c>alias</c> pointer shape.</summary>
	Alias = 0,

	/// <summary>The CSS <c>cell</c> pointer shape.</summary>
	Cell = 1,

	/// <summary>The CSS <c>copy</c> pointer shape.</summary>
	Copy = 2,

	/// <summary>The CSS <c>crosshair</c> pointer shape.</summary>
	Crosshair = 3,

	/// <summary>The CSS <c>default</c> pointer shape.</summary>
	Default = 4,

	/// <summary>The CSS east-resize pointer shape.</summary>
	EastResize = 5,

	/// <summary>The CSS east-west-resize pointer shape.</summary>
	EastWestResize = 6,

	/// <summary>The CSS <c>grab</c> pointer shape.</summary>
	Grab = 7,

	/// <summary>The CSS <c>grabbing</c> pointer shape.</summary>
	Grabbing = 8,

	/// <summary>The CSS <c>help</c> pointer shape.</summary>
	Help = 9,

	/// <summary>The CSS <c>move</c> pointer shape.</summary>
	Move = 10,

	/// <summary>The CSS north-resize pointer shape.</summary>
	NorthResize = 11,

	/// <summary>The CSS north-east-resize pointer shape.</summary>
	NorthEastResize = 12,

	/// <summary>The CSS north-east/south-west-resize pointer shape.</summary>
	NorthEastSouthWestResize = 13,

	/// <summary>The CSS <c>no-drop</c> pointer shape.</summary>
	NoDrop = 14,

	/// <summary>The CSS <c>not-allowed</c> pointer shape.</summary>
	NotAllowed = 15,

	/// <summary>The CSS north-south-resize pointer shape.</summary>
	NorthSouthResize = 16,

	/// <summary>The CSS north-west-resize pointer shape.</summary>
	NorthWestResize = 17,

	/// <summary>The CSS north-west/south-east-resize pointer shape.</summary>
	NorthWestSouthEastResize = 18,

	/// <summary>The CSS <c>pointer</c> pointer shape.</summary>
	Pointer = 19,

	/// <summary>The CSS <c>progress</c> pointer shape.</summary>
	Progress = 20,

	/// <summary>The CSS south-resize pointer shape.</summary>
	SouthResize = 21,

	/// <summary>The CSS south-east-resize pointer shape.</summary>
	SouthEastResize = 22,

	/// <summary>The CSS south-west-resize pointer shape.</summary>
	SouthWestResize = 23,

	/// <summary>The CSS <c>text</c> pointer shape.</summary>
	Text = 24,

	/// <summary>The CSS <c>vertical-text</c> pointer shape.</summary>
	VerticalText = 25,

	/// <summary>The CSS west-resize pointer shape.</summary>
	WestResize = 26,

	/// <summary>The CSS <c>wait</c> pointer shape.</summary>
	Wait = 27,

	/// <summary>The CSS <c>zoom-in</c> pointer shape.</summary>
	ZoomIn = 28,

	/// <summary>The CSS <c>zoom-out</c> pointer shape.</summary>
	ZoomOut = 29
}
