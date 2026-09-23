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
	public void EmptyTextProducesOneEmptyVisualLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			string.Empty,
			new CursesTextLayoutOptions( 5 ) { StartingColumn = 2 }
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( 0, line.SourceStart.Offset );
		Assert.Equal( 0, line.SourceEnd.Offset );
		Assert.Equal( 2, line.Column );
		Assert.Equal( 0, line.Columns );
		Assert.Empty( line.Fragments );
		Assert.False( line.EndsWithHardBreak );
		Assert.False( line.EndsWithSoftWrap );
		Assert.False( line.IsClipped );
		Assert.Equal( 0, layout.CellCount );
		Assert.False( layout.IsTruncated );
	}

	[Fact]
	public void HardBreaksProduceLogicalLinesAndTrailingEmptyLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab\r\nc\n",
			new CursesTextLayoutOptions( 5 )
		);

		Assert.Collection(
			layout.Lines,
			line => AssertLine( line, 0, 2, "ab", 2, endsWithHardBreak: true ),
			line => AssertLine( line, 4, 5, "c", 1, endsWithHardBreak: true ),
			line => AssertLine( line, 6, 6, string.Empty, 0 )
		);
		Assert.Equal( 3, layout.CellCount );
		Assert.False( layout.IsTruncated );
	}

	[Fact]
	public void MaximumRowsZeroPublishesNoPartialLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abc",
			new CursesTextLayoutOptions( 5 ) { MaximumRows = 0 }
		);

		Assert.Empty( layout.Lines );
		Assert.Equal( 0, layout.CellCount );
		Assert.True( layout.IsTruncated );
	}

	[Fact]
	public void MaximumRowsHidesLaterLogicalLines() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"a\nb",
			new CursesTextLayoutOptions( 5 ) { MaximumRows = 1 }
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		AssertLine( line, 0, 1, "a", 1, endsWithHardBreak: true );
		Assert.Equal( 1, layout.CellCount );
		Assert.True( layout.IsTruncated );
	}

	[Fact]
	public void NoWrapClipsOnlyAtCompleteElementBoundaries() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcd",
			new CursesTextLayoutOptions( 2 )
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( 0, line.SourceStart.Offset );
		Assert.Equal( 2, line.SourceEnd.Offset );
		Assert.Equal( 2, line.Columns );
		Assert.True( line.IsClipped );
		CursesTextFragment fragment = Assert.Single( line.Fragments );
		Assert.Equal( "ab", fragment.Text );
		Assert.Equal( 0, fragment.SourceStart.Offset );
		Assert.Equal( 2, fragment.SourceEnd.Offset );
		Assert.Equal( 2, layout.CellCount );
		Assert.True( layout.IsTruncated );
	}

	[Fact]
	public void StartingColumnOffsetsLineAndFragments() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abc",
			new CursesTextLayoutOptions( 5 ) { StartingColumn = 3 }
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( 3, line.Column );
		Assert.Equal( 3, Assert.Single( line.Fragments ).Column );
	}

	[Fact]
	public void StyledSpansSplitUncoveredSourceWithoutPrecedence() {
		CursesStyle emphasized = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.invalid/" )
		);
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcde",
			new CursesTextLayoutOptions( 5 ),
			[
				new CursesTextSpan(
					new CursesTextPosition( 1 ),
					2,
					emphasized,
					metadata
				)
			]
		);

		Assert.Collection(
			Assert.Single( layout.Lines ).Fragments,
			fragment => AssertFragment( fragment, 0, 1, "a", CursesStyle.Default, null ),
			fragment => AssertFragment( fragment, 1, 3, "bc", emphasized, metadata ),
			fragment => AssertFragment( fragment, 3, 5, "de", CursesStyle.Default, null )
		);
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

	private static void AssertLine(
		CursesTextVisualLine line,
		int sourceStart,
		int sourceEnd,
		string text,
		int columns,
		bool endsWithHardBreak = false
	) {
		Assert.Equal( sourceStart, line.SourceStart.Offset );
		Assert.Equal( sourceEnd, line.SourceEnd.Offset );
		Assert.Equal( columns, line.Columns );
		Assert.Equal( endsWithHardBreak, line.EndsWithHardBreak );
		Assert.False( line.EndsWithSoftWrap );
		Assert.False( line.IsClipped );
		if ( 0 == text.Length ) {
			Assert.Empty( line.Fragments );
			return;
		}

		Assert.Equal( text, Assert.Single( line.Fragments ).Text );
	}

	private static void AssertFragment(
		CursesTextFragment fragment,
		int sourceStart,
		int sourceEnd,
		string text,
		CursesStyle style,
		CursesCellMetadata? metadata
	) {
		Assert.Equal( sourceStart, fragment.SourceStart.Offset );
		Assert.Equal( sourceEnd, fragment.SourceEnd.Offset );
		Assert.Equal( text, fragment.Text );
		Assert.Equal( style, fragment.Style );
		Assert.Same( metadata, fragment.Metadata );
		Assert.False( fragment.IsEllipsis );
	}
}
