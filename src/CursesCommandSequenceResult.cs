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

/// <summary>Represents one immutable command-sequence processing result.</summary>
public sealed class CursesCommandSequenceResult {
	private CursesCommandSequenceResult(
		CursesCommandSequenceResultKind kind,
		CursesInputEvent input,
		IReadOnlyList<CursesKeyGesture> matchedGestures,
		CursesCommand? command,
		CursesInteractionResult? fallback
	) {
		this.Kind = kind;
		this.Input = input;
		this.MatchedGestures = matchedGestures;
		this.Command = command;
		this.Fallback = fallback;
	}

	/// <summary>Gets the processing outcome.</summary>
	public CursesCommandSequenceResultKind Kind {
		get;
	}

	/// <summary>Gets the input supplied to the processing call.</summary>
	public CursesInputEvent Input {
		get;
	}

	/// <summary>Gets the detached matched or abandoned prefix.</summary>
	public IReadOnlyList<CursesKeyGesture> MatchedGestures {
		get;
	}

	/// <summary>Gets the completed command, when any.</summary>
	public CursesCommand? Command {
		get;
	}

	/// <summary>Gets the ordinary routing result for fallback or mismatch.</summary>
	public CursesInteractionResult? Fallback {
		get;
	}

	internal static CursesCommandSequenceResult FallbackResult(
		CursesInputEvent input,
		CursesInteractionResult fallback
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( fallback );
		return new CursesCommandSequenceResult(
			CursesCommandSequenceResultKind.Fallback,
			input,
			Array.AsReadOnly( Array.Empty<CursesKeyGesture>() ),
			command: null,
			fallback
		);
	}

	internal static CursesCommandSequenceResult PendingResult(
		CursesInputEvent input,
		IReadOnlyList<CursesKeyGesture> matchedGestures
	) {
		ArgumentNullException.ThrowIfNull( input );
		return new CursesCommandSequenceResult(
			CursesCommandSequenceResultKind.Pending,
			input,
			Snapshot( matchedGestures ),
			command: null,
			fallback: null
		);
	}

	internal static CursesCommandSequenceResult CompletedResult(
		CursesInputEvent input,
		IReadOnlyList<CursesKeyGesture> matchedGestures,
		CursesCommand command
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( command );
		return new CursesCommandSequenceResult(
			CursesCommandSequenceResultKind.Completed,
			input,
			Snapshot( matchedGestures ),
			command,
			fallback: null
		);
	}

	internal static CursesCommandSequenceResult MismatchResult(
		CursesInputEvent input,
		IReadOnlyList<CursesKeyGesture> matchedGestures,
		CursesInteractionResult fallback
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( fallback );
		return new CursesCommandSequenceResult(
			CursesCommandSequenceResultKind.Mismatch,
			input,
			Snapshot( matchedGestures ),
			command: null,
			fallback
		);
	}

	private static IReadOnlyList<CursesKeyGesture> Snapshot(
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		ArgumentNullException.ThrowIfNull( gestures );
		CursesKeyGesture[] copy = new CursesKeyGesture[gestures.Count];
		for ( int index = 0; index < copy.Length; ++index ) {
			copy[index] = gestures[index];
		}
		return Array.AsReadOnly( copy );
	}
}
