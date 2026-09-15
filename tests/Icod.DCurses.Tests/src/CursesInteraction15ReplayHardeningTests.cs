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

/// <summary>Stresses repeated 1.5 ownership churn and deterministic replay.</summary>
public sealed class CursesInteraction15ReplayHardeningTests {
	[Fact]
	public void CaptureChurnRecoversSingularOwnershipAfterExplicitAndAutomaticRelease() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 2, 2 )
			)
		);

		for ( int iteration = 0; iteration < 1024; iteration++ ) {
			CursesMouseButton button = 0 == iteration % 2
				? CursesMouseButton.Primary
				: CursesMouseButton.Secondary
			;
			CursesPointerCaptureLease lease = router.CapturePointer(
				region,
				button
			);

			if ( 0 == iteration % 2 ) {
				lease.Dispose();
			} else {
				CursesInteractionResult release = router.Route(
					CursesInputEvent.FromMouse(
						new CursesMouseEvent(
							CursesMouseAction.Release,
							button,
							column: 10,
							row: 6
						)
					)
				);
				Assert.Same( region, release.Region );
				Assert.NotNull( release.PointerTarget );
				lease.Dispose();
			}

			lease.Dispose();
		}

		using CursesPointerCaptureLease finalLease = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
	}

	[Fact]
	public void CombinedInteractionLifecycleReplayIsDeterministic() {
		int[] first = RunCombinedReplay();
		int[] second = RunCombinedReplay();

		Assert.Equal( first, second );
		Assert.Equal( 256, first.Length );
	}

	private static int[] RunCombinedReplay() {
		CursesScreen screen = new( 20, 8 );
		using CursesPanel panel = screen.CreatePanel( 1, 1, 3, 3 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionScope outer = router.RegisterScope();
		using CursesInteractionScope inner = router.RegisterScope(
			new CursesInteractionScopeOptions {
				Parent = outer
			}
		);
		using CursesInteractionRegion rootRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);
		using CursesInteractionRegion panelRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				Panel = panel
			}
		);
		using CursesInteractionRegion outerRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 6, 1, 1 )
			) {
				Scope = outer,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion innerRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 10, 1, 1 )
			) {
				Scope = inner,
				IsFocusable = true
			}
		);
		using CursesInteractionRegion edgeRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 6, 15, 1, 1 )
			)
		);

		CursesCommand outerCommand = new( "outer.command" );
		CursesCommand innerCommand = new( "inner.command" );
		CursesCommand globalCommand = new( "global.command" );
		outer.BindGesture(
			CursesKeyGesture.ForCharacter( new Rune( 'o' ) ),
			outerCommand
		);
		inner.BindGesture(
			CursesKeyGesture.ForCharacter( new Rune( 'i' ) ),
			innerCommand
		);
		router.BindGlobalGesture(
			CursesKeyGesture.ForCharacter( new Rune( 'g' ) ),
			globalCommand
		);

		int[] observed = new int[ 256 ];
		int observedIndex = 0;
		for ( int iteration = 0; iteration < 32; iteration++ ) {
			CursesPointerCaptureLease rootCapture = router.CapturePointer(
				rootRegion,
				CursesMouseButton.Primary
			);
			CursesInteractionScopeLease outerLease = router.ActivateScope( outer );
			CursesInteractionResult excludedCapture = router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Move,
						CursesMouseButton.Primary,
						column: 19,
						row: 7
					)
				)
			);
			observed[ observedIndex++ ] = (int)excludedCapture.Kind;
			rootCapture.Dispose();

			CursesInteractionScopeLease innerLease = router.ActivateScope( inner );
			observed[ observedIndex++ ] = ReferenceEquals(
				router.Route( CursesInputEvent.FromText( new Rune( 'i' ) ) ).Command,
				innerCommand
			)
				? 1
				: 0
			;
			observed[ observedIndex++ ] = (int)router.Route(
				CursesInputEvent.FromText( new Rune( 'o' ) )
			).Kind;
			innerLease.Dispose();
			observed[ observedIndex++ ] = ReferenceEquals(
				router.Route( CursesInputEvent.FromText( new Rune( 'o' ) ) ).Command,
				outerCommand
			)
				? 1
				: 0
			;
			outerLease.Dispose();

			_ = router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Press,
						CursesMouseButton.Secondary,
						column: 1,
						row: 1
					)
				)
			);
			panel.Hide();
			panel.Show();
			CursesInteractionResult panelRelease = router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Release,
						CursesMouseButton.Secondary,
						column: 1,
						row: 1
					)
				)
			);
			observed[ observedIndex++ ] = (int)( panelRelease.PointerGesture?.Kind
				?? CursesPointerGestureKind.Move );

			CursesPointerCaptureLease edgeCapture = router.CapturePointer(
				edgeRegion,
				CursesMouseButton.Middle
			);
			screen.Resize( 10, 4 );
			screen.Resize( 20, 8 );
			CursesInteractionResult resizedCapture = router.Route(
				CursesInputEvent.FromMouse(
					new CursesMouseEvent(
						CursesMouseAction.Move,
						CursesMouseButton.Middle,
						column: 19,
						row: 7
					)
				)
			);
			observed[ observedIndex++ ] = (int)resizedCapture.Kind;
			edgeCapture.Dispose();

			Assert.True( router.Focus( rootRegion ) );
			observed[ observedIndex++ ] = ReferenceEquals(
				router.MoveFocus( CursesFocusDirection.Right ),
				outerRegion
			)
				? 1
				: 0
			;
			observed[ observedIndex++ ] = ReferenceEquals(
				router.Route( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Command,
				globalCommand
			)
				? 1
				: 0
			;
			router.ClearFocus();
		}

		Assert.Equal( observed.Length, observedIndex );
		return observed;
	}
}
