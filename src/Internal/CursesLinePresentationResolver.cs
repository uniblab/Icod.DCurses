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

using Icod.TermInfo;

/// <summary>Represents one physical terminal rendering of a semantic line glyph.</summary>
internal readonly record struct CursesPhysicalLineGlyph(
	string Content,
	bool UsesAlternateCharacterSet
);

/// <summary>Resolves semantic line glyphs through terminal ACS metadata with Unicode/ASCII fallback.</summary>
internal sealed class CursesLinePresentationResolver {
	private readonly TerminalDescription terminal;

	internal CursesLinePresentationResolver( TerminalDescription terminal ) {
		ArgumentNullException.ThrowIfNull( terminal );
		this.terminal = terminal;
	}

	internal CursesPhysicalLineGlyph Resolve(
		CursesLineGlyph glyph,
		ICursesTextWidthProvider textWidthProvider
	) {
		if ( !Enum.IsDefined( glyph ) ) {
			throw new ArgumentOutOfRangeException( nameof( glyph ) );
		}
		ArgumentNullException.ThrowIfNull( textWidthProvider );

		if ( TryResolveAlternateCharacterSet(
			glyph,
			out string alternateContent
		) ) {
			return new CursesPhysicalLineGlyph(
				alternateContent,
				UsesAlternateCharacterSet: true
			);
		}

		string unicodeContent = CursesLineGlyphInfo.GetCanonicalContent( glyph );
		if ( 1 == textWidthProvider.GetWidth( unicodeContent ) ) {
			return new CursesPhysicalLineGlyph(
				unicodeContent,
				UsesAlternateCharacterSet: false
			);
		}

		return new CursesPhysicalLineGlyph(
			GetAsciiContent( glyph ),
			UsesAlternateCharacterSet: false
		);
	}

	private bool TryResolveAlternateCharacterSet(
		CursesLineGlyph glyph,
		out string content
	) {
		content = string.Empty;
		if ( null == terminal.GetString( StringCapability.EnterAlternateCharacterSetMode )
			|| null == terminal.GetString( StringCapability.ExitAlternateCharacterSetMode ) ) {
			return false;
		}

		string? mapping = terminal.GetString( StringCapability.AlternateCharacterSet );
		if ( string.IsNullOrEmpty( mapping ) ) {
			return false;
		}

		char source = GetAlternateCharacterSetSource( glyph );
		for ( int index = 0; index + 1 < mapping.Length; index += 2 ) {
			if ( mapping[ index ] != source ) {
				continue;
			}

			char mapped = mapping[ index + 1 ];
			if ( char.IsControl( mapped ) ) {
				return false;
			}

			content = mapped.ToString();
			return true;
		}

		return false;
	}

	private static char GetAlternateCharacterSetSource( CursesLineGlyph glyph ) {
		return glyph switch {
			CursesLineGlyph.Horizontal => 'q',
			CursesLineGlyph.Vertical => 'x',
			CursesLineGlyph.UpperLeftCorner => 'l',
			CursesLineGlyph.UpperRightCorner => 'k',
			CursesLineGlyph.LowerLeftCorner => 'm',
			CursesLineGlyph.LowerRightCorner => 'j',
			CursesLineGlyph.TeeUp => 'v',
			CursesLineGlyph.TeeDown => 'w',
			CursesLineGlyph.TeeLeft => 'u',
			CursesLineGlyph.TeeRight => 't',
			CursesLineGlyph.Crossing => 'n',
			_ => throw new ArgumentOutOfRangeException(
				nameof( glyph ),
				glyph,
				"Unknown curses line glyph."
			)
		};
	}

	private static string GetAsciiContent( CursesLineGlyph glyph ) {
		return glyph switch {
			CursesLineGlyph.Horizontal => "-",
			CursesLineGlyph.Vertical => "|",
			CursesLineGlyph.UpperLeftCorner
				or CursesLineGlyph.UpperRightCorner
				or CursesLineGlyph.LowerLeftCorner
				or CursesLineGlyph.LowerRightCorner
				or CursesLineGlyph.TeeUp
				or CursesLineGlyph.TeeDown
				or CursesLineGlyph.TeeLeft
				or CursesLineGlyph.TeeRight
				or CursesLineGlyph.Crossing => "+",
			_ => throw new ArgumentOutOfRangeException(
				nameof( glyph ),
				glyph,
				"Unknown curses line glyph."
			)
		};
	}
}
