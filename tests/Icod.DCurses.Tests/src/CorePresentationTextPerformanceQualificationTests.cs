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

using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Checks that application geometry and sparse refresh remain proportional to visible work.</summary>
public sealed class CorePresentationTextPerformanceQualificationTests {
	[Fact]
	public async Task WorldViewportAndLocalPatchStayBoundedByVisibleCells() {
		CursesRectangle[] editorRegions = CursesLayout.ArrangeRows(
			new CursesRectangle( 0, 0, 48, 160 ),
			[ CursesTrack.Weighted(), CursesTrack.Fixed( 1 ), CursesTrack.Fixed( 1 ) ] );
		Assert.Equal( 46, editorRegions[ 0 ].Rows );
		Assert.Equal( 160, editorRegions[ 0 ].Columns );
		CursesViewport world = new( 10_000_000, 2048, 24, 80, 9_000_000, 100 );
		Assert.Equal( new CursesRectangle( 9_000_000, 100, 24, 80 ), world.VisibleContent );

		TerminalDescription profile = new TerminalDescriptionBuilder( "world-diagnostics" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.Build();
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync(
			profile, new RecordingTerminalOutput() );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminal,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				EnableRefreshDiagnostics = true
			} );
		CursesCell[] frame = new CursesCell[ world.Rows * world.Columns ];
		Array.Fill( frame, new CursesCell( "." ) );
		session.StandardScreen.WriteCells( 0, 0, 24, 80, frame, 80 );
		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot full = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 24, full.DamagedRows );
		Assert.True( full.LogicalCellsExamined >= frame.Length );

		CursesCell[] nearby = new CursesCell[ 9 ];
		Array.Fill( nearby, new CursesCell( "*" ) );
		session.StandardScreen.WriteCells( 11, 39, 3, 3, nearby, 3 );
		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot local = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( CursesRefreshOutcome.Succeeded, local.Outcome );
		Assert.False( local.IsFullRepaint );
		Assert.Equal( 3, local.DamagedRows );
		Assert.True( local.PreparedOutputItemCount < full.PreparedOutputItemCount );
		Assert.True( local.LogicalCellsChanged < full.LogicalCellsChanged );
	}
}
