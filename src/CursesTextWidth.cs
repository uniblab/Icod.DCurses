namespace Icod.DCurses;

using System.Buffers;
using System.Globalization;
using System.Text;

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
/// Provides the conservative Unicode display-width policy used by DCurses 0.1.
/// </summary>
/// <remarks>
/// This policy intentionally centralizes display-width ownership so later Unicode releases can refine
/// East Asian, emoji, and locale-sensitive behavior without changing the window API.
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

		Rune first = GetFirstRune( textElement );
		UnicodeCategory category = Rune.GetUnicodeCategory( first );
		if ( IsZeroWidthCategory( category ) ) {
			return 0;
		}

		return IsWide( first.Value )
			? 2
			: 1
		;
	}

	private static Rune GetFirstRune( string text ) {
		OperationStatus status = Rune.DecodeFromUtf16(
			text.AsSpan(),
			out Rune rune,
			out _
		);
		return OperationStatus.Done == status
			? rune
			: Rune.ReplacementChar
		;
	}

	private static bool IsZeroWidthCategory( UnicodeCategory category ) {
		return category is UnicodeCategory.NonSpacingMark
			or UnicodeCategory.SpacingCombiningMark
			or UnicodeCategory.EnclosingMark
			or UnicodeCategory.Format;
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
