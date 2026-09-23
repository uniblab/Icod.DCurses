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

using Icod.DCurses.Internal;

/// <summary>Represents one immutable, terminal-independent rich-text layout.</summary>
public sealed partial class CursesTextLayout {
	private readonly IReadOnlyList<CursesTextSpan> spans;
	private readonly IReadOnlyList<CursesTextVisualLine> lines;

	internal CursesTextLayout(
		string text,
		CursesTextLayoutOptions options,
		CursesTextSpan[] spans,
		CursesTextVisualLine[] lines,
		int cellCount,
		bool isTruncated
	) {
		Text = text;
		Options = options;
		this.spans = Array.AsReadOnly( spans );
		this.lines = Array.AsReadOnly( lines );
		CellCount = cellCount;
		IsTruncated = isTruncated;
	}

	/// <summary>Creates an immutable rich-text layout.</summary>
	/// <param name="text">The source text.</param>
	/// <param name="options">The layout options.</param>
	/// <param name="spans">Optional ordered, non-overlapping presentation spans.</param>
	/// <returns>The completed immutable layout.</returns>
	public static CursesTextLayout Create(
		string text,
		CursesTextLayoutOptions options,
		IReadOnlyList<CursesTextSpan>? spans = null
	) {
		return CursesTextLayoutBuilder.Build(
			text,
			options,
			spans
		);
	}

	/// <summary>Gets the caller's immutable source text.</summary>
	public string Text { get; }

	/// <summary>Gets the validated immutable option copy.</summary>
	public CursesTextLayoutOptions Options { get; }

	/// <summary>Gets the copied presentation spans.</summary>
	public IReadOnlyList<CursesTextSpan> Spans => spans;

	/// <summary>Gets the immutable visual lines.</summary>
	public IReadOnlyList<CursesTextVisualLine> Lines => lines;

	/// <summary>Gets the number of produced terminal cells.</summary>
	public int CellCount { get; }

	/// <summary>Gets whether valid source content was hidden.</summary>
	public bool IsTruncated { get; }
}
