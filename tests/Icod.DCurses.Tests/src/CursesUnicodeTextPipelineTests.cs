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
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Regression coverage for the normalized Unicode text-element and cluster-width foundation.
/// </summary>
public sealed class CursesUnicodeTextPipelineTests {
	[Fact]
	public void MalformedUtf16IsNormalizedBeforeSegmentation() {
		string malformed = new( [ '\uD800', 'A' ] );

		string[] elements = CursesUnicodeText
			.EnumerateTextElements( malformed )
			.ToArray();

		Assert.Equal(
			[ "\uFFFD", "A" ],
			elements
		);
	}

	[Fact]
	public void WindowWriteUsesNormalizedElementsBeforeWidthMeasurement() {
		RecordingWidthProvider provider = new();
		CursesScreen screen = new(
			4,
			1,
			provider
		);
		string malformed = new( [ '\uD800', '\u0301', 'X' ] );

		screen.StandardWindow.Write( malformed );

		Assert.Equal(
			[ "\uFFFD\u0301", "X" ],
			provider.TextElements
		);
		Assert.Equal( "\uFFFD\u0301", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( 2, screen.StandardWindow.CursorColumn );
	}

	[Theory]
	[InlineData( "e\u0301" )]
	[InlineData( "\U0001F1FA\U0001F1F8" )]
	[InlineData( "1\uFE0F\u20E3" )]
	[InlineData( "\u2764\uFE0F" )]
	[InlineData( "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466" )]
	public void RepresentativeExtendedGraphemesRemainSingleTextElements( string text ) {
		string[] elements = CursesUnicodeText
			.EnumerateTextElements( text )
			.ToArray();

		Assert.Single( elements );
		Assert.Equal( text, elements[ 0 ] );
	}

	[Theory]
	[InlineData( "e\u0301", 1 )]
	[InlineData( "\u0301", 0 )]
	[InlineData( "\u2764\uFE0E", 1 )]
	[InlineData( "\u2764\uFE0F", 2 )]
	[InlineData( "1\uFE0F\u20E3", 2 )]
	[InlineData( "\U0001F1FA\U0001F1F8", 2 )]
	[InlineData( "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 2 )]
	public void DefaultProviderMeasuresCompleteTextElement( string text, int expectedWidth ) {
		Assert.Equal(
			expectedWidth,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
	}

	[Theory]
	[InlineData( "\u2764\uFE0F" )]
	[InlineData( "1\uFE0F\u20E3" )]
	[InlineData( "\U0001F1FA\U0001F1F8" )]
	[InlineData( "\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466" )]
	public void TwoColumnClustersUseOneLeaderAndOneContinuation( string text ) {
		CursesScreen screen = new( 4, 1 );

		screen.StandardWindow.Write( text );

		Assert.Equal( text, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 2, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsContinuation );
		Assert.Equal( 2, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void TextPresentationSelectorRemainsOneColumn() {
		CursesScreen screen = new( 4, 1 );
		string textPresentation = "\u2764\uFE0E";

		screen.StandardWindow.Write( textPresentation );

		Assert.Equal( textPresentation, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	private sealed class RecordingWidthProvider
		: ICursesTextWidthProvider {
		private readonly List<string> textElements = [];

		internal IReadOnlyList<string> TextElements => textElements;

		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			textElements.Add( textElement );
			return 1;
		}
	}
}
