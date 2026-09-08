using System.Text;
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Exercises the 0.3 Unicode terminal-cell contract across representative grapheme and width families.
/// </summary>
public sealed class CursesUnicodeConformanceTests {
	public static TheoryData<string, int> WidthCases => new() {
		{ "A", 1 },
		{ "a\u0301\u0323", 1 },
		{ "\U0001D11E", 1 },
		{ "\u2764\uFE0E", 1 },
		{ "\u2764\uFE0F", 2 },
		{ "\U0001F44D\U0001F3FD", 2 },
		{ "\U0001F469\u200D\U0001F4BB", 2 },
		{ "\U0001F1FA\U0001F1F8", 2 },
		{ "1\uFE0F\u20E3", 2 },
		{ "\u754C", 2 },
		{ "\uFF21", 2 },
		{ "\u03A9", 1 }
	};

	public static TheoryData<string> TwoColumnCases => new() {
		"\u2764\uFE0F",
		"\U0001F44D\U0001F3FD",
		"\U0001F469\u200D\U0001F4BB",
		"\U0001F1FA\U0001F1F8",
		"1\uFE0F\u20E3",
		"\u754C",
		"\uFF21"
	};

	[Theory]
	[MemberData( nameof( WidthCases ) )]
	public void RepresentativeFamiliesMeasureDeterministically(
		string text,
		int expectedColumns
	) {
		Assert.Equal(
			expectedColumns,
			CursesText.MeasureColumns( text )
		);
	}

	[Theory]
	[MemberData( nameof( TwoColumnCases ) )]
	public void TwoColumnFamiliesClipAsWholeElements( string text ) {
		CursesScreen screen = new( 3, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WrapMode = CursesWrapMode.Clip;
		window.Move( 0, 2 );

		window.Write( text );

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Theory]
	[MemberData( nameof( TwoColumnCases ) )]
	public void TwoColumnFamiliesWrapAsWholeElements( string text ) {
		CursesScreen screen = new( 3, 2 );
		CursesWindow window = screen.StandardWindow;
		window.Move( 0, 2 );

		window.Write( text );

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( text, screen.VirtualScreen[ 1, 0 ].Content );
		Assert.Equal( 2, screen.VirtualScreen[ 1, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 1, 1 ].IsContinuation );
	}

	[Fact]
	public void WideAmbiguousPolicyPropagatesThroughHelpersAndWindows() {
		ICursesTextWidthProvider provider =
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance;
		const string text = "A\u03A9B";

		Assert.Equal(
			4,
			CursesText.MeasureColumns(
				text,
				provider
			)
		);
		Assert.Equal(
			"A",
			CursesText.TruncateToColumns(
				text,
				2,
				provider
			)
		);
		Assert.Equal(
			"\u03A9",
			CursesText.SliceByColumns(
				text,
				1,
				2,
				provider
			)
		);

		CursesScreen screen = new(
			6,
			1,
			provider
		);
		screen.StandardWindow.Write( text );
		Assert.Equal( 4, screen.StandardWindow.CursorColumn );
		Assert.True( screen.VirtualScreen[ 0, 2 ].IsContinuation );
	}

	[Fact]
	public void CombiningMarkStackRemainsOnePlacedElement() {
		const string text = "a\u0301\u0323";
		CursesScreen screen = new( 4, 1 );

		screen.StandardWindow.Write( text );

		Assert.Equal( text, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void MalformedUtf16NormalizesIdenticallyForHelperAndWindow() {
		string malformed = new( [ 'A', '\uD800', '\u0301', 'B' ] );
		const string normalized = "A\uFFFD\u0301B";

		Assert.Equal( 3, CursesText.MeasureColumns( malformed ) );
		Assert.Equal(
			normalized,
			CursesText.TruncateToColumns(
				malformed,
				3
			)
		);

		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.Write( malformed );
		Assert.Equal( "A", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( "\uFFFD\u0301", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( "B", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( 3, screen.StandardWindow.CursorColumn );
	}

	[Fact]
	public void SupplementaryScalarRemainsOneTextElement() {
		const string text = "\U0001D11E";
		CursesScreen screen = new( 3, 1 );

		screen.StandardWindow.Write( text );

		Assert.Equal( text, screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 1, screen.StandardWindow.CursorColumn );
	}
}
