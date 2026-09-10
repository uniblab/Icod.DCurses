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
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies capability-aware physical resolution of semantic line glyphs.</summary>
public sealed class CursesLinePresentationResolverTests {
	[Fact]
	public void AdvertisedAlternateCharacterSetIsPreferred() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "acs-test" )
			.SetString( StringCapability.EnterAlternateCharacterSetMode, "<smacs>" )
			.SetString( StringCapability.ExitAlternateCharacterSetMode, "<rmacs>" )
			.SetString( StringCapability.AlternateCharacterSet, "q=x|" )
			.Build();
		CursesLinePresentationResolver resolver = new( terminal );

		CursesPhysicalLineGlyph horizontal = resolver.Resolve(
			CursesLineGlyph.Horizontal,
			UnicodeCursesTextWidthProvider.Instance
		);
		CursesPhysicalLineGlyph vertical = resolver.Resolve(
			CursesLineGlyph.Vertical,
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.True( horizontal.UsesAlternateCharacterSet );
		Assert.Equal( "=", horizontal.Content );
		Assert.True( vertical.UsesAlternateCharacterSet );
		Assert.Equal( "|", vertical.Content );
	}

	[Fact]
	public void MissingAcsMappingFallsBackToCanonicalUnicode() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "unicode-test" )
			.Build();
		CursesLinePresentationResolver resolver = new( terminal );

		CursesPhysicalLineGlyph resolved = resolver.Resolve(
			CursesLineGlyph.Crossing,
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.False( resolved.UsesAlternateCharacterSet );
		Assert.Equal( "┼", resolved.Content );
	}

	[Fact]
	public void UnsafeUnicodeWidthFallsBackToAscii() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ascii-test" )
			.Build();
		CursesLinePresentationResolver resolver = new( terminal );
		ICursesTextWidthProvider widthProvider = new TwoColumnBoxDrawingWidthProvider();

		CursesPhysicalLineGlyph horizontal = resolver.Resolve(
			CursesLineGlyph.Horizontal,
			widthProvider
		);
		CursesPhysicalLineGlyph corner = resolver.Resolve(
			CursesLineGlyph.UpperLeftCorner,
			widthProvider
		);

		Assert.False( horizontal.UsesAlternateCharacterSet );
		Assert.Equal( "-", horizontal.Content );
		Assert.False( corner.UsesAlternateCharacterSet );
		Assert.Equal( "+", corner.Content );
	}

	[Fact]
	public void ResolverValidatesGlyphAndWidthProvider() {
		CursesLinePresentationResolver resolver = new( TerminalProfiles.Dumb );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => resolver.Resolve(
				(CursesLineGlyph)99,
				UnicodeCursesTextWidthProvider.Instance
			)
		);
		Assert.Throws<ArgumentNullException>(
			() => resolver.Resolve(
				CursesLineGlyph.Horizontal,
				null!
			)
		);
	}

	private sealed class TwoColumnBoxDrawingWidthProvider
		: ICursesTextWidthProvider {
		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return textElement[ 0 ] is >= '\u2500' and <= '\u257F'
				? 2
				: 1
			;
		}
	}
}
