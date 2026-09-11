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

using Icod.DCurses.Internal;

/// <summary>
/// Represents one independent retained surface positioned over a destination logical screen.
/// </summary>
/// <remarks>
/// Panel content is independent of the destination screen and of other panels. Editing the
/// <see cref="ContentWindow"/> therefore does not directly modify cells on the destination
/// <see cref="CursesScreen"/>. Logical composition projects visible panels onto that destination
/// in deterministic z-order. Disposing a panel permanently removes it from its owning screen's
/// composition order; the managed retained content remains readable through previously obtained
/// references but the disposed panel cannot be manipulated or reattached.
/// </remarks>
public sealed class CursesPanel : IDisposable {
	private readonly CursesScreen owner;
	private readonly CursesPanelSurface surface;
	private CursesPanelTransparency transparency;
	private bool disposed;

	/// <summary>Initializes one screen-owned independent retained panel.</summary>
	/// <param name="owner">The destination logical screen.</param>
	/// <param name="row">The zero-based destination row of the panel origin.</param>
	/// <param name="column">The zero-based destination column of the panel origin.</param>
	/// <param name="rows">The positive panel height.</param>
	/// <param name="columns">The positive panel width.</param>
	internal CursesPanel(
		CursesScreen owner,
		int row,
		int column,
		int rows,
		int columns
	) {
		ArgumentNullException.ThrowIfNull( owner );
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			rows,
			columns,
			owner.Rows,
			owner.Columns
		);

		this.owner = owner;
		Row = row;
		Column = column;
		surface = new CursesPanelSurface(
			rows,
			columns,
			owner.TextWidthProvider
		);
		IsVisible = true;
	}

	/// <summary>Gets the retained content window owned only by this panel.</summary>
	public CursesWindow ContentWindow => surface.ContentWindow;

	/// <summary>Gets the zero-based destination row of the panel origin.</summary>
	public int Row {
		get;
		private set;
	}

	/// <summary>Gets the zero-based destination column of the panel origin.</summary>
	public int Column {
		get;
		private set;
	}

	/// <summary>Gets the panel height in terminal cells.</summary>
	public int Rows => surface.Rows;

	/// <summary>Gets the panel width in terminal cells.</summary>
	public int Columns => surface.Columns;

	/// <summary>Gets the panel rectangle relative to its owning screen.</summary>
	public CursesRectangle Bounds => new(
		Row,
		Column,
		Rows,
		Columns
	);

	/// <summary>Gets whether this panel currently participates in logical composition.</summary>
	public bool IsVisible {
		get;
		private set;
	}

	/// <summary>Gets or sets how blank cells in this panel participate in logical composition.</summary>
	public CursesPanelTransparency Transparency {
		get => transparency;
		set {
			ThrowIfDisposed();
			if ( value is not CursesPanelTransparency.Opaque
				and not CursesPanelTransparency.BlankCellsTransparent ) {
				throw new ArgumentOutOfRangeException( nameof( value ) );
			}

			transparency = value;
		}
	}

	/// <summary>Makes this panel visible without changing its remembered z-order position.</summary>
	public void Show() {
		ThrowIfDisposed();
		if ( IsVisible ) {
			return;
		}

		IsVisible = true;
	}

	/// <summary>Hides this panel while retaining its content, position, and z-order membership.</summary>
	public void Hide() {
		ThrowIfDisposed();
		if ( !IsVisible ) {
			return;
		}

		IsVisible = false;
	}

	/// <summary>Moves this panel to a new destination-screen origin without changing its content or z-order.</summary>
	/// <param name="row">The new zero-based destination row.</param>
	/// <param name="column">The new zero-based destination column.</param>
	public void MoveTo(
		int row,
		int column
	) {
		ThrowIfDisposed();
		CursesScreen.ValidateWindowRectangle(
			row,
			column,
			Rows,
			Columns,
			owner.Rows,
			owner.Columns
		);

		if ( row == Row
			&& column == Column ) {
			return;
		}

		Row = row;
		Column = column;
	}

	/// <summary>Changes this panel's retained dimensions without changing its destination origin.</summary>
	/// <param name="rows">The positive new panel height.</param>
	/// <param name="columns">The positive new panel width.</param>
	/// <remarks>
	/// Overlapping retained content and semantic metadata are preserved. The retained content-window
	/// object remains the same instance, and its cursor is clamped when the new dimensions shrink.
	/// </remarks>
	public void Resize(
		int rows,
		int columns
	) {
		ThrowIfDisposed();
		CursesScreen.ValidateWindowRectangle(
			Row,
			Column,
			rows,
			columns,
			owner.Rows,
			owner.Columns
		);

		surface.Resize(
			rows,
			columns
		);
	}

	/// <summary>Atomically assigns this panel's final rectangle relative to its owning screen.</summary>
	/// <param name="bounds">The positive-sized final rectangle.</param>
	/// <remarks>
	/// Overlapping retained content and semantic metadata are preserved when dimensions change.
	/// Validation is performed against the complete final rectangle before either position or size mutates.
	/// </remarks>
	public void SetBounds( CursesRectangle bounds ) {
		ThrowIfDisposed();
		CursesScreen.ValidateWindowRectangle(
			bounds.Row,
			bounds.Column,
			bounds.Rows,
			bounds.Columns,
			owner.Rows,
			owner.Columns
		);

		if ( bounds.Rows != Rows
			|| bounds.Columns != Columns ) {
			surface.Resize(
				bounds.Rows,
				bounds.Columns
			);
		}
		Row = bounds.Row;
		Column = bounds.Column;
	}

	/// <summary>Moves this panel to the top of its owning screen's panel order.</summary>
	public void MoveToTop() {
		ThrowIfDisposed();
		owner.MovePanelToTop( this );
	}

	/// <summary>Moves this panel to the bottom of its owning screen's panel order.</summary>
	public void MoveToBottom() {
		ThrowIfDisposed();
		owner.MovePanelToBottom( this );
	}

	/// <summary>Moves this panel immediately above another panel on the same screen.</summary>
	/// <param name="sibling">The panel which should immediately precede this panel.</param>
	public void MoveAbove( CursesPanel sibling ) {
		ArgumentNullException.ThrowIfNull( sibling );
		ThrowIfDisposed();
		owner.MovePanelAbove(
			this,
			sibling
		);
	}

	/// <summary>Moves this panel immediately below another panel on the same screen.</summary>
	/// <param name="sibling">The panel which should immediately follow this panel.</param>
	public void MoveBelow( CursesPanel sibling ) {
		ArgumentNullException.ThrowIfNull( sibling );
		ThrowIfDisposed();
		owner.MovePanelBelow(
			this,
			sibling
		);
	}

	/// <summary>Permanently removes this panel from its owning screen's composition order.</summary>
	public void Dispose() {
		if ( disposed ) {
			return;
		}

		owner.RemovePanel( this );
		IsVisible = false;
		disposed = true;
	}

	/// <summary>Gets the destination screen which owns this panel.</summary>
	internal CursesScreen Owner => owner;

	/// <summary>Gets whether this panel has been permanently removed from its owning screen.</summary>
	internal bool IsDisposed => disposed;

	/// <summary>Gets the panel-private virtual screen for logical composition.</summary>
	internal CursesVirtualScreen VirtualScreen => surface.VirtualScreen;

	private void ThrowIfDisposed() {
		if ( disposed ) {
			throw new ObjectDisposedException( nameof( CursesPanel ) );
		}
	}
}