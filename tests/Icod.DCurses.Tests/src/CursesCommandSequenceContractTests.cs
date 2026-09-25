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

	[Fact]
	public void BindingAndRegistrationCopyGestureLists() {
		CursesKeyGesture[] source = [ Character( 'g' ), Character( 'g' ) ];
		CursesCommand command = new( "go-top" );
		CursesCommandSequenceBinding binding = new( source, command );
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		router.BindGlobalGestureSequence( source, command );

		source[0] = Character( 'x' );

		Assert.Equal( Character( 'g' ), binding.Gestures[0] );
		Assert.Equal(
			Character( 'g' ),
			router.GetEffectiveGestureSequenceBindings()[0].Gestures[0]
		);
		Assert.Throws<NotSupportedException>(
			() => ((IList<CursesKeyGesture>)binding.Gestures).Clear()
		);
	}

	[Fact]
	public void SameOwnerSingleAndSequenceConflictInBothOrders() {
		CursesKeyGesture g = Character( 'g' );
		CursesKeyGesture[] sequence = [ g, Character( 'd' ) ];
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter first = new( screen );
		first.BindGlobalGesture( g, new CursesCommand( "single" ) );
		Assert.Throws<InvalidOperationException>(
			() => first.BindGlobalGestureSequence(
				sequence,
				new CursesCommand( "sequence" )
			)
		);
		Assert.Empty( first.GetEffectiveGestureSequenceBindings() );

		using CursesInteractionRouter second = new( screen );
		second.BindGlobalGestureSequence(
			sequence,
			new CursesCommand( "sequence" )
		);
		Assert.Throws<InvalidOperationException>(
			() => second.BindGlobalGesture(
				g,
				new CursesCommand( "single" )
			)
		);
		Assert.Single( second.GetEffectiveGestureSequenceBindings() );
	}

	[Fact]
	public void BindingRejectsNullShortLongAndDefaultGestures() {
		CursesCommand command = new( "command" );
		Assert.Throws<ArgumentNullException>(
			() => new CursesCommandSequenceBinding( null!, command )
		);
		Assert.Throws<ArgumentNullException>(
			() => new CursesCommandSequenceBinding(
				[ Character( 'g' ), Character( 'g' ) ],
				null!
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesCommandSequenceBinding( [], command )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesCommandSequenceBinding( [ Character( 'g' ) ], command )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesCommandSequenceBinding(
				Enumerable.Range( 0, 9 )
					.Select( index => IndexedCharacter( index ) )
					.ToArray(),
				command
			)
		);
		Assert.Throws<ArgumentException>(
			() => new CursesCommandSequenceBinding(
				[ Character( 'g' ), default ],
				command
			)
		);
	}

	[Fact]
	public void DuplicateAndProperPrefixConflictsDoNotMutateOwner() {
		CursesKeyGesture g = Character( 'g' );
		CursesKeyGesture d = Character( 'd' );
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter shorterFirst = new( screen );
		shorterFirst.BindGlobalGestureSequence(
			[ g, g ],
			new CursesCommand( "short" )
		);
		Assert.Throws<InvalidOperationException>(
			() => shorterFirst.BindGlobalGestureSequence(
				[ g, g ],
				new CursesCommand( "duplicate" )
			)
		);
		Assert.Throws<InvalidOperationException>(
			() => shorterFirst.BindGlobalGestureSequence(
				[ g, g, d ],
				new CursesCommand( "long" )
			)
		);
		Assert.Single( shorterFirst.GetEffectiveGestureSequenceBindings() );

		using CursesInteractionRouter longerFirst = new( screen );
		longerFirst.BindGlobalGestureSequence(
			[ g, g, d ],
			new CursesCommand( "long" )
		);
		Assert.Throws<InvalidOperationException>(
			() => longerFirst.BindGlobalGestureSequence(
				[ g, g ],
				new CursesCommand( "short" )
			)
		);
		Assert.Single( longerFirst.GetEffectiveGestureSequenceBindings() );
	}

	[Fact]
	public void SharedNonterminalPrefixIsAllowed() {
		CursesKeyGesture g = Character( 'g' );
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		router.BindGlobalGestureSequence(
			[ g, Character( 'g' ) ],
			new CursesCommand( "top" )
		);
		router.BindGlobalGestureSequence(
			[ g, Character( 'd' ) ],
			new CursesCommand( "definition" )
		);

		Assert.Equal( 2, router.GetEffectiveGestureSequenceBindings().Count );
	}

	[Fact]
	public void UnbindUsesExactSequenceAndInvalidArgumentsDoNotMutate() {
		CursesKeyGesture g = Character( 'g' );
		CursesKeyGesture[] sequence = [ g, Character( 'g' ) ];
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		router.BindGlobalGestureSequence(
			sequence,
			new CursesCommand( "top" )
		);

		Assert.False(
			router.UnbindGlobalGestureSequence( [ g, Character( 'd' ) ] )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => router.UnbindGlobalGestureSequence( [ g ] )
		);
		Assert.Single( router.GetEffectiveGestureSequenceBindings() );
		Assert.True( router.UnbindGlobalGestureSequence( sequence ) );
		Assert.False( router.UnbindGlobalGestureSequence( sequence ) );
		Assert.Empty( router.GetEffectiveGestureSequenceBindings() );
	}

	[Fact]
	public void PerOwnerSequenceCapacitiesFailBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter globalRouter = new( screen );
		for ( int index = 0;
			index < CursesInteractionRouter.MaximumGlobalCommandSequenceBindings;
			++index ) {
			globalRouter.BindGlobalGestureSequence(
				IndexedSequence( index ),
				new CursesCommand( $"global-{index}" )
			);
		}
		Assert.Throws<InvalidOperationException>(
			() => globalRouter.BindGlobalGestureSequence(
				IndexedSequence(
					CursesInteractionRouter.MaximumGlobalCommandSequenceBindings
				),
				new CursesCommand( "overflow" )
			)
		);
		Assert.Equal(
			CursesInteractionRouter.MaximumGlobalCommandSequenceBindings,
			globalRouter.GetEffectiveGestureSequenceBindings().Count
		);

		using CursesInteractionRouter regionRouter = new( screen );
		using CursesInteractionRegion region = RegisterFocusable( regionRouter );
		for ( int index = 0;
			index < CursesInteractionRouter.MaximumRegionCommandSequenceBindings;
			++index ) {
			region.BindGestureSequence(
				IndexedSequence( index ),
				new CursesCommand( $"region-{index}" )
			);
		}
		Assert.Throws<InvalidOperationException>(
			() => region.BindGestureSequence(
				IndexedSequence(
					CursesInteractionRouter.MaximumRegionCommandSequenceBindings
				),
				new CursesCommand( "overflow" )
			)
		);

		using CursesInteractionRouter scopeRouter = new( screen );
		using CursesInteractionScope scope = scopeRouter.RegisterScope();
		for ( int index = 0;
			index < CursesInteractionRouter.MaximumScopeCommandSequenceBindings;
			++index ) {
			scope.BindGestureSequence(
				IndexedSequence( index ),
				new CursesCommand( $"scope-{index}" )
			);
		}
		Assert.Throws<InvalidOperationException>(
			() => scope.BindGestureSequence(
				IndexedSequence(
					CursesInteractionRouter.MaximumScopeCommandSequenceBindings
				),
				new CursesCommand( "overflow" )
			)
		);
	}

	[Fact]
	public void RouterTotalSequenceCapacityFailsBeforeMutation() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		List<CursesInteractionRegion> regions = [];
		try {
			for ( int regionIndex = 0; regionIndex < 32; ++regionIndex ) {
				CursesInteractionRegion region = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					)
				);
				regions.Add( region );
				for ( int bindingIndex = 0;
					bindingIndex < CursesInteractionRouter.MaximumRegionCommandSequenceBindings;
					++bindingIndex ) {
					region.BindGestureSequence(
						IndexedSequence( bindingIndex ),
						new CursesCommand( $"{regionIndex}-{bindingIndex}" )
					);
				}
			}

			Assert.Throws<InvalidOperationException>(
				() => router.BindGlobalGestureSequence(
					[ Character( 'x' ), Character( 'x' ) ],
					new CursesCommand( "overflow" )
				)
			);
			Assert.Empty( router.GetEffectiveGestureSequenceBindings() );
		} finally {
			foreach ( CursesInteractionRegion region in regions ) {
				region.Dispose();
			}
		}
	}

	private static CursesKeyGesture Character(
		char value
	) {
		return CursesKeyGesture.ForCharacter( new Rune( value ) );
	}

	private static CursesKeyGesture IndexedCharacter(
		int index
	) {
		return CursesKeyGesture.ForCharacter( new Rune( 0x1000 + index ) );
	}

	private static CursesKeyGesture[] IndexedSequence(
		int index
	) {
		return [ Character( 'g' ), IndexedCharacter( index ) ];
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
