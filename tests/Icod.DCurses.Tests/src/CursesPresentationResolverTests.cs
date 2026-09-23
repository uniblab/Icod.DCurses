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

/// <summary>Verifies Terminal-owned logical-to-physical presentation planning.</summary>
public sealed class CursesPresentationResolverTests {
	[Fact]
	public async Task InRangeIndexedColorIsPreserved() {
		await using TerminalSession session = await OpenSessionAsync( CreateEightColorTerminal() );
		CursesPresentationResolver resolver = new( session.Screen );
		CursesStyle requested = new( CursesColor.Indexed( 7 ), CursesColor.Default );

		CursesStyle resolved = resolver.Normalize( requested );

		Assert.Equal( requested, resolved );
	}

	[Fact]
	public async Task OutOfRangeIndexedColorDegradesToTerminalDefault() {
		await using TerminalSession session = await OpenSessionAsync( CreateEightColorTerminal() );
		CursesPresentationResolver resolver = new( session.Screen );
		CursesStyle requested = new( CursesColor.Indexed( 8 ), CursesColor.Default );

		CursesStyle resolved = resolver.Normalize( requested );

		Assert.True( resolved.Foreground.IsDefault );
		Assert.True( resolved.Background.IsDefault );
	}

	[Fact]
	public async Task IndexedColorWithoutSafeDefaultRestorationDegradesToDefault() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "no-op" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationResolver resolver = new( session.Screen );

		CursesStyle resolved = resolver.Normalize(
			new CursesStyle( CursesColor.Indexed( 2 ), CursesColor.Default )
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public async Task RgbOnIndexedTerminalDegradesToDefault() {
		await using TerminalSession session = await OpenSessionAsync( CreateEightColorTerminal() );
		CursesPresentationResolver resolver = new( session.Screen );

		CursesStyle resolved = resolver.Normalize(
			new CursesStyle( CursesColor.Rgb( 12, 34, 56 ), CursesColor.Default )
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public async Task DirectRgbIsPreservedWhenPackedValueIsSafe() {
		await using TerminalSession session = await OpenSessionAsync( TerminalProfiles.XtermDirect256 );
		CursesPresentationResolver resolver = new( session.Screen );
		CursesColor requested = CursesColor.Rgb( 255, 0, 0 );

		CursesStyle resolved = resolver.Normalize(
			new CursesStyle( requested, CursesColor.Default )
		);

		Assert.Equal( requested, resolved.Foreground );
	}

	[Fact]
	public async Task DirectRgbCollisionWithRetainedIndexedPrefixDegradesToDefault() {
		await using TerminalSession session = await OpenSessionAsync( TerminalProfiles.XtermDirect256 );
		CursesPresentationResolver resolver = new( session.Screen );

		CursesStyle resolved = resolver.Normalize(
			new CursesStyle( CursesColor.Rgb( 0, 0, 1 ), CursesColor.Default )
		);

		Assert.True( resolved.Foreground.IsDefault );
	}

	[Fact]
	public async Task UnsupportedAttributesAreOmittedAndStandoutFallsBackToReverse() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "reverse-only" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationResolver resolver = new( session.Screen );

		CursesStyle resolved = resolver.Normalize(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Standout | CursesTextAttributes.Italic
			)
		);

		Assert.Equal( CursesTextAttributes.Reverse, resolved.Attributes );
	}

	[Fact]
	public async Task NoColorVideoRestrictionsApplyOnlyWhenPhysicalColorIsActive() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ncv" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetNumber( NumericCapability.NoColorVideo, 32 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationResolver resolver = new( session.Screen );

		CursesStyle colored = resolver.Normalize(
			new CursesStyle(
				CursesColor.Indexed( 1 ),
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		CursesStyle uncolored = resolver.Normalize(
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);

		Assert.Equal( CursesTextAttributes.None, colored.Attributes );
		Assert.Equal( CursesTextAttributes.Bold, uncolored.Attributes );
	}

	[Fact]
	public async Task BaselineTransitionAndResetPlansCommitTerminalOwnedOutput() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "rendition-plans" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			output
		);
		CursesPresentationResolver resolver = new( session.Screen );
		CursesStyle target = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Indexed( 3 ),
			CursesTextAttributes.Bold
		);
		TerminalScreenOperationPlan baseline = Assert.IsType<TerminalScreenOperationPlan>(
			resolver.PlanBaseline()
		);
		TerminalScreenOperationPlan transition = Assert.IsType<TerminalScreenOperationPlan>(
			resolver.PlanTransition( CursesStyle.Default, target )
		);
		TerminalScreenOperationPlan reset = Assert.IsType<TerminalScreenOperationPlan>(
			resolver.PlanReset( target )
		);
		TerminalScreenOutputTransaction transaction = session.CreateScreenOutputTransaction();
		transaction.Add( baseline );
		transaction.Add( transition );
		transaction.Add( reset );

		await transaction.CommitAsync();

		Assert.Equal( TerminalScreenOperationKind.Rendition, baseline.Kind );
		Assert.Equal( 10, baseline.ByteCount );
		Assert.Equal( "<sgr0><op><fg:2><bg:3><bold><sgr0><op>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task UnsafeUnknownRenditionBaselineIsUnavailable() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "unsafe-baseline" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.Build();
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationResolver resolver = new( session.Screen );

		Assert.Null( resolver.PlanBaseline() );
	}

	private static ValueTask<TerminalSession> OpenSessionAsync(
		TerminalDescription terminal
	) {
		return TerminalScreenTestSession.OpenAsync(
			terminal,
			new RecordingTerminalOutput()
		);
	}

	private static TerminalDescription CreateEightColorTerminal() {
		return new TerminalDescriptionBuilder( "eight-color" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}
}
