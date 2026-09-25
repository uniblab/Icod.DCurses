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

internal sealed class CursesCommandSequenceRegistration {
	internal CursesCommandSequenceRegistration(
		IReadOnlyList<CursesKeyGesture> gestures,
		CursesCommand command
	) {
		ArgumentNullException.ThrowIfNull( command );
		this.Gestures = ValidateAndCopy( gestures );
		this.Command = command;
	}

	internal CursesKeyGesture[] Gestures {
		get;
	}

	internal CursesCommand Command {
		get;
	}

	internal bool MatchesFirst(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		return this.Gestures[0].Matches( input );
	}

	internal static CursesKeyGesture[] ValidateAndCopy(
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		ArgumentNullException.ThrowIfNull( gestures );
		if ( gestures.Count is < 2
			or > CursesInteractionRouter.MaximumCommandSequenceLength ) {
			throw new ArgumentOutOfRangeException( nameof( gestures ) );
		}

		CursesKeyGesture[] copy = new CursesKeyGesture[gestures.Count];
		for ( int index = 0; index < copy.Length; ++index ) {
			if ( !gestures[index].IsBindable ) {
				throw new ArgumentException(
					"Every sequence gesture must be created by a CursesKeyGesture factory.",
					nameof( gestures )
				);
			}
			copy[index] = gestures[index];
		}
		return copy;
	}

	internal static bool SequenceEquals(
		IReadOnlyList<CursesKeyGesture> left,
		IReadOnlyList<CursesKeyGesture> right
	) {
		ArgumentNullException.ThrowIfNull( left );
		ArgumentNullException.ThrowIfNull( right );
		if ( left.Count != right.Count ) {
			return false;
		}
		for ( int index = 0; index < left.Count; ++index ) {
			if ( left[index] != right[index] ) {
				return false;
			}
		}
		return true;
	}

	internal static bool IsProperPrefix(
		IReadOnlyList<CursesKeyGesture> prefix,
		IReadOnlyList<CursesKeyGesture> sequence
	) {
		ArgumentNullException.ThrowIfNull( prefix );
		ArgumentNullException.ThrowIfNull( sequence );
		if ( prefix.Count >= sequence.Count ) {
			return false;
		}
		for ( int index = 0; index < prefix.Count; ++index ) {
			if ( prefix[index] != sequence[index] ) {
				return false;
			}
		}
		return true;
	}
}
