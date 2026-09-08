using System.Text;
using Icod.DCurses;
using Icod.DCurses.Internal.Generated;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Verifies the T303/T307 Unicode 17 terminal-width data and explicit Ambiguous-width policy.
/// </summary>
public sealed class CursesUnicodeWidthPolicyTests {
	[Fact]
	public void BuiltInProvidersExposePinnedVersionAndExplicitPolicies() {
		Assert.Equal(
			"17.0.0",
			UnicodeCursesTextWidthProvider.UnicodeDataVersion
		);
		Assert.Equal(
			UnicodeEastAsianWidthData.UnicodeVersion,
			UnicodeEmojiData.UnicodeVersion
		);
		Assert.Equal(
			CursesAmbiguousWidthPolicy.Narrow,
			UnicodeCursesTextWidthProvider.Instance.AmbiguousWidthPolicy
		);
		Assert.Equal(
			CursesAmbiguousWidthPolicy.Wide,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.AmbiguousWidthPolicy
		);
	}

	[Fact]
	public void GeneratedUnicode17RangeTablesAreStableSortedAndNonOverlapping() {
		Assert.Equal( 179, UnicodeEastAsianWidthData.AmbiguousRanges.Length );
		Assert.Equal( 120, UnicodeEastAsianWidthData.WideOrFullwidthRanges.Length );
		Assert.Equal( 151, UnicodeEmojiData.EmojiRanges.Length );
		AssertRangesAreStrictlySeparated( UnicodeEastAsianWidthData.AmbiguousRanges );
		AssertRangesAreStrictlySeparated( UnicodeEastAsianWidthData.WideOrFullwidthRanges );
		AssertRangesAreStrictlySeparated( UnicodeEmojiData.EmojiRanges );
	}

	[Theory]
	[InlineData( 0x00A1 )]
	[InlineData( 0x03A9 )]
	[InlineData( 0x2665 )]
	[InlineData( 0xFFFD )]
	[InlineData( 0xE000 )]
	public void AmbiguousScalarsFollowSelectedPolicy( int codePoint ) {
		string text = new Rune( codePoint ).ToString();

		Assert.Equal(
			1,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.GetWidth( text )
		);
	}

	[Theory]
	[InlineData( 0x4E00 )]
	[InlineData( 0x16FF2 )]
	[InlineData( 0x18D80 )]
	[InlineData( 0x1F6D8 )]
	[InlineData( 0x1FA8A )]
	[InlineData( 0x2FFFD )]
	[InlineData( 0x3FFFD )]
	public void Unicode17WideAndDefaultWideScalarsUseTwoColumns( int codePoint ) {
		string text = new Rune( codePoint ).ToString();

		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.GetWidth( text )
		);
	}

	[Theory]
	[InlineData( 0x10FF )]
	[InlineData( 0x1160 )]
	[InlineData( 0x2E7F )]
	[InlineData( 0x3FFFE )]
	public void NarrowScalarsNearWideBoundariesDoNotBecomeWideAccidentally( int codePoint ) {
		string text = new Rune( codePoint ).ToString();

		Assert.Equal(
			1,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
	}

	[Fact]
	public void TextPresentationUsesBaseAmbiguousPolicy() {
		const string textPresentationHeart = "\u2665\uFE0E";

		Assert.Equal(
			1,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( textPresentationHeart )
		);
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.GetWidth(
				textPresentationHeart
			)
		);
	}

	[Fact]
	public void EmojiPresentationRemainsTwoColumnsUnderEitherAmbiguousPolicy() {
		const string emojiPresentationHeart = "\u2665\uFE0F";

		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( emojiPresentationHeart )
		);
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.GetWidth(
				emojiPresentationHeart
			)
		);
	}

	[Theory]
	[InlineData( "\u00A9\uFE0F" )]
	[InlineData( "\u00AE\uFE0F" )]
	[InlineData( "\u2122\uFE0F" )]
	[InlineData( "\u2194\uFE0F" )]
	public void Vs16UsesUnicodeEmojiPropertyOutsideLegacyHeuristicRanges( string text ) {
		Assert.Equal(
			2,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
	}

	[Theory]
	[InlineData( "\u2605\uFE0F" )]
	[InlineData( "\U0001F000\uFE0F" )]
	public void Vs16DoesNotPromoteScalarsWithoutUnicodeEmojiProperty( string text ) {
		Assert.Equal(
			1,
			UnicodeCursesTextWidthProvider.Instance.GetWidth( text )
		);
	}

	[Fact]
	public void ScreenCanSelectWideAmbiguousPolicyExplicitly() {
		CursesScreen screen = new(
			4,
			1,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance
		);

		screen.StandardWindow.Write( "\u03A9X" );

		Assert.Equal( "\u03A9", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 2, screen.VirtualScreen[ 0, 0 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsContinuation );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( 3, screen.StandardWindow.CursorColumn );
	}

	private static void AssertRangesAreStrictlySeparated(
		IReadOnlyList<UnicodeWidthRange> ranges
	) {
		ArgumentNullException.ThrowIfNull( ranges );
		for ( int index = 0; index < ranges.Count; index++ ) {
			UnicodeWidthRange current = ranges[ index ];
			Assert.InRange( current.First, 0, 0x10FFFF );
			Assert.InRange( current.Last, current.First, 0x10FFFF );
			if ( 0 == index ) {
				continue;
			}

			UnicodeWidthRange previous = ranges[ index - 1 ];
			Assert.True(
				previous.Last + 1 < current.First,
				$"Generated Unicode ranges overlap or remain mergeable at index {index}."
			);
		}
	}
}
