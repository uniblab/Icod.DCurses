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

public sealed class CursesInteractionRegionRegistryTests {
	[Fact]
	public void RouterRejectsNullScreenAndNullRegistrationOptions() {
		Assert.Throws<ArgumentNullException>(
			() => {
				_ = new CursesInteractionRouter( null! );
			}
		);

		CursesScreen screen = new( 80, 24 );
		using CursesInteractionRouter router = new( screen );
		Assert.Throws<ArgumentNullException>(
			() => {
				_ = router.RegisterRegion( null! );
			}
		);
	}

	[Fact]
	public void RouterRegistersOrdinaryRegionAndSnapshotsInitialState() {
		CursesScreen screen = new( 80, 24 );
		using CursesInteractionRouter router = new( screen );
		CursesRectangle bounds = new( 2, 3, 4, 5 );
		CursesInteractionRegionOptions options = new( bounds ) {
			IsEnabled = false,
			IsFocusable = true,
			TraversalOrder = -4,
			HitTestPriority = 7
		};

		using CursesInteractionRegion region = router.RegisterRegion( options );

		Assert.Same( screen, router.Screen );
		Assert.Equal( bounds, region.Bounds );
		Assert.Null( region.Panel );
		Assert.False( region.IsEnabled );
		Assert.True( region.IsFocusable );
		Assert.Equal( -4, region.TraversalOrder );
		Assert.Equal( 7, region.HitTestPriority );
	}

	[Fact]
	public void RegionAllowsEmptyAndOutOfScreenDeclaredGeometry() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion empty = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 50, 70, 0, 0 )
			)
		);

		Assert.Equal(
			new CursesRectangle( 50, 70, 0, 0 ),
			empty.Bounds
		);
	}

	[Fact]
	public void RouterRegistersPanelRelativeRegionOnOwningScreen() {
		CursesScreen screen = new( 80, 24 );
		using CursesPanel panel = screen.CreatePanel( 3, 4, 8, 12 );
		using CursesInteractionRouter router = new( screen );
		CursesRectangle relativeBounds = new( 1, 2, 3, 4 );

		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions( relativeBounds ) {
				Panel = panel,
				IsFocusable = true
			}
		);

		Assert.Same( panel, region.Panel );
		Assert.Equal( relativeBounds, region.Bounds );
		Assert.True( region.IsEnabled );
		Assert.True( region.IsFocusable );
	}

	[Fact]
	public void RouterRejectsForeignAndDisposedPanelAssociation() {
		CursesScreen first = new( 80, 24 );
		CursesScreen second = new( 80, 24 );
		using CursesInteractionRouter router = new( first );
		using CursesPanel foreign = second.CreatePanel( 0, 0, 2, 2 );

		Assert.Throws<ArgumentException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					) {
						Panel = foreign
					}
				);
			}
		);

		CursesPanel disposed = first.CreatePanel( 0, 0, 2, 2 );
		disposed.Dispose();
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					) {
						Panel = disposed
					}
				);
			}
		);
	}

	[Fact]
	public void RegionMutationsUpdateStateAndBounds() {
		CursesScreen screen = new( 80, 24 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			)
		);

		region.IsEnabled = false;
		region.IsFocusable = true;
		region.TraversalOrder = 11;
		region.HitTestPriority = -9;
		region.SetBounds( new CursesRectangle( 30, 40, 0, 5 ) );

		Assert.False( region.IsEnabled );
		Assert.True( region.IsFocusable );
		Assert.Equal( 11, region.TraversalOrder );
		Assert.Equal( -9, region.HitTestPriority );
		Assert.Equal(
			new CursesRectangle( 30, 40, 0, 5 ),
			region.Bounds
		);
	}

	[Fact]
	public void RouterEnforcesMaximumRegionCountAndDisposalReleasesSlot() {
		CursesScreen screen = new( 1, 1 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion[] regions =
			new CursesInteractionRegion[ CursesInteractionRouter.MaximumRegions ];

		for ( int index = 0; index < regions.Length; index++ ) {
			regions[ index ] = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle( 0, 0, 0, 0 )
				)
			);
		}

		Assert.Throws<InvalidOperationException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 0, 0 )
					)
				);
			}
		);

		regions[ 0 ].Dispose();
		using CursesInteractionRegion replacement = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 0, 0 )
			)
		);

		for ( int index = 1; index < regions.Length; index++ ) {
			regions[ index ].Dispose();
		}
	}

	[Fact]
	public void RegionDisposalIsIdempotentAndObservationsRemainReadable() {
		CursesScreen screen = new( 80, 24 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			) {
				IsFocusable = true,
				TraversalOrder = 5,
				HitTestPriority = 6
			}
		);

		region.Dispose();
		region.Dispose();

		Assert.Equal( new CursesRectangle( 1, 2, 3, 4 ), region.Bounds );
		Assert.Null( region.Panel );
		Assert.True( region.IsEnabled );
		Assert.True( region.IsFocusable );
		Assert.Equal( 5, region.TraversalOrder );
		Assert.Equal( 6, region.HitTestPriority );
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.IsEnabled = false;
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.IsFocusable = false;
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.TraversalOrder = 0;
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.HitTestPriority = 0;
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.SetBounds( new CursesRectangle( 0, 0, 1, 1 ) );
			}
		);
	}

	[Fact]
	public void RouterDisposalIsIdempotentAndInvalidatesRegionMutation() {
		CursesScreen screen = new( 80, 24 );
		CursesInteractionRouter router = new( screen );
		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 2, 3, 4 )
			)
		);

		router.Dispose();
		router.Dispose();

		Assert.Same( screen, router.Screen );
		Assert.Throws<ObjectDisposedException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					)
				);
			}
		);
		Assert.Throws<ObjectDisposedException>(
			() => {
				region.IsEnabled = false;
			}
		);
	}
}
