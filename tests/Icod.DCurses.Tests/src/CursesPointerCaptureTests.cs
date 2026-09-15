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

/// <summary>Exercises explicit singular pointer capture and capture lifetime.</summary>
public sealed class CursesPointerCaptureTests {
	[Fact]
	public void CaptureRejectsInvalidOwnershipAndDoesNotImplyFocus() {
		CursesScreen screen = new( 20, 8 );
		CursesScreen foreignScreen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRouter foreignRouter = new( foreignScreen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		using CursesInteractionRegion foreign = foreignRouter.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);

		Assert.Throws<ArgumentOutOfRangeException>( () => {
			_ = router.CapturePointer( region, CursesMouseButton.None );
		} );
		Assert.Throws<ArgumentException>( () => {
			_ = router.CapturePointer( foreign, CursesMouseButton.Primary );
		} );

		using CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		Assert.Null( router.FocusedRegion );
		Assert.Throws<InvalidOperationException>( () => {
			_ = router.CapturePointer( region, CursesMouseButton.Secondary );
		} );
	}

	[Fact]
	public void CaptureRejectsDisposedDisabledAndEmptyRegions() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion disposed = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);
		disposed.Dispose();
		using CursesInteractionRegion disabled = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsEnabled = false
			}
		);
		using CursesInteractionRegion empty = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 0, 1 )
			)
		);

		Assert.Throws<ObjectDisposedException>( () => {
			_ = router.CapturePointer( disposed, CursesMouseButton.Primary );
		} );
		Assert.Throws<InvalidOperationException>( () => {
			_ = router.CapturePointer( disabled, CursesMouseButton.Primary );
		} );
		Assert.Throws<InvalidOperationException>( () => {
			_ = router.CapturePointer( empty, CursesMouseButton.Primary );
		} );
	}

	[Fact]
	public void CapturedMoveUsesSignedOutOfBoundsLocalCoordinates() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		using CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		CursesInteractionResult result = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 0,
					row: 0
				)
			)
		);

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Kind );
		Assert.Same( region, result.Region );
		Assert.Null( result.Hit );
		CursesPointerTarget target = Assert.IsType<CursesPointerTarget>( result.PointerTarget );
		Assert.Same( region, target.Region );
		Assert.Equal( -2, target.LocalRow );
		Assert.Equal( -3, target.LocalColumn );
		Assert.False( target.IsInside );
	}

	[Fact]
	public void MatchingReleaseRoutesToCaptureThenReleasesIt() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 10,
					row: 7
				)
			)
		);
		Assert.Same( region, release.Region );
		Assert.False( Assert.IsType<CursesPointerTarget>( release.PointerTarget ).IsInside );

		CursesInteractionResult later = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 10,
					row: 7
				)
			)
		);
		Assert.Equal( CursesInteractionResultKind.Unrouted, later.Kind );
		Assert.Null( later.PointerTarget );
		lease.Dispose();
	}

	[Fact]
	public void NonmatchingReleaseDoesNotEndCapture() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		using CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		_ = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Secondary,
					column: 10,
					row: 7
				)
			)
		);
		CursesInteractionResult capturedMove = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 10,
					row: 7
				)
			)
		);

		Assert.Same( region, capturedMove.Region );
		Assert.NotNull( capturedMove.PointerTarget );
	}

	[Fact]
	public void LeaseDisposalAndRegionInvalidationReleaseCapture() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		lease.Dispose();
		lease.Dispose();

		using CursesPointerCaptureLease second = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		region.IsEnabled = false;
		CursesInteractionResult result = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 10,
					row: 7
				)
			)
		);
		Assert.Equal( CursesInteractionResultKind.Unrouted, result.Kind );
		second.Dispose();
	}

	[Fact]
	public void ScopeActivationAndPanelHideReleaseExcludedCapture() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				Panel = panel
			}
		);
		using CursesPointerCaptureLease first = router.CapturePointer(
			panelRegion,
			CursesMouseButton.Primary
		);
		panel.Hide();
		Assert.Equal(
			CursesInteractionResultKind.Unrouted,
			router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Move,
						CursesMouseButton.Primary,
						column: 8,
						row: 6
					)
				)
			).Kind
		);
		first.Dispose();

		using CursesInteractionRegion rootRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);
		using CursesInteractionScope modal = router.RegisterScope();
		using CursesPointerCaptureLease second = router.CapturePointer(
			rootRegion,
			CursesMouseButton.Primary
		);
		using CursesInteractionScopeLease scopeLease = router.ActivateScope( modal );
		Assert.Equal(
			CursesInteractionResultKind.Unrouted,
			router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Move,
						CursesMouseButton.Primary,
						column: 8,
						row: 6
					)
				)
			).Kind
		);
		second.Dispose();
	}

	[Fact]
	public void RouterDisposalMakesCaptureLeaseStaleAndIdempotent() {
		CursesScreen screen = new( 20, 8 );
		CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);
		CursesPointerCaptureLease lease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		router.Dispose();
		lease.Dispose();
		lease.Dispose();
	}
}
