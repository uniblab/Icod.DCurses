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

	[Fact]
	public void CaptureCannotResurrectAcrossPanelVisibilityRoundTrip() {
		CursesScreen screen = new( 20, 8 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				Panel = panel
			}
		);
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		panel.Hide();
		panel.Show();

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

	[Fact]
	public void ClickOwnershipCannotResurrectAcrossPanelVisibilityRoundTrip() {
		CursesScreen screen = new( 20, 8 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				Panel = panel
			}
		);

		CursesInteractionResult press = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 1,
					row: 1
				)
			)
		);
		Assert.Same( region, press.Region );
		Assert.Equal( CursesPointerGestureKind.Press, press.PointerGesture?.Kind );

		panel.Hide();
		panel.Show();

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 1,
					row: 1
				)
			)
		);

		Assert.Same( region, release.Region );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
	}
}
