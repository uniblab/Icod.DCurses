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
using Icod.DCurses.Internal;

/// <summary>
/// Provides Unicode text operations expressed in terminal display columns.
/// </summary>
/// <remarks>
/// These helpers use the same malformed-UTF-16 normalization, Unicode text-element segmentation,
/// and width-provider contract as <see cref="CursesWindow"/>. Terminal control characters are not
/// accepted by these printable-text helpers.
/// </remarks>
public static class CursesText {
	/// <summary>Measures printable text in terminal display columns.</summary>
	/// <param name="text">The text to measure.</param>
	/// <param name="textWidthProvider">
	/// The width provider to use, or <see langword="null"/> for
	/// <see cref="UnicodeCursesTextWidthProvider.Instance"/>.
	/// </param>
	/// <returns>The number of terminal columns occupied by the normalized text.</returns>
	public static int MeasureColumns(
		string text,
		ICursesTextWidthProvider? textWidthProvider = null
	) {
		ArgumentNullException.ThrowIfNull( text );
		ICursesTextWidthProvider provider = ResolveProvider( textWidthProvider );

		int columns = 0;
		foreach ( string textElement in EnumeratePrintableTextElements( text ) ) {
			int width = GetValidatedWidth(
				provider,
				textElement
			);
			columns = checked( columns + width );
		}

		return columns;
	}

	/// <summary>
	/// Returns the longest normalized printable prefix which fits in the requested terminal-column limit.
	/// </summary>
	/// <param name="text">The text to truncate.</param>
	/// <param name="maxColumns">The nonnegative maximum terminal-column width.</param>
	/// <param name="textWidthProvider">
	/// The width provider to use, or <see langword="null"/> for
	/// <see cref="UnicodeCursesTextWidthProvider.Instance"/>.
	/// </param>
	/// <returns>A normalized prefix containing only complete Unicode text elements.</returns>
	/// <remarks>
	/// A two-column element is omitted when it would cross the requested limit. Leading zero-width
	/// elements are omitted because they have no visible cell to attach to; zero-width elements following
	/// an included visible element are retained.
	/// </remarks>
	public static string TruncateToColumns(
		string text,
		int maxColumns,
		ICursesTextWidthProvider? textWidthProvider = null
	) {
		ArgumentNullException.ThrowIfNull( text );
		if ( 0 > maxColumns ) {
			throw new ArgumentOutOfRangeException( nameof( maxColumns ) );
		}
		ICursesTextWidthProvider provider = ResolveProvider( textWidthProvider );

		StringBuilder result = new();
		int columns = 0;
		bool hasIncludedVisibleElement = false;
		foreach ( string textElement in EnumeratePrintableTextElements( text ) ) {
			int width = GetValidatedWidth(
				provider,
				textElement
			);
			if ( 0 == width ) {
				if ( hasIncludedVisibleElement ) {
					result.Append( textElement );
				}
				continue;
			}
			if ( width > maxColumns - columns ) {
				break;
			}

			result.Append( textElement );
			columns += width;
			hasIncludedVisibleElement = true;
		}

		return result.ToString();
	}

	/// <summary>
	/// Returns complete normalized Unicode text elements whose terminal-column spans lie wholly inside
	/// the requested column interval.
	/// </summary>
	/// <param name="text">The text to slice.</param>
	/// <param name="startColumn">The nonnegative starting terminal column.</param>
	/// <param name="columnCount">The nonnegative number of terminal columns in the requested interval.</param>
	/// <param name="textWidthProvider">
	/// The width provider to use, or <see langword="null"/> for
	/// <see cref="UnicodeCursesTextWidthProvider.Instance"/>.
	/// </param>
	/// <returns>A normalized string containing only complete elements inside the requested interval.</returns>
	/// <remarks>
	/// The interval is half-open: <c>[startColumn, startColumn + columnCount)</c>. If either boundary falls
	/// inside a two-column element, that element is omitted rather than split. A zero-width element is retained
	/// only when it follows an included visible element, matching the attachment behavior of window writes.
	/// </remarks>
	public static string SliceByColumns(
		string text,
		int startColumn,
		int columnCount,
		ICursesTextWidthProvider? textWidthProvider = null
	) {
		ArgumentNullException.ThrowIfNull( text );
		if ( 0 > startColumn ) {
			throw new ArgumentOutOfRangeException( nameof( startColumn ) );
		}
		if ( 0 > columnCount ) {
			throw new ArgumentOutOfRangeException( nameof( columnCount ) );
		}
		ICursesTextWidthProvider provider = ResolveProvider( textWidthProvider );
		int endColumn = checked( startColumn + columnCount );

		StringBuilder result = new();
		int currentColumn = 0;
		bool previousVisibleElementIncluded = false;
		foreach ( string textElement in EnumeratePrintableTextElements( text ) ) {
			int width = GetValidatedWidth(
				provider,
				textElement
			);
			if ( 0 == width ) {
				if ( previousVisibleElementIncluded ) {
					result.Append( textElement );
				}
				continue;
			}

			int elementStart = currentColumn;
			int elementEnd = checked( elementStart + width );
			bool include = elementStart >= startColumn
				&& elementEnd <= endColumn;
			if ( include ) {
				result.Append( textElement );
			}
			previousVisibleElementIncluded = include;
			currentColumn = elementEnd;
		}

		return result.ToString();
	}

	private static ICursesTextWidthProvider ResolveProvider(
		ICursesTextWidthProvider? textWidthProvider
	) {
		return textWidthProvider
			?? UnicodeCursesTextWidthProvider.Instance;
	}

	private static IEnumerable<string> EnumeratePrintableTextElements(
		string text
	) {
		ArgumentNullException.ThrowIfNull( text );
		foreach ( string textElement in CursesUnicodeText.EnumerateTextElements( text ) ) {
			ValidatePrintableTextElement( textElement );
			yield return textElement;
		}
	}

	private static void ValidatePrintableTextElement(
		string textElement
	) {
		ArgumentException.ThrowIfNullOrEmpty( textElement );
		foreach ( Rune rune in textElement.EnumerateRunes() ) {
			if ( rune.Value <= 0x1F
				|| rune.Value is >= 0x7F and <= 0x9F ) {
				throw new ArgumentException(
					"Column-oriented text helpers cannot process terminal control characters.",
					"text"
				);
			}
		}
	}

	private static int GetValidatedWidth(
		ICursesTextWidthProvider provider,
		string textElement
	) {
		ArgumentNullException.ThrowIfNull( provider );
		ArgumentException.ThrowIfNullOrEmpty( textElement );
		int width = provider.GetWidth( textElement );
		if ( width < 0 || 2 < width ) {
			throw new InvalidOperationException(
				"The configured curses text-width provider returned a width outside the supported range."
			);
		}

		return width;
	}
}
