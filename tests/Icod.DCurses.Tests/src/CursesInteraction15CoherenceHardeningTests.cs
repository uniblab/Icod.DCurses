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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Hardens combined 1.5 interaction ownership and lifecycle coherence.</summary>
public sealed class CursesInteraction15CoherenceHardeningTests {
	[Fact]
	public void CaptureCannotResurrectAcrossExcludedScopeRoundTrip() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion rootRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			rootRegion,
			CursesMouseButton.Primary
		);
		using CursesInteractionScope modal = router.RegisterScope();

		CursesInteractionScopeLease scopeLease = router.ActivateScope( modal );
		scopeLease.Dispose();

		CursesInteractionResult later = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 10,
					row: 6
				)
			)
		);

		Assert.Equal( CursesInteractionResultKind.Unrouted, later.Kind );
		Assert.Null( later.Region );
		Assert.Null( later.PointerTarget );
		captureLease.Dispose();
		captureLease.Dispose();
	}
}
