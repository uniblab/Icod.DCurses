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

public sealed class CursesCommandSequenceDiscoveryTests {
	[Fact]
	public void DiscoveryUsesFirstOwnerAndSingleBindingPrecedence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture g = Character( 'g' );
		CursesCommand local = new( "local-sequence" );
		region.BindGestureSequence( [ g, Character( 'd' ) ], local );
		router.BindGlobalGesture( g, new CursesCommand( "global-single" ) );
		router.BindGlobalGestureSequence(
			[ Character( 'x' ), Character( 'x' ) ],
			new CursesCommand( "global-sequence" )
		);
		Assert.True( router.Focus( region ) );

		IReadOnlyList<CursesCommandSequenceBinding> bindings =
			router.GetEffectiveGestureSequenceBindings();

		Assert.Equal(
			new[] { "local-sequence", "global-sequence" },
			bindings.Select( binding => binding.Command.Name )
		);
	}

	[Fact]
	public void HigherSingleBindingHidesLowerSequence() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture g = Character( 'g' );
		region.BindGesture( g, new CursesCommand( "local-single" ) );
		router.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "global-sequence" )
		);
		Assert.True( router.Focus( region ) );

		Assert.Empty( router.GetEffectiveGestureSequenceBindings() );
	}

	[Fact]
	public void ModalScopeHidesAncestorSequences() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope root = router.RegisterScope();
		using CursesInteractionScope modal = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = root
			}
		);
		root.BindGestureSequence(
			[ Character( 'g' ), Character( 'g' ) ],
			new CursesCommand( "ancestor" )
		);
		modal.BindGestureSequence(
			[ Character( 'x' ), Character( 'x' ) ],
			new CursesCommand( "modal" )
		);
		using CursesInteractionScopeLease rootLease = router.ActivateScope( root );
		using CursesInteractionScopeLease modalLease = router.ActivateScope( modal );

		IReadOnlyList<CursesCommandSequenceBinding> bindings =
			router.GetEffectiveGestureSequenceBindings();

		Assert.Single( bindings );
		Assert.Equal( "modal", bindings[0].Command.Name );
	}

	[Fact]
	public void DiscoveryOrderIsOwnerThenLexicographicAndIndependentOfRegistrationOrder() {
		string[] expected = [
			"region-aa",
			"region-ab",
			"region-control-a",
			"region-enter",
			"scope-x",
			"global-function"
		];

		Assert.Equal( expected, DiscoverNames( reverse: false ) );
		Assert.Equal( expected, DiscoverNames( reverse: true ) );
	}

	[Fact]
	public void SnapshotAndNestedGestureListsAreReadOnlyAndDetached() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture[] source = [ Character( 'g' ), Character( 'g' ) ];
		CursesCommand command = new( "top" );
		router.BindGlobalGestureSequence( source, command );

		IReadOnlyList<CursesCommandSequenceBinding> snapshot =
			router.GetEffectiveGestureSequenceBindings();
		source[0] = Character( 'x' );
		Assert.True(
			router.UnbindGlobalGestureSequence(
				[ Character( 'g' ), Character( 'g' ) ]
			)
		);

		Assert.Empty( router.GetEffectiveGestureSequenceBindings() );
		Assert.Single( snapshot );
		Assert.Equal( Character( 'g' ), snapshot[0].Gestures[0] );
		Assert.Same( command, snapshot[0].Command );
		Assert.Throws<NotSupportedException>(
			() => ((IList<CursesCommandSequenceBinding>)snapshot).Clear()
		);
		Assert.Throws<NotSupportedException>(
			() => ((IList<CursesKeyGesture>)snapshot[0].Gestures).Clear()
		);
	}

	[Fact]
	public void DiscoveryIsEmptyWithoutSequencesAndThrowsAfterDispose() {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		router.BindGlobalGesture(
			Character( 'g' ),
			new CursesCommand( "single" )
		);

		Assert.Empty( router.GetEffectiveGestureSequenceBindings() );

		router.Dispose();
		Assert.Throws<ObjectDisposedException>(
			() => router.GetEffectiveGestureSequenceBindings()
		);
	}

	private static string[] DiscoverNames(
		bool reverse
	) {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope scope = router.RegisterScope();
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 4, 4 )
			) {
				IsFocusable = true,
				Scope = scope
			}
		);
		CursesKeyGesture a = Character( 'a' );
		CursesKeyGesture controlA = CursesKeyGesture.ForCharacter(
			new Rune( 'a' ),
			CursesKeyModifiers.Control
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		(CursesKeyGesture[] Gestures, string Name)[] regionEntries = [
			( [ a, Character( 'a' ) ], "region-aa" ),
			( [ a, Character( 'b' ) ], "region-ab" ),
			( [ controlA, Character( 'a' ) ], "region-control-a" ),
			( [ enter, Character( 'a' ) ], "region-enter" )
		];
		IEnumerable<(CursesKeyGesture[] Gestures, string Name)> ordered = reverse
			? regionEntries.Reverse()
			: regionEntries;
		foreach ( (CursesKeyGesture[] gestures, string name) in ordered ) {
			region.BindGestureSequence( gestures, new CursesCommand( name ) );
		}
		scope.BindGestureSequence(
			[ Character( 'x' ), Character( 'x' ) ],
			new CursesCommand( "scope-x" )
		);
		router.BindGlobalGestureSequence(
			[
				CursesKeyGesture.ForFunctionKey( 1 ),
				CursesKeyGesture.ForKey( CursesKey.Enter )
			],
			new CursesCommand( "global-function" )
		);
		using CursesInteractionScopeLease lease = router.ActivateScope( scope );
		Assert.True( router.Focus( region ) );

		return router.GetEffectiveGestureSequenceBindings()
			.Select( binding => binding.Command.Name )
			.ToArray();
	}

	private static CursesKeyGesture Character(
		char value
	) {
		return CursesKeyGesture.ForCharacter( new Rune( value ) );
	}

	private static CursesInteractionRegion RegisterFocusable(
		CursesInteractionRouter router
	) {
		return router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 4, 4 )
			) {
				IsFocusable = true
			}
		);
	}
}
