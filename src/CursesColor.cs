namespace Icod.DCurses;

/// <summary>
/// Identifies how a curses color is represented semantically.
/// </summary>
public enum CursesColorKind {
	/// <summary>Use the terminal's default color.</summary>
	Default,

	/// <summary>Use an indexed terminal color.</summary>
	Indexed,

	/// <summary>Use an explicit RGB color.</summary>
	Rgb
}

/// <summary>
/// Represents a terminal-independent color request.
/// </summary>
/// <remarks>
/// A color carries semantic color information only. It never contains a terminal escape sequence.
/// Capability mapping is owned by the refresh/output layer.
/// </remarks>
public readonly record struct CursesColor {
	private CursesColor(
		CursesColorKind kind,
		int? index,
		byte? red,
		byte? green,
		byte? blue ) {
		Kind = kind;
		Index = index;
		Red = red;
		Green = green;
		Blue = blue;
	}

	/// <summary>Gets the terminal-default color.</summary>
	public static CursesColor Default => default;

	/// <summary>Gets the semantic representation kind.</summary>
	public CursesColorKind Kind {
		get;
	}

	/// <summary>Gets the indexed color number when <see cref="Kind"/> is <see cref="CursesColorKind.Indexed"/>.</summary>
	public int? Index {
		get;
	}

	/// <summary>Gets the red component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Red {
		get;
	}

	/// <summary>Gets the green component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Green {
		get;
	}

	/// <summary>Gets the blue component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Blue {
		get;
	}

	/// <summary>Gets whether this color requests the terminal default.</summary>
	public bool IsDefault => CursesColorKind.Default == Kind;

	/// <summary>Creates an indexed-color request.</summary>
	/// <param name="index">The non-negative terminal color index.</param>
	/// <returns>The semantic indexed color.</returns>
	public static CursesColor Indexed( int index ) {
		if ( 0 > index ) {
			throw new ArgumentOutOfRangeException(
				nameof( index ),
				index,
				"A terminal color index cannot be negative."
			);
		}

		return new CursesColor(
			CursesColorKind.Indexed,
			index,
			null,
			null,
			null
		);
	}

	/// <summary>Creates a direct RGB color request.</summary>
	/// <param name="red">The red component.</param>
	/// <param name="green">The green component.</param>
	/// <param name="blue">The blue component.</param>
	/// <returns>The semantic RGB color.</returns>
	public static CursesColor Rgb(
		byte red,
		byte green,
		byte blue ) {
		return new CursesColor(
			CursesColorKind.Rgb,
			null,
			red,
			green,
			blue
		);
	}
}
