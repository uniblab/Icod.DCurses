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
/// Represents one application-owned interaction region registered with a
/// <see cref="CursesInteractionRouter"/>.
/// </summary>
public sealed class CursesInteractionRegion : IDisposable {
	private readonly CursesInteractionRouter owner;
	private CursesRectangle bounds;
	private bool isEnabled;
	private bool isFocusable;
	private int traversalOrder;
	private int hitTestPriority;
	private bool disposed;

	internal CursesInteractionRegion(
		CursesInteractionRouter owner,
		CursesInteractionRegionOptions options,
		long registrationOrdinal
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( options );
		if ( 0 > registrationOrdinal ) {
			throw new ArgumentOutOfRangeException( nameof( registrationOrdinal ) );
		}

		this.owner = owner;
		this.bounds = options.Bounds;
		this.Panel = options.Panel;
		this.isEnabled = options.IsEnabled;
		this.isFocusable = options.IsFocusable;
		this.traversalOrder = options.TraversalOrder;
		this.hitTestPriority = options.HitTestPriority;
		this.RegistrationOrdinal = registrationOrdinal;
	}

	/// <summary>Gets the declared region rectangle.</summary>
	public CursesRectangle Bounds => this.bounds;

	/// <summary>Gets the optional panel whose coordinate space owns <see cref="Bounds"/>.</summary>
	public CursesPanel? Panel {
		get;
	}

	/// <summary>Gets or sets whether this region participates in interaction routing.</summary>
	public bool IsEnabled {
		get => this.isEnabled;
		set {
			this.ThrowIfDisposed();
			this.isEnabled = value;
		}
	}

	/// <summary>Gets or sets whether this region participates in logical focus traversal.</summary>
	public bool IsFocusable {
		get => this.isFocusable;
		set {
			this.ThrowIfDisposed();
			this.isFocusable = value;
		}
	}

	/// <summary>Gets or sets the logical-focus traversal order.</summary>
	public int TraversalOrder {
		get => this.traversalOrder;
		set {
			this.ThrowIfDisposed();
			this.traversalOrder = value;
		}
	}

	/// <summary>Gets or sets the same-surface hit-test priority.</summary>
	public int HitTestPriority {
		get => this.hitTestPriority;
		set {
			this.ThrowIfDisposed();
			this.hitTestPriority = value;
		}
	}

	/// <summary>Replaces the declared region rectangle.</summary>
	/// <param name="bounds">The new region rectangle.</param>
	public void SetBounds(
		CursesRectangle bounds
	) {
		this.ThrowIfDisposed();
		this.bounds = bounds;
	}

	/// <summary>Permanently removes this region from its owning interaction router.</summary>
	public void Dispose() {
		if ( this.disposed ) {
			return;
		}

		this.owner.RemoveRegion( this );
		this.disposed = true;
	}

	internal CursesInteractionRouter Owner => this.owner;

	internal long RegistrationOrdinal {
		get;
	}

	internal bool IsDisposed => this.disposed;

	private void ThrowIfDisposed() {
		if ( this.disposed ) {
			throw new ObjectDisposedException( nameof( CursesInteractionRegion ) );
		}
	}
}
