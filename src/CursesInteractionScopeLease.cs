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

/// <summary>Owns one LIFO activation of an explicit interaction scope.</summary>
public sealed class CursesInteractionScopeLease : IDisposable {
	private readonly CursesInteractionRouter owner;
	private bool released;

	internal CursesInteractionScopeLease(
		CursesInteractionRouter owner,
		CursesInteractionScope scope,
		CursesInteractionRegion? savedFocus
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( scope );
		this.owner = owner;
		this.Scope = scope;
		this.SavedFocus = savedFocus;
	}

	/// <summary>Releases this activation when it is the router's current top scope lease.</summary>
	public void Dispose() {
		if ( this.released ) {
			return;
		}

		this.owner.DeactivateScope( this );
	}

	internal CursesInteractionScope Scope {
		get;
	}

	internal CursesInteractionRegion? SavedFocus {
		get;
	}

	internal bool IsReleased => this.released;

	internal void MarkReleased() {
		this.released = true;
	}
}
