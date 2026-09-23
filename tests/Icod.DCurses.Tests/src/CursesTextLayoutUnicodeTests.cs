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

/// <summary>Specifies Unicode, tab, zero-width, and ellipsis layout behavior.</summary>
public sealed class CursesTextLayoutUnicodeTests {
	[Theory]
	[InlineData( 0, 5 )]
	[InlineData( 1, 4 )]
	[InlineData( 2, 3 )]
	[InlineData( 3, 2 )]
	public void TabsUseAbsoluteColumnStops(
		int startingColumn,
		int expectedColumns
	) {
		CursesTextLayout layout = CursesTextLayout.Create(
			"\tX",
			new CursesTextLayoutOptions( 8 ) {
				StartingColumn = startingColumn,
				TabInterval = 4
			}
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( startingColumn, line.Column );
		Assert.Equal( expectedColumns, line.Columns );
		CursesTextFragment fragment = Assert.Single( line.Fragments );
		Assert.Equal( "\tX", fragment.Text );
		Assert.Equal( expectedColumns, fragment.Columns );
	}

	[Fact]
	public void MalformedSourceUsesReplacementTextAndStableOffsets() {
		string malformed = new( [ '\uD800', 'X', '\uDC00' ] );

		CursesTextLayout layout = CursesTextLayout.Create(
			malformed,
			new CursesTextLayoutOptions( 3 )
		);

		CursesTextFragment fragment = Assert.Single(
			Assert.Single( layout.Lines ).Fragments
		);
		Assert.Equal( "\uFFFDX\uFFFD", fragment.Text );
		Assert.Equal( 0, fragment.SourceStart.Offset );
		Assert.Equal( 3, fragment.SourceEnd.Offset );
		Assert.Equal( 3, fragment.Columns );
		Assert.Equal( malformed, layout.Text );
	}

	[Theory]
	[InlineData( "e\u0301", 1 )]
	[InlineData( "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 2 )]
	[InlineData( "1\uFE0F\u20E3", 2 )]
	[InlineData( "\U0001F1FA\U0001F1F8", 2 )]
	[InlineData( "\u754C", 2 )]
	public void CompleteUnicodeElementProducesItsConfiguredWidth(
		string text,
		int expectedColumns
	) {
		CursesTextLayout layout = CursesTextLayout.Create(
			text,
			new CursesTextLayoutOptions( 2 )
		);

		CursesTextFragment fragment = Assert.Single(
			Assert.Single( layout.Lines ).Fragments
		);
		Assert.Equal( text, fragment.Text );
		Assert.Equal( expectedColumns, fragment.Columns );
		Assert.Equal( text.Length, fragment.SourceEnd.Offset );
	}

	[Fact]
	public void AmbiguousElementUsesTheConfiguredWidthPolicy() {
		CursesTextLayout narrow = CursesTextLayout.Create(
			"\u00B7",
			new CursesTextLayoutOptions( 2 )
		);
		CursesTextLayout wide = CursesTextLayout.Create(
			"\u00B7",
			new CursesTextLayoutOptions( 2 ) {
				WidthProvider = UnicodeCursesTextWidthProvider.WideAmbiguousInstance
			}
		);

		Assert.Equal( 1, Assert.Single( narrow.Lines ).Columns );
		Assert.Equal( 2, Assert.Single( wide.Lines ).Columns );
	}

	[Fact]
	public void LeadingZeroWidthElementMapsButProducesNoFragment() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"\u0301A",
			new CursesTextLayoutOptions( 1 )
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		CursesTextFragment fragment = Assert.Single( line.Fragments );
		Assert.Equal( 0, line.SourceStart.Offset );
		Assert.Equal( 2, line.SourceEnd.Offset );
		Assert.Equal( "A", fragment.Text );
		Assert.Equal( 1, fragment.SourceStart.Offset );
		Assert.Equal( 2, fragment.SourceEnd.Offset );
		Assert.Equal( 1, line.Columns );
	}

	[Fact]
	public void FollowingZeroWidthElementAttachesToVisibleFragment() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"A\u200B",
			new CursesTextLayoutOptions( 1 )
		);

		CursesTextFragment fragment = Assert.Single(
			Assert.Single( layout.Lines ).Fragments
		);
		Assert.Equal( "A\u200B", fragment.Text );
		Assert.Equal( 2, fragment.SourceEnd.Offset );
		Assert.Equal( 1, fragment.Columns );
	}

	[Fact]
	public void TooWideElementIsClippedWithoutSplittingAndLayoutAdvances() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"\u754CA",
			new CursesTextLayoutOptions( 1 ) {
				WrapMode = CursesTextWrapMode.TextElement
			}
		);

		Assert.Collection(
			layout.Lines,
			line => {
				Assert.Empty( line.Fragments );
				Assert.Equal( 0, line.Columns );
				Assert.True( line.IsClipped );
				Assert.True( line.EndsWithSoftWrap );
			},
			line => {
				Assert.Equal( "A", Assert.Single( line.Fragments ).Text );
				Assert.Equal( 1, line.Columns );
			}
		);
		Assert.True( layout.IsTruncated );
	}

	[Fact]
	public void EllipsisUsesFirstHiddenPresentationAndEmptySourceRange() {
		CursesStyle hiddenStyle = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);
		CursesCellMetadata hiddenMetadata = new(
			new CursesHyperlink( "https://example.invalid/hidden" )
		);
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcd",
			new CursesTextLayoutOptions( 3 ) {
				Overflow = CursesTextOverflow.Ellipsis
			},
			[
				new CursesTextSpan(
					new CursesTextPosition( 2 ),
					1,
					hiddenStyle,
					hiddenMetadata
				)
			]
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Collection(
			line.Fragments,
			fragment => Assert.Equal( "ab", fragment.Text ),
			fragment => {
				Assert.Equal( "\u2026", fragment.Text );
				Assert.Equal( 2, fragment.SourceStart.Offset );
				Assert.Equal( 2, fragment.SourceEnd.Offset );
				Assert.Equal( hiddenStyle, fragment.Style );
				Assert.Same( hiddenMetadata, fragment.Metadata );
				Assert.True( fragment.IsEllipsis );
			}
		);
		Assert.Equal( 3, line.Columns );
		Assert.True( line.IsClipped );
		Assert.True( layout.IsTruncated );
	}

	[Fact]
	public void EllipsisThatCannotFitProducesEmptyClippedLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"ab",
			new CursesTextLayoutOptions( 1 ) {
				Overflow = CursesTextOverflow.Ellipsis,
				WidthProvider = UnicodeCursesTextWidthProvider.WideAmbiguousInstance
			}
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Empty( line.Fragments );
		Assert.Equal( 0, line.Columns );
		Assert.True( line.IsClipped );
		Assert.True( layout.IsTruncated );
	}
}
