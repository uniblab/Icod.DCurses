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

namespace Icod.DCurses.Internal;

using Icod.Terminal;

/// <summary>Converts explicitly between DCurses and Terminal screen semantics.</summary>
internal static class CursesTerminalScreenMapper {
	private const CursesTextAttributes KnownCursesAttributes =
		CursesTextAttributes.Bold
		| CursesTextAttributes.Dim
		| CursesTextAttributes.Underline
		| CursesTextAttributes.Reverse
		| CursesTextAttributes.Standout
		| CursesTextAttributes.Italic
		| CursesTextAttributes.Blink
		| CursesTextAttributes.Conceal
		| CursesTextAttributes.Strikeout;

	private const TerminalTextAttributes KnownTerminalAttributes =
		TerminalTextAttributes.Bold
		| TerminalTextAttributes.Dim
		| TerminalTextAttributes.Underline
		| TerminalTextAttributes.Reverse
		| TerminalTextAttributes.Standout
		| TerminalTextAttributes.Italic
		| TerminalTextAttributes.Blink
		| TerminalTextAttributes.Conceal
		| TerminalTextAttributes.Strikeout;

	internal static TerminalTextAttributes ToTerminal(
		CursesTextAttributes value
	) {
		if ( 0 != ( value & ~KnownCursesAttributes ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( value ),
				value,
				"The curses text attributes contain flags unknown to this Terminal mapping."
			);
		}

		TerminalTextAttributes result = TerminalTextAttributes.None;
		if ( 0 != ( value & CursesTextAttributes.Bold ) ) {
			result |= TerminalTextAttributes.Bold;
		}
		if ( 0 != ( value & CursesTextAttributes.Dim ) ) {
			result |= TerminalTextAttributes.Dim;
		}
		if ( 0 != ( value & CursesTextAttributes.Underline ) ) {
			result |= TerminalTextAttributes.Underline;
		}
		if ( 0 != ( value & CursesTextAttributes.Reverse ) ) {
			result |= TerminalTextAttributes.Reverse;
		}
		if ( 0 != ( value & CursesTextAttributes.Standout ) ) {
			result |= TerminalTextAttributes.Standout;
		}
		if ( 0 != ( value & CursesTextAttributes.Italic ) ) {
			result |= TerminalTextAttributes.Italic;
		}
		if ( 0 != ( value & CursesTextAttributes.Blink ) ) {
			result |= TerminalTextAttributes.Blink;
		}
		if ( 0 != ( value & CursesTextAttributes.Conceal ) ) {
			result |= TerminalTextAttributes.Conceal;
		}
		if ( 0 != ( value & CursesTextAttributes.Strikeout ) ) {
			result |= TerminalTextAttributes.Strikeout;
		}
		return result;
	}

	internal static CursesTextAttributes ToCurses(
		TerminalTextAttributes value
	) {
		if ( 0 != ( value & ~KnownTerminalAttributes ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( value ),
				value,
				"The Terminal text attributes contain flags unknown to this DCurses release."
			);
		}

		CursesTextAttributes result = CursesTextAttributes.None;
		if ( 0 != ( value & TerminalTextAttributes.Bold ) ) {
			result |= CursesTextAttributes.Bold;
		}
		if ( 0 != ( value & TerminalTextAttributes.Dim ) ) {
			result |= CursesTextAttributes.Dim;
		}
		if ( 0 != ( value & TerminalTextAttributes.Underline ) ) {
			result |= CursesTextAttributes.Underline;
		}
		if ( 0 != ( value & TerminalTextAttributes.Reverse ) ) {
			result |= CursesTextAttributes.Reverse;
		}
		if ( 0 != ( value & TerminalTextAttributes.Standout ) ) {
			result |= CursesTextAttributes.Standout;
		}
		if ( 0 != ( value & TerminalTextAttributes.Italic ) ) {
			result |= CursesTextAttributes.Italic;
		}
		if ( 0 != ( value & TerminalTextAttributes.Blink ) ) {
			result |= CursesTextAttributes.Blink;
		}
		if ( 0 != ( value & TerminalTextAttributes.Conceal ) ) {
			result |= CursesTextAttributes.Conceal;
		}
		if ( 0 != ( value & TerminalTextAttributes.Strikeout ) ) {
			result |= CursesTextAttributes.Strikeout;
		}
		return result;
	}

