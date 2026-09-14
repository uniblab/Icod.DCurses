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

public sealed class CursesInteractionHitTestingTests {
	[Fact]
	public void HitTestRejectsNegativeCoordinatesAndReturnsNullOutsideScreen() {
		CursesScreen screen = new( 10, 5 );
		using CursesInteractionRouter router = new( screen );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = router.HitTest( -1, 0 );
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = router.HitTest( 0, -1 );
			}
		);
		Assert.Null( router.HitTest( 5, 0 ) );
		Assert.Null( router.HitTest( 0, 10 ) );
	}

	[Fact]
	public void OrdinaryRegionHitUsesDeclaredOriginForLocalCoordinates() {
		CursesScreen screen = new( 10, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 2, 3 )
			)
		);

		CursesInteractionHit topLeft = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 1, 2 )
		);
		CursesInteractionHit bottomRight = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 2, 4 )
		);

		Assert.Same( region, topLeft.Region );
		Assert.Equal( 0, topLeft.LocalRow );
		Assert.Equal( 0, topLeft.LocalColumn );
		Assert.Same( region, bottomRight.Region );
		Assert.Equal( 1, bottomRight.LocalRow );
		Assert.Equal( 2, bottomRight.LocalColumn );
		Assert.Null( router.HitTest( 0, 2 ) );
		Assert.Null( router.HitTest( 3, 2 ) );
		Assert.Null( router.HitTest( 1, 5 ) );
	}

	[Fact]
	public void OrdinaryRegionIsClippedByCurrentScreenAndEmptyRegionDoesNotHit() {
		CursesScreen screen = new( 10, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion clipped = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 8, 4, 4 )
			)
		);
		using CursesInteractionRegion empty = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 0, 5 )
			) {
				HitTestPriority = int.MaxValue
			}
		);

		CursesInteractionHit hit = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 4, 9 )
		);

		Assert.Same( clipped, hit.Region );
		Assert.Equal( 0, hit.LocalRow );
		Assert.Equal( 1, hit.LocalColumn );
		Assert.Null( router.HitTest( 1, 1 ) );
		Assert.Null( router.HitTest( 5, 9 ) );
	}

	[Fact]
	public void OrdinaryOverlapUsesPriorityThenLaterRegistration() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 5, 5 )
			) {
				HitTestPriority = 10
			}
		);
		using CursesInteractionRegion second = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 5, 5 )
			) {
				HitTestPriority = 9
			}
		);

		Assert.Same( first, router.HitTest( 2, 2 )!.Region );

		second.HitTestPriority = 10;
		Assert.Same( second, router.HitTest( 2, 2 )!.Region );

		second.HitTestPriority = 11;
		Assert.Same( second, router.HitTest( 2, 2 )!.Region );
	}

	[Fact]
	public void DisabledRegionIsIgnoredByHitTesting() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion lower = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 5, 5 )
			)
		);
		using CursesInteractionRegion upper = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 5, 5 )
			) {
				HitTestPriority = 100
			}
		);

		upper.IsEnabled = false;

		Assert.Same( lower, router.HitTest( 2, 2 )!.Region );
	}

	[Fact]
	public void PanelRegionUsesPanelRelativeCoordinatesAndCurrentGeometry() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel panel = screen.CreatePanel( 3, 4, 4, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			) {
				Panel = panel
			}
		);

		CursesInteractionHit initial = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 4, 6 )
		);
		Assert.Same( region, initial.Region );
		Assert.Equal( 0, initial.LocalRow );
		Assert.Equal( 0, initial.LocalColumn );

		Assert.Null( router.HitTest( 6, 9 ) );

		panel.MoveTo( 5, 6 );
		Assert.Null( router.HitTest( 4, 6 ) );
		CursesInteractionHit moved = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 6, 8 )
		);
		Assert.Equal( 0, moved.LocalRow );
		Assert.Equal( 0, moved.LocalColumn );

		panel.Resize( 2, 3 );
		Assert.NotNull( router.HitTest( 6, 8 ) );
		Assert.Null( router.HitTest( 7, 8 ) );
	}

	[Fact]
	public void PanelRegionsAlwaysPrecedeOrdinaryRegions() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel panel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion ordinary = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 15, 30 )
			) {
				HitTestPriority = int.MaxValue
			}
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel,
				HitTestPriority = int.MinValue
			}
		);

		Assert.Same( panelRegion, router.HitTest( 3, 3 )!.Region );
		Assert.Same( ordinary, router.HitTest( 10, 10 )!.Region );
	}

	[Fact]
	public void CurrentPanelZOrderPrecedesCrossPanelRegionPriority() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel firstPanel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesPanel secondPanel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = firstPanel,
				HitTestPriority = int.MaxValue
			}
		);
		using CursesInteractionRegion second = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = secondPanel,
				HitTestPriority = int.MinValue
			}
		);

		Assert.Same( second, router.HitTest( 3, 3 )!.Region );

		firstPanel.MoveToTop();
		Assert.Same( first, router.HitTest( 3, 3 )!.Region );
	}

	[Fact]
	public void SamePanelOverlapUsesPriorityThenLaterRegistration() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel panel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel,
				HitTestPriority = 5
			}
		);
		using CursesInteractionRegion second = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel,
				HitTestPriority = 4
			}
		);

		Assert.Same( first, router.HitTest( 3, 3 )!.Region );

		second.HitTestPriority = 5;
		Assert.Same( second, router.HitTest( 3, 3 )!.Region );
	}

	[Fact]
	public void PanelVisibilityAndDisposalChangeEligibilityWithoutReregistration() {
		CursesScreen screen = new( 30, 15 );
		CursesPanel panel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion ordinary = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 15, 30 )
			)
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel
			}
		);

		Assert.Same( panelRegion, router.HitTest( 3, 3 )!.Region );

		panel.Hide();
		Assert.Same( ordinary, router.HitTest( 3, 3 )!.Region );

		panel.Show();
		Assert.Same( panelRegion, router.HitTest( 3, 3 )!.Region );

		panel.Dispose();
		Assert.Same( ordinary, router.HitTest( 3, 3 )!.Region );
	}

	[Fact]
	public void BlankTransparentPanelStillOwnsInteractionRegion() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel panel = screen.CreatePanel( 2, 2, 5, 5 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion ordinary = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 15, 30 )
			)
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel
			}
		);

		Assert.Same( panelRegion, router.HitTest( 3, 3 )!.Region );
	}

	[Fact]
	public void EquivalentRepeatedHitTestsAreDeterministic() {
		CursesScreen screen = new( 30, 15 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 10, 10 )
			) {
				HitTestPriority = 3
			}
		);
		using CursesInteractionRegion second = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 10, 10 )
			) {
				HitTestPriority = 3
			}
		);

		for ( int iteration = 0; iteration < 1000; iteration++ ) {
			Assert.Same( second, router.HitTest( 5, 5 )!.Region );
		}
	}

	[Fact]
	public void HitTestAfterRouterDisposalThrows() {
		CursesScreen screen = new( 10, 5 );
		CursesInteractionRouter router = new( screen );
		router.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.HitTest( 0, 0 );
			}
		);
	}
}
