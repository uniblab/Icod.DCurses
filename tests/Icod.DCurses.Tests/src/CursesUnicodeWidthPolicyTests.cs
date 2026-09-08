using System.Text;
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Verifies the T303 Unicode 17 terminal-width data and explicit Ambiguous-width policy.
/// </summary>
public sealed class CursesUnicodeWidthPolicyTests {
	[Fact]
	public void BuiltInProvidersExposePinnedVersionAndExplicitPolicies() {
		Assert.Equal(
			"17.0.0",
			UnicodeCursesTextWidthProvider.UnicodeDataVersion
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
	[InlineData( 0x33FF )]
	[InlineData( 0x4DC0 )]
	[InlineData( 0xA000 )]
	[InlineData( 0x3FFFE )]
	public void ScalarsOutsideDefaultWideBoundariesDoNotBecomeWideAccidentally( int codePoint ) {
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
}
