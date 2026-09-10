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

/// <summary>
/// Owns a large off-screen logical cell surface whose contents can be edited through a normal
/// <see cref="CursesWindow"/>.
/// </summary>
/// <remarks>
/// A pad does not own a terminal session or perform physical terminal output. Its content window reuses
/// the standard DCurses Unicode, cell, editing, composition, drawing, and damage semantics. Selected pad
/// rectangles can be projected into ordinary destination windows through <see cref="PresentTo"/> or an
/// independently pannable <see cref="CursesPadViewport"/>.
/// </remarks>
public sealed class CursesPad {
	private readonly CursesScreen backingScreen;

	/// <summary>Initializes a blank off-screen pad.</summary>
	/// <param name="columns">The positive number of pad columns.</param>
	/// <param name="rows">The positive number of pad rows.</param>
	/// <param name="textWidthProvider">Optional terminal display-width policy used by the pad content.</param>
	public CursesPad(
		int columns,
		int rows,
		ICursesTextWidthProvider? textWidthProvider = null
	) {
		backingScreen = new CursesScreen(
			columns,
			rows,
			textWidthProvider
		);
		backingScreen.VirtualScreen.EnableChangeTracking();
		ContentWindow = backingScreen.StandardWindow;
	}

	/// <summary>Gets the number of columns in the off-screen pad.</summary>
	public int Columns => backingScreen.Columns;

	/// <summary>Gets the number of rows in the off-screen pad.</summary>
	public int Rows => backingScreen.Rows;

	/// <summary>Gets the text-width policy used by the pad content window.</summary>
	public ICursesTextWidthProvider TextWidthProvider => backingScreen.TextWidthProvider;

	/// <summary>
	/// Gets the standard content window covering the complete off-screen pad.
	/// </summary>
	/// <remarks>
	/// Cursor state is local to this window. Callers can use the established <see cref="CursesWindow"/>
	/// APIs directly, including <see cref="CursesWindow.CreateSubwindow(int,int,int,int)"/> when a shared
	/// derived view into the pad is needed.
	/// </remarks>
	public CursesWindow ContentWindow {
		get;
	}

	/// <summary>Projects one rectangular pad viewport into an ordinary destination window.</summary>
	/// <param name="destination">The destination logical window.</param>
	/// <param name="padRow">The zero-based first pad row.</param>
	/// <param name="padColumn">The zero-based first pad column.</param>
	/// <param name="rows">The positive viewport height.</param>
	/// <param name="columns">The positive viewport width.</param>
	/// <param name="destinationRow">The zero-based first destination row.</param>
	/// <param name="destinationColumn">The zero-based first destination column.</param>
	/// <remarks>
	/// Presentation is a logical destructive copy: ordinary pad blank cells replace destination cells.
	/// The pad and destination cursors are preserved. Source and destination rectangles must fit their
	/// respective logical surfaces. No physical terminal refresh is implied.
	/// </remarks>
	public void PresentTo(
		CursesWindow destination,
		int padRow,
		int padColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn
	) {
		ArgumentNullException.ThrowIfNull( destination );

		ContentWindow.CopyRectangleTo(
			destination,
			padRow,
			padColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn
		);
	}

	/// <summary>Creates an independently pannable viewport bound to one destination window.</summary>
	/// <param name="destination">The destination logical window.</param>
	/// <param name="padRow">The initial zero-based first pad row.</param>
	/// <param name="padColumn">The initial zero-based first pad column.</param>
	/// <param name="rows">The positive viewport height.</param>
	/// <param name="columns">The positive viewport width.</param>
	/// <param name="destinationRow">The zero-based first destination row.</param>
	/// <param name="destinationColumn">The zero-based first destination column.</param>
	/// <returns>The independent logical viewport state.</returns>
	public CursesPadViewport CreateViewport(
		CursesWindow destination,
		int padRow,
		int padColumn,
		int rows,
		int columns,
		int destinationRow,
		int destinationColumn
	) {
		ArgumentNullException.ThrowIfNull( destination );
		return new CursesPadViewport(
			this,
			destination,
			padRow,
			padColumn,
			rows,
			columns,
			destinationRow,
			destinationColumn
		);
	}

	/// <summary>Gets the latest pad-local content/damage revision.</summary>
	internal ulong ChangeRevision => backingScreen.VirtualScreen.ChangeRevision;

	/// <summary>Gets one pad cell's latest content/damage revision.</summary>
	internal ulong GetCellChangeRevision(
		int row,
		int column
	) {
		return backingScreen.VirtualScreen.GetCellChangeRevision(
			row,
			column
		);
	}
}
