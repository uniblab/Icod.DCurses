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

/// <summary>
/// Verifies T304 column-oriented text helpers against the window-placement contract.
/// </summary>
public sealed class CursesTextColumnTests {
	[Theory]
	[InlineData( "ASCII", 5 )]
	[InlineData( "e\u0301", 1 )]
	[InlineData( "\u754C", 2 )]
	[InlineData( "\u2764\uFE0F", 2 )]
	[InlineData( "\U0001F1FA\U0001F1F8", 2 )]
	[InlineData( "1\uFE0F\u20E3", 2 )]
	public void MeasureColumnsUsesWindowUnicodeSemantics(
		string text,
		int expectedColumns
	) {
		Assert.Equal(
			expectedColumns,
			CursesText.MeasureColumns( text )
		);
	}

	[Fact]
	public void MeasureColumnsUsesCallerSuppliedWidthProvider() {
		Assert.Equal(
			4,
			CursesText.MeasureColumns(
				"AB",
				new FixedWidthProvider( 2 )
			)
		);
	}

	[Fact]
	public void MeasureColumnsRespectsWideAmbiguousPolicy() {
		Assert.Equal(
			1,
			CursesText.MeasureColumns( "\u03A9" )
		);
		Assert.Equal(
			2,
			CursesText.MeasureColumns(
				"\u03A9",
				UnicodeCursesTextWidthProvider.WideAmbiguousInstance
			)
		);
	}

	[Fact]
	public void MalformedUtf16IsNormalizedBeforeHelperResultsAreReturned() {
		string malformed = new( [ '\uD800', '\u0301', 'A' ] );

		Assert.Equal( 2, CursesText.MeasureColumns( malformed ) );
		Assert.Equal(
			"\uFFFD\u0301A",
			CursesText.TruncateToColumns(
				malformed,
				2
			)
		);
	}

	[Theory]
	[InlineData( "\t" )]
	[InlineData( "\r" )]
	[InlineData( "\n" )]
	[InlineData( "\u001B" )]
	[InlineData( "\u0085" )]
	public void PrintableColumnHelpersRejectTerminalControls( string text ) {
		Assert.Throws<ArgumentException>(
			() => CursesText.MeasureColumns( text )
		);
		Assert.Throws<ArgumentException>(
			() => CursesText.TruncateToColumns(
				text,
				10
			)
		);
		Assert.Throws<ArgumentException>(
			() => CursesText.SliceByColumns(
				text,
				0,
				10
			)
		);
	}

	[Fact]
	public void TruncateNeverReturnsHalfOfWideElement() {
		const string text = "A\u754CB";

		Assert.Equal( "A", CursesText.TruncateToColumns( text, 2 ) );
		Assert.Equal( "A\u754C", CursesText.TruncateToColumns( text, 3 ) );
		Assert.Equal( text, CursesText.TruncateToColumns( text, 4 ) );
	}

	[Fact]
	public void TruncateOmitsUnattachedLeadingZeroWidthElement() {
		const string text = "\u0301A";

		Assert.Equal(
			"A",
			CursesText.TruncateToColumns(
				text,
				1
			)
		);
	}

	[Fact]
	public void TruncateRetainsZeroWidthElementAttachedToIncludedVisibleElement() {
		CursesScreen screen = new(
			4,
			1,
			new SplitCombiningWidthProvider()
		);
		const string text = "A\u200BB";

		string truncated = CursesText.TruncateToColumns(
			text,
			1,
			new SplitCombiningWidthProvider()
		);
		screen.StandardWindow.Write( truncated );

		Assert.Equal( "A\u200B", truncated );
		Assert.Equal( "A\u200B", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void SliceOmitsWideElementWhenStartBoundaryCutsThroughIt() {
		const string text = "A\u754CBC";

		Assert.Equal(
			"B",
			CursesText.SliceByColumns(
				text,
				2,
				2
			)
		);
	}

	[Fact]
	public void SliceOmitsWideElementWhenEndBoundaryCutsThroughIt() {
		const string text = "A\u754CBC";

		Assert.Equal(
			"A",
			CursesText.SliceByColumns(
				text,
				0,
				2
			)
		);
	}

	[Fact]
	public void SliceIncludesWholeWideElementWhenIntervalContainsItsSpan() {
		const string text = "A\u754CBC";

		Assert.Equal(
			"\u754CB",
			CursesText.SliceByColumns(
				text,
				1,
				3
			)
		);
	}

	[Fact]
	public void SliceRetainsZeroWidthTextOnlyWithIncludedPrecedingElement() {
		ICursesTextWidthProvider provider = new SplitCombiningWidthProvider();
		const string text = "A\u200BB";

		Assert.Equal(
			"A\u200B",
			CursesText.SliceByColumns(
				text,
				0,
				1,
				provider
			)
		);
		Assert.Equal(
			"B",
			CursesText.SliceByColumns(
				text,
				1,
				1,
				provider
			)
		);
	}

	[Theory]
	[InlineData( "A\u754CB" )]
	[InlineData( "e\u0301\u754C" )]
	[InlineData( "\u2764\uFE0FX" )]
	[InlineData( "\U0001F1FA\U0001F1F8X" )]
	public void MeasuredColumnsAgreeWithWindowCursorPlacement( string text ) {
		int columns = CursesText.MeasureColumns( text );
		CursesScreen screen = new(
			columns + 2,
			1
		);

		screen.StandardWindow.Write( text );

		Assert.Equal( columns, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void InvalidColumnArgumentsAreRejected() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesText.TruncateToColumns(
				"A",
				-1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesText.SliceByColumns(
				"A",
				-1,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesText.SliceByColumns(
				"A",
				0,
				-1
			)
		);
	}

	private sealed class FixedWidthProvider
		: ICursesTextWidthProvider {
		private readonly int width;

		internal FixedWidthProvider(
			int width
		) {
			this.width = width;
		}

		public int GetWidth(
			string textElement
		) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return this.width;
		}
	}

	private sealed class SplitCombiningWidthProvider
		: ICursesTextWidthProvider {
		public int GetWidth(
			string textElement
		) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return "\u200B" == textElement
				? 0
				: 1
			;
		}
	}
}
