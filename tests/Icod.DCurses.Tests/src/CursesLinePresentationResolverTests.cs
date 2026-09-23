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

namespace Icod.DCurses.Tests;

using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Verifies Terminal-planned physical resolution of semantic line glyphs.</summary>
public sealed class CursesLinePresentationResolverTests {
	[Fact]
	public async Task AdvertisedAlternateCharacterSetIsPreferred() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "acs-test" )
			.SetString( StringCapability.EnterAlternateCharacterSetMode, "<smacs>" )
			.SetString( StringCapability.ExitAlternateCharacterSetMode, "<rmacs>" )
			.SetString( StringCapability.AlternateCharacterSet, "q=x|" )
			.Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesLinePresentationResolver resolver = new( session.Screen );

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
	public async Task MissingAcsMappingFallsBackToCanonicalUnicode() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "unicode-test" ).Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesLinePresentationResolver resolver = new( session.Screen );

		CursesPhysicalLineGlyph resolved = resolver.Resolve(
			CursesLineGlyph.Crossing,
			UnicodeCursesTextWidthProvider.Instance
		);

		Assert.False( resolved.UsesAlternateCharacterSet );
		Assert.Equal( "┼", resolved.Content );
	}

	[Fact]
	public async Task UnsafeUnicodeWidthFallsBackToAscii() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ascii-test" ).Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesLinePresentationResolver resolver = new( session.Screen );
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
	public async Task ResolverValidatesGlyphAndWidthProvider() {
		await using TerminalSession session = await OpenSessionAsync( TerminalProfiles.Dumb );
		CursesLinePresentationResolver resolver = new( session.Screen );

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

	[Fact]
	public async Task AlternateCharacterSetPlansCommitInRequestedOrder() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "acs-plans" )
			.SetString( StringCapability.EnterAlternateCharacterSetMode, "<smacs>" )
			.SetString( StringCapability.ExitAlternateCharacterSetMode, "<rmacs>" )
			.SetString( StringCapability.AlternateCharacterSet, "q=" )
			.Build();
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			output
		);
		CursesLinePresentationResolver resolver = new( session.Screen );
		TerminalScreenOperationPlan enter = Assert.IsType<TerminalScreenOperationPlan>(
			resolver.PlanAlternateCharacterSet( enabled: true )
		);
		TerminalScreenOperationPlan exit = Assert.IsType<TerminalScreenOperationPlan>(
			resolver.PlanAlternateCharacterSet( enabled: false )
		);
		TerminalScreenOutputTransaction transaction = session.CreateScreenOutputTransaction();
		transaction.Add( enter );
		transaction.Add( exit );

		await transaction.CommitAsync();

		Assert.Equal( TerminalScreenOperationKind.AlternateCharacterSet, enter.Kind );
		Assert.Equal( "<smacs><rmacs>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	private static ValueTask<TerminalSession> OpenSessionAsync(
		TerminalDescription terminal
	) {
		return TerminalScreenTestSession.OpenAsync(
			terminal,
			new RecordingTerminalOutput()
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
