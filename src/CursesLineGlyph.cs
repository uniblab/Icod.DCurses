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
