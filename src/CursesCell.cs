namespace Icod.DCurses;

using System.Text;

/// <summary>
/// Represents one logical terminal screen cell.
/// </summary>
/// <remarks>
/// A continuation cell reserves a following column for content whose display width spans multiple
/// terminal columns. Width calculation and continuation placement policy are owned by later text/window layers.
/// </remarks>
public readonly struct CursesCell
	: IEquatable<CursesCell> {
	private readonly string? content;

	/// <summary>Initializes a visible or blank logical cell.</summary>
	/// <param name="content">Visible text content, or an empty string for a blank cell.</param>
	/// <param name="style">The semantic cell style.</param>
	public CursesCell(
		string content,
		CursesStyle style = default ) {
		ArgumentNullException.ThrowIfNull( content );
		ValidateVisibleContent( content );

		this.content = content;
		Style = style;
		IsContinuation = false;
	}

	private CursesCell(
		CursesStyle style,
		bool isContinuation ) {
		content = string.Empty;
		Style = style;
		IsContinuation = isContinuation;
	}

	/// <summary>Gets the visible text content. Blank and continuation cells return an empty string.</summary>
	public string Content => content ?? string.Empty;

	/// <summary>Gets the semantic cell style.</summary>
	public CursesStyle Style {
		get;
	}

	/// <summary>Gets whether this cell is a continuation column of preceding multi-column content.</summary>
	public bool IsContinuation {
		get;
	}

	/// <summary>Gets whether this is an ordinary blank cell.</summary>
	public bool IsBlank => !IsContinuation && 0 == Content.Length;

	/// <summary>Creates a blank cell with the supplied style.</summary>
	public static CursesCell Blank( CursesStyle style = default ) {
		return new CursesCell(
			style,
			isContinuation: false
		);
	}

	/// <summary>Creates a continuation cell for preceding multi-column content.</summary>
	public static CursesCell Continuation( CursesStyle style = default ) {
		return new CursesCell(
			style,
			isContinuation: true
		);
	}

	/// <inheritdoc />
	public bool Equals( CursesCell other ) {
		return IsContinuation == other.IsContinuation
			&& Style == other.Style
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
			Style,
			IsContinuation
		);
	}

	/// <summary>Tests two cells for semantic equality.</summary>
	public static bool operator ==(
		CursesCell left,
		CursesCell right ) {
		return left.Equals( right );
	}

	/// <summary>Tests two cells for semantic inequality.</summary>
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
