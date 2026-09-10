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

using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesColorStyleTests {
	[Fact]
	public void DefaultColorRepresentsTerminalDefaultWithoutEncodedOutput() {
		CursesColor color = CursesColor.Default;

		Assert.True( color.IsDefault );
		Assert.Equal( CursesColorKind.Default, color.Kind );
		Assert.Null( color.Index );
		Assert.Null( color.Red );
		Assert.Null( color.Green );
		Assert.Null( color.Blue );
	}

	[Fact]
	public void IndexedAndRgbColorsPreserveSemanticValues() {
		CursesColor indexed = CursesColor.Indexed( 237 );
		CursesColor rgb = CursesColor.Rgb( 12, 34, 56 );

		Assert.Equal( CursesColorKind.Indexed, indexed.Kind );
		Assert.Equal( (int?)237, indexed.Index );
		Assert.Equal( CursesColorKind.Rgb, rgb.Kind );
		Assert.Equal( (byte?)12, rgb.Red );
		Assert.Equal( (byte?)34, rgb.Green );
		Assert.Equal( (byte?)56, rgb.Blue );
	}

	[Fact]
	public void IndexedColorRejectsNegativeIndex() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesColor.Indexed( -1 )
		);
	}

	[Fact]
	public void TextAttributeNumericValuesRemainAppendOnly() {
		Assert.Equal( 0, (int)CursesTextAttributes.None );
		Assert.Equal( 1, (int)CursesTextAttributes.Bold );
		Assert.Equal( 2, (int)CursesTextAttributes.Dim );
		Assert.Equal( 4, (int)CursesTextAttributes.Underline );
		Assert.Equal( 8, (int)CursesTextAttributes.Reverse );
		Assert.Equal( 16, (int)CursesTextAttributes.Standout );
		Assert.Equal( 32, (int)CursesTextAttributes.Italic );
		Assert.Equal( 64, (int)CursesTextAttributes.Blink );
		Assert.Equal( 128, (int)CursesTextAttributes.Conceal );
		Assert.Equal( 256, (int)CursesTextAttributes.Strikeout );
	}

	[Fact]
	public void StyleCombinesColorsAndSemanticAttributes() {
		CursesStyle style = new(
			CursesColor.Indexed( 15 ),
			CursesColor.Rgb( 1, 2, 3 ),
			CursesTextAttributes.Bold
				| CursesTextAttributes.Dim
				| CursesTextAttributes.Underline
				| CursesTextAttributes.Reverse
				| CursesTextAttributes.Standout
				| CursesTextAttributes.Italic
				| CursesTextAttributes.Blink
				| CursesTextAttributes.Conceal
				| CursesTextAttributes.Strikeout
		);

		Assert.Equal( (int?)15, style.Foreground.Index );
		Assert.Equal( (byte?)1, style.Background.Red );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Bold ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Dim ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Underline ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Standout ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Italic ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Blink ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Conceal ) );
		Assert.True( style.Attributes.HasFlag( CursesTextAttributes.Strikeout ) );
	}

	[Fact]
	public void StyleRejectsUnknownAttributeFlags() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				(CursesTextAttributes)0x4000
			)
		);
	}

	[Fact]
	public void CellRejectsTerminalControlCharactersInVisibleContent() {
		Assert.Throws<ArgumentException>(
			() => new CursesCell( "\u001b[31mred" )
		);
		Assert.Throws<ArgumentException>(
			() => new CursesCell( "line\nfeed" )
		);
	}

	[Fact]
	public void ContinuationCellCarriesStyleWithoutVisibleContent() {
		CursesStyle style = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);

		CursesCell cell = CursesCell.Continuation( style );

		Assert.True( cell.IsContinuation );
		Assert.False( cell.IsBlank );
		Assert.Empty( cell.Content );
		Assert.Equal( style, cell.Style );
	}

	[Fact]
	public void DefaultCellAndBlankCellAreSemanticallyEqual() {
		Assert.Equal(
			default( CursesCell ),
			CursesCell.Blank()
		);
	}
}
