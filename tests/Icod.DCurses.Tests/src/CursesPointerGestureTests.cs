/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	Free Software Foundation, either version 3 of the License, or
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

/// <summary>Exercises deterministic clock-free pointer-gesture normalization.</summary>
public sealed class CursesPointerGestureTests {
	[Fact]
	public void PointerGestureKindNumericsAreFrozen() {
		Assert.Equal( 0, (int)CursesPointerGestureKind.Press );
		Assert.Equal( 1, (int)CursesPointerGestureKind.Release );
		Assert.Equal( 2, (int)CursesPointerGestureKind.Move );
		Assert.Equal( 3, (int)CursesPointerGestureKind.Click );
		Assert.Equal( 4, (int)CursesPointerGestureKind.DragStart );
		Assert.Equal( 5, (int)CursesPointerGestureKind.DragMove );
		Assert.Equal( 6, (int)CursesPointerGestureKind.DragEnd );
		Assert.Equal( 7, (int)CursesPointerGestureKind.WheelUp );
		Assert.Equal( 8, (int)CursesPointerGestureKind.WheelDown );
		Assert.Equal( 9, (int)CursesPointerGestureKind.WheelLeft );
		Assert.Equal( 10, (int)CursesPointerGestureKind.WheelRight );
	}

	[Fact]
	public void StandaloneMouseReportsMapOneToOne() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		AssertGesture(
			router.Route(
				Mouse(
					CursesMouseAction.Press,
					CursesMouseButton.Primary,
					column: 4,
					row: 3,
					CursesKeyModifiers.Control
				)
			),
			CursesPointerGestureKind.Press,
			CursesMouseButton.Primary,
			CursesKeyModifiers.Control,
			region
		);

		using CursesInteractionRouter releaseRouter = new( screen );
		using CursesInteractionRegion releaseRegion = RegisterRegion( releaseRouter );
		AssertGesture(
			releaseRouter.Route(
				Mouse(
					CursesMouseAction.Release,
					CursesMouseButton.Secondary,
					column: 4,
					row: 3
				)
			),
			CursesPointerGestureKind.Release,
			CursesMouseButton.Secondary,
			CursesKeyModifiers.None,
			releaseRegion
		);

		AssertGesture(
			releaseRouter.Route(
				Mouse(
					CursesMouseAction.Move,
					CursesMouseButton.None,
					column: 5,
					row: 3
				)
			),
			CursesPointerGestureKind.Move,
			CursesMouseButton.None,
			CursesKeyModifiers.None,
			releaseRegion
		);

