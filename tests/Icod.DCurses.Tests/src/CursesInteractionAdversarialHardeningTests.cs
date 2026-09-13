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

/// <summary>Stresses bounded 1.4 interaction state under deterministic adversarial churn.</summary>
public sealed class CursesInteractionAdversarialHardeningTests {
	[Fact]
	public void RegionCapacityChurnRecoversSlotsWithoutChangingAtomicOverflowBehavior() {
		CursesScreen screen = new( 40, 20 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion[] regions = new CursesInteractionRegion[ CursesInteractionRouter.MaximumRegions ];
		for ( int index = 0; index < regions.Length; index++ ) {
			regions[ index ] = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle( 0, 0, 1, 1 )
				)
			);
		}

		CursesInteractionHit initialHit = router.HitTest(
			0,
			0
		) ?? throw new InvalidOperationException( "Expected a hit at maximum region capacity." );
		Assert.Same( regions[ ^1 ], initialHit.Region );

		Assert.Throws<InvalidOperationException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					)
				);
			}
		);
		Assert.Same(
			regions[ ^1 ],
			router.HitTest(
				0,
				0
			)?.Region
		);

		for ( int index = 0; index < regions.Length; index += 2 ) {
			regions[ index ].Dispose();
		}

		CursesInteractionRegion? lastReplacement = null;
		for ( int index = 0; index < regions.Length / 2; index++ ) {
			lastReplacement = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle( 0, 0, 1, 1 )
				)
			);
		}
		Assert.NotNull( lastReplacement );
		Assert.Same(
			lastReplacement,
			router.HitTest(
				0,
				0
			)?.Region
		);

		Assert.Throws<InvalidOperationException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					)
				);
			}
		);
		Assert.Same(
			lastReplacement,
			router.HitTest(
				0,
				0
			)?.Region
		);

		for ( int index = 1; index < regions.Length; index += 2 ) {
			Assert.False( regions[ index ].IsDisposed );
		}
	}

	[Fact]
	public void TotalBindingCapacityChurnRecoversDisposedRegionCapacityExactly() {
		CursesScreen screen = new( 80, 24 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion[] regions = new CursesInteractionRegion[ 64 ];
		for ( int regionIndex = 0; regionIndex < regions.Length; regionIndex++ ) {
			CursesInteractionRegion region = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle( 0, 0, 1, 1 )
				)
			);
			regions[ regionIndex ] = region;
			for ( int bindingIndex = 0; bindingIndex < CursesInteractionRouter.MaximumRegionGestureBindings; bindingIndex++ ) {
				region.BindGesture(
					CursesKeyGesture.ForCharacter( new Rune( 0x1000 + bindingIndex ) ),
					new CursesCommand( $"region.{regionIndex:D2}.{bindingIndex:D3}" )
				);
			}
		}

		CursesKeyGesture firstGlobalGesture = CursesKeyGesture.ForCharacter( new Rune( 0x2000 ) );
		Assert.Throws<InvalidOperationException>(
			() => router.BindGlobalGesture(
				firstGlobalGesture,
				new CursesCommand( "global.blocked" )
			)
		);

		regions[ 0 ].Dispose();
		CursesCommand finalGlobalCommand = new( "global.255" );
		for ( int index = 0; index < CursesInteractionRouter.MaximumRegionGestureBindings; index++ ) {
			CursesCommand command = index == CursesInteractionRouter.MaximumRegionGestureBindings - 1
				? finalGlobalCommand
				: new CursesCommand( $"global.{index:D3}" )
			;
			router.BindGlobalGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x2000 + index ) ),
				command
			);
		}

		CursesInputEvent finalInput = CursesInputEvent.FromText(
			new Rune( 0x2000 + CursesInteractionRouter.MaximumRegionGestureBindings - 1 )
		);
		Assert.Equal(
			finalGlobalCommand,
			router.Route( finalInput ).Command
		);

		Assert.Throws<InvalidOperationException>(
			() => router.BindGlobalGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x3000 ) ),
				new CursesCommand( "global.overflow" )
			)
		);
		Assert.Equal(
			finalGlobalCommand,
			router.Route( finalInput ).Command
		);
	}

	[Fact]
	public void OverlappingPanelTopologyReplayIsDeterministicAcrossZOrderGeometryAndVisibilityChurn() {
		int[] first = RunPanelTopologyReplay();
		int[] second = RunPanelTopologyReplay();

		Assert.Equal( first, second );
		Assert.Equal( 320, first.Length );
	}

	[Fact]
	public void FocusEligibilityReplayRemainsDeterministicAcrossRepeatedChurn() {
		int[] first = RunFocusEligibilityReplay();
		int[] second = RunFocusEligibilityReplay();

		Assert.Equal( first, second );
		Assert.Equal( 1024, first.Length );
	}

	[Fact]
	public void InvalidBindingsAndFocusAttemptsFailBeforeObservableStateMutation() {
		CursesScreen screen = new( 20, 10 );
		CursesScreen foreignScreen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRouter foreignRouter = new( foreignScreen );
		using CursesInteractionRegion first = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);
		using CursesInteractionRegion second = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 1, 1, 1 )
			) {
				IsFocusable = true,
				TraversalOrder = 1
			}
		);
		using CursesInteractionRegion foreign = foreignRouter.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);

		CursesKeyGesture globalGesture = CursesKeyGesture.ForCharacter( new Rune( 'g' ) );
		CursesCommand originalGlobal = new( "global.original" );
		router.BindGlobalGesture(
			globalGesture,
			originalGlobal
		);
		Assert.Throws<InvalidOperationException>(
			() => router.BindGlobalGesture(
				globalGesture,
				new CursesCommand( "global.replacement" )
			)
		);
		Assert.Equal(
			originalGlobal,
			router.Route( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Command
		);

		CursesKeyGesture localGesture = CursesKeyGesture.ForCharacter( new Rune( 'l' ) );
		CursesCommand originalLocal = new( "local.original" );
		first.BindGesture(
			localGesture,
			originalLocal
		);
		Assert.Throws<InvalidOperationException>(
			() => first.BindGesture(
				localGesture,
				new CursesCommand( "local.replacement" )
			)
		);
		Assert.True( router.Focus( first ) );
		Assert.Equal(
			originalLocal,
			router.Route( CursesInputEvent.FromText( new Rune( 'l' ) ) ).Command
		);

		Assert.Throws<ArgumentException>(
			() => {
				_ = router.Focus( foreign );
			}
		);
		Assert.Same( first, router.FocusedRegion );

		second.IsEnabled = false;
		Assert.False( router.Focus( second ) );
		Assert.Same( first, router.FocusedRegion );

		CursesInteractionRegion disposed = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 0, 1, 1 )
			)
		);
		disposed.Dispose();
		Assert.Throws<ObjectDisposedException>(
			() => disposed.BindGesture(
				CursesKeyGesture.ForCharacter( new Rune( 'z' ) ),
				new CursesCommand( "disposed" )
			)
		);
		Assert.Same( first, router.FocusedRegion );
	}

	private static int[] RunFocusEligibilityReplay() {
		const int regionCount = 128;
		CursesScreen screen = new( 256, 2 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion[] regions = new CursesInteractionRegion[ regionCount ];
		Dictionary<CursesInteractionRegion, int> indices = [];
		for ( int index = 0; index < regionCount; index++ ) {
			CursesInteractionRegion region = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle(
						0,
						index,
						1,
						1
					)
				) {
					IsFocusable = true,
					TraversalOrder = index
				}
			);
			regions[ index ] = region;
			indices.Add(
				region,
				index
			);
		}
		Assert.True( router.Focus( regions[ 0 ] ) );

		int[] observed = new int[ 1024 ];
		for ( int iteration = 0; iteration < observed.Length; iteration++ ) {
			CursesInteractionRegion current = router.FocusedRegion
				?? throw new InvalidOperationException( "Focus unexpectedly cleared during churn." );
			int currentIndex = indices[ current ];
			switch ( iteration % 4 ) {
				case 0:
					current.IsEnabled = false;
					Assert.Equal(
						( currentIndex + 1 ) % regionCount,
						indices[ router.FocusedRegion! ]
					);
					current.IsEnabled = true;
					break;
				case 1:
					current.IsFocusable = false;
					Assert.Equal(
						( currentIndex + 1 ) % regionCount,
						indices[ router.FocusedRegion! ]
					);
					current.IsFocusable = true;
					break;
				case 2:
					current.SetBounds( new CursesRectangle( 10, 10, 1, 1 ) );
					Assert.Equal(
						( currentIndex + 1 ) % regionCount,
						indices[ router.FocusedRegion! ]
					);
					current.SetBounds(
						new CursesRectangle(
							0,
							currentIndex,
							1,
							1
						)
					);
					break;
				case 3:
					router.ClearFocus();
					CursesFocusDirection direction = 0 == ( iteration / 4 ) % 2
						? CursesFocusDirection.Forward
						: CursesFocusDirection.Backward
					;
					CursesInteractionRegion traversed = router.MoveFocus( direction )
						?? throw new InvalidOperationException( "Traversal unexpectedly found no region." );
					Assert.Equal(
						CursesFocusDirection.Forward == direction
							? 0
							: regionCount - 1,
						indices[ traversed ]
					);
					break;
			}

			CursesInteractionRegion focused = router.FocusedRegion
				?? throw new InvalidOperationException( "Focus unexpectedly cleared after churn operation." );
			observed[ iteration ] = indices[ focused ];
		}
		return observed;
	}

	private static int[] RunPanelTopologyReplay() {
		const int panelCount = 16;
		const int regionsPerPanel = 8;
		CursesScreen screen = new( 80, 30 );
		using CursesInteractionRouter router = new( screen );
		CursesPanel[] panels = new CursesPanel[ panelCount ];
		CursesInteractionRegion[][] regions = new CursesInteractionRegion[ panelCount ][];
		Dictionary<CursesInteractionRegion, int> identities = [];
		try {
			for ( int panelIndex = 0; panelIndex < panelCount; panelIndex++ ) {
				CursesPanel panel = screen.CreatePanel( 10, 10, 4, 4 );
				panels[ panelIndex ] = panel;
				regions[ panelIndex ] = new CursesInteractionRegion[ regionsPerPanel ];
				for ( int regionIndex = 0; regionIndex < regionsPerPanel; regionIndex++ ) {
					CursesInteractionRegion region = router.RegisterRegion(
						new CursesInteractionRegionOptions(
							new CursesRectangle( 0, 0, 4, 4 )
						) {
							Panel = panel,
							HitTestPriority = regionIndex
						}
					);
					regions[ panelIndex ][ regionIndex ] = region;
					identities.Add(
						region,
						panelIndex * regionsPerPanel + regionIndex
					);
				}
			}

			List<int> observed = [];
			for ( int iteration = 0; iteration < 256; iteration++ ) {
				int panelIndex = iteration % panelCount;
				CursesPanel panel = panels[ panelIndex ];
				panel.MoveToTop();
				regions[ panelIndex ][ 0 ].HitTestPriority = 0 == iteration % 2
					? 100
					: 0
				;

				if ( 0 == iteration % 4 ) {
					panel.Hide();
					CursesInteractionHit hiddenHit = router.HitTest(
						10,
						10
					) ?? throw new InvalidOperationException( "Expected an underlying panel hit while top panel was hidden." );
					observed.Add( identities[ hiddenHit.Region ] );
					panel.Show();
				}

				if ( 0 == iteration % 3 ) {
					panel.SetBounds( new CursesRectangle( 9, 9, 5, 5 ) );
				} else {
					panel.SetBounds( new CursesRectangle( 10, 10, 4, 4 ) );
				}

				CursesInteractionHit hit = router.HitTest(
					10,
					10
				) ?? throw new InvalidOperationException( "Expected a panel hit after topology mutation." );
				int expectedRegionIndex = 0 == iteration % 2
					? 0
					: regionsPerPanel - 1
				;
				Assert.Same(
					regions[ panelIndex ][ expectedRegionIndex ],
					hit.Region
				);
				observed.Add( identities[ hit.Region ] );
			}
			return observed.ToArray();
		} finally {
			foreach ( CursesPanel? panel in panels ) {
				panel?.Dispose();
			}
		}
	}
}
