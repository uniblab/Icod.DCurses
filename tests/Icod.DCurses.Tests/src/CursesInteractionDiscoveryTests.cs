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

public sealed class CursesInteractionDiscoveryTests {
	[Fact]
	public void FocusedRegionWinsOverGlobalInDiscovery() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand local = new( "local" );
		router.BindGlobalGesture( enter, new CursesCommand( "global" ) );
		region.BindGesture( enter, local );
		Assert.True( router.Focus( region ) );

		IReadOnlyList<CursesCommandBinding> bindings = router.GetEffectiveGestureBindings();

		Assert.Single( bindings );
		Assert.Equal( enter, bindings[0].Gesture );
		Assert.Same( local, bindings[0].Command );
		Assert.Same( local, router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command );
	}

	[Fact]
	public void DiscoverySnapshotRetainsBindingsAfterUnbind() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand local = new( "local" );
		CursesCommand global = new( "global" );
		router.BindGlobalGesture( enter, global );
		region.BindGesture( enter, local );
		Assert.True( router.Focus( region ) );
		IReadOnlyList<CursesCommandBinding> before = router.GetEffectiveGestureBindings();

		Assert.True( region.UnbindGesture( enter ) );

		Assert.Same( local, Assert.Single( before ).Command );
		Assert.Same( global, Assert.Single( router.GetEffectiveGestureBindings() ).Command );
	}

	[Fact]
	public void ModalScopeDiscoveryAgreesWithCommandRouting() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope parent = router.RegisterScope();
		using CursesInteractionScope child = router.RegisterScope(
			new CursesInteractionScopeOptions { Parent = parent }
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions( new CursesRectangle( 0, 0, 2, 2 ) ) {
				Scope = child,
				IsFocusable = true
			}
		);
		CursesCommand local = new( "local" );
		CursesCommand scoped = new( "scoped" );
		CursesCommand hidden = new( "hidden" );
		CursesCommand global = new( "global" );
		region.BindGesture( CursesKeyGesture.ForKey( CursesKey.Enter ), local );
		child.BindGesture( CursesKeyGesture.ForKey( CursesKey.Escape ), scoped );
		parent.BindGesture( CursesKeyGesture.ForKey( CursesKey.Tab ), hidden );
		router.BindGlobalGesture( CursesKeyGesture.ForKey( CursesKey.Backspace ), global );
		using CursesInteractionScopeLease lease = router.ActivateScope( child );
		Assert.True( router.Focus( region ) );

		IReadOnlyList<CursesCommandBinding> bindings = router.GetEffectiveGestureBindings();

		Assert.Equal( new[] { local, scoped, global },
			bindings.Select( static entry => entry.Command ) );
		Assert.Null( router.Route( CursesInputEvent.FromKey( CursesKey.Tab ) ).Command );
		Assert.Same( scoped, router.Route( CursesInputEvent.FromKey( CursesKey.Escape ) ).Command );
		Assert.Same( global, router.Route( CursesInputEvent.FromKey( CursesKey.Backspace ) ).Command );
		router.ClearFocus();
		Assert.Equal( new[] { scoped, global },
			router.GetEffectiveGestureBindings().Select( static entry => entry.Command ) );
	}

	[Fact]
	public void DiscoveryIsReadOnlyAndRejectsDisposedRouter() {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		router.BindGlobalGesture( CursesKeyGesture.ForKey( CursesKey.Enter ),
			new CursesCommand( "global" ) );
		IReadOnlyList<CursesCommandBinding> bindings = router.GetEffectiveGestureBindings();
		Assert.Throws<NotSupportedException>(
			() => ((IList<CursesCommandBinding>)bindings).Clear()
		);
		router.Dispose();
		Assert.Throws<ObjectDisposedException>(
			() => router.GetEffectiveGestureBindings()
		);
	}

	[Fact]
	public void BindingRejectsDefaultGestureAndNullCommand() {
		Assert.Throws<ArgumentException>(
			() => new CursesCommandBinding( default, new CursesCommand( "any" ) )
		);
		Assert.Throws<ArgumentNullException>(
			() => new CursesCommandBinding( CursesKeyGesture.ForKey( CursesKey.Enter ), null! )
		);
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router
	) {
		return router.RegisterRegion(
			new CursesInteractionRegionOptions( new CursesRectangle( 0, 0, 2, 2 ) ) {
				IsFocusable = true
			}
		);
	}
}
