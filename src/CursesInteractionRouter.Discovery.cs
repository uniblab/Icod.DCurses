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
	/// <summary>Copies effective single-key bindings in routing precedence order.</summary>
	/// <returns>A read-only snapshot unaffected by later binding or focus changes.</returns>
	public IReadOnlyList<CursesCommandBinding> GetEffectiveGestureBindings() {
		this.ThrowIfDisposed();
		this.RepairFocusIfNeeded();

		List<CursesCommandBinding> entries = [];
		HashSet<CursesKeyGesture> seen = [];
		CursesInteractionScope? active = this.ActiveScope;
		CursesInteractionRegion? focused = this.focusedRegion;
		if ( focused is not null ) {
			AppendBindings( focused.GestureBindings, seen, entries );
			CursesInteractionScope? scope = focused.Scope;
			while ( scope is not null ) {
				AppendBindings( scope.GestureBindings, seen, entries );
				if ( ReferenceEquals( scope, active ) ) {
					break;
				}
				scope = scope.Parent;
			}
		} else if ( active is not null ) {
			AppendBindings( active.GestureBindings, seen, entries );
		}

		AppendBindings( this.globalGestureBindings, seen, entries );
		return Array.AsReadOnly( entries.ToArray() );
	}

	private static void AppendBindings(
		IEnumerable<KeyValuePair<CursesKeyGesture, CursesCommand>> bindings,
		HashSet<CursesKeyGesture> seen,
		List<CursesCommandBinding> entries
	) {
		foreach ( KeyValuePair<CursesKeyGesture, CursesCommand> binding in bindings ) {
			if ( seen.Add( binding.Key ) ) {
				entries.Add( new CursesCommandBinding( binding.Key, binding.Value ) );
			}
		}
	}
}
