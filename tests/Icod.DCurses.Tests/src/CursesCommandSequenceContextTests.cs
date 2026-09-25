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

public sealed class CursesCommandSequenceContextTests {
	[Fact]
	public void RoutingContextChangesInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion second = RegisterFocusable(
			router,
			new CursesRectangle( 5, 0, 4, 4 ),
			traversalOrder: 1
		);
		first.BindGestureSequence(
			[ Character( 'g' ), Character( 'g' ) ],
			new CursesCommand( "go-top" )
		);
		Assert.True( router.Focus( first ) );
		BeginPending( router );

		Assert.True( router.Focus( second ) );

		Assert.False( router.HasPendingCommandSequence );
		Assert.NotEqual(
			CursesCommandSequenceResultKind.Completed,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);
	}

	[Fact]
	public void ClearAndMoveFocusInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion first = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion second = RegisterFocusable(
			router,
			new CursesRectangle( 5, 0, 4, 4 ),
			traversalOrder: 1
		);
		BindGlobalSequence( router );
		Assert.True( router.Focus( first ) );
		BeginPending( router );

		router.ClearFocus();

		Assert.False( router.HasPendingCommandSequence );
		Assert.True( router.Focus( first ) );
		BeginPending( router );

		Assert.Same(
			second,
			router.MoveFocus( CursesFocusDirection.Forward )
		);

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void SpatialFocusMovementInvalidatesPendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion left = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion right = RegisterFocusable(
			router,
			new CursesRectangle( 0, 8, 4, 4 ),
			traversalOrder: 1
		);
		BindGlobalSequence( router );
		Assert.True( router.Focus( left ) );
		BeginPending( router );

		Assert.Same( right, router.MoveFocus( CursesFocusDirection.Right ) );

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void ScopeActivationAndDeactivationInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope scope = router.RegisterScope();
		BindGlobalSequence( router );
		BeginPending( router );

		CursesInteractionScopeLease lease = router.ActivateScope( scope );

		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );

		lease.Dispose();

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void RegionEligibilityAndBoundsInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		BindGlobalSequence( router );
		Assert.True( router.Focus( region ) );
		BeginPending( router );

		region.IsEnabled = false;

		Assert.False( router.HasPendingCommandSequence );
		region.IsEnabled = true;
		Assert.True( router.Focus( region ) );
		BeginPending( router );

		region.SetBounds( new CursesRectangle( 1, 1, 4, 4 ) );

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void PanelEligibilityChangesInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 2, 2, 2 )
			) {
				Panel = panel,
				IsFocusable = true
			}
		);
		BindGlobalSequence( router );
		Assert.True( router.Focus( region ) );
		BeginPending( router );

		panel.Hide();

		Assert.False( router.HasPendingCommandSequence );
		panel.Show();
		Assert.True( router.Focus( region ) );
		BeginPending( router );

		panel.Resize( 2, 2 );

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void ScreenResizeAndRegionDisposalInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		BindGlobalSequence( router );
		Assert.True( router.Focus( region ) );
		BeginPending( router );

		screen.Resize( 19, 10 );

		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );

		region.Dispose();

		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void BindingMutationsInvalidatePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 4, 4 ),
			traversalOrder: 0
		);
		using CursesInteractionScope scope = router.RegisterScope();
		BindGlobalSequence( router );
		CursesKeyGesture x = Character( 'x' );
		CursesKeyGesture y = Character( 'y' );
		BeginPending( router );

		region.BindGesture( x, new CursesCommand( "single" ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );
		Assert.True( region.UnbindGesture( x ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );

		scope.BindGestureSequence( [ x, x ], new CursesCommand( "scope" ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );
		Assert.True( scope.UnbindGestureSequence( [ x, x ] ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );

		router.BindGlobalGesture( y, new CursesCommand( "global-single" ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );
		Assert.True( router.UnbindGlobalGesture( y ) );
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );

		router.BindGlobalGestureSequence(
			[ y, y ],
			new CursesCommand( "global-sequence" )
		);
		Assert.False( router.HasPendingCommandSequence );
		BeginPending( router );
		Assert.True( router.UnbindGlobalGestureSequence( [ y, y ] ) );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void FailedBindingMutationsPreservePendingSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture x = Character( 'x' );
		BindGlobalSequence( router );
		router.BindGlobalGesture( x, new CursesCommand( "single" ) );
		BeginPending( router );

		Assert.Throws<InvalidOperationException>(
			() => router.BindGlobalGesture(
				x,
				new CursesCommand( "duplicate" )
			)
		);
		Assert.True( router.HasPendingCommandSequence );
		Assert.False( router.UnbindGlobalGesture( Character( 'z' ) ) );
		Assert.True( router.HasPendingCommandSequence );
	}

	[Fact]
	public void LazyFocusRepairClearsPendingBeforeProcessing() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion fallback = RegisterFocusable(
			router,
			new CursesRectangle( 0, 0, 1, 1 ),
			traversalOrder: 0
		);
		using CursesInteractionRegion edge = RegisterFocusable(
			router,
			new CursesRectangle( 8, 18, 2, 2 ),
			traversalOrder: 1
		);
		edge.BindGestureSequence(
			[ Character( 'g' ), Character( 'g' ) ],
			new CursesCommand( "edge" )
		);
		Assert.True( router.Focus( edge ) );
		BeginPending( router );

		screen.Resize( 10, 5 );
		CursesCommandSequenceResult result = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'g' ) )
		);

		Assert.Same( fallback, router.FocusedRegion );
		Assert.NotEqual( CursesCommandSequenceResultKind.Completed, result.Kind );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void DisposedRouterRejectsSequenceMembers() {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		CursesKeyGesture[] sequence = [ Character( 'g' ), Character( 'g' ) ];
		router.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => _ = router.HasPendingCommandSequence
		);
		Assert.Throws<ObjectDisposedException>(
			() => router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			)
		);
		Assert.Throws<ObjectDisposedException>(
			() => router.CancelPendingCommandSequence()
		);
		Assert.Throws<ObjectDisposedException>(
			() => router.BindGlobalGestureSequence(
				sequence,
				new CursesCommand( "command" )
			)
		);
		Assert.Throws<ObjectDisposedException>(
			() => router.UnbindGlobalGestureSequence( sequence )
		);
		Assert.Throws<ObjectDisposedException>(
			() => router.GetEffectiveGestureSequenceBindings()
		);
	}

	private static void BeginPending(
		CursesInteractionRouter router
	) {
		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);
		Assert.True( router.HasPendingCommandSequence );
	}

	private static void BindGlobalSequence(
		CursesInteractionRouter router
	) {
		router.BindGlobalGestureSequence(
			[ Character( 'g' ), Character( 'g' ) ],
			new CursesCommand( "global" )
		);
	}

	private static CursesKeyGesture Character(
		char value
	) {
		return CursesKeyGesture.ForCharacter( new Rune( value ) );
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router,
		CursesRectangle bounds,
		int traversalOrder
	) {
		return router.RegisterRegion(
			new CursesInteractionRegionOptions( bounds ) {
				IsFocusable = true,
				TraversalOrder = traversalOrder
			}
		);
	}
}
