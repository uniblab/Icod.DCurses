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

public sealed class CursesInteractionFocusTests {
	[Fact]
	public void ExplicitFocusAndClearAreDeterministic() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			new CursesRectangle( 1, 2, 3, 4 )
		);

		Assert.Null( router.FocusedRegion );
		Assert.True( router.Focus( region ) );
		Assert.Same( region, router.FocusedRegion );

		router.ClearFocus();
		Assert.Null( router.FocusedRegion );
		router.ClearFocus();
		Assert.Null( router.FocusedRegion );
	}

	[Fact]
	public void FocusValidatesOwnershipDisposalAndEligibilityBeforeMutation() {
		CursesScreen firstScreen = new( 20, 10 );
		CursesScreen secondScreen = new( 20, 10 );
		using CursesInteractionRouter first = new( firstScreen );
		using CursesInteractionRouter second = new( secondScreen );
		using CursesInteractionRegion current = RegisterFocusable(
			first,
			new CursesRectangle( 0, 0, 1, 1 )
		);
		using CursesInteractionRegion foreign = RegisterFocusable(
			second,
			new CursesRectangle( 0, 0, 1, 1 )
		);
		using CursesInteractionRegion disabled = first.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 0, 1, 1 )
			) {
				IsEnabled = false,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion nonFocusable = first.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 0, 1, 1 )
			)
		);
		using CursesInteractionRegion clipped = RegisterFocusable(
			first,
			new CursesRectangle( 50, 50, 1, 1 )
		);

		Assert.True( first.Focus( current ) );

		Assert.Throws<ArgumentNullException>(
			() => {
				_ = first.Focus( null! );
			}
		);
		Assert.Throws<ArgumentException>(
			() => {
				_ = first.Focus( foreign );
			}
		);

		CursesInteractionRegion disposed = RegisterFocusable(
			first,
			new CursesRectangle( 3, 0, 1, 1 )
		);
		disposed.Dispose();
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = first.Focus( disposed );
			}
		);

		Assert.False( first.Focus( disabled ) );
		Assert.Same( current, first.FocusedRegion );
		Assert.False( first.Focus( nonFocusable ) );
		Assert.Same( current, first.FocusedRegion );
		Assert.False( first.Focus( clipped ) );
		Assert.Same( current, first.FocusedRegion );
	}

	[Fact]
	public void MoveFocusUsesTraversalOrderThenRegistrationAndAlwaysWraps() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion last = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 10
		);
		using CursesInteractionRegion first = RegisterFocusable(
			router,
			new CursesRectangle( 0, 1, 1, 1 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion second = RegisterFocusable(
			router,
			new CursesRectangle( 0, 2, 1, 1 ),
			traversalOrder: 0
		);

		Assert.Same(
			first,
			router.MoveFocus( CursesFocusDirection.Forward )
		);
		Assert.Same(
			second,
			router.MoveFocus( CursesFocusDirection.Forward )
		);
		Assert.Same(
			last,
			router.MoveFocus( CursesFocusDirection.Forward )
		);
		Assert.Same(
			first,
			router.MoveFocus( CursesFocusDirection.Forward )
		);
		Assert.Same(
			last,
			router.MoveFocus( CursesFocusDirection.Backward )
		);

		router.ClearFocus();
		Assert.Same(
			last,
			router.MoveFocus( CursesFocusDirection.Backward )
		);
	}

	[Fact]
	public void MoveFocusRejectsUndefinedDirectionAndReturnsNullWithoutEligibleRegions() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion disabled = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsEnabled = false,
				IsFocusable = true
			}
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = router.MoveFocus( (CursesFocusDirection)99 );
			}
		);
		Assert.Null( router.MoveFocus( CursesFocusDirection.Forward ) );
		Assert.Null( router.FocusedRegion );
		Assert.Null( router.MoveFocus( CursesFocusDirection.Backward ) );
	}

	[Fact]
	public void RegionOwnedEligibilityChangesRepairFocusedRegionForward() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion second = RegisterFocusable(
			router,
			new CursesRectangle( 0, 1, 1, 1 ),
			traversalOrder: 1
		);
		using CursesInteractionRegion third = RegisterFocusable(
			router,
			new CursesRectangle( 0, 2, 1, 1 ),
			traversalOrder: 2
		);

		Assert.True( router.Focus( second ) );
		second.IsEnabled = false;
		Assert.Same( third, router.FocusedRegion );

		third.IsFocusable = false;
		Assert.Same( first, router.FocusedRegion );

		first.SetBounds( new CursesRectangle( 40, 40, 1, 1 ) );
		Assert.Null( router.FocusedRegion );
	}

	[Fact]
	public void DisposingFocusedRegionRepairsFromItsFormerTraversalSlot() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 0
		);
		CursesInteractionRegion second = RegisterFocusable(
			router,
			new CursesRectangle( 0, 1, 1, 1 ),
			traversalOrder: 1
		);
		using CursesInteractionRegion third = RegisterFocusable(
			router,
			new CursesRectangle( 0, 2, 1, 1 ),
			traversalOrder: 2
		);

		Assert.True( router.Focus( second ) );
		second.Dispose();
		Assert.Same( third, router.FocusedRegion );

		third.Dispose();
		Assert.Same( first, router.FocusedRegion );
	}

	[Fact]
	public void FocusedRegionLazilyRepairsAfterScreenClipping() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion fallback = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion clipped = RegisterFocusable(
			router,
			new CursesRectangle( 8, 18, 2, 2 ),
			traversalOrder: 1
		);

		Assert.True( router.Focus( clipped ) );
		screen.Resize( 10, 5 );

		Assert.Same( fallback, router.FocusedRegion );
	}

	[Fact]
	public void FocusedRegionLazilyRepairsAfterPanelStateChanges() {
		CursesScreen screen = new( 20, 10 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 4, 4 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion fallback = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 2, 2, 2 )
			) {
				Panel = panel,
				IsFocusable = true,
				TraversalOrder = 1
			}
		);

		Assert.True( router.Focus( panelRegion ) );
		panel.Hide();
		Assert.Same( fallback, router.FocusedRegion );

		panel.Show();
		Assert.True( router.Focus( panelRegion ) );
		panel.Resize( 2, 2 );
		Assert.Same( fallback, router.FocusedRegion );
	}

	[Fact]
	public void HitTestDoesNotRepairLogicalFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion ordinary = RegisterFocusable(
			router,
			new CursesRectangle( 1, 1, 3, 3 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 3, 3 )
			) {
				Panel = panel,
				IsFocusable = true,
				TraversalOrder = 1
			}
		);

		Assert.True( router.Focus( panelRegion ) );
		panel.Hide();
		CursesInteractionHit? hit = router.HitTest( 1, 1 );
		Assert.NotNull( hit );
		Assert.Same( ordinary, hit.Region );

		panel.Show();
		Assert.Same( panelRegion, router.FocusedRegion );
	}

	[Fact]
	public void FocusOperationsRejectDisposedRouter() {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 )
		);
		router.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.FocusedRegion;
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.Focus( region );
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				router.ClearFocus();
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.MoveFocus( CursesFocusDirection.Forward );
			}
		);
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router,
		CursesRectangle bounds,
		int traversalOrder = 0
	) {
		ArgumentNullException.ThrowIfNull( router );
		return router.RegisterRegion(
			new CursesInteractionRegionOptions( bounds ) {
				IsFocusable = true,
				TraversalOrder = traversalOrder
			}
		);
	}
}
