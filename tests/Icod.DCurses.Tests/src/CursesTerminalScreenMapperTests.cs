/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Tests;

using Icod.DCurses.Internal;
using Icod.Terminal;
using Xunit;

/// <summary>Verifies explicit conversion between DCurses and Terminal screen semantics.</summary>
public sealed class CursesTerminalScreenMapperTests {
	[Theory]
	[InlineData( CursesTextAttributes.Bold, TerminalTextAttributes.Bold )]
	[InlineData( CursesTextAttributes.Dim, TerminalTextAttributes.Dim )]
	[InlineData( CursesTextAttributes.Underline, TerminalTextAttributes.Underline )]
	[InlineData( CursesTextAttributes.Reverse, TerminalTextAttributes.Reverse )]
	[InlineData( CursesTextAttributes.Standout, TerminalTextAttributes.Standout )]
	[InlineData( CursesTextAttributes.Italic, TerminalTextAttributes.Italic )]
	[InlineData( CursesTextAttributes.Blink, TerminalTextAttributes.Blink )]
	[InlineData( CursesTextAttributes.Conceal, TerminalTextAttributes.Conceal )]
	[InlineData( CursesTextAttributes.Strikeout, TerminalTextAttributes.Strikeout )]
	public void EveryCursesAttributeMapsExplicitly(
		CursesTextAttributes curses,
		TerminalTextAttributes terminal
	) {
		Assert.Equal( terminal, CursesTerminalScreenMapper.ToTerminal( curses ) );
		Assert.Equal( curses, CursesTerminalScreenMapper.ToCurses( terminal ) );
	}

	[Fact]
	public void CombinedAttributesMapWithoutAssumingNumericEquivalence() {
		CursesTextAttributes curses =
			CursesTextAttributes.Bold
			| CursesTextAttributes.Underline
			| CursesTextAttributes.Italic
			| CursesTextAttributes.Strikeout;
		TerminalTextAttributes terminal =
			TerminalTextAttributes.Bold
			| TerminalTextAttributes.Underline
			| TerminalTextAttributes.Italic
			| TerminalTextAttributes.Strikeout;

		Assert.Equal( terminal, CursesTerminalScreenMapper.ToTerminal( curses ) );
		Assert.Equal( curses, CursesTerminalScreenMapper.ToCurses( terminal ) );
	}

	[Fact]
	public void UnknownAttributeFlagsAreRejectedInBothDirections() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTerminalScreenMapper.ToTerminal(
				(CursesTextAttributes)( 1 << 20 )
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTerminalScreenMapper.ToCurses(
				(TerminalTextAttributes)( 1 << 20 )
			)
		);
	}

	[Fact]
	public void DefaultIndexedAndRgbColorsRoundTrip() {
		CursesColor[] colors = [
			CursesColor.Default,
			CursesColor.Indexed( 37 ),
			CursesColor.Rgb( 12, 34, 56 )
		];

		foreach ( CursesColor color in colors ) {
			TerminalScreenColor terminal = CursesTerminalScreenMapper.ToTerminal( color );

			Assert.Equal( color, CursesTerminalScreenMapper.ToCurses( terminal ) );
		}
	}

	[Fact]
	public void CompleteRenditionMapsColorsAndCombinedAttributes() {
		CursesStyle curses = new(
			CursesColor.Indexed( 5 ),
			CursesColor.Rgb( 10, 20, 30 ),
			CursesTextAttributes.Dim
				| CursesTextAttributes.Reverse
				| CursesTextAttributes.Conceal
		);

		TerminalScreenRendition terminal = CursesTerminalScreenMapper.ToTerminal( curses );

		Assert.Equal( TerminalScreenColor.Indexed( 5 ), terminal.Foreground );
		Assert.Equal( TerminalScreenColor.Rgb( 10, 20, 30 ), terminal.Background );
		Assert.Equal(
			TerminalTextAttributes.Dim
				| TerminalTextAttributes.Reverse
				| TerminalTextAttributes.Conceal,
			terminal.Attributes
		);
		Assert.Equal( curses, CursesTerminalScreenMapper.ToCurses( terminal ) );
	}

	[Theory]
	[InlineData( CursesLineGlyph.Horizontal, TerminalLineGlyph.Horizontal )]
	[InlineData( CursesLineGlyph.Vertical, TerminalLineGlyph.Vertical )]
	[InlineData( CursesLineGlyph.UpperLeftCorner, TerminalLineGlyph.UpperLeftCorner )]
	[InlineData( CursesLineGlyph.UpperRightCorner, TerminalLineGlyph.UpperRightCorner )]
	[InlineData( CursesLineGlyph.LowerLeftCorner, TerminalLineGlyph.LowerLeftCorner )]
	[InlineData( CursesLineGlyph.LowerRightCorner, TerminalLineGlyph.LowerRightCorner )]
	[InlineData( CursesLineGlyph.TeeUp, TerminalLineGlyph.TeeUp )]
	[InlineData( CursesLineGlyph.TeeDown, TerminalLineGlyph.TeeDown )]
	[InlineData( CursesLineGlyph.TeeLeft, TerminalLineGlyph.TeeLeft )]
	[InlineData( CursesLineGlyph.TeeRight, TerminalLineGlyph.TeeRight )]
	[InlineData( CursesLineGlyph.Crossing, TerminalLineGlyph.Crossing )]
	public void EveryLineGlyphMapsExplicitly(
		CursesLineGlyph curses,
		TerminalLineGlyph terminal
	) {
		Assert.Equal( terminal, CursesTerminalScreenMapper.ToTerminal( curses ) );
	}

	[Theory]
	[InlineData( CursesAlertKind.Audible, TerminalAlertKind.Audible )]
	[InlineData( CursesAlertKind.Visual, TerminalAlertKind.Visual )]
	public void EveryAlertKindMapsExplicitly(
		CursesAlertKind curses,
		TerminalAlertKind terminal
	) {
		Assert.Equal( terminal, CursesTerminalScreenMapper.ToTerminal( curses ) );
	}

	[Fact]
	public void UnknownLineGlyphAndAlertKindAreRejected() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTerminalScreenMapper.ToTerminal( (CursesLineGlyph)int.MaxValue )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTerminalScreenMapper.ToTerminal( (CursesAlertKind)int.MaxValue )
		);
	}

	[Fact]
	public void NullableCursorPositionPreservesKnownAndUnknownState() {
		Assert.Null( CursesTerminalScreenMapper.ToTerminalPosition( null, null ) );

		TerminalScreenPosition position = Assert.IsType<TerminalScreenPosition>(
			CursesTerminalScreenMapper.ToTerminalPosition( 7, 11 )
		);
		Assert.Equal( 7, position.Row );
		Assert.Equal( 11, position.Column );
	}

	[Theory]
	[InlineData( 1, null )]
	[InlineData( null, 1 )]
	public void PartiallyKnownCursorPositionIsRejected(
		int? row,
		int? column
	) {
		Assert.Throws<InvalidOperationException>(
			() => CursesTerminalScreenMapper.ToTerminalPosition( row, column )
		);
	}
}
