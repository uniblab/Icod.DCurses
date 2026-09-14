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

public sealed partial class CursesInteractionRouter {
	private readonly List<CursesInteractionScope> scopes = [];
	private readonly List<CursesInteractionScopeLease> activeScopeLeases = [];

	/// <summary>Registers one bounded explicit interaction scope.</summary>
	/// <param name="options">Optional immutable parent-scope options.</param>
	/// <returns>The registered explicit scope.</returns>
	public CursesInteractionScope RegisterScope(
		CursesInteractionScopeOptions? options = null
	) {
		this.ThrowIfDisposed();

		CursesInteractionScope? parent = options?.Parent;
		if ( parent is not null ) {
			this.ValidateOwnedScope(
				parent,
				nameof( options )
			);
		}
		if ( MaximumScopes <= this.scopes.Count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumScopes} live explicit scopes."
			);
		}

		int depth = parent is null
			? 1
			: checked( parent.Depth + 1 )
		;
		if ( MaximumScopeDepth < depth ) {
			throw new InvalidOperationException(
				$"An interaction scope cannot exceed explicit depth {MaximumScopeDepth}."
			);
		}

		CursesInteractionScope scope = new(
			this,
			parent,
			depth
		);
		this.scopes.Add( scope );
		return scope;
	}

	/// <summary>Activates one explicit descendant scope until the returned lease is disposed.</summary>
	/// <param name="scope">The owned explicit scope to activate.</param>
	/// <returns>A LIFO scope-activation lease.</returns>
	public CursesInteractionScopeLease ActivateScope(
		CursesInteractionScope scope
	) {
		ArgumentNullException.ThrowIfNull( scope );
		this.ThrowIfDisposed();
		this.ValidateOwnedScope(
			scope,
			nameof( scope )
		);

		CursesInteractionScope? active = this.ActiveScope;
		if ( active is not null
			&& !IsScopeDescendantOf(
				scope,
				active
			) ) {
			throw new ArgumentException(
				"A nested active interaction scope must be a descendant of the current active scope.",
				nameof( scope )
			);
		}

		this.RepairFocusIfNeeded();
		CursesInteractionScopeLease lease = new(
			this,
			scope,
			this.focusedRegion
		);
		this.activeScopeLeases.Add( lease );
		this.RepairFocusIfNeeded();
		this.RepairPointerGestureStateIfNeeded();
		return lease;
	}

	internal CursesInteractionScope? ActiveScope => 0 == this.activeScopeLeases.Count
		? null
		: this.activeScopeLeases[^1].Scope;

	internal void DeactivateScope(
		CursesInteractionScopeLease lease
	) {
		ArgumentNullException.ThrowIfNull( lease );
		if ( lease.IsReleased ) {
			return;
		}
		if ( 0 == this.activeScopeLeases.Count
			|| !ReferenceEquals(
				this.activeScopeLeases[^1],
				lease
			) ) {
			throw new InvalidOperationException(
				"Interaction scope leases must be disposed in LIFO order."
			);
		}

		this.activeScopeLeases.RemoveAt( this.activeScopeLeases.Count - 1 );
		lease.MarkReleased();
		this.RepairPointerGestureStateIfNeeded();

		CursesInteractionRegion? savedFocus = lease.SavedFocus;
		if ( savedFocus is not null
			&& !savedFocus.IsDisposed
			&& this.IsRegionEligible( savedFocus ) ) {
			this.focusedRegion = savedFocus;
			return;
		}

		this.RepairFocusIfNeeded();
	}

	internal void RemoveScope(
		CursesInteractionScope scope
	) {
		ArgumentNullException.ThrowIfNull( scope );
		if ( !ReferenceEquals(
			scope.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The interaction scope belongs to another router.",
				nameof( scope )
			);
		}

		foreach ( CursesInteractionRegion region in this.regions ) {
			if ( ReferenceEquals(
				region.Scope,
				scope
			) ) {
				throw new InvalidOperationException(
					"An interaction scope with live regions cannot be disposed."
				);
			}
		}
		foreach ( CursesInteractionScope candidate in this.scopes ) {
			if ( ReferenceEquals(
				candidate.Parent,
				scope
			) ) {
				throw new InvalidOperationException(
					"An interaction scope with live child scopes cannot be disposed."
				);
			}
		}
		foreach ( CursesInteractionScopeLease lease in this.activeScopeLeases ) {
			if ( ReferenceEquals(
				lease.Scope,
				scope
			) ) {
				throw new InvalidOperationException(
					"An active interaction scope cannot be disposed."
				);
			}
		}

		if ( !this.scopes.Remove( scope ) ) {
			throw new InvalidOperationException(
				"The interaction scope is not registered with this router."
			);
		}
	}

	internal void ValidateRegionScope(
		CursesInteractionScope? scope
	) {
		if ( scope is null ) {
			return;
		}

		this.ValidateOwnedScope(
			scope,
			nameof( CursesInteractionRegionOptions.Scope )
		);
	}

	internal bool IsRegionWithinActiveScope(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		CursesInteractionScope? active = this.ActiveScope;
		if ( active is null ) {
			return true;
		}

		CursesInteractionScope? scope = region.Scope;
		return scope is not null
			&& ( ReferenceEquals(
				scope,
				active
			) || IsScopeDescendantOf(
				scope,
				active
			) );
	}

	internal void DisposeScopeState() {
		foreach ( CursesInteractionScopeLease lease in this.activeScopeLeases ) {
			lease.MarkReleased();
		}
		this.activeScopeLeases.Clear();

		foreach ( CursesInteractionScope scope in this.scopes ) {
			scope.ForceDispose();
		}
		this.scopes.Clear();
	}

	private void ValidateOwnedScope(
		CursesInteractionScope scope,
		string parameterName
	) {
		ArgumentNullException.ThrowIfNull( scope );
		ArgumentException.ThrowIfNullOrEmpty( parameterName );
		if ( !ReferenceEquals(
			scope.Owner,
			this
		) ) {
			throw new ArgumentException(
				"The interaction scope belongs to another router.",
				parameterName
			);
		}
		if ( scope.IsDisposed ) {
			throw new ObjectDisposedException( parameterName );
		}
	}

	private static bool IsScopeDescendantOf(
		CursesInteractionScope candidate,
		CursesInteractionScope ancestor
	) {
		ArgumentNullException.ThrowIfNull( candidate );
		ArgumentNullException.ThrowIfNull( ancestor );

		CursesInteractionScope? current = candidate.Parent;
		while ( current is not null ) {
			if ( ReferenceEquals(
				current,
				ancestor
			) ) {
				return true;
			}
			current = current.Parent;
		}

		return false;
	}
}
