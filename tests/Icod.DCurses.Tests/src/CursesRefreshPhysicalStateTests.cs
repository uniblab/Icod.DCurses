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
using Xunit;

/// <summary>Verifies detached physical-state snapshots used during refresh preparation.</summary>
public sealed class CursesRefreshPhysicalStateTests {
	[Fact]
	public void CloneIsDetachedAndPreservesKnownUnknownSemanticAndRasterState() {
		CursesPhysicalScreenState screen = new( 2, 1 );
		CursesCell originalCell = new(
			"A",
			new CursesStyle(
				CursesColor.Indexed( 2 ),
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.invalid/original" )
		);
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateDisposedLogicalRasterCell();
		screen.SetCell(
			0,
			0,
			originalCell,
			metadata,
			raster
		);
		CursesRefreshPhysicalState original = new( screen ) {
			CurrentStyle = originalCell.Style,
			CursorRow = 0,
			CursorColumn = 1
		};

		CursesRefreshPhysicalState copy = original.Clone();
		copy.Screen.SetCell( 0, 0, new CursesCell( "B" ) );
		copy.CurrentStyle = CursesStyle.Default;
		copy.CursorRow = 1;
		copy.CursorColumn = 0;

		Assert.True( original.Screen.TryGetCell( 0, 0, out CursesCell retained ) );
		Assert.Equal( originalCell, retained );
		Assert.Same( metadata, original.Screen.GetMetadata( 0, 0 ) );
		Assert.True( original.Screen.GetRasterCell( 0, 0 ).HasValue );
		Assert.False( original.Screen.TryGetCell( 0, 1, out _ ) );
		Assert.NotEqual( original.CursorRow, copy.CursorRow );
		Assert.NotEqual( original.CurrentStyle, copy.CurrentStyle );

		original.Screen.SetCell( 0, 0, new CursesCell( "C" ) );
		Assert.True( copy.Screen.TryGetCell( 0, 0, out CursesCell copied ) );
		Assert.Equal( new CursesCell( "B" ), copied );
	}

	[Fact]
	public void InvalidateClearsAllPhysicalCertainty() {
		CursesPhysicalScreenState screen = new( 1, 1 );
		screen.SetCell( 0, 0, new CursesCell( "A" ) );
		CursesRefreshPhysicalState state = new( screen ) {
			CurrentStyle = CursesStyle.Default,
			CursorRow = 0,
			CursorColumn = 0
		};

		state.Invalidate();

		Assert.False( state.Screen.TryGetCell( 0, 0, out _ ) );
		Assert.Null( state.CurrentStyle );
		Assert.Null( state.CursorRow );
		Assert.Null( state.CursorColumn );
	}
}
