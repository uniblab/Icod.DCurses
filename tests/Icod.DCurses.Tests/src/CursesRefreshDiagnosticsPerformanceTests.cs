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

/// <summary>Qualifies opt-in observation cost on a warmed, clean application screen.</summary>
[Collection( AllocationMeasurementCollection.Name )]
public sealed class CursesRefreshDiagnosticsPerformanceTests {
	private const int RefreshesPerSample = 32;

	[Fact]
	public async Task DisabledNoOpRefreshHasNoDiagnosticSnapshotOrIncrementalAllocation() {
		await using CursesSession disabled = await OpenAsync( enabled: false );
		await using CursesSession enabled = await OpenAsync( enabled: true );
		await disabled.RefreshAsync();
		await enabled.RefreshAsync();
		for ( int index = 0; index < 8; index++ ) {
			await disabled.RefreshAsync();
			await enabled.RefreshAsync();
		}

		long disabledBytes = await MeasureMinimumAllocatedBytes( disabled );
		long enabledBytes = await MeasureMinimumAllocatedBytes( enabled );
		Assert.Null( disabled.LatestRefreshDiagnostics );
		Assert.NotNull( enabled.LatestRefreshDiagnostics );
		Assert.True( disabledBytes <= enabledBytes,
			$"Disabled refresh allocated {disabledBytes} bytes; enabled allocated {enabledBytes} bytes." );
		Assert.InRange( enabledBytes - disabledBytes, 1, 1_024L * RefreshesPerSample );
	}

	private static async Task<long> MeasureMinimumAllocatedBytes( CursesSession session ) {
		long minimum = long.MaxValue;
		for ( int sample = 0; sample < 4; sample++ ) {
			int thread = Environment.CurrentManagedThreadId;
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < RefreshesPerSample; index++ ) {
				await session.RefreshAsync();
			}
			if ( thread == Environment.CurrentManagedThreadId ) {
				minimum = Math.Min( minimum, GC.GetAllocatedBytesForCurrentThread() - before );
			}
		}
		Assert.NotEqual( long.MaxValue, minimum );
		return minimum;
	}

	private static async ValueTask<CursesSession> OpenAsync( bool enabled ) {
		TerminalDescription profile = new TerminalDescriptionBuilder( "diagnostics-cost" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.Build();
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync(
			profile, new RecordingTerminalOutput() );
		return await CursesSession.OpenAsync( terminal, new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false,
			EnableRefreshDiagnostics = enabled
		} );
	}
}
