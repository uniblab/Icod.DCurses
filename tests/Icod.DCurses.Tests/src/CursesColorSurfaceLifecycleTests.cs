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

/// <summary>Qualifies color capabilities, fallback, resize, invalidation, and lifecycle repaint.</summary>
public sealed class CursesColorSurfaceLifecycleTests {
	private static readonly CursesStyle ColorStyle = new(
		CursesColor.Indexed( 2 ),
		CursesColor.Indexed( 3 ),
		CursesTextAttributes.Bold
	);

	[Fact]
	public async Task CapabilityMatrixReportsAndNormalizesProfiles() {
		await AssertIndexedProfileAsync( TerminalProfiles.Dumb, 0 );
		await AssertIndexedProfileAsync( CreateIndexedTerminal( 8 ), 8 );
		await AssertIndexedProfileAsync( CreateIndexedTerminal( 16 ), 16 );
		await AssertIndexedProfileAsync( CreateIndexedTerminal( 256 ), 256 );

		await using TerminalSession direct = await OpenSessionAsync(
			TerminalProfiles.XtermDirect256
		);
		CursesPresentationCapabilities directCapabilities =
			CursesPresentationCapabilities.Create( direct.Profile.Screen );
		CursesStyle directStyle = new(
			CursesColor.Rgb( 255, 64, 32 ),
			CursesColor.Rgb( 32, 64, 255 )
		);
		Assert.True( directCapabilities.SupportsDirectRgb );
		Assert.Equal(
			directStyle,
			new CursesPresentationResolver( direct.Screen ).Normalize( directStyle )
		);

		TerminalDescription noRestore = new TerminalDescriptionBuilder( "no-restore" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		await using TerminalSession unsafeSession = await OpenSessionAsync( noRestore );
		CursesPresentationCapabilities unsafeCapabilities =
			CursesPresentationCapabilities.Create( unsafeSession.Profile.Screen );
		CursesStyle unsafeResolved = new CursesPresentationResolver(
			unsafeSession.Screen
		).Normalize( ColorStyle );
		Assert.False( unsafeCapabilities.SupportsDefaultColorRestoration );
		Assert.True( unsafeResolved.Foreground.IsDefault );
		Assert.True( unsafeResolved.Background.IsDefault );

		TerminalDescription restricted = new TerminalDescriptionBuilder( "restricted" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetNumber( NumericCapability.NoColorVideo, 32 )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		await using TerminalSession restrictedSession = await OpenSessionAsync( restricted );
		CursesPresentationCapabilities restrictedCapabilities =
			CursesPresentationCapabilities.Create( restrictedSession.Profile.Screen );
		CursesStyle restrictedResolved = new CursesPresentationResolver(
			restrictedSession.Screen
		).Normalize( ColorStyle );
		Assert.Equal( CursesTextAttributes.Bold, restrictedCapabilities.ColorRestrictedAttributes );
		Assert.Equal( CursesTextAttributes.None, restrictedResolved.Attributes );
	}

	[Fact]
	public async Task UnsupportedForegroundDoesNotDiscardSupportedBackground() {
		TerminalDescription terminal = CreateSingleAxisTerminal( foreground: false );
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( session.Profile.Screen );
		CursesStyle resolved = new CursesPresentationResolver( session.Screen ).Normalize(
			new CursesStyle( CursesColor.Indexed( 2 ), CursesColor.Indexed( 3 ) )
		);

		Assert.False( capabilities.SupportsForegroundColor );
		Assert.True( capabilities.SupportsBackgroundColor );
		Assert.True( resolved.Foreground.IsDefault );
		Assert.Equal( CursesColor.Indexed( 3 ), resolved.Background );
	}

	[Fact]
	public async Task UnsupportedBackgroundDoesNotDiscardSupportedForeground() {
		TerminalDescription terminal = CreateSingleAxisTerminal( foreground: true );
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( session.Profile.Screen );
		CursesStyle resolved = new CursesPresentationResolver( session.Screen ).Normalize(
			new CursesStyle( CursesColor.Indexed( 2 ), CursesColor.Indexed( 3 ) )
		);

		Assert.True( capabilities.SupportsForegroundColor );
		Assert.False( capabilities.SupportsBackgroundColor );
		Assert.Equal( CursesColor.Indexed( 2 ), resolved.Foreground );
		Assert.True( resolved.Background.IsDefault );
	}

	[Fact]
	public async Task UnsafeRgbDegradesWithoutNearestColorGuess() {
		await using TerminalSession session = await OpenSessionAsync(
			TerminalProfiles.XtermDirect256
		);
		CursesStyle resolved = new CursesPresentationResolver( session.Screen ).Normalize(
			new CursesStyle(
				CursesColor.Rgb( 0, 0, 1 ),
				CursesColor.Indexed( 7 )
			)
		);

		Assert.Equal( CursesColor.Default, resolved.Foreground );
		Assert.Equal( CursesColor.Indexed( 7 ), resolved.Background );
	}

	[Fact]
	public void ResizePreservesOverlappingCompleteStyles() {
		CursesScreen screen = new( 3, 2 );
		screen.VirtualScreen[ 1, 2 ] = new CursesCell( "X", ColorStyle );

		screen.Resize( 5, 4, preserveContents: true );

		Assert.Equal( new CursesCell( "X", ColorStyle ), screen.VirtualScreen[ 1, 2 ] );
	}

	[Fact]
	public void ResizeGrowthCreatesDefaultCellsUntilRepaint() {
		CursesScreen screen = new( 2, 1 );
		screen.StandardWindow.FillRectangle(
			0, 0, 1, 2, CursesCell.Blank( ColorStyle )
		);

		screen.Resize( 4, 2, preserveContents: true );

		Assert.Equal( ColorStyle, screen.VirtualScreen[ 0, 0 ].Style );
		Assert.Equal( ColorStyle, screen.VirtualScreen[ 0, 1 ].Style );
		Assert.Equal( CursesStyle.Default, screen.VirtualScreen[ 0, 2 ].Style );
		Assert.Equal( CursesStyle.Default, screen.VirtualScreen[ 1, 3 ].Style );
	}

	[Fact]
	public void ResizeWideBoundaryLeavesNoStaleStyle() {
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.Move( 0, 2 );
		screen.StandardWindow.Write( "\u754C", ColorStyle );

		screen.Resize( 3, 1, preserveContents: true );

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.Equal( ColorStyle, screen.VirtualScreen[ 0, 2 ].Style );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void InvalidateRetainsLogicalMulticolorFrame() {
		CursesScreen screen = new( 2, 1 );
		CursesStyle second = new(
			CursesColor.Rgb( 12, 34, 56 ),
			CursesColor.Indexed( 4 ),
			CursesTextAttributes.Underline
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", ColorStyle );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "B", second );
		screen.VirtualScreen.MarkClean();

		screen.StandardWindow.Invalidate();

		Assert.Equal( new CursesCell( "A", ColorStyle ), screen.VirtualScreen[ 0, 0 ] );
		Assert.Equal( new CursesCell( "B", second ), screen.VirtualScreen[ 0, 1 ] );
		Assert.Equal( 2, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public async Task ResumeRequiresCompleteRepaintWithoutStalePhysicalStyle() {
		RecordingTerminalOutput output = new();
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync(
			CreateIndexedTerminal( 8 ),
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminal,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
		session.Screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", ColorStyle );
		session.Screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			new CursesStyle(
				CursesColor.Indexed( 4 ),
				CursesColor.Indexed( 5 ),
				CursesTextAttributes.Underline
			)
		);
		await session.RefreshAsync();
		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
		output.Clear();

		await session.RefreshAsync();

		Assert.Equal( ColorStyle, session.Screen.VirtualScreen[ 0, 0 ].Style );
		Assert.Contains( "<fg:2>", output.Text );
		Assert.Contains( "<bg:3>", output.Text );
		Assert.Contains( "<fg:4>", output.Text );
		Assert.Contains( "<bg:5>", output.Text );
	}

	private static async Task AssertIndexedProfileAsync(
		TerminalDescription terminal,
		int expectedCount
	) {
		await using TerminalSession session = await OpenSessionAsync( terminal );
		CursesPresentationCapabilities capabilities =
			CursesPresentationCapabilities.Create( session.Profile.Screen );
		CursesStyle requested = new(
			CursesColor.Indexed( 0 ),
			CursesColor.Indexed( 0 )
		);
		CursesStyle resolved = new CursesPresentationResolver( session.Screen ).Normalize(
			requested
		);

		Assert.Equal( expectedCount, capabilities.IndexedColorCount );
		Assert.Equal( 0 < expectedCount, capabilities.SupportsColor );
		Assert.Equal( 0 < expectedCount ? requested : CursesStyle.Default, resolved );
	}

	private static TerminalDescription CreateIndexedTerminal( int colors ) {
		return new TerminalDescriptionBuilder( $"indexed-{colors}" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, colors )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}

	private static TerminalDescription CreateSingleAxisTerminal( bool foreground ) {
		TerminalDescriptionBuilder builder = new TerminalDescriptionBuilder(
			foreground ? "foreground-only" : "background-only"
		)
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 );
		return ( foreground
			? builder.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			: builder.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
		).Build();
	}

	private static ValueTask<TerminalSession> OpenSessionAsync(
		TerminalDescription terminal
	) {
		return TerminalScreenTestSession.OpenAsync(
			terminal,
			new RecordingTerminalOutput()
		);
	}
}
