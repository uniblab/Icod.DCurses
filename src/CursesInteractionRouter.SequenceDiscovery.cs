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
	private static readonly IComparer<CursesCommandSequenceRegistration>
		commandSequenceRegistrationComparer =
			Comparer<CursesCommandSequenceRegistration>.Create( CompareCommandSequences );

	/// <summary>Gets a detached snapshot of effective command-sequence bindings.</summary>
	/// <returns>A detached read-only binding snapshot.</returns>
	public IReadOnlyList<CursesCommandSequenceBinding>
		GetEffectiveGestureSequenceBindings() {
		this.ThrowIfDisposed();
		this.RepairFocusIfNeeded();

		List<CursesCommandSequenceBinding> entries = [];
		HashSet<CursesKeyGesture> claimedFirstGestures = [];
		CursesInteractionScope? activeScope = this.ActiveScope;
		CursesInteractionRegion? focused = this.focusedRegion;
		if ( focused is not null ) {
			AppendSequenceBindings(
				focused.GestureBindings,
				focused.CommandSequences,
				claimedFirstGestures,
				entries
			);
			CursesInteractionScope? scope = focused.Scope;
			while ( scope is not null ) {
				AppendSequenceBindings(
					scope.GestureBindings,
					scope.CommandSequences,
					claimedFirstGestures,
					entries
				);
				if ( ReferenceEquals( scope, activeScope ) ) {
					break;
				}
				scope = scope.Parent;
			}
		} else if ( activeScope is not null ) {
			AppendSequenceBindings(
				activeScope.GestureBindings,
				activeScope.CommandSequences,
				claimedFirstGestures,
				entries
			);
		}

		AppendSequenceBindings(
			this.globalGestureBindings,
			this.globalCommandSequences,
			claimedFirstGestures,
			entries
		);
		return Array.AsReadOnly( entries.ToArray() );
	}

	private static void AppendSequenceBindings(
		IEnumerable<KeyValuePair<CursesKeyGesture, CursesCommand>> singleBindings,
		IReadOnlyList<CursesCommandSequenceRegistration> registrations,
		HashSet<CursesKeyGesture> claimedFirstGestures,
		List<CursesCommandSequenceBinding> entries
	) {
		ArgumentNullException.ThrowIfNull( singleBindings );
		ArgumentNullException.ThrowIfNull( registrations );
		ArgumentNullException.ThrowIfNull( claimedFirstGestures );
		ArgumentNullException.ThrowIfNull( entries );
		foreach ( KeyValuePair<CursesKeyGesture, CursesCommand> binding
			in singleBindings ) {
			_ = claimedFirstGestures.Add( binding.Key );
		}

		HashSet<CursesKeyGesture> ownerFirstGestures = [];
		foreach ( CursesCommandSequenceRegistration registration
			in registrations.OrderBy(
				static current => current,
				commandSequenceRegistrationComparer
			) ) {
			CursesKeyGesture first = registration.Gestures[0];
			if ( claimedFirstGestures.Contains( first ) ) {
				continue;
			}
			entries.Add(
				new CursesCommandSequenceBinding(
					registration.Gestures,
					registration.Command
				)
			);
			_ = ownerFirstGestures.Add( first );
		}

		foreach ( CursesKeyGesture first in ownerFirstGestures ) {
			_ = claimedFirstGestures.Add( first );
		}
	}

	private static int CompareCommandSequences(
		CursesCommandSequenceRegistration? left,
		CursesCommandSequenceRegistration? right
	) {
		if ( ReferenceEquals( left, right ) ) {
			return 0;
		}
		if ( left is null ) {
			return -1;
		}
		if ( right is null ) {
			return 1;
		}

		int length = Math.Min( left.Gestures.Length, right.Gestures.Length );
		for ( int index = 0; index < length; ++index ) {
			int comparison = CompareGesture(
				left.Gestures[index],
				right.Gestures[index]
			);
			if ( comparison is not 0 ) {
				return comparison;
			}
		}
		return left.Gestures.Length.CompareTo( right.Gestures.Length );
	}

	private static int CompareGesture(
		CursesKeyGesture left,
		CursesKeyGesture right
	) {
		int comparison = left.Key.CompareTo( right.Key );
		if ( comparison is not 0 ) {
			return comparison;
		}
		comparison = ( left.Character?.Value ?? -1 ).CompareTo(
			right.Character?.Value ?? -1
		);
		if ( comparison is not 0 ) {
			return comparison;
		}
		comparison = left.Modifiers.CompareTo( right.Modifiers );
		if ( comparison is not 0 ) {
			return comparison;
		}
		comparison = left.Phase.CompareTo( right.Phase );
		if ( comparison is not 0 ) {
			return comparison;
		}
		return ( left.FunctionKeyNumber ?? -1 ).CompareTo(
			right.FunctionKeyNumber ?? -1
		);
	}
}
