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
