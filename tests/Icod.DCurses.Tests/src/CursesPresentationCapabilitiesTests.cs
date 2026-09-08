using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies curses-shaped presentation observations over stable TermInfo profiles.</summary>
public sealed class CursesPresentationCapabilitiesTests {
	[Fact]
	public void DumbTerminalReportsNoRichPresentation() {
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( TerminalProfiles.Dumb );

		Assert.Equal( 0, capabilities.IndexedColorCount );
		Assert.False( capabilities.SupportsColor );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.False( capabilities.SupportsForegroundColor );
		Assert.False( capabilities.SupportsBackgroundColor );
		Assert.False( capabilities.SupportsDefaultColorRestoration );
		Assert.Equal( CursesTextAttributes.None, capabilities.SupportedAttributes );
		Assert.Equal( CursesTextAttributes.None, capabilities.ColorRestrictedAttributes );
		Assert.False( capabilities.SupportsAlternateCharacterSet );
		Assert.False( capabilities.SupportsCursorHidden );
		Assert.False( capabilities.SupportsCursorNormal );
		Assert.False( capabilities.SupportsCursorVeryVisible );
	}

	[Fact]
	public void AnsiReportsIndexedColorAndNativeAttributeSubset() {
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( TerminalProfiles.Ansi );

		Assert.Equal( 8, capabilities.IndexedColorCount );
		Assert.True( capabilities.SupportsColor );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.True( capabilities.SupportsForegroundColor );
		Assert.True( capabilities.SupportsBackgroundColor );
		Assert.True( capabilities.SupportsDefaultColorRestoration );
		Assert.True( capabilities.SupportsBold );
		Assert.False( capabilities.SupportsDim );
		Assert.True( capabilities.SupportsUnderline );
		Assert.True( capabilities.SupportsReverse );
		Assert.True( capabilities.SupportsStandout );
		Assert.False( capabilities.SupportsItalic );
		Assert.True( capabilities.SupportsBlink );
		Assert.True( capabilities.SupportsConceal );
		Assert.False( capabilities.SupportsStrikeout );
		Assert.Equal(
			CursesTextAttributes.Standout
				| CursesTextAttributes.Underline,
			capabilities.ColorRestrictedAttributes
		);
		Assert.False( capabilities.SupportsAlternateCharacterSet );
	}

	[Fact]
	public void Xterm256ReportsCompleteModernPresentationVocabulary() {
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( TerminalProfiles.Xterm256Color );

		Assert.Equal( 256, capabilities.IndexedColorCount );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.True( capabilities.SupportsBold );
		Assert.True( capabilities.SupportsDim );
		Assert.True( capabilities.SupportsUnderline );
		Assert.True( capabilities.SupportsReverse );
		Assert.True( capabilities.SupportsStandout );
		Assert.True( capabilities.SupportsItalic );
		Assert.True( capabilities.SupportsBlink );
		Assert.True( capabilities.SupportsConceal );
		Assert.True( capabilities.SupportsStrikeout );
		Assert.True( capabilities.SupportsAlternateCharacterSet );
		Assert.True( capabilities.SupportsCursorHidden );
		Assert.True( capabilities.SupportsCursorNormal );
		Assert.True( capabilities.SupportsCursorVeryVisible );
	}

	[Fact]
	public void Direct256ReportsDirectRgbAndRetainedIndexedPrefix() {
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( TerminalProfiles.XtermDirect256 );

		Assert.True( capabilities.SupportsDirectRgb );
		Assert.Equal( 256, capabilities.IndexedColorCount );
		Assert.True( capabilities.SupportsForegroundColor );
		Assert.True( capabilities.SupportsBackgroundColor );
	}
}
