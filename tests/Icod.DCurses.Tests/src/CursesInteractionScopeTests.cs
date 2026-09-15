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

/// <summary>Freezes bounded 1.5 interaction-scope ownership and active-scope routing.</summary>
public sealed class CursesInteractionScopeTests {
	[Fact]
	public void RootCompatibilityLeavesUnscopedRegionsRoutable() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			) {
				IsFocusable = true
			}
		);

		Assert.Null( region.Scope );
		Assert.True( router.Focus( region ) );
		Assert.Same( region, router.FocusedRegion );
		Assert.Same( region, router.HitTest( 1, 2 )?.Region );
	}

	[Fact]
	public void RegisterScopeFreezesParentAndRegionAssociation() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope parent = router.RegisterScope();
		using CursesInteractionScope child = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = parent
			}
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			) {
				Scope = child,
				IsFocusable = true
			}
		);

		Assert.Null( parent.Parent );
		Assert.Same( parent, child.Parent );
		Assert.Same( child, region.Scope );
	}

	[Fact]
	public void ForeignParentAndForeignRegionScopeAreRejectedAtomically() {
		CursesScreen firstScreen = new( 20, 8 );
		CursesScreen secondScreen = new( 20, 8 );
		using CursesInteractionRouter first = new( firstScreen );
		using CursesInteractionRouter second = new( secondScreen );
		using CursesInteractionScope foreign = first.RegisterScope();

		Assert.Throws<ArgumentException>(
			() => second.RegisterScope(
				new CursesInteractionScopeOptions {
					Parent = foreign
				}
			)
		);
		Assert.Throws<ArgumentException>(
			() => second.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle( 0, 0, 1, 1 )
				) {
					Scope = foreign
				}
			)
		);
	}

	[Fact]
	public void ScopeDepthIsBounded() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		List<CursesInteractionScope> scopes = [];
		CursesInteractionScope? parent = null;
		for ( int depth = 0; depth < CursesInteractionRouter.MaximumScopeDepth; depth++ ) {
			CursesInteractionScope current = router.RegisterScope(
				new CursesInteractionScopeOptions {
					Parent = parent
				}
			);
			scopes.Add( current );
			parent = current;
		}

		Assert.Throws<InvalidOperationException>(
			() => router.RegisterScope(
				new CursesInteractionScopeOptions {
					Parent = parent
				}
			)
		);

		for ( int index = scopes.Count - 1; 0 <= index; index-- ) {
			scopes[index].Dispose();
		}
	}

	[Fact]
	public void ScopeCountIsBounded() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		List<CursesInteractionScope> scopes = [];
		for ( int index = 0; index < CursesInteractionRouter.MaximumScopes; index++ ) {
			scopes.Add( router.RegisterScope() );
		}

		Assert.Throws<InvalidOperationException>( () => router.RegisterScope() );

		foreach ( CursesInteractionScope scope in scopes ) {
			scope.Dispose();
		}
	}

	[Fact]
	public void ActivationRestrictsHitTestingAndFocusToActiveSubtree() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion rootRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				IsFocusable = true
			}
		);
		using CursesInteractionScope modal = router.RegisterScope();
		using CursesInteractionRegion modalRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 4, 2, 2 )
			) {
				Scope = modal,
				IsFocusable = true
			}
		);

		Assert.True( router.Focus( rootRegion ) );
		using CursesInteractionScopeLease lease = router.ActivateScope( modal );

		Assert.Null( router.HitTest( 0, 0 ) );
		Assert.Same( modalRegion, router.HitTest( 4, 4 )?.Region );
		Assert.False( router.Focus( rootRegion ) );
		Assert.Same( modalRegion, router.FocusedRegion );
	}

	[Fact]
	public void NestedActivationRequiresDescendantAndLeaseDisposalIsLifo() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionScope sibling = router.RegisterScope();
		using CursesInteractionScopeLease outerLease = router.ActivateScope( outer );

		Assert.Throws<ArgumentException>( () => router.ActivateScope( sibling ) );
		using CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
		Assert.Throws<InvalidOperationException>( () => outerLease.Dispose() );

		innerLease.Dispose();
		outerLease.Dispose();
	}

	[Fact]
	public void DeactivationRestoresSavedFocusWhenItBecomesEligibleAgain() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion rootRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				IsFocusable = true
			}
		);
		using CursesInteractionScope modal = router.RegisterScope();
		using CursesInteractionRegion modalRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 4, 2, 2 )
			) {
				Scope = modal,
				IsFocusable = true
			}
		);

		Assert.True( router.Focus( rootRegion ) );
		CursesInteractionScopeLease lease = router.ActivateScope( modal );
		Assert.Same( modalRegion, router.FocusedRegion );
		lease.Dispose();

		Assert.Same( rootRegion, router.FocusedRegion );
	}

	[Fact]
	public void ScopeDisposalRejectsLiveOwnershipAndActiveLifetime() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionScope parent = router.RegisterScope();
		CursesInteractionScope child = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = parent
			}
		);
		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				Scope = child
			}
		);

		Assert.Throws<InvalidOperationException>( () => parent.Dispose() );
		Assert.Throws<InvalidOperationException>( () => child.Dispose() );
		region.Dispose();
		child.Dispose();

		using CursesInteractionScopeLease lease = router.ActivateScope( parent );
		Assert.Throws<InvalidOperationException>( () => parent.Dispose() );
		lease.Dispose();
		parent.Dispose();
		parent.Dispose();
	}
}
