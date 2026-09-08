namespace Icod.DCurses;

using System.Globalization;
using System.Text;
using Icod.DCurses.Internal;

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
/// Provides the default Unicode display-width policy used by DCurses.
/// </summary>
/// <remarks>
/// The 0.3 development line evaluates the complete Unicode text element for sequence families whose
/// terminal width cannot be determined from the first scalar alone. East Asian Ambiguous characters
/// remain narrow by default; the explicit selectable ambiguous-width policy is completed by T303.
/// </remarks>
public sealed class UnicodeCursesTextWidthProvider
	: ICursesTextWidthProvider {
	private static readonly UnicodeCursesTextWidthProvider instance = new();

	private UnicodeCursesTextWidthProvider() {
	}

	/// <summary>Gets the shared default width provider.</summary>
	public static UnicodeCursesTextWidthProvider Instance => instance;

	/// <inheritdoc />
	public int GetWidth( string textElement ) {
		ArgumentException.ThrowIfNullOrEmpty( textElement );

		string normalized = CursesUnicodeText.NormalizeMalformedUtf16( textElement );
		Rune? firstVisible = null;
		bool hasEmojiCandidate = false;
		bool hasZeroWidthJoiner = false;
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

		return IsWide( firstValue )
			? 2
			: 1
		;
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
		return value is >= 0x1F000 and <= 0x1FAFF
			|| value is >= 0x2600 and <= 0x27BF;
	}

	private static bool IsWide( int value ) {
		return value >= 0x1100
			&& (
				value <= 0x115F
				|| 0x2329 == value
				|| 0x232A == value
				|| ( value >= 0x2E80 && value <= 0xA4CF && 0x303F != value )
				|| ( value >= 0xAC00 && value <= 0xD7A3 )
				|| ( value >= 0xF900 && value <= 0xFAFF )
				|| ( value >= 0xFE10 && value <= 0xFE19 )
				|| ( value >= 0xFE30 && value <= 0xFE6F )
				|| ( value >= 0xFF00 && value <= 0xFF60 )
				|| ( value >= 0xFFE0 && value <= 0xFFE6 )
				|| ( value >= 0x1F300 && value <= 0x1FAFF )
				|| ( value >= 0x20000 && value <= 0x3FFFD )
			);
	}
}
