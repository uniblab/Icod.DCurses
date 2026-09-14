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

/// <summary>Represents one bounded application interaction scope owned by a router.</summary>
public sealed partial class CursesInteractionScope : IDisposable {
	private readonly CursesInteractionRouter owner;
	private bool disposed;

	internal CursesInteractionScope(
		CursesInteractionRouter owner,
		CursesInteractionScope? parent,
		int depth
	) {
		ArgumentNullException.ThrowIfNull( owner );
		if ( 1 > depth ) {
			throw new ArgumentOutOfRangeException( nameof( depth ) );
		}

		this.owner = owner;
		this.Parent = parent;
		this.Depth = depth;
	}

	/// <summary>Gets the optional explicit parent scope; null means the implicit root scope.</summary>
	public CursesInteractionScope? Parent {
		get;
	}

	/// <summary>Permanently removes this empty inactive scope from its owning router.</summary>
	public void Dispose() {
		if ( this.disposed ) {
			return;
		}

		this.owner.RemoveScope( this );
		this.disposed = true;
	}

	internal CursesInteractionRouter Owner => this.owner;

	internal int Depth {
		get;
	}

	internal bool IsDisposed => this.disposed;

	internal void ForceDispose() {
		this.disposed = true;
	}
}
