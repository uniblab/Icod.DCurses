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

/// <summary>Represents one physical terminal rendering of a semantic line glyph.</summary>
internal readonly record struct CursesPhysicalLineGlyph(
	string Content,
	bool UsesAlternateCharacterSet
);

/// <summary>Resolves semantic line glyphs through Terminal with Unicode/ASCII fallback.</summary>
internal sealed class CursesLinePresentationResolver {
	private readonly TerminalScreenPlanner planner;

	internal CursesLinePresentationResolver( TerminalScreenPlanner planner ) {
		ArgumentNullException.ThrowIfNull( planner );
		this.planner = planner;
	}

	internal CursesPhysicalLineGlyph Resolve(
		CursesLineGlyph glyph,
		ICursesTextWidthProvider textWidthProvider
	) {
		ArgumentNullException.ThrowIfNull( textWidthProvider );

		TerminalLineGlyph terminalGlyph = CursesTerminalScreenMapper.ToTerminal( glyph );
		TerminalLineGlyphRepresentation? representation =
			this.planner.ResolveLineGlyph( terminalGlyph );
		if ( representation.HasValue ) {
			return new CursesPhysicalLineGlyph(
				representation.Value.Content,
				representation.Value.UsesAlternateCharacterSet
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

	internal TerminalScreenOperationPlan? PlanAlternateCharacterSet(
		bool enabled
	) {
		return this.planner.PlanAlternateCharacterSet( enabled );
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
