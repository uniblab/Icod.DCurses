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

/// <summary>Describes one interaction region before it is registered with a router.</summary>
public sealed class CursesInteractionRegionOptions {
	/// <summary>Initializes interaction-region options with the declared rectangle.</summary>
	/// <param name="bounds">
	/// The region rectangle. It is screen-relative when <see cref="Panel"/> is null and
	/// panel-relative otherwise.
	/// </param>
	public CursesInteractionRegionOptions(
		CursesRectangle bounds
	) {
		this.Bounds = bounds;
	}

	/// <summary>Gets the declared region rectangle.</summary>
	public CursesRectangle Bounds {
		get;
		init;
	}

	/// <summary>Gets the optional panel whose retained coordinate space owns <see cref="Bounds"/>.</summary>
	public CursesPanel? Panel {
		get;
		init;
	}

	/// <summary>Gets whether the region initially participates in interaction routing.</summary>
	public bool IsEnabled {
		get;
		init;
	} = true;

	/// <summary>Gets whether the region initially participates in logical focus traversal.</summary>
	public bool IsFocusable {
		get;
		init;
	}

	/// <summary>Gets the initial logical-focus traversal order.</summary>
	public int TraversalOrder {
		get;
		init;
	}

	/// <summary>Gets the initial same-surface hit-test priority.</summary>
	public int HitTestPriority {
		get;
		init;
	}
}
