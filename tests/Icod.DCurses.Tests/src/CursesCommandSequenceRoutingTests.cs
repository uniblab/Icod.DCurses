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

public sealed class CursesCommandSequenceRoutingTests {
	[Fact]
	public void RegionSequenceWinsOverLowerGlobalSingle() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture g = Character( 'g' );
		CursesCommand command = new( "go-top" );
		region.BindGestureSequence( [ g, g ], command );
		router.BindGlobalGesture( g, new CursesCommand( "global-single" ) );
		Assert.True( router.Focus( region ) );

		CursesCommandSequenceResult pending = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'g' ) )
		);
		Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
		Assert.True( router.HasPendingCommandSequence );
		CursesCommandSequenceResult completed = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'g' ) )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( [ g, g ], completed.MatchedGestures );
		Assert.Same( command, completed.Command );
		Assert.Null( completed.Fallback );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void NearestEligibleScopeSequenceWins() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope root = router.RegisterScope();
		using CursesInteractionScope child = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = root
			}
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 4, 4 )
			) {
				IsFocusable = true,
				Scope = child
			}
		);
		CursesKeyGesture g = Character( 'g' );
		CursesCommand outer = new( "outer" );
		CursesCommand inner = new( "inner" );
		root.BindGestureSequence( [ g, g ], outer );
		child.BindGestureSequence( [ g, g ], inner );
		using CursesInteractionScopeLease lease = router.ActivateScope( root );
		Assert.True( router.Focus( region ) );

		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);
		CursesCommandSequenceResult completed = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'g' ) )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Same( inner, completed.Command );
	}

	[Fact]
	public void MismatchFallsBackOnceWithoutRestart() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture g = Character( 'g' );
		CursesKeyGesture x = Character( 'x' );
		router.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "go-top" )
		);
		router.BindGlobalGestureSequence(
			[ x, x ],
			new CursesCommand( "other-sequence" )
		);
		CursesCommand fallback = new( "ordinary-x" );
		using CursesInteractionScope scope = router.RegisterScope();
		scope.BindGesture( x, fallback );
		using CursesInteractionScopeLease lease = router.ActivateScope( scope );
		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);

		CursesCommandSequenceResult mismatch = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'x' ) )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Mismatch, mismatch.Kind );
		Assert.Equal( [ g ], mismatch.MatchedGestures );
		Assert.Same( fallback, mismatch.Fallback!.Command );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void HigherSingleBindingPreventsLowerSequenceStart() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		CursesKeyGesture g = Character( 'g' );
		CursesCommand command = new( "region-single" );
		region.BindGesture( g, command );
		router.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "global-sequence" )
		);
		Assert.True( router.Focus( region ) );

		CursesCommandSequenceResult result = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'g' ) )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Fallback, result.Kind );
		Assert.Same( command, result.Fallback!.Command );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void SharedPrefixSelectsTheCompletedCommand() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture g = Character( 'g' );
		CursesKeyGesture d = Character( 'd' );
		CursesCommand top = new( "top" );
		CursesCommand definition = new( "definition" );
		router.BindGlobalGestureSequence( [ g, g ], top );
		router.BindGlobalGestureSequence( [ g, d ], definition );

		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);
		CursesCommandSequenceResult completed = router.ProcessCommandSequence(
			CursesInputEvent.FromText( new Rune( 'd' ) )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( [ g, d ], completed.MatchedGestures );
		Assert.Same( definition, completed.Command );
		Assert.NotSame( top, completed.Command );
	}

	[Fact]
	public void NonKeyboardInputFallsBackOrMismatches() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture g = Character( 'g' );
		router.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "top" )
		);
		CursesInputEvent firstEnd = CursesInputEvent.EndOfInput();

		CursesCommandSequenceResult fallback =
			router.ProcessCommandSequence( firstEnd );
		Assert.Equal( CursesCommandSequenceResultKind.Fallback, fallback.Kind );
		Assert.Same( firstEnd, fallback.Fallback!.Input );
		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);

		CursesInputEvent secondEnd = CursesInputEvent.EndOfInput();
		CursesCommandSequenceResult mismatch =
			router.ProcessCommandSequence( secondEnd );

		Assert.Equal( CursesCommandSequenceResultKind.Mismatch, mismatch.Kind );
		Assert.Equal( [ g ], mismatch.MatchedGestures );
		Assert.Same( secondEnd, mismatch.Fallback!.Input );
		Assert.False( router.HasPendingCommandSequence );
	}

	[Fact]
	public void CancelIsExplicitAndIdempotent() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture g = Character( 'g' );
		router.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "top" )
		);

		Assert.False( router.CancelPendingCommandSequence() );
		Assert.Equal(
			CursesCommandSequenceResultKind.Pending,
			router.ProcessCommandSequence(
				CursesInputEvent.FromText( new Rune( 'g' ) )
			).Kind
		);
		Assert.True( router.HasPendingCommandSequence );
		Assert.True( router.CancelPendingCommandSequence() );
		Assert.False( router.HasPendingCommandSequence );
		Assert.False( router.CancelPendingCommandSequence() );
	}

	[Fact]
	public void SequenceMatchingUsesSemanticGestures() {
		AssertCompletes(
			CursesKeyGesture.ForCharacter( new Rune( ' ' ) ),
			CursesInputEvent.FromKey( CursesKey.Space )
		);
		AssertCompletes(
			CursesKeyGesture.ForCharacter( new Rune( '界' ) ),
			CursesInputEvent.FromText( new Rune( '界' ) )
		);
		AssertCompletes(
			CursesKeyGesture.ForCharacter(
				new Rune( 'x' ),
				CursesKeyModifiers.Control
			),
			CursesInputEvent.FromKey(
				CursesKey.Character,
				CursesKeyModifiers.Control,
				character: new Rune( 'x' )
			)
		);
		AssertCompletes(
			CursesKeyGesture.ForKey(
				CursesKey.Tab,
				phase: CursesKeyEventPhase.Release
			),
			CursesInputEvent.FromKey(
				CursesKey.Tab,
				keyPhase: CursesKeyEventPhase.Release
			)
		);
		AssertCompletes(
			CursesKeyGesture.ForFunctionKey( 7 ),
			CursesInputEvent.FromKey(
				CursesKey.Function,
				functionKeyNumber: 7
			)
		);
	}

	private static void AssertCompletes(
		CursesKeyGesture first,
		CursesInputEvent input
	) {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "semantic" );
		router.BindGlobalGestureSequence( [ first, enter ], command );

		CursesCommandSequenceResult pending = router.ProcessCommandSequence( input );
		CursesCommandSequenceResult completed = router.ProcessCommandSequence(
			CursesInputEvent.FromKey( CursesKey.Enter )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
		Assert.Equal( [ first ], pending.MatchedGestures );
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( [ first, enter ], completed.MatchedGestures );
		Assert.Same( command, completed.Command );
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
