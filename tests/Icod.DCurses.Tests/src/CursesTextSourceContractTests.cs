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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the 2.1 source-position and rich-span value contracts.</summary>
public sealed class CursesTextSourceContractTests {
	[Fact]
	public void RichTextSpanCarriesValidatedSourcePresentation() {
		CursesTextPosition start = new( 2 );
		CursesTextSpan span = new(
			start,
			3,
			CursesStyle.Default
		);

		Assert.Equal( 2, span.Start.Offset );
		Assert.Equal( 3, span.Length );
		Assert.Equal( 5, span.End.Offset );
		Assert.Equal( CursesStyle.Default, span.Style );
		Assert.Null( span.Metadata );
	}

	[Fact]
	public void TextPositionRejectsNegativeOffset() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			static () => new CursesTextPosition( -1 )
		);

		Assert.Equal( "offset", exception.ParamName );
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( -1 )]
	public void TextSpanRejectsNonPositiveLength( int length ) {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesTextSpan(
				new CursesTextPosition( 0 ),
				length,
				CursesStyle.Default
			)
		);

		Assert.Equal( "length", exception.ParamName );
	}

	[Fact]
	public void TextSpanReportsEndOverflowAsInvalidLength() {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			static () => new CursesTextSpan(
				new CursesTextPosition( int.MaxValue ),
				1,
				CursesStyle.Default
			)
		);

		Assert.Equal( "length", exception.ParamName );
	}
}