	internal static TerminalScreenColor ToTerminal(
		CursesColor value
	) {
		return value.Kind switch {
			CursesColorKind.Default => TerminalScreenColor.Default,
			CursesColorKind.Indexed => TerminalScreenColor.Indexed(
				value.Index ?? throw new InvalidOperationException(
					"An indexed curses color must contain an index."
				)
			),
			CursesColorKind.Rgb => TerminalScreenColor.Rgb(
				value.Red ?? throw new InvalidOperationException(
					"An RGB curses color must contain a red component."
				),
				value.Green ?? throw new InvalidOperationException(
					"An RGB curses color must contain a green component."
				),
				value.Blue ?? throw new InvalidOperationException(
					"An RGB curses color must contain a blue component."
				)
			),
			_ => throw new ArgumentOutOfRangeException(
				nameof( value ),
				value.Kind,
				"The curses color kind is not recognized by this Terminal mapping."
			)
		};
	}

	internal static CursesColor ToCurses(
		TerminalScreenColor value
	) {
		return value.Kind switch {
			TerminalScreenColorKind.Default => CursesColor.Default,
			TerminalScreenColorKind.Indexed => CursesColor.Indexed(
				value.Index ?? throw new InvalidOperationException(
					"An indexed Terminal color must contain an index."
				)
			),
			TerminalScreenColorKind.Rgb => CursesColor.Rgb(
				value.Red ?? throw new InvalidOperationException(
					"An RGB Terminal color must contain a red component."
				),
				value.Green ?? throw new InvalidOperationException(
					"An RGB Terminal color must contain a green component."
				),
				value.Blue ?? throw new InvalidOperationException(
					"An RGB Terminal color must contain a blue component."
				)
			),
			_ => throw new ArgumentOutOfRangeException(
				nameof( value ),
				value.Kind,
				"The Terminal color kind is not recognized by this DCurses release."
			)
		};
	}

	internal static TerminalScreenRendition ToTerminal(
		CursesStyle value
	) {
		return new TerminalScreenRendition(
			ToTerminal( value.Foreground ),
			ToTerminal( value.Background ),
			ToTerminal( value.Attributes )
		);
	}

	internal static CursesStyle ToCurses(
		TerminalScreenRendition value
	) {
		return new CursesStyle(
			ToCurses( value.Foreground ),
			ToCurses( value.Background ),
			ToCurses( value.Attributes )
		);
	}

	internal static TerminalLineGlyph ToTerminal(
		CursesLineGlyph value
	) {
		return value switch {
			CursesLineGlyph.Horizontal => TerminalLineGlyph.Horizontal,
			CursesLineGlyph.Vertical => TerminalLineGlyph.Vertical,
			CursesLineGlyph.UpperLeftCorner => TerminalLineGlyph.UpperLeftCorner,
			CursesLineGlyph.UpperRightCorner => TerminalLineGlyph.UpperRightCorner,
			CursesLineGlyph.LowerLeftCorner => TerminalLineGlyph.LowerLeftCorner,
			CursesLineGlyph.LowerRightCorner => TerminalLineGlyph.LowerRightCorner,
			CursesLineGlyph.TeeUp => TerminalLineGlyph.TeeUp,
			CursesLineGlyph.TeeDown => TerminalLineGlyph.TeeDown,
			CursesLineGlyph.TeeLeft => TerminalLineGlyph.TeeLeft,
			CursesLineGlyph.TeeRight => TerminalLineGlyph.TeeRight,
			CursesLineGlyph.Crossing => TerminalLineGlyph.Crossing,
			_ => throw new ArgumentOutOfRangeException(
				nameof( value ),
				value,
				"The curses line glyph is not recognized by this Terminal mapping."
			)
		};
	}

	internal static TerminalAlertKind ToTerminal(
		CursesAlertKind value
	) {
		return value switch {
			CursesAlertKind.Audible => TerminalAlertKind.Audible,
			CursesAlertKind.Visual => TerminalAlertKind.Visual,
			_ => throw new ArgumentOutOfRangeException(
				nameof( value ),
				value,
				"The curses alert kind is not recognized by this Terminal mapping."
			)
		};
	}

	internal static TerminalScreenPosition? ToTerminalPosition(
		int? row,
		int? column
	) {
		if ( row.HasValue != column.HasValue ) {
			throw new InvalidOperationException(
				"A known curses cursor position requires both row and column coordinates."
			);
		}
		if ( !row.HasValue ) {
			return null;
		}

		return new TerminalScreenPosition(
			row.Value,
			column!.Value
		);
	}
}
