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

/// <summary>Holds detached physical certainty while one refresh is prepared and committed.</summary>
internal sealed class CursesRefreshPhysicalState {
	internal CursesRefreshPhysicalState(
		CursesPhysicalScreenState screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		this.Screen = screen;
	}

	internal CursesPhysicalScreenState Screen {
		get;
	}

	internal CursesStyle? CurrentStyle {
		get;
		set;
	}

	internal int? CursorRow {
		get;
		set;
	}

	internal int? CursorColumn {
		get;
		set;
	}

	internal CursesRefreshPhysicalState Clone() {
		return new CursesRefreshPhysicalState( this.Screen.Clone() ) {
			CurrentStyle = this.CurrentStyle,
			CursorRow = this.CursorRow,
			CursorColumn = this.CursorColumn
		};
	}

	internal void Invalidate() {
		this.Screen.Invalidate();
		this.CurrentStyle = null;
		this.CursorRow = null;
		this.CursorColumn = null;
	}
}
