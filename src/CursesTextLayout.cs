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
		bool isTruncated,
		CursesTextElement[] elements
	) {
		Text = text;
		Options = options;
		this.spans = Array.AsReadOnly( spans );
		this.lines = Array.AsReadOnly( lines );
		CellCount = cellCount;
		IsTruncated = isTruncated;
		( legalOffsets, geometryLines ) = CreateGeometryIndexes(
			elements,
			lines,
			options
		);
	}

	/// <summary>Creates an immutable rich-text layout.</summary>
	/// <param name="text">The source text.</param>
	/// <param name="options">The layout options.</param>
	/// <param name="spans">Optional ordered, non-overlapping presentation spans.</param>
	/// <returns>The completed immutable layout.</returns>
	/// <remarks>
	/// Source text is limited to 16,777,216 UTF-16 code units and spans to 1,048,576.
	/// A result contains at most 4,194,304 fragments and 16,777,216 terminal cells.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="text"/>, <paramref name="options"/>, or the configured width provider is null.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A source, span, or option capacity is outside its supported range.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// A span is unordered, overlapping, outside the source, or not aligned to legal text-element boundaries.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// The width provider returns an unsupported width or the result exceeds its fragment or cell capacity.
	/// </exception>
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
