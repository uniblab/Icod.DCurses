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

using System.Globalization;
using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Internal.Generated;

/// <summary>
/// Computes terminal display width for one Unicode text element.
/// </summary>
public interface ICursesTextWidthProvider {
	/// <summary>
	/// Returns the terminal column width of one text element.
	/// </summary>
	/// <param name="textElement">One nonempty Unicode text element.</param>
	/// <returns>Zero, one, or two terminal columns.</returns>
	int GetWidth( string textElement );
}

/// <summary>
/// Controls how East Asian Ambiguous characters consume terminal columns.
/// </summary>
public enum CursesAmbiguousWidthPolicy {
	/// <summary>Treat East Asian Ambiguous characters as one terminal column.</summary>
	Narrow = 0,

	/// <summary>Treat East Asian Ambiguous characters as two terminal columns.</summary>
	Wide = 1
}

/// <summary>
/// Provides the versioned Unicode display-width policy used by DCurses.
/// </summary>
/// <remarks>
/// The provider uses Unicode 17.0.0 East Asian Width and Emoji data for scalar classification
/// and complete Unicode text elements for emoji/keycap/flag/ZWJ presentation decisions.
/// East Asian Ambiguous characters are narrow by default; applications which require wide
/// Ambiguous semantics can explicitly select <see cref="WideAmbiguousInstance"/>.
/// </remarks>
public sealed class UnicodeCursesTextWidthProvider
	: ICursesTextWidthProvider {
	private static readonly UnicodeCursesTextWidthProvider instance = new(
		CursesAmbiguousWidthPolicy.Narrow
	);
	private static readonly UnicodeCursesTextWidthProvider wideAmbiguousInstance = new(
		CursesAmbiguousWidthPolicy.Wide
	);

	private UnicodeCursesTextWidthProvider(
		CursesAmbiguousWidthPolicy ambiguousWidthPolicy
	) {
		if ( !Enum.IsDefined( ambiguousWidthPolicy ) ) {
			throw new ArgumentOutOfRangeException( nameof( ambiguousWidthPolicy ) );
		}

		this.AmbiguousWidthPolicy = ambiguousWidthPolicy;
	}

	/// <summary>Gets the shared default provider, which treats East Asian Ambiguous characters as narrow.</summary>
	public static UnicodeCursesTextWidthProvider Instance => instance;

	/// <summary>Gets the shared provider which treats East Asian Ambiguous characters as wide.</summary>
	public static UnicodeCursesTextWidthProvider WideAmbiguousInstance => wideAmbiguousInstance;

	/// <summary>Gets the Unicode data version used by the built-in width providers.</summary>
	public static string UnicodeDataVersion => UnicodeEastAsianWidthData.UnicodeVersion;

	/// <summary>Gets this provider's East Asian Ambiguous-width policy.</summary>
	public CursesAmbiguousWidthPolicy AmbiguousWidthPolicy {
		get;
	}

	/// <inheritdoc />
	public int GetWidth( string textElement ) {
		ArgumentException.ThrowIfNullOrEmpty( textElement );

		string normalized = CursesUnicodeText.NormalizeMalformedUtf16( textElement );
		Rune? firstVisible = null;
		bool hasEmojiCandidate = false;
		bool hasZeroWidthJoiner = false;
		bool hasTextPresentationSelector = false;
		bool hasEmojiPresentationSelector = false;
		bool hasKeycap = false;
		int regionalIndicatorCount = 0;

		foreach ( Rune rune in normalized.EnumerateRunes() ) {
			int value = rune.Value;
			if ( 0x200D == value ) {
				hasZeroWidthJoiner = true;
				continue;
			}
			if ( 0xFE0F == value ) {
				hasEmojiPresentationSelector = true;
				continue;
			}
			if ( 0xFE0E == value ) {
				hasTextPresentationSelector = true;
				continue;
			}
			if ( 0x20E3 == value ) {
				hasKeycap = true;
				continue;
			}
			if ( IsRegionalIndicator( value ) ) {
				regionalIndicatorCount++;
			}
			if ( IsEmojiCandidate( value ) ) {
				hasEmojiCandidate = true;
			}

			UnicodeCategory category = Rune.GetUnicodeCategory( rune );
			if ( !IsZeroWidthCategory( category ) && !firstVisible.HasValue ) {
				firstVisible = rune;
			}
		}

		if ( !firstVisible.HasValue ) {
			return 0;
		}

		int firstValue = firstVisible.Value.Value;
		if ( hasTextPresentationSelector ) {
			return GetBaseWidth( firstValue );
		}
		if ( hasKeycap && IsKeycapBase( firstValue ) ) {
			return 2;
		}
		if ( 2 <= regionalIndicatorCount && IsRegionalIndicator( firstValue ) ) {
			return 2;
		}
		if ( hasEmojiPresentationSelector && IsEmojiCandidate( firstValue ) ) {
			return 2;
		}
		if ( hasZeroWidthJoiner && hasEmojiCandidate ) {
			return 2;
		}

		return GetBaseWidth( firstValue );
	}

	private int GetBaseWidth( int value ) {
		if ( IsInRanges(
			value,
			UnicodeEastAsianWidthData.WideOrFullwidthRanges
		) ) {
			return 2;
		}
		if ( CursesAmbiguousWidthPolicy.Wide == this.AmbiguousWidthPolicy
			&& IsInRanges(
				value,
				UnicodeEastAsianWidthData.AmbiguousRanges
			) ) {
			return 2;
		}

		return 1;
	}

	private static bool IsZeroWidthCategory( UnicodeCategory category ) {
		return category is UnicodeCategory.NonSpacingMark
			or UnicodeCategory.SpacingCombiningMark
			or UnicodeCategory.EnclosingMark
			or UnicodeCategory.Format;
	}

	private static bool IsKeycapBase( int value ) {
		return '#' == value
			|| '*' == value
			|| value is >= '0' and <= '9';
	}

	private static bool IsRegionalIndicator( int value ) {
		return value is >= 0x1F1E6 and <= 0x1F1FF;
	}

	private static bool IsEmojiCandidate( int value ) {
		return IsInRanges(
			value,
			UnicodeEmojiData.EmojiRanges
		);
	}

	private static bool IsInRanges(
		int value,
		UnicodeWidthRange[] ranges
	) {
		ArgumentNullException.ThrowIfNull( ranges );
		int low = 0;
		int high = ranges.Length - 1;
		while ( low <= high ) {
			int middle = low + ( ( high - low ) / 2 );
			UnicodeWidthRange range = ranges[ middle ];
			if ( value < range.First ) {
				high = middle - 1;
				continue;
			}
			if ( value > range.Last ) {
				low = middle + 1;
				continue;
			}

			return true;
		}

		return false;
	}
}
