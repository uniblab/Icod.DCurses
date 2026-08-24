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

	/// <summary>Initializes a visible or blank logical cell.</summary>
	/// <param name="content">Visible text content, or an empty string for a blank cell.</param>
	/// <param name="style">The semantic cell style.</param>
	public CursesCell(
		string content,
		CursesStyle style = default )
		: this(
			content,
			style,
			0 == content.Length
				? 1
				: 1
		) {
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
		Style = style;
		IsContinuation = false;
	}

	private CursesCell(
		CursesStyle style,
		bool isContinuation ) {
		content = string.Empty;
		displayWidth = 1;
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

	/// <inheritdoc />
	public bool Equals( CursesCell other ) {
		return IsContinuation == other.IsContinuation
			&& DisplayWidth == other.DisplayWidth
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
			DisplayWidth,
			Style,
			IsContinuation
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
