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

/// <summary>Freezes deterministic integer-only spatial focus navigation for 1.5.</summary>
public sealed class CursesSpatialFocusTests {
	[Fact]
	public void CardinalDirectionsChooseExpectedRegions() {
		CursesScreen screen = new( 20, 12 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion center = RegisterFocusable( router, 5, 8, 2, 2 );
		using CursesInteractionRegion up = RegisterFocusable( router, 1, 8, 2, 2 );
		using CursesInteractionRegion down = RegisterFocusable( router, 9, 8, 2, 2 );
		using CursesInteractionRegion left = RegisterFocusable( router, 5, 2, 2, 2 );
		using CursesInteractionRegion right = RegisterFocusable( router, 5, 14, 2, 2 );

		Assert.True( router.Focus( center ) );
		Assert.Same( up, router.MoveFocus( CursesFocusDirection.Up ) );
		Assert.True( router.Focus( center ) );
		Assert.Same( down, router.MoveFocus( CursesFocusDirection.Down ) );
		Assert.True( router.Focus( center ) );
		Assert.Same( left, router.MoveFocus( CursesFocusDirection.Left ) );
		Assert.True( router.Focus( center ) );
		Assert.Same( right, router.MoveFocus( CursesFocusDirection.Right ) );
	}

	[Fact]
	public void SpatialMoveWithoutCurrentFocusDoesNotSynthesizeFocus() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router, 2, 8, 2, 2 );

		Assert.Null( router.FocusedRegion );
		Assert.Null( router.MoveFocus( CursesFocusDirection.Right ) );
		Assert.Null( router.FocusedRegion );
	}

	[Fact]
	public void SpatialMoveDoesNotWrapAndPreservesCurrentFocus() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion left = RegisterFocusable( router, 2, 2, 2, 2 );
		using CursesInteractionRegion right = RegisterFocusable( router, 2, 12, 2, 2 );

		Assert.True( router.Focus( right ) );
		Assert.Null( router.MoveFocus( CursesFocusDirection.Right ) );
		Assert.Same( right, router.FocusedRegion );
	}

	[Fact]
	public void PerpendicularOverlapOutranksCloserPrimaryGap() {
		CursesScreen screen = new( 30, 12 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focus = RegisterFocusable( router, 5, 5, 2, 2 );
		using CursesInteractionRegion overlapping = RegisterFocusable( router, 5, 12, 2, 2 );
		using CursesInteractionRegion nearerButDiagonal = RegisterFocusable( router, 1, 8, 2, 2 );

		Assert.True( router.Focus( focus ) );
		Assert.Same(
			overlapping,
			router.MoveFocus( CursesFocusDirection.Right )
		);
	}

	[Fact]
	public void PrimaryEdgeDistanceBreaksOverlapTie() {
		CursesScreen screen = new( 30, 12 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focus = RegisterFocusable( router, 5, 5, 2, 2 );
		using CursesInteractionRegion nearer = RegisterFocusable( router, 5, 9, 2, 2 );
		using CursesInteractionRegion farther = RegisterFocusable( router, 5, 13, 2, 2 );

		Assert.True( router.Focus( focus ) );
		Assert.Same( nearer, router.MoveFocus( CursesFocusDirection.Right ) );
	}

	[Fact]
	public void PerpendicularCenterDistanceBreaksEqualGapTie() {
		CursesScreen screen = new( 30, 16 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focus = RegisterFocusable( router, 6, 5, 2, 2 );
		using CursesInteractionRegion nearerCenter = RegisterFocusable( router, 3, 10, 2, 2 );
		using CursesInteractionRegion fartherCenter = RegisterFocusable( router, 11, 10, 2, 2 );

		Assert.True( router.Focus( focus ) );
		Assert.Same(
			nearerCenter,
			router.MoveFocus( CursesFocusDirection.Right )
		);
	}

	[Fact]
	public void TraversalOrderThenRegistrationOrdinalBreakExactGeometryTies() {
		CursesScreen screen = new( 30, 12 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focus = RegisterFocusable( router, 5, 5, 2, 2 );
		using CursesInteractionRegion laterTraversal = RegisterFocusable(
			router,
			5,
			10,
			2,
			2,
			traversalOrder: 5
		);
		using CursesInteractionRegion lowerTraversal = RegisterFocusable(
			router,
			5,
			10,
			2,
			2,
			traversalOrder: 2
		);

		Assert.True( router.Focus( focus ) );
		Assert.Same(
			lowerTraversal,
			router.MoveFocus( CursesFocusDirection.Right )
		);

		using CursesInteractionRouter ordinalRouter = new( screen );
		using CursesInteractionRegion ordinalFocus = RegisterFocusable( ordinalRouter, 5, 5, 2, 2 );
		using CursesInteractionRegion first = RegisterFocusable( ordinalRouter, 5, 10, 2, 2 );
		using CursesInteractionRegion second = RegisterFocusable( ordinalRouter, 5, 10, 2, 2 );

		Assert.True( ordinalRouter.Focus( ordinalFocus ) );
		Assert.Same(
			first,
			ordinalRouter.MoveFocus( CursesFocusDirection.Right )
		);
	}

	[Fact]
	public void SpatialGeometryUsesEffectivePanelClipping() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		CursesPanel panel = screen.CreatePanel( 2, 5, 3, 4 );
		using CursesInteractionRegion clippedFocus = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 2, 2, 4 )
			) {
				Panel = panel,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion candidate = RegisterFocusable( router, 2, 9, 2, 2 );

		Assert.True( router.Focus( clippedFocus ) );
		Assert.Same(
			candidate,
			router.MoveFocus( CursesFocusDirection.Right )
		);
	}

	[Fact]
	public void ActiveScopeExcludesCloserOuterRegion() {
		CursesScreen screen = new( 30, 12 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope modal = router.RegisterScope();
		using CursesInteractionRegion focus = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 5, 5, 2, 2 )
			) {
				Scope = modal,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion modalCandidate = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 5, 14, 2, 2 )
			) {
				Scope = modal,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion outerCloser = RegisterFocusable( router, 5, 9, 2, 2 );
		using CursesInteractionScopeLease lease = router.ActivateScope( modal );

		Assert.True( router.Focus( focus ) );
		Assert.Same(
			modalCandidate,
			router.MoveFocus( CursesFocusDirection.Right )
		);
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router,
		int row,
		int column,
		int rows,
		int columns,
		int traversalOrder = 0
	) {
		ArgumentNullException.ThrowIfNull( router );
		return router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle(
					row,
					column,
					rows,
					columns
				)
			) {
				IsFocusable = true,
				TraversalOrder = traversalOrder
			}
		);
	}
}
