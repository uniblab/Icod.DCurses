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

using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Specifies the shared 2.1 source-element scanner contract.</summary>
public sealed class CursesTextSourceUnicodeTests {
	[Fact]
	public void EmptySourceProducesNoElements() {
		Assert.Empty(
			CursesTextElementScanner.Scan(
				string.Empty,
				UnicodeCursesTextWidthProvider.Instance
			)
		);
	}

	[Fact]
	public void ScannerPreservesOrdinaryBreakTabAndEndBoundaries() {
		CursesTextElement[] elements = CursesTextElementScanner.Scan(
			"A\r\n\u754C\tB",
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.Collection(
			elements,
			current => AssertElement( current, 0, 1, 1 ),
			current => AssertElement( current, 1, 3, 0, isHardBreak: true ),
			current => AssertElement( current, 3, 4, 2 ),
			current => AssertElement( current, 4, 5, 0, isTab: true ),
			current => AssertElement( current, 5, 6, 1 )
		);
	}

	[Theory]
	[InlineData( "e\u0301", 1 )]
	[InlineData( "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 2 )]
	[InlineData( "1\uFE0F\u20E3", 2 )]
	[InlineData( "\U0001F1FA\U0001F1F8", 2 )]
	[InlineData( "\u754C", 2 )]
	public void ExtendedElementsRemainIndivisible(
		string text,
		int width
	) {
		CursesTextElement element = Assert.Single(
			CursesTextElementScanner.Scan(
				text,
				UnicodeCursesTextWidthProvider.Instance
			)
		);

		AssertElement(
			element,
			0,
			text.Length,
			width
		);
	}

	[Fact]
	public void MalformedSurrogatesBecomeSeparateReplacementElementsAtStableOffsets() {
		string text = new( [ '\uD800', 'X', '\uDC00' ] );

		CursesTextElement[] elements = CursesTextElementScanner.Scan(
			text,
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.Collection(
			elements,
			current => AssertElement( current, 0, 1, 1 ),
			current => AssertElement( current, 1, 2, 1 ),
			current => AssertElement( current, 2, 3, 1 )
		);
	}

	[Fact]
	public void ScannerRecordsLeadingAndAttachedZeroWidthSemantics() {
		CursesTextElement[] leading = CursesTextElementScanner.Scan(
			"\u0301A",
			UnicodeCursesTextWidthProvider.Instance
		);
		CursesTextElement[] attached = CursesTextElementScanner.Scan(
			"A\u0301",
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.Collection(
			leading,
			current => AssertElement( current, 0, 1, 0 ),
			current => AssertElement( current, 1, 2, 1 )
		);
		CursesTextElement combined = Assert.Single( attached );
		AssertElement( combined, 0, 2, 1 );
	}

	[Fact]
	public void AmbiguousWidthUsesTheSuppliedProvider() {
		const string text = "\u00B7";

		CursesTextElement narrow = Assert.Single(
			CursesTextElementScanner.Scan(
				text,
				UnicodeCursesTextWidthProvider.Instance
			)
		);
		CursesTextElement wide = Assert.Single(
			CursesTextElementScanner.Scan(
				text,
				UnicodeCursesTextWidthProvider.WideAmbiguousInstance
			)
		);

		Assert.Equal( 1, narrow.Width );
		Assert.Equal( 2, wide.Width );
	}

	[Theory]
	[InlineData( "\0" )]
	[InlineData( "\u001B" )]
	[InlineData( "\u007F" )]
	[InlineData( "\u009F" )]
	public void ScannerRejectsUnsupportedControls( string text ) {
		ArgumentException exception = Assert.Throws<ArgumentException>(
			() => CursesTextElementScanner.Scan(
				text,
				UnicodeCursesTextWidthProvider.Instance
			)
		);

		Assert.Equal( "text", exception.ParamName );
	}

	[Fact]
	public void ScannerDoesNotMeasureBreaksOrTabs() {
		RecordingWidthProvider provider = new();

		CursesTextElement[] elements = CursesTextElementScanner.Scan(
			"A\r\n\tB",
			provider
		);

		Assert.Equal( 4, elements.Length );
		Assert.Equal( [ "A", "B" ], provider.Elements );
	}

	[Theory]
	[InlineData( -1 )]
	[InlineData( 3 )]
	public void ScannerRejectsUnsupportedProviderWidths( int width ) {
		Assert.Throws<InvalidOperationException>(
			() => CursesTextElementScanner.Scan(
				"A",
				new FixedWidthProvider( width )
			)
		);
	}

	private static void AssertElement(
		CursesTextElement element,
		int sourceStart,
		int sourceEnd,
		int width,
		bool isHardBreak = false,
		bool isTab = false
	) {
		Assert.Equal( sourceStart, element.SourceStart );
		Assert.Equal( sourceEnd, element.SourceEnd );
		Assert.Equal( width, element.Width );
		Assert.Equal( isHardBreak, element.IsHardBreak );
		Assert.Equal( isTab, element.IsTab );
	}

	private sealed class RecordingWidthProvider
		: ICursesTextWidthProvider {
		private readonly List<string> elements = [];

		internal IReadOnlyList<string> Elements => elements;

		public int GetWidth( string textElement ) {
			elements.Add( textElement );
			return 1;
		}
	}

	private sealed class FixedWidthProvider
		: ICursesTextWidthProvider {
		private readonly int width;

		internal FixedWidthProvider( int width ) {
			this.width = width;
		}

		public int GetWidth( string textElement ) {
			return width;
		}
	}
}
