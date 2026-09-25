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

public sealed partial class CursesInteractionScope {
	private readonly List<CursesCommandSequenceRegistration> commandSequences = [];

	/// <summary>Binds one ordered gesture sequence to an application command in this scope.</summary>
	/// <param name="gestures">Two through eight bindable semantic gestures.</param>
	/// <param name="command">The application command identity.</param>
	public void BindGestureSequence(
		IReadOnlyList<CursesKeyGesture> gestures,
		CursesCommand command
	) {
		CursesCommandSequenceRegistration registration = new( gestures, command );
		this.ThrowIfDisposed();
		if ( this.gestureBindings.ContainsKey( registration.Gestures[0] ) ) {
			throw new InvalidOperationException(
				"The interaction scope already has a single-key binding for this sequence prefix."
			);
		}
		EnsureNoSequenceConflict( this.commandSequences, registration.Gestures );
		if ( CursesInteractionRouter.MaximumScopeCommandSequenceBindings
			<= this.commandSequences.Count ) {
			throw new InvalidOperationException(
				$"An interaction scope cannot own more than {CursesInteractionRouter.MaximumScopeCommandSequenceBindings} command sequence bindings."
			);
		}

		this.owner.EnsureCommandSequenceCapacity();
		this.commandSequences.Add( registration );
		this.owner.ClearPendingCommandSequence();
	}

	/// <summary>Removes one exact command-sequence binding from this scope.</summary>
	/// <param name="gestures">The ordered bindable semantic gestures.</param>
	/// <returns><see langword="true"/> when a binding was removed; otherwise <see langword="false"/>.</returns>
	public bool UnbindGestureSequence(
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		CursesKeyGesture[] copy =
			CursesCommandSequenceRegistration.ValidateAndCopy( gestures );
		this.ThrowIfDisposed();
		int index = FindSequence( this.commandSequences, copy );
		if ( 0 > index ) {
			return false;
		}

		this.commandSequences.RemoveAt( index );
		this.owner.ClearPendingCommandSequence();
		return true;
	}

	internal int CommandSequenceCount => this.commandSequences.Count;

	internal IReadOnlyList<CursesCommandSequenceRegistration> CommandSequences =>
		this.commandSequences;

	internal bool HasCommandSequenceStartingWith(
		CursesKeyGesture gesture
	) {
		foreach ( CursesCommandSequenceRegistration registration in this.commandSequences ) {
			if ( registration.Gestures[0] == gesture ) {
				return true;
			}
		}
		return false;
	}

	private static void EnsureNoSequenceConflict(
		IReadOnlyList<CursesCommandSequenceRegistration> registrations,
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		foreach ( CursesCommandSequenceRegistration registration in registrations ) {
			if ( CursesCommandSequenceRegistration.SequenceEquals(
				registration.Gestures,
				gestures
			) || CursesCommandSequenceRegistration.IsProperPrefix(
				registration.Gestures,
				gestures
			) || CursesCommandSequenceRegistration.IsProperPrefix(
				gestures,
				registration.Gestures
			) ) {
				throw new InvalidOperationException(
					"The interaction scope already has a duplicate or ambiguous command sequence binding."
				);
			}
		}
	}

	private static int FindSequence(
		IReadOnlyList<CursesCommandSequenceRegistration> registrations,
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		for ( int index = 0; index < registrations.Count; ++index ) {
			if ( CursesCommandSequenceRegistration.SequenceEquals(
				registrations[index].Gestures,
				gestures
			) ) {
				return index;
			}
		}
		return -1;
	}
}
