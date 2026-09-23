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

/// <summary>Specifies which visual edge represents a source boundary.</summary>
public enum CursesTextAffinity {
	/// <summary>Uses the leading edge of the following text.</summary>
	Leading = 0,

	/// <summary>Uses the trailing edge of the preceding text.</summary>
	Trailing = 1
}

/// <summary>Represents one immutable visual caret position.</summary>
public readonly record struct CursesTextVisualPosition {
	/// <summary>Initializes one immutable visual caret position.</summary>
	/// <param name="line">The non-negative visual-line index.</param>
	/// <param name="column">The non-negative absolute terminal column.</param>
	/// <param name="affinity">The source-boundary affinity.</param>
	public CursesTextVisualPosition(
		int line,
		int column,
		CursesTextAffinity affinity = CursesTextAffinity.Leading
	) {
		if ( 0 > line ) {
			throw new ArgumentOutOfRangeException( nameof( line ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( !Enum.IsDefined( affinity ) ) {
			throw new ArgumentOutOfRangeException( nameof( affinity ) );
		}

		Line = line;
		Column = column;
		Affinity = affinity;
	}

	/// <summary>Gets the zero-based visual-line index.</summary>
	public int Line { get; }

	/// <summary>Gets the absolute terminal column.</summary>
	public int Column { get; }

	/// <summary>Gets the source-boundary affinity.</summary>
	public CursesTextAffinity Affinity { get; }
}

/// <summary>Represents one immutable visual hit-test result.</summary>
public readonly record struct CursesTextHitTestResult {
	/// <summary>Initializes one immutable visual hit-test result.</summary>
	/// <param name="position">The nearest legal source position.</param>
	/// <param name="affinity">The selected edge affinity.</param>
	/// <param name="isInside">Whether the requested column was inside represented content.</param>
	public CursesTextHitTestResult(
		CursesTextPosition position,
		CursesTextAffinity affinity,
		bool isInside
	) {
		if ( !Enum.IsDefined( affinity ) ) {
			throw new ArgumentOutOfRangeException( nameof( affinity ) );
		}

		Position = position;
		Affinity = affinity;
		IsInside = isInside;
	}

	/// <summary>Gets the nearest legal source position.</summary>
	public CursesTextPosition Position { get; }

	/// <summary>Gets the selected edge affinity.</summary>
	public CursesTextAffinity Affinity { get; }

	/// <summary>Gets whether the requested column was inside represented content.</summary>
	public bool IsInside { get; }
}

/// <summary>Represents one immutable half-open source selection.</summary>
public readonly record struct CursesTextSelection {
	/// <summary>Initializes one immutable source selection.</summary>
	/// <param name="anchor">The fixed source endpoint.</param>
	/// <param name="active">The moving source endpoint.</param>
	public CursesTextSelection(
		CursesTextPosition anchor,
		CursesTextPosition active
	) {
		Anchor = anchor;
		Active = active;
		if ( anchor.Offset <= active.Offset ) {
			Start = anchor;
			End = active;
		} else {
			Start = active;
			End = anchor;
		}
	}

	/// <summary>Gets the fixed source endpoint.</summary>
	public CursesTextPosition Anchor { get; }

	/// <summary>Gets the moving source endpoint.</summary>
	public CursesTextPosition Active { get; }

	/// <summary>Gets the lesser ordered source endpoint.</summary>
	public CursesTextPosition Start { get; }

	/// <summary>Gets the greater ordered source endpoint.</summary>
	public CursesTextPosition End { get; }

	/// <summary>Gets whether both endpoints are equal.</summary>
	public bool IsEmpty => Start == End;
}
