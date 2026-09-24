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

/// <summary>Specifies how text is wrapped into visual lines.</summary>
public enum CursesTextWrapMode {
	/// <summary>Does not wrap a logical line.</summary>
	NoWrap = 0,

	/// <summary>Wraps at complete text-element boundaries.</summary>
	TextElement = 1,

	/// <summary>Prefers whitespace boundaries and falls back to text-element wrapping.</summary>
	Word = 2
}

/// <summary>Specifies horizontal alignment within the available columns.</summary>
public enum CursesTextAlignment {
	/// <summary>Aligns content at the starting column.</summary>
	Start = 0,

	/// <summary>Centers content, leaving an odd remainder after it.</summary>
	Center = 1,

	/// <summary>Aligns content at the ending column.</summary>
	End = 2
}

/// <summary>Specifies how hidden layout content is represented.</summary>
public enum CursesTextOverflow {
	/// <summary>Clips hidden content.</summary>
	Clip = 0,

	/// <summary>Uses an ellipsis when one complete ellipsis element fits.</summary>
	Ellipsis = 1
}

/// <summary>Represents one immutable laid-out visual line.</summary>
public sealed class CursesTextVisualLine {
	private readonly IReadOnlyList<CursesTextFragment> fragments;

	internal CursesTextVisualLine(
		int index,
		CursesTextPosition sourceStart,
		CursesTextPosition sourceEnd,
		int column,
		int columns,
		bool endsWithHardBreak,
		bool endsWithSoftWrap,
		bool isClipped,
		CursesTextFragment[] fragments
	) {
		Index = index;
		SourceStart = sourceStart;
		SourceEnd = sourceEnd;
		Column = column;
		Columns = columns;
		EndsWithHardBreak = endsWithHardBreak;
		EndsWithSoftWrap = endsWithSoftWrap;
		IsClipped = isClipped;
		this.fragments = Array.AsReadOnly( fragments );
	}

	/// <summary>Gets the zero-based visual-line index.</summary>
	public int Index { get; }

	/// <summary>Gets the first represented source position.</summary>
	public CursesTextPosition SourceStart { get; }

	/// <summary>Gets the first source position after represented content.</summary>
	public CursesTextPosition SourceEnd { get; }

	/// <summary>Gets the absolute first terminal column.</summary>
	public int Column { get; }

	/// <summary>Gets the number of represented terminal columns.</summary>
	public int Columns { get; }

	/// <summary>Gets whether this line ends at a hard source break.</summary>
	public bool EndsWithHardBreak { get; }

	/// <summary>Gets whether this line ends at a soft wrap.</summary>
	public bool EndsWithSoftWrap { get; }

	/// <summary>Gets whether source content is hidden from this line.</summary>
	public bool IsClipped { get; }

	/// <summary>Gets the immutable presentation fragments.</summary>
	public IReadOnlyList<CursesTextFragment> Fragments => fragments;
}

/// <summary>Represents one immutable styled text fragment in a visual line.</summary>
public sealed class CursesTextFragment {
	internal CursesTextFragment(
		CursesTextPosition sourceStart,
		CursesTextPosition sourceEnd,
		int column,
		int columns,
		string text,
		CursesStyle style,
		CursesCellMetadata? metadata,
		bool isEllipsis
	) {
		SourceStart = sourceStart;
		SourceEnd = sourceEnd;
		Column = column;
		Columns = columns;
		Text = text;
		Style = style;
		Metadata = metadata;
		IsEllipsis = isEllipsis;
	}

	/// <summary>Gets the first represented source position.</summary>
	public CursesTextPosition SourceStart { get; }

	/// <summary>Gets the first source position after represented content.</summary>
	public CursesTextPosition SourceEnd { get; }

	/// <summary>Gets the fragment's absolute first terminal column.</summary>
	public int Column { get; }

	/// <summary>Gets the fragment's terminal-column width.</summary>
	public int Columns { get; }

	/// <summary>Gets the normalized complete text elements represented by this fragment.</summary>
	public string Text { get; }

	/// <summary>Gets the fragment's presentation style.</summary>
	public CursesStyle Style { get; }

	/// <summary>Gets the fragment's optional semantic metadata.</summary>
	public CursesCellMetadata? Metadata { get; }

	/// <summary>Gets whether this fragment is a synthesized ellipsis.</summary>
	public bool IsEllipsis { get; }
}
