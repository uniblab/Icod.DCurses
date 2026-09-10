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

using System.Text;

/// <summary>
/// Represents one logical terminal screen cell.
/// </summary>
/// <remarks>
/// A continuation cell reserves a following column for content whose display width spans multiple
/// terminal columns. Width calculation is owned by the screen's <see cref="ICursesTextWidthProvider"/>.
/// </remarks>
public readonly struct CursesCell
	: IEquatable<CursesCell> {
	private readonly string? content;
	private readonly byte displayWidth;
	private readonly CursesLineGlyph? lineGlyph;

	/// <summary>Initializes a one-column visible or blank logical cell.</summary>
	/// <remarks>
	/// Application text should normally be written through <see cref="CursesWindow"/> so the
	/// screen's configured text-width provider can determine multi-column and combining behavior.
	/// </remarks>
	/// <param name="content">Visible text content, or an empty string for a blank cell.</param>
	/// <param name="style">The semantic cell style.</param>
	public CursesCell(
		string content,
		CursesStyle style = default
	) {
		ArgumentNullException.ThrowIfNull( content );
		ValidateVisibleContent( content );

		this.content = content;
		this.displayWidth = 1;
		this.lineGlyph = null;
		this.Style = style;
		this.IsContinuation = false;
	}

	/// <summary>Initializes a leading logical cell with an explicit terminal display width.</summary>
	/// <param name="content">The visible text content.</param>
	/// <param name="style">The semantic cell style.</param>
	/// <param name="displayWidth">The terminal column width, either one or two.</param>
	internal CursesCell(
		string content,
		CursesStyle style,
		int displayWidth ) {
		ArgumentNullException.ThrowIfNull( content );
		if ( displayWidth < 1 || displayWidth > 2 ) {
			throw new ArgumentOutOfRangeException( nameof( displayWidth ) );
		}
		ValidateVisibleContent( content );

		this.content = content;
		this.displayWidth = (byte)displayWidth;
		this.lineGlyph = null;
		Style = style;
		IsContinuation = false;
	}

	private CursesCell(
		string content,
		CursesStyle style,
		CursesLineGlyph lineGlyph ) {
		ArgumentNullException.ThrowIfNull( content );
		ValidateVisibleContent( content );

		this.content = content;
		this.displayWidth = 1;
		this.lineGlyph = lineGlyph;
		Style = style;
		IsContinuation = false;
	}

	private CursesCell(
		CursesStyle style,
		bool isContinuation ) {
		content = string.Empty;
		displayWidth = 1;
		lineGlyph = null;
		Style = style;
		IsContinuation = isContinuation;
	}

	/// <summary>Gets the visible text content. Blank and continuation cells return an empty string.</summary>
	public string Content => content ?? string.Empty;

	/// <summary>Gets the number of terminal columns occupied by this leading cell.</summary>
	public int DisplayWidth => IsContinuation
		? 0
		: 0 == displayWidth
			? 1
			: displayWidth
	;

	/// <summary>Gets the semantic cell style.</summary>
	public CursesStyle Style {
		get;
	}

	/// <summary>Gets the semantic line glyph represented by this cell, when applicable.</summary>
	public CursesLineGlyph? LineGlyph => lineGlyph;

	/// <summary>Gets whether this cell represents semantic line-drawing content.</summary>
	public bool IsLineGlyph => lineGlyph.HasValue;

	/// <summary>Gets whether this cell is a continuation column of preceding multi-column content.</summary>
	public bool IsContinuation {
		get;
	}

	/// <summary>Gets whether this is an ordinary blank cell.</summary>
	public bool IsBlank => !IsContinuation && 0 == Content.Length;

	/// <summary>Creates a blank cell with the supplied style.</summary>
	/// <param name="style">The semantic style assigned to the blank cell.</param>
	/// <returns>A blank logical cell.</returns>
	public static CursesCell Blank( CursesStyle style = default ) {
		return new CursesCell(
			style,
			isContinuation: false
		);
	}

	/// <summary>Creates a continuation cell for preceding multi-column content.</summary>
	/// <param name="style">The semantic style associated with the multi-column content.</param>
	/// <returns>A continuation logical cell.</returns>
	public static CursesCell Continuation( CursesStyle style = default ) {
		return new CursesCell(
			style,
			isContinuation: true
		);
	}

	/// <summary>Creates a one-column cell carrying semantic line-drawing identity.</summary>
	/// <param name="glyph">The semantic line glyph.</param>
	/// <param name="style">The semantic style assigned to the line cell.</param>
	/// <returns>A semantic line-drawing cell with canonical Unicode content.</returns>
	public static CursesCell Line(
		CursesLineGlyph glyph,
		CursesStyle style = default
	) {
		if ( !Enum.IsDefined( glyph ) ) {
			throw new ArgumentOutOfRangeException( nameof( glyph ) );
		}

		return new CursesCell(
			CursesLineGlyphInfo.GetCanonicalContent( glyph ),
			style,
			glyph
		);
	}

	/// <inheritdoc />
	public bool Equals( CursesCell other ) {
		return IsContinuation == other.IsContinuation
			&& DisplayWidth == other.DisplayWidth
			&& Style == other.Style
			&& LineGlyph == other.LineGlyph
			&& string.Equals(
				Content,
				other.Content,
				StringComparison.Ordinal
			)
		;
	}

	/// <inheritdoc />
	public override bool Equals( object? obj ) {
		return obj is CursesCell other
			&& Equals( other );
	}

	/// <inheritdoc />
	public override int GetHashCode() {
		return HashCode.Combine(
			Content,
			DisplayWidth,
			Style,
			IsContinuation,
			LineGlyph
		);
	}

	/// <summary>Tests two cells for semantic equality.</summary>
	/// <param name="left">The left cell.</param>
	/// <param name="right">The right cell.</param>
	/// <returns><see langword="true"/> when the cells are semantically equal.</returns>
	public static bool operator ==(
		CursesCell left,
		CursesCell right ) {
		return left.Equals( right );
	}

	/// <summary>Tests two cells for semantic inequality.</summary>
	/// <param name="left">The left cell.</param>
	/// <param name="right">The right cell.</param>
	/// <returns><see langword="true"/> when the cells are not semantically equal.</returns>
	public static bool operator !=(
		CursesCell left,
		CursesCell right ) {
		return !left.Equals( right );
	}

	private static void ValidateVisibleContent( string content ) {
		ArgumentNullException.ThrowIfNull( content );

		foreach ( Rune rune in content.EnumerateRunes() ) {
			int value = rune.Value;
			if ( value <= 0x1F
				|| ( value >= 0x7F && value <= 0x9F ) ) {
				throw new ArgumentException(
					"Cell content cannot contain terminal control characters or escape sequences.",
					nameof( content )
				);
			}
		}
	}
}
