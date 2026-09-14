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

using System.Text;
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

	[Fact]
	public void CaptureCannotResurrectAcrossScreenResizeRoundTrip() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 6, 15, 1, 1 )
			)
		);
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		screen.Resize( 10, 4 );
		screen.Resize( 20, 8 );

		CursesInteractionResult later = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 19,
					row: 7
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
	public void ClickOwnershipCannotResurrectAcrossScreenResizeRoundTrip() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 6, 15, 1, 1 )
			)
		);

		CursesInteractionResult press = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 15,
					row: 6
				)
			)
		);
		Assert.Same( region, press.Region );
		Assert.Equal( CursesPointerGestureKind.Press, press.PointerGesture?.Kind );

		screen.Resize( 10, 4 );
		screen.Resize( 20, 8 );

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 15,
					row: 6
				)
			)
		);

		Assert.Same( region, release.Region );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
	}

	[Fact]
	public void RegionBoundsRoundTripCannotResurrectPointerOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 1, 1 )
			)
		);
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		_ = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 3,
					row: 2
				)
			)
		);

		region.SetBounds( new CursesRectangle( 20, 20, 1, 1 ) );
		region.SetBounds( new CursesRectangle( 2, 3, 1, 1 ) );

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 3,
					row: 2
				)
			)
		);
		Assert.Same( region, release.Region );
		Assert.Null( release.PointerTarget );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
		captureLease.Dispose();
		captureLease.Dispose();
	}

	[Fact]
	public void PanelResizeRoundTripCannotResurrectPointerOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 4, 4 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 3, 3, 1, 1 )
			) {
				Panel = panel
			}
		);
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		_ = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 4,
					row: 4
				)
			)
		);

		panel.Resize( 2, 2 );
		panel.Resize( 4, 4 );

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 4,
					row: 4
				)
			)
		);
		Assert.Same( region, release.Region );
		Assert.Null( release.PointerTarget );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
		captureLease.Dispose();
		captureLease.Dispose();
	}

	[Fact]
	public void PanelDisposalCancelsCaptureAndGestureOwnership() {
		CursesScreen screen = new( 20, 8 );
		CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
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
		_ = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 1,
					row: 1
				)
			)
		);

		panel.Dispose();

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
		Assert.Equal( CursesInteractionResultKind.Unrouted, release.Kind );
		Assert.Null( release.Region );
		Assert.Null( release.PointerTarget );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
		captureLease.Dispose();
		captureLease.Dispose();
	}

	[Fact]
	public void NestedScopeRoundTripCancelsOuterPointerOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 1, 1 )
			) {
				Scope = outer
			}
		);
		using CursesInteractionScopeLease outerLease = router.ActivateScope( outer );
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		_ = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 3,
					row: 2
				)
			)
		);

		CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
		innerLease.Dispose();

		CursesInteractionResult release = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Release,
					CursesMouseButton.Primary,
					column: 3,
					row: 2
				)
			)
		);
		Assert.Same( region, release.Region );
		Assert.Null( release.PointerTarget );
		Assert.Equal( CursesPointerGestureKind.Release, release.PointerGesture?.Kind );
		captureLease.Dispose();
		captureLease.Dispose();
	}

	[Fact]
	public void OutOfOrderScopeLeaseFailureIsAtomicForCaptureAndCommands() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 1, 1 )
			) {
				Scope = inner
			}
		);
		CursesCommand command = new( "inner.command" );
		inner.BindGesture(
			CursesKeyGesture.ForCharacter( new Rune( 'x' ) ),
			command
		);
		CursesInteractionScopeLease outerLease = router.ActivateScope( outer );
		CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		Assert.Throws<InvalidOperationException>( () => outerLease.Dispose() );

		CursesInteractionResult captured = router.Route(
			CursesInputEvent.FromMouse(
				new CursesMouseEvent(
					CursesMouseAction.Move,
					CursesMouseButton.Primary,
					column: 10,
					row: 6
				)
			)
		);
		Assert.Same( region, captured.Region );
		Assert.NotNull( captured.PointerTarget );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromText( new Rune( 'x' ) ) ).Command
		);

		captureLease.Dispose();
		innerLease.Dispose();
		outerLease.Dispose();
	}

	[Fact]
	public void RouterDisposalUnsubscribesResizeAndLeavesLeasesIdempotent() {
		CursesScreen screen = new( 20, 8 );
		CursesInteractionRouter router = new( screen );
		CursesInteractionScope scope = router.RegisterScope();
		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				Scope = scope
			}
		);
		CursesInteractionScopeLease scopeLease = router.ActivateScope( scope );
		CursesPointerCaptureLease captureLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		router.Dispose();
		screen.Resize( 10, 4 );
		screen.Resize( 20, 8 );
		captureLease.Dispose();
		captureLease.Dispose();
		scopeLease.Dispose();
		scopeLease.Dispose();
		router.Dispose();
	}
}