		AssertWheelGesture( releaseRouter, releaseRegion, CursesMouseAction.WheelUp, CursesPointerGestureKind.WheelUp );
		AssertWheelGesture( releaseRouter, releaseRegion, CursesMouseAction.WheelDown, CursesPointerGestureKind.WheelDown );
		AssertWheelGesture( releaseRouter, releaseRegion, CursesMouseAction.WheelLeft, CursesPointerGestureKind.WheelLeft );
		AssertWheelGesture( releaseRouter, releaseRegion, CursesMouseAction.WheelRight, CursesPointerGestureKind.WheelRight );
	}

	[Fact]
	public void SameTargetPressReleaseWithoutMovementProducesClick() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		CursesInteractionResult press = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal( CursesPointerGestureKind.Press, Assert.IsType<CursesPointerGesture>( press.PointerGesture ).Kind );

		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 6,
				row: 4
			)
		);
		CursesPointerGesture click = Assert.IsType<CursesPointerGesture>( release.PointerGesture );
		Assert.Equal( CursesPointerGestureKind.Click, click.Kind );
		Assert.Same( region, Assert.IsType<CursesPointerTarget>( click.Target ).Region );
	}

	[Fact]
	public void ReleaseToDifferentTargetIsNotClick() {
		CursesScreen screen = new( 24, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion left = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 2, 3, 4 )
			)
		);
		using CursesInteractionRegion right = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 12, 3, 4 )
			)
		);

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 3,
				row: 3
			)
		);
		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 13,
				row: 3
			)
		);

		CursesPointerGesture gesture = Assert.IsType<CursesPointerGesture>( release.PointerGesture );
		Assert.Equal( CursesPointerGestureKind.Release, gesture.Kind );
		Assert.Same( right, Assert.IsType<CursesPointerTarget>( gesture.Target ).Region );
		Assert.Same( right, release.Region );
		Assert.NotSame( left, release.Region );
	}

	[Fact]
	public void SameCellMoveDoesNotStartDragOrPreventClick() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		CursesInteractionResult move = router.Route(
			Mouse(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.Move,
			Assert.IsType<CursesPointerGesture>( move.PointerGesture ).Kind
		);

		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.Click,
			Assert.IsType<CursesPointerGesture>( release.PointerGesture ).Kind
		);
		Assert.Same( region, release.Region );
	}

	[Fact]
	public void ChangedMoveStartsContinuesAndEndsDragWithoutClick() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		CursesInteractionResult start = router.Route(
			Mouse(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 5,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.DragStart,
			Assert.IsType<CursesPointerGesture>( start.PointerGesture ).Kind
		);

		CursesInteractionResult move = router.Route(
			Mouse(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 6,
				row: 4
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.DragMove,
			Assert.IsType<CursesPointerGesture>( move.PointerGesture ).Kind
		);

		CursesInteractionResult end = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 6,
				row: 4
			)
		);
		CursesPointerGesture endGesture = Assert.IsType<CursesPointerGesture>( end.PointerGesture );
		Assert.Equal( CursesPointerGestureKind.DragEnd, endGesture.Kind );
		Assert.NotEqual( CursesPointerGestureKind.Click, endGesture.Kind );
		Assert.Same( region, Assert.IsType<CursesPointerTarget>( endGesture.Target ).Region );
	}

	[Fact]
	public void ExplicitCaptureFeedsSignedOutOfBoundsTargetsIntoDragGestures() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		CursesPointerCaptureLease capture = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		CursesInteractionResult start = router.Route(
			Mouse(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 0,
				row: 0
			)
		);
		CursesPointerGesture startGesture = Assert.IsType<CursesPointerGesture>( start.PointerGesture );
		Assert.Equal( CursesPointerGestureKind.DragStart, startGesture.Kind );
		CursesPointerTarget startTarget = Assert.IsType<CursesPointerTarget>( startGesture.Target );
		Assert.Same( region, startTarget.Region );
		Assert.Equal( -2, startTarget.LocalRow );
		Assert.Equal( -3, startTarget.LocalColumn );
		Assert.False( startTarget.IsInside );

		CursesInteractionResult end = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 19,
				row: 7
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.DragEnd,
			Assert.IsType<CursesPointerGesture>( end.PointerGesture ).Kind
		);
		capture.Dispose();
	}

	[Fact]
	public void RegionInvalidationCancelsPendingClickOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		region.IsEnabled = false;
		region.IsEnabled = true;

		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.Release,
			Assert.IsType<CursesPointerGesture>( release.PointerGesture ).Kind
		);
	}

	[Fact]
	public void ScopeChangeCancelsPendingClickOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );
		using CursesInteractionScope modal = router.RegisterScope();

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		CursesInteractionScopeLease lease = router.ActivateScope( modal );
		lease.Dispose();

		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.Release,
			Assert.IsType<CursesPointerGesture>( release.PointerGesture ).Kind
		);
	}

	[Fact]
	public void CaptureLeaseDisposalCancelsPendingClickOwnership() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		CursesPointerCaptureLease capture = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		capture.Dispose();

		CursesInteractionResult release = router.Route(
			Mouse(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.Release,
			Assert.IsType<CursesPointerGesture>( release.PointerGesture ).Kind
		);
	}

	[Fact]
	public void WheelDoesNotMutatePressOrCaptureState() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterRegion( router );

		_ = router.Route(
			Mouse(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 4,
				row: 3
			)
		);
		using CursesPointerCaptureLease capture = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);

		CursesInteractionResult wheel = router.Route(
			Mouse(
				CursesMouseAction.WheelUp,
				CursesMouseButton.None,
				column: 4,
				row: 3
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.WheelUp,
			Assert.IsType<CursesPointerGesture>( wheel.PointerGesture ).Kind
		);

		CursesInteractionResult move = router.Route(
			Mouse(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 0,
				row: 0
			)
		);
		Assert.Equal(
			CursesPointerGestureKind.DragStart,
			Assert.IsType<CursesPointerGesture>( move.PointerGesture ).Kind
		);
		Assert.Same( region, move.Region );
	}

	private static CursesInteractionRegion RegisterRegion(
		CursesInteractionRouter router
	) {
		ArgumentNullException.ThrowIfNull( router );
		return router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 4, 5 )
			)
		);
	}

	private static CursesInputEvent Mouse(
		CursesMouseAction action,
		CursesMouseButton button,
		int column,
		int row,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None
	) {
		return CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				action,
				button,
				column,
				row,
				modifiers
			)
		);
	}

	private static void AssertGesture(
		CursesInteractionResult result,
		CursesPointerGestureKind expectedKind,
		CursesMouseButton expectedButton,
		CursesKeyModifiers expectedModifiers,
		CursesInteractionRegion expectedRegion
	) {
		ArgumentNullException.ThrowIfNull( result );
		ArgumentNullException.ThrowIfNull( expectedRegion );
		CursesPointerGesture gesture = Assert.IsType<CursesPointerGesture>( result.PointerGesture );
		Assert.Equal( expectedKind, gesture.Kind );
		Assert.Equal( expectedButton, gesture.Button );
		Assert.Equal( expectedModifiers, gesture.Modifiers );
		CursesPointerTarget target = Assert.IsType<CursesPointerTarget>( gesture.Target );
		Assert.Same( expectedRegion, target.Region );
		Assert.True( target.IsInside );
	}

	private static void AssertWheelGesture(
		CursesInteractionRouter router,
		CursesInteractionRegion region,
		CursesMouseAction action,
		CursesPointerGestureKind expectedKind
	) {
		ArgumentNullException.ThrowIfNull( router );
		ArgumentNullException.ThrowIfNull( region );
		AssertGesture(
			router.Route(
				Mouse(
					action,
					CursesMouseButton.None,
					column: 4,
					row: 3
				)
			),
			expectedKind,
			CursesMouseButton.None,
			CursesKeyModifiers.None,
			region
		);
	}
}
