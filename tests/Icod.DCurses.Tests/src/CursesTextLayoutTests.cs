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

/// <summary>Specifies the immutable 2.1 rich-text layout contract.</summary>
public sealed class CursesTextLayoutTests {
	[Theory]
	[InlineData( -1 )]
	[InlineData( 0 )]
	[InlineData( 1_048_577 )]
	public void OptionsRejectInvalidColumnExtent( int columns ) {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesTextLayoutOptions( columns )
		);

		Assert.Equal( "columns", exception.ParamName );
	}

	[Fact]
	public void CreateRejectsNullInputs() {
		ArgumentNullException textException = Assert.Throws<ArgumentNullException>(
			static () => CursesTextLayout.Create(
				null!,
				new CursesTextLayoutOptions( 1 )
			)
		);
		ArgumentNullException optionsException = Assert.Throws<ArgumentNullException>(
			static () => CursesTextLayout.Create(
				string.Empty,
				null!
			)
		);

		Assert.Equal( "text", textException.ParamName );
		Assert.Equal( "options", optionsException.ParamName );
	}

	[Fact]
	public void CreateRejectsInvalidOptionValues() {
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { MaximumRows = -1 },
			"MaximumRows"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { MaximumRows = 1_048_577 },
			"MaximumRows"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) {
				WrapMode = (CursesTextWrapMode)int.MaxValue
			},
			"WrapMode"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) {
				Alignment = (CursesTextAlignment)int.MaxValue
			},
			"Alignment"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) {
				Overflow = (CursesTextOverflow)int.MaxValue
			},
			"Overflow"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { StartingColumn = -1 },
			"StartingColumn"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { StartingColumn = 1_048_577 },
			"StartingColumn"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 2 ) { StartingColumn = int.MaxValue },
			"StartingColumn"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { TabInterval = 0 },
			"TabInterval"
		);
		AssertOptionFailure(
			new CursesTextLayoutOptions( 1 ) { TabInterval = 1_048_577 },
			"TabInterval"
		);

		ArgumentNullException widthException = Assert.Throws<ArgumentNullException>(
			static () => CursesTextLayout.Create(
				string.Empty,
				new CursesTextLayoutOptions( 1 ) { WidthProvider = null! }
			)
		);
		Assert.Equal( "WidthProvider", widthException.ParamName );
	}

	[Fact]
	public void CreateCopiesOptionsAndCallerSpanList() {
		CursesTextLayoutOptions options = new( 5 );
		List<CursesTextSpan> spans = [
			new CursesTextSpan(
				new CursesTextPosition( 0 ),
				1,
				CursesStyle.Default
			)
		];

		CursesTextLayout layout = CursesTextLayout.Create(
			"abc",
			options,
			spans
		);
		spans.Clear();

		Assert.NotSame( options, layout.Options );
		Assert.Single( layout.Spans );
		Assert.Equal( 1, layout.Spans[ 0 ].Length );
	}

	[Fact]
	public void CreateProducesOneStyledVisualLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abc",
			new CursesTextLayoutOptions( 5 )
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( 3, line.Columns );
		Assert.Equal( "abc", Assert.Single( line.Fragments ).Text );
		Assert.Equal( 3, layout.CellCount );
		Assert.False( layout.IsTruncated );
	}

	private static void AssertOptionFailure(
		CursesTextLayoutOptions options,
		string parameterName
	) {
		ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesTextLayout.Create(
				string.Empty,
				options
			)
		);
		Assert.Equal( parameterName, exception.ParamName );
	}
}
