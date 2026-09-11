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
/// Owns one panel-private logical surface while reusing the ordinary screen/window semantics.
/// </summary>
internal sealed class CursesPanelSurface {
	private readonly CursesScreen backingScreen;

	/// <summary>Initializes an independent retained surface for one panel.</summary>
	/// <param name="rows">The positive panel height.</param>
	/// <param name="columns">The positive panel width.</param>
	/// <param name="textWidthProvider">The destination screen's display-width policy.</param>
	internal CursesPanelSurface(
		int rows,
		int columns,
		ICursesTextWidthProvider textWidthProvider
	) {
		if ( 0 >= rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		ArgumentNullException.ThrowIfNull( textWidthProvider );

		backingScreen = new CursesScreen(
			columns,
			rows,
			textWidthProvider
		);
	}

	/// <summary>Gets the retained content window.</summary>
	internal CursesWindow ContentWindow => backingScreen.StandardWindow;

	/// <summary>Gets the backing virtual screen.</summary>
	internal CursesVirtualScreen VirtualScreen => backingScreen.VirtualScreen;

	/// <summary>Gets the panel height.</summary>
	internal int Rows => backingScreen.Rows;

	/// <summary>Gets the panel width.</summary>
	internal int Columns => backingScreen.Columns;

	/// <summary>Resizes the retained surface while preserving overlapping content and metadata.</summary>
	/// <param name="rows">The positive new panel height.</param>
	/// <param name="columns">The positive new panel width.</param>
	internal void Resize(
		int rows,
		int columns
	) {
		if ( 0 >= rows ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		backingScreen.Resize(
			columns,
			rows,
			preserveContents: true
		);
	}
}
