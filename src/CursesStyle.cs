namespace Icod.DCurses;

/// <summary>
/// Identifies terminal-independent text rendition attributes.
/// </summary>
[Flags]
public enum CursesTextAttributes {
	/// <summary>No special rendition attribute.</summary>
	None = 0,

	/// <summary>Bold or intense rendition.</summary>
	Bold = 1,

	/// <summary>Dim rendition.</summary>
	Dim = 2,

	/// <summary>Underline rendition.</summary>
	Underline = 4,

	/// <summary>Reverse-video rendition.</summary>
	Reverse = 8,

	/// <summary>Standout rendition or the terminal's managed semantic equivalent.</summary>
	Standout = 16
}

/// <summary>
/// Describes terminal-independent rendition for one logical screen cell.
/// </summary>
public readonly record struct CursesStyle {
	private const CursesTextAttributes SupportedAttributes =
		CursesTextAttributes.Bold
		| CursesTextAttributes.Dim
		| CursesTextAttributes.Underline
		| CursesTextAttributes.Reverse
		| CursesTextAttributes.Standout
	;

	/// <summary>Initializes a cell style.</summary>
	/// <param name="foreground">The requested foreground color.</param>
	/// <param name="background">The requested background color.</param>
	/// <param name="attributes">The requested text attributes.</param>
	public CursesStyle(
		CursesColor foreground,
		CursesColor background,
		CursesTextAttributes attributes = CursesTextAttributes.None ) {
		if ( 0 != ( attributes & ~SupportedAttributes ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( attributes ),
				attributes,
				"The curses text attributes contain unsupported flags."
			);
		}

		Foreground = foreground;
		Background = background;
		Attributes = attributes;
	}

	/// <summary>Gets the default style using terminal-default colors and no attributes.</summary>
	public static CursesStyle Default => default;

	/// <summary>Gets the requested foreground color.</summary>
	public CursesColor Foreground {
		get;
	}

	/// <summary>Gets the requested background color.</summary>
	public CursesColor Background {
		get;
	}

	/// <summary>Gets the requested rendition attributes.</summary>
	public CursesTextAttributes Attributes {
		get;
	}

	/// <summary>Gets whether this is the default style.</summary>
	public bool IsDefault => default == this;

	/// <summary>Creates a copy with a different foreground color.</summary>
	/// <param name="foreground">The replacement foreground color.</param>
	/// <returns>A style with the requested foreground color.</returns>
	public CursesStyle WithForeground( CursesColor foreground ) {
		return new CursesStyle(
			foreground,
			Background,
			Attributes
		);
	}

	/// <summary>Creates a copy with a different background color.</summary>
	/// <param name="background">The replacement background color.</param>
	/// <returns>A style with the requested background color.</returns>
	public CursesStyle WithBackground( CursesColor background ) {
		return new CursesStyle(
			Foreground,
			background,
			Attributes
		);
	}

	/// <summary>Creates a copy with different rendition attributes.</summary>
	/// <param name="attributes">The replacement rendition attributes.</param>
	/// <returns>A style with the requested rendition attributes.</returns>
	public CursesStyle WithAttributes( CursesTextAttributes attributes ) {
		return new CursesStyle(
			Foreground,
			Background,
			attributes
		);
	}
}
