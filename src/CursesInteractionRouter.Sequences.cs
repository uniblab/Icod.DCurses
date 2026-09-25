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
	private readonly List<CursesCommandSequenceRegistration> globalCommandSequences = [];
	private object? pendingCommandSequenceOwner;
	private CursesKeyGesture[] pendingCommandSequenceGestures = [];
	private CursesCommandSequenceRegistration[] pendingCommandSequenceCandidates = [];

	/// <summary>Binds one ordered gesture sequence to a router-global application command.</summary>
	/// <param name="gestures">Two through eight bindable semantic gestures.</param>
	/// <param name="command">The application command identity.</param>
	public void BindGlobalGestureSequence(
		IReadOnlyList<CursesKeyGesture> gestures,
		CursesCommand command
	) {
		CursesCommandSequenceRegistration registration = new( gestures, command );
		this.ThrowIfDisposed();
		if ( this.globalGestureBindings.ContainsKey( registration.Gestures[0] ) ) {
			throw new InvalidOperationException(
				"The interaction router already has a global single-key binding for this sequence prefix."
			);
		}
		EnsureNoGlobalSequenceConflict( this.globalCommandSequences, registration.Gestures );
		if ( MaximumGlobalCommandSequenceBindings <= this.globalCommandSequences.Count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumGlobalCommandSequenceBindings} global command sequence bindings."
			);
		}

		this.EnsureCommandSequenceCapacity();
		this.globalCommandSequences.Add( registration );
		this.ClearPendingCommandSequence();
	}

	/// <summary>Removes one exact router-global command-sequence binding.</summary>
	/// <param name="gestures">The ordered bindable semantic gestures.</param>
	/// <returns><see langword="true"/> when a binding was removed; otherwise <see langword="false"/>.</returns>
	public bool UnbindGlobalGestureSequence(
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		CursesKeyGesture[] copy =
			CursesCommandSequenceRegistration.ValidateAndCopy( gestures );
		this.ThrowIfDisposed();
		int index = FindGlobalSequence( this.globalCommandSequences, copy );
		if ( 0 > index ) {
			return false;
		}

		this.globalCommandSequences.RemoveAt( index );
		this.ClearPendingCommandSequence();
		return true;
	}

	/// <summary>Gets whether this router is retaining a live command-sequence prefix.</summary>
	public bool HasPendingCommandSequence {
		get {
			this.ThrowIfDisposed();
			return this.pendingCommandSequenceOwner is not null;
		}
	}

	/// <summary>Processes one normalized input event through command-sequence routing.</summary>
	/// <param name="input">The normalized DCurses input event.</param>
	/// <returns>An immutable command-sequence processing result.</returns>
	public CursesCommandSequenceResult ProcessCommandSequence(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		this.ThrowIfDisposed();
		this.RepairFocusIfNeeded();

		if ( this.pendingCommandSequenceOwner is not null ) {
			return this.ContinuePendingCommandSequence( input );
		}
		if ( input.Kind is not CursesInputEventKind.Text
			and not CursesInputEventKind.Key ) {
			return CursesCommandSequenceResult.FallbackResult(
				input,
				this.Route( input )
			);
		}

		CursesInteractionScope? activeScope = this.ActiveScope;
		CursesInteractionRegion? focused = this.focusedRegion;
		if ( focused is not null ) {
			CursesCommandSequenceResult? regionResult = this.TryStartCommandSequence(
				input,
				focused,
				focused.CommandSequences,
				focused.TryGetCommand( input, out _ )
			);
			if ( regionResult is not null ) {
				return regionResult;
			}

			CursesInteractionScope? scope = focused.Scope;
			while ( scope is not null ) {
				CursesCommandSequenceResult? scopeResult = this.TryStartCommandSequence(
					input,
					scope,
					scope.CommandSequences,
					scope.TryGetCommand( input, out _ )
				);
				if ( scopeResult is not null ) {
					return scopeResult;
				}
				if ( ReferenceEquals( scope, activeScope ) ) {
					break;
				}
				scope = scope.Parent;
			}
		} else if ( activeScope is not null ) {
			CursesCommandSequenceResult? scopeResult = this.TryStartCommandSequence(
				input,
				activeScope,
				activeScope.CommandSequences,
				activeScope.TryGetCommand( input, out _ )
			);
			if ( scopeResult is not null ) {
				return scopeResult;
			}
		}

		CursesCommandSequenceResult? globalResult = this.TryStartCommandSequence(
			input,
			this,
			this.globalCommandSequences,
			this.TryGetGlobalCommand( input, out _ )
		);
		if ( globalResult is not null ) {
			return globalResult;
		}

		return CursesCommandSequenceResult.FallbackResult(
			input,
			this.Route( input )
		);
	}

	/// <summary>Cancels the live command-sequence prefix, when any.</summary>
	/// <returns><see langword="true"/> when pending state was cleared; otherwise <see langword="false"/>.</returns>
	public bool CancelPendingCommandSequence() {
		this.ThrowIfDisposed();
		if ( this.pendingCommandSequenceOwner is null ) {
			return false;
		}

		this.ClearPendingCommandSequence();
		return true;
	}

	/// <summary>Gets a detached snapshot of effective command-sequence bindings.</summary>
	/// <returns>A detached read-only binding snapshot.</returns>
	public IReadOnlyList<CursesCommandSequenceBinding>
		GetEffectiveGestureSequenceBindings() {
		this.ThrowIfDisposed();
		List<CursesCommandSequenceBinding> bindings = [];
		foreach ( CursesCommandSequenceRegistration registration
			in this.globalCommandSequences ) {
			bindings.Add(
				new CursesCommandSequenceBinding(
					registration.Gestures,
					registration.Command
				)
			);
		}
		return bindings.AsReadOnly();
	}

	internal void EnsureCommandSequenceCapacity() {
		int count = this.globalCommandSequences.Count;
		foreach ( CursesInteractionRegion region in this.regions ) {
			count = checked( count + region.CommandSequenceCount );
			if ( MaximumCommandSequenceBindings <= count ) {
				throw new InvalidOperationException(
					$"An interaction router cannot own more than {MaximumCommandSequenceBindings} total command sequence bindings."
				);
			}
		}
		foreach ( CursesInteractionScope scope in this.scopes ) {
			count = checked( count + scope.CommandSequenceCount );
			if ( MaximumCommandSequenceBindings <= count ) {
				throw new InvalidOperationException(
					$"An interaction router cannot own more than {MaximumCommandSequenceBindings} total command sequence bindings."
				);
			}
		}

		if ( MaximumCommandSequenceBindings <= count ) {
			throw new InvalidOperationException(
				$"An interaction router cannot own more than {MaximumCommandSequenceBindings} total command sequence bindings."
			);
		}
	}

	internal void ClearPendingCommandSequence() {
		this.pendingCommandSequenceOwner = null;
		this.pendingCommandSequenceGestures = [];
		this.pendingCommandSequenceCandidates = [];
	}

	private CursesCommandSequenceResult? TryStartCommandSequence(
		CursesInputEvent input,
		object owner,
		IReadOnlyList<CursesCommandSequenceRegistration> registrations,
		bool hasMatchingSingleBinding
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( registrations );
		if ( hasMatchingSingleBinding ) {
			return CursesCommandSequenceResult.FallbackResult(
				input,
				this.Route( input )
			);
		}

		List<CursesCommandSequenceRegistration> candidates = [];
		foreach ( CursesCommandSequenceRegistration registration in registrations ) {
			if ( registration.MatchesFirst( input ) ) {
				candidates.Add( registration );
			}
		}
		if ( candidates.Count is 0 ) {
			return null;
		}

		CursesKeyGesture[] matched = [ candidates[0].Gestures[0] ];
		this.SetPendingCommandSequence(
			owner,
			matched,
			candidates.ToArray()
		);
		return CursesCommandSequenceResult.PendingResult( input, matched );
	}

	private CursesCommandSequenceResult ContinuePendingCommandSequence(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		CursesKeyGesture[] abandoned = this.pendingCommandSequenceGestures;
		List<CursesCommandSequenceRegistration> matches = [];
		CursesCommandSequenceRegistration? completed = null;
		foreach ( CursesCommandSequenceRegistration candidate
			in this.pendingCommandSequenceCandidates ) {
			if ( candidate.Gestures.Length <= abandoned.Length
				|| !candidate.Gestures[abandoned.Length].Matches( input ) ) {
				continue;
			}
			matches.Add( candidate );
			if ( candidate.Gestures.Length == abandoned.Length + 1 ) {
				completed = candidate;
			}
		}
		if ( matches.Count is 0 ) {
			this.ClearPendingCommandSequence();
			return CursesCommandSequenceResult.MismatchResult(
				input,
				abandoned,
				this.Route( input )
			);
		}

		CursesKeyGesture[] extended = [
			.. abandoned,
			matches[0].Gestures[abandoned.Length]
		];
		if ( completed is not null ) {
			this.ClearPendingCommandSequence();
			return CursesCommandSequenceResult.CompletedResult(
				input,
				extended,
				completed.Command
			);
		}

		this.SetPendingCommandSequence(
			this.pendingCommandSequenceOwner!,
			extended,
			matches.ToArray()
		);
		return CursesCommandSequenceResult.PendingResult( input, extended );
	}

	private void SetPendingCommandSequence(
		object owner,
		CursesKeyGesture[] gestures,
		CursesCommandSequenceRegistration[] candidates
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( gestures );
		ArgumentNullException.ThrowIfNull( candidates );
		this.pendingCommandSequenceOwner = owner;
		this.pendingCommandSequenceGestures = gestures;
		this.pendingCommandSequenceCandidates = candidates;
	}

	private bool HasGlobalCommandSequenceStartingWith(
		CursesKeyGesture gesture
	) {
		foreach ( CursesCommandSequenceRegistration registration
			in this.globalCommandSequences ) {
			if ( registration.Gestures[0] == gesture ) {
				return true;
			}
		}
		return false;
	}

	private static void EnsureNoGlobalSequenceConflict(
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
					"The interaction router already has a duplicate or ambiguous global command sequence binding."
				);
			}
		}
	}

	private static int FindGlobalSequence(
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
