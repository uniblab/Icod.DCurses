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

/// <summary>Freezes bounded scope-level command bindings and deterministic precedence.</summary>
public sealed class CursesScopedCommandTests {
	[Fact]
	public void ScopeBindingRejectsInvalidArgumentsAndDuplicatesBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope scope = router.RegisterScope();
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand first = new( "First" );
		CursesCommand second = new( "Second" );

		Assert.Throws<ArgumentException>(
			() => scope.BindGesture( default, first )
		);
		Assert.Throws<ArgumentNullException>(
			() => scope.BindGesture( gesture, null! )
		);

		scope.BindGesture( gesture, first );
		Assert.Throws<InvalidOperationException>(
			() => scope.BindGesture( gesture, second )
		);

		Assert.True( scope.UnbindGesture( gesture ) );
		Assert.False( scope.UnbindGesture( gesture ) );
		scope.BindGesture( gesture, second );
	}

	[Fact]
	public void DisposedScopeRejectsBindingOperations() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionScope scope = router.RegisterScope();
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Enter" );
		scope.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => scope.BindGesture( gesture, command )
		);
		Assert.Throws<ObjectDisposedException>(
			() => scope.UnbindGesture( gesture )
		);
	}

	[Fact]
	public void PerScopeBindingLimitIsEnforcedAndUnbindReleasesCapacity() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope scope = router.RegisterScope();
		CursesCommand command = new( "Command" );

		for ( int index = 0; index < CursesInteractionRouter.MaximumScopeGestureBindings; index++ ) {
			scope.BindGesture(
				CharacterGesture( index ),
				command
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => scope.BindGesture(
				CharacterGesture( CursesInteractionRouter.MaximumScopeGestureBindings ),
				command
			)
		);

		Assert.True( scope.UnbindGesture( CharacterGesture( 0 ) ) );
		scope.BindGesture(
			CharacterGesture( CursesInteractionRouter.MaximumScopeGestureBindings ),
			command
		);
	}

	[Fact]
	public void ScopeBindingsConsumeTheExistingRouterWideBindingBudget() {
		CursesScreen screen = new( 100, 100 );
		using CursesInteractionRouter router = new( screen );
		CursesCommand command = new( "Command" );
		List<CursesInteractionRegion> regions = [];
		using CursesInteractionScope nearlyFullScope = router.RegisterScope();
		using CursesInteractionScope overflowScope = router.RegisterScope();

		try {
			const int fullyBoundRegionCount = 63;
			for ( int regionIndex = 0; regionIndex < fullyBoundRegionCount; regionIndex++ ) {
				CursesInteractionRegion region = RegisterFocusable(
					router,
					scope: null,
					row: regionIndex,
					column: 0
				);
				regions.Add( region );
				for ( int gestureIndex = 0; gestureIndex < CursesInteractionRouter.MaximumRegionGestureBindings; gestureIndex++ ) {
					region.BindGesture(
						CharacterGesture( gestureIndex ),
						command
					);
				}
			}

			for ( int index = 0; index < CursesInteractionRouter.MaximumScopeGestureBindings - 1; index++ ) {
				nearlyFullScope.BindGesture(
					CharacterGesture( index ),
					command
				);
			}

			overflowScope.BindGesture(
				CharacterGesture( 400 ),
				command
			);
			Assert.Throws<InvalidOperationException>(
				() => overflowScope.BindGesture(
					CharacterGesture( 401 ),
					command
				)
			);

			Assert.True( nearlyFullScope.UnbindGesture( CharacterGesture( 0 ) ) );
			overflowScope.BindGesture(
				CharacterGesture( 401 ),
				command
			);
		} finally {
			foreach ( CursesInteractionRegion region in regions ) {
				region.Dispose();
			}
		}
	}

	[Fact]
	public void CommandPrecedenceIsRegionThenNearestScopeThenParentToBoundaryThenGlobal() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			inner
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand local = new( "Local" );
		CursesCommand nearest = new( "Nearest" );
		CursesCommand parent = new( "Parent" );
		CursesCommand global = new( "Global" );
		region.BindGesture( gesture, local );
		inner.BindGesture( gesture, nearest );
		outer.BindGesture( gesture, parent );
		router.BindGlobalGesture( gesture, global );
		using CursesInteractionScopeLease lease = router.ActivateScope( outer );
		Assert.True( router.Focus( region ) );

		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Enter );
		Assert.Equal( local, router.Route( input ).Command );

		Assert.True( region.UnbindGesture( gesture ) );
		Assert.Equal( nearest, router.Route( input ).Command );

		Assert.True( inner.UnbindGesture( gesture ) );
		Assert.Equal( parent, router.Route( input ).Command );

		Assert.True( outer.UnbindGesture( gesture ) );
		CursesInteractionResult globalResult = router.Route( input );
		Assert.Equal( global, globalResult.Command );
		Assert.Null( globalResult.Region );
	}

	[Fact]
	public void ActiveModalBoundaryPreventsOuterScopeBindingFromLeakingInward() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			inner
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Escape );
		CursesCommand outerCommand = new( "Outer" );
		CursesCommand global = new( "Global" );
		outer.BindGesture( gesture, outerCommand );
		router.BindGlobalGesture( gesture, global );

		using CursesInteractionScopeLease outerLease = router.ActivateScope( outer );
		using CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
		Assert.True( router.Focus( region ) );

		CursesInteractionResult result = router.Route(
			CursesInputEvent.FromKey( CursesKey.Escape )
		);
		Assert.Equal( global, result.Command );
		Assert.Null( result.Region );
	}

	[Fact]
	public void NoFocusConsultsOnlyTheActiveScopeBeforeGlobal() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Tab );
		CursesCommand outerCommand = new( "Outer" );
		CursesCommand innerCommand = new( "Inner" );
		CursesCommand global = new( "Global" );
		outer.BindGesture( gesture, outerCommand );
		inner.BindGesture( gesture, innerCommand );
		router.BindGlobalGesture( gesture, global );

		using CursesInteractionScopeLease outerLease = router.ActivateScope( outer );
		using CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
		router.ClearFocus();
		Assert.Equal(
			innerCommand,
			router.Route(
				CursesInputEvent.FromKey( CursesKey.Tab )
			).Command
		);

		Assert.True( inner.UnbindGesture( gesture ) );
		CursesInteractionResult globalResult = router.Route(
			CursesInputEvent.FromKey( CursesKey.Tab )
		);
		Assert.Equal( global, globalResult.Command );
		Assert.Null( globalResult.Region );
	}

	[Fact]
	public void RootOnlyConsumersPreservePublishedLocalThenGlobalBehavior() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable(
			router,
			scope: null
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand local = new( "Local" );
		CursesCommand global = new( "Global" );
		region.BindGesture( gesture, local );
		router.BindGlobalGesture( gesture, global );
		Assert.True( router.Focus( region ) );

		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Enter );
		CursesInteractionResult localResult = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Command, localResult.Kind );
		Assert.Same( region, localResult.Region );
		Assert.Equal( local, localResult.Command );

		Assert.True( region.UnbindGesture( gesture ) );
		CursesInteractionResult globalResult = router.Route( input );
		Assert.Equal( CursesInteractionResultKind.Command, globalResult.Kind );
		Assert.Null( globalResult.Region );
		Assert.Equal( global, globalResult.Command );
	}

	private static CursesKeyGesture CharacterGesture(
		int index
	) {
		return CursesKeyGesture.ForCharacter(
			new Rune( 0x2000 + index )
		);
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router,
		CursesInteractionScope? scope,
		int row = 0,
		int column = 0
	) {
		ArgumentNullException.ThrowIfNull( router );
		return router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle(
					row,
					column,
					1,
					1
				)
			) {
				Scope = scope,
				IsFocusable = true
			}
		);
	}
}
