namespace Icod.DCurses;

/// <summary>
/// Identifies one semantic single-line box-drawing glyph independently of its physical terminal representation.
/// </summary>
public enum CursesLineGlyph {
	/// <summary>Horizontal line.</summary>
	Horizontal = 0,

	/// <summary>Vertical line.</summary>
	Vertical = 1,

	/// <summary>Upper-left corner.</summary>
	UpperLeftCorner = 2,

	/// <summary>Upper-right corner.</summary>
	UpperRightCorner = 3,

	/// <summary>Lower-left corner.</summary>
	LowerLeftCorner = 4,

	/// <summary>Lower-right corner.</summary>
	LowerRightCorner = 5,

	/// <summary>T-junction whose branch extends upward.</summary>
	TeeUp = 6,

	/// <summary>T-junction whose branch extends downward.</summary>
	TeeDown = 7,

	/// <summary>T-junction whose branch extends leftward.</summary>
	TeeLeft = 8,

	/// <summary>T-junction whose branch extends rightward.</summary>
	TeeRight = 9,

	/// <summary>Four-way crossing.</summary>
	Crossing = 10
}

/// <summary>Provides canonical logical representations for semantic line glyphs.</summary>
internal static class CursesLineGlyphInfo {
	internal static string GetCanonicalContent( CursesLineGlyph glyph ) {
		return glyph switch {
			CursesLineGlyph.Horizontal => "─",
			CursesLineGlyph.Vertical => "│",
			CursesLineGlyph.UpperLeftCorner => "┌",
			CursesLineGlyph.UpperRightCorner => "┐",
			CursesLineGlyph.LowerLeftCorner => "└",
			CursesLineGlyph.LowerRightCorner => "┘",
			CursesLineGlyph.TeeUp => "┴",
			CursesLineGlyph.TeeDown => "┬",
			CursesLineGlyph.TeeLeft => "┤",
			CursesLineGlyph.TeeRight => "├",
			CursesLineGlyph.Crossing => "┼",
			_ => throw new ArgumentOutOfRangeException(
				nameof( glyph ),
				glyph,
				"Unknown curses line glyph."
			)
		};
	}
}
