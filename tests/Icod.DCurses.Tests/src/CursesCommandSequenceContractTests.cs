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

public sealed class CursesCommandSequenceContractTests {
	[Fact]
	public void FrozenSequenceSurfaceIsAvailable() {
		CursesKeyGesture[] gestures = [ Character( 'g' ), Character( 'g' ) ];
		CursesCommand command = new( "go-top" );
		CursesCommandSequenceBinding binding = new( gestures, command );
		Assert.Equal( 8, CursesInteractionRouter.MaximumCommandSequenceLength );
		Assert.Equal( 128, CursesInteractionRouter.MaximumRegionCommandSequenceBindings );
		Assert.Equal( 128, CursesInteractionRouter.MaximumScopeCommandSequenceBindings );
		Assert.Equal( 512, CursesInteractionRouter.MaximumGlobalCommandSequenceBindings );
		Assert.Equal( 4096, CursesInteractionRouter.MaximumCommandSequenceBindings );
		Assert.Equal( gestures, binding.Gestures.ToArray() );
		Assert.Same( command, binding.Command );
		Assert.Equal( 0, (int)CursesCommandSequenceResultKind.Fallback );
		Assert.Equal( 1, (int)CursesCommandSequenceResultKind.Pending );
		Assert.Equal( 2, (int)CursesCommandSequenceResultKind.Completed );
		Assert.Equal( 3, (int)CursesCommandSequenceResultKind.Mismatch );

		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( router );
		using CursesInteractionScope scope = router.RegisterScope();
		region.BindGestureSequence( gestures, command );
		Assert.True( region.UnbindGestureSequence( gestures ) );
		scope.BindGestureSequence( gestures, command );
		Assert.True( scope.UnbindGestureSequence( gestures ) );
		router.BindGlobalGestureSequence( gestures, command );
		Assert.False( router.HasPendingCommandSequence );
		Assert.Single( router.GetEffectiveGestureSequenceBindings() );
		CursesInputEvent input = CursesInputEvent.FromText( new Rune( 'g' ) );
		CursesCommandSequenceResult result = router.ProcessCommandSequence( input );
		Assert.Equal( CursesCommandSequenceResultKind.Pending, result.Kind );
		Assert.Same( input, result.Input );
		Assert.Single( result.MatchedGestures );
		Assert.Null( result.Command );
		Assert.Null( result.Fallback );
		Assert.True( router.CancelPendingCommandSequence() );
		Assert.True( router.UnbindGlobalGestureSequence( gestures ) );
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
