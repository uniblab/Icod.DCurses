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

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes representative 1.5 interaction-control allocation and throughput ceilings.</summary>
[Collection( AllocationMeasurementCollection.Name )]
public sealed class CursesInteraction15PerformanceTests {
	private const int AllocationIterations = 10000;
	private const long AllocationMeasurementNoiseAllowance = 1024;
	private const int AllocationSamples = 8;
	private const int SpatialAllocationIterations = 1000;
	private const int SpatialRegionCountPerAxis = 16;
	private const int WarmupIterations = 4096;

	[Fact]
	public void RootScopeCommandRoutingRetainsPublished14AllocationCeiling() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "root.command" );
		region.BindGesture( gesture, command );
		Assert.True( router.Focus( region ) );
		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Enter );

		Action operation = () => {
			CursesInteractionResult result = router.Route( input );
			if ( CursesInteractionResultKind.Command != result.Kind
				|| !ReferenceEquals( region, result.Region )
				|| command != result.Command ) {
				throw new InvalidOperationException(
					"Root-scope command routing changed during the allocation gate."
				);
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			( 96L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void NestedScopeHitTestingRetainsPublishedHitSnapshotAllocationCeiling() {
		CursesScreen screen = new( 40, 20 );
		using CursesInteractionRouter router = new( screen );
		List<CursesInteractionScope> scopes = CreateScopeChain(
			router,
			depth: 8
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 3, 4, 2, 2 )
			) {
				Scope = scopes[^1]
			}
		);
		using CursesInteractionScopeLease lease = router.ActivateScope( scopes[0] );

		Action operation = () => {
			CursesInteractionHit hit = router.HitTest(
				3,
				4
			) ?? throw new InvalidOperationException(
				"Nested-scope hit testing unexpectedly missed the representative region."
			);
			if ( !ReferenceEquals( region, hit.Region )
				|| 0 != hit.LocalRow
				|| 0 != hit.LocalColumn ) {
				throw new InvalidOperationException(
					"Nested-scope hit testing changed during the allocation gate."
				);
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			( 96L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void SpatialFocusIsAllocationFreeApartFromMeasurementNoise() {
		CursesScreen screen = new( 40, 40 );
		using CursesInteractionRouter router = new( screen );
		CursesInteractionScope scope = router.RegisterScope();
		CursesInteractionRegion? origin = null;

		for ( int row = 0; row < SpatialRegionCountPerAxis; row++ ) {
			for ( int column = 0; column < SpatialRegionCountPerAxis; column++ ) {
				CursesInteractionRegion region = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle(
							row * 2,
							column * 2,
							1,
							1
						)
					) {
						Scope = scope,
						IsFocusable = true,
						TraversalOrder = ( row * SpatialRegionCountPerAxis ) + column
					}
				);
				if ( 7 == row && 7 == column ) {
					origin = region;
				}
			}
		}

		CursesInteractionRegion start = origin
			?? throw new InvalidOperationException( "The spatial-focus fixture did not register its origin." );
		using CursesInteractionScopeLease lease = router.ActivateScope( scope );
		Assert.True( router.Focus( start ) );

		Action operation = () => {
			CursesInteractionRegion right = router.MoveFocus( CursesFocusDirection.Right )
				?? throw new InvalidOperationException( "Spatial focus unexpectedly found no right candidate." );
			if ( ReferenceEquals( start, right ) ) {
				throw new InvalidOperationException( "Spatial focus did not leave the origin." );
			}
			CursesInteractionRegion left = router.MoveFocus( CursesFocusDirection.Left )
				?? throw new InvalidOperationException( "Spatial focus unexpectedly found no left candidate." );
			if ( !ReferenceEquals( start, left ) ) {
				throw new InvalidOperationException( "Spatial focus did not return deterministically to the origin." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes(
				operation,
				SpatialAllocationIterations
			),
			0,
			AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void DeepScopedCommandLookupRetainsPublishedCommandResultAllocationCeiling() {
		CursesScreen screen = new( 40, 20 );
		using CursesInteractionRouter router = new( screen );
		List<CursesInteractionScope> scopes = CreateScopeChain(
			router,
			depth: 16
		);
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 2, 1, 1 )
			) {
				Scope = scopes[^1],
				IsFocusable = true
			}
		);
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Escape );
		CursesCommand command = new( "scope.outer" );
		scopes[0].BindGesture( gesture, command );
		using CursesInteractionScopeLease lease = router.ActivateScope( scopes[0] );
		Assert.True( router.Focus( region ) );
		CursesInputEvent input = CursesInputEvent.FromKey( CursesKey.Escape );

		Action operation = () => {
			CursesInteractionResult result = router.Route( input );
			if ( CursesInteractionResultKind.Command != result.Kind
				|| !ReferenceEquals( region, result.Region )
				|| command != result.Command ) {
				throw new InvalidOperationException(
					"Deep scoped-command lookup changed during the allocation gate."
				);
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			( 96L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void CapturedMouseRoutingStaysWithinAdvancedSnapshotAllocationCeiling() {
		CursesScreen screen = new( 20, 8 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 2, 2 )
			)
		);
		using CursesPointerCaptureLease capture = router.CapturePointer(
			region,
			CursesMouseButton.Primary
		);
		CursesInputEvent input = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 19,
				row: 7
			)
		);

		Action operation = () => {
			CursesInteractionResult result = router.Route( input );
			CursesPointerTarget target = result.PointerTarget
				?? throw new InvalidOperationException( "Captured routing produced no pointer target." );
			CursesPointerGesture gesture = result.PointerGesture
				?? throw new InvalidOperationException( "Captured routing produced no pointer gesture." );
			if ( CursesInteractionResultKind.Targeted != result.Kind
				|| !ReferenceEquals( region, result.Region )
				|| !ReferenceEquals( region, target.Region )
				|| target.IsInside
				|| CursesPointerGestureKind.Move != gesture.Kind ) {
				throw new InvalidOperationException(
					"Captured pointer routing changed during the allocation gate."
				);
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			( 256L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void MaximumCapacityChurnCompletesWithinBroadRegressionGate() {
		Stopwatch stopwatch = Stopwatch.StartNew();

		ChurnMaximumScopes();
		ChurnMaximumTotalBindings();
		ChurnCaptureAndGestureState();

		stopwatch.Stop();
		Assert.True(
			stopwatch.Elapsed <= TimeSpan.FromSeconds( 60 ),
			$"Maximum-capacity advanced-interaction churn took {stopwatch.Elapsed}."
		);
	}

	private static List<CursesInteractionScope> CreateScopeChain(
		CursesInteractionRouter router,
		int depth
	) {
		ArgumentNullException.ThrowIfNull( router );
		if ( 1 > depth ) {
			throw new ArgumentOutOfRangeException( nameof( depth ) );
		}

		List<CursesInteractionScope> scopes = new( depth );
		CursesInteractionScope? parent = null;
		for ( int index = 0; index < depth; index++ ) {
			CursesInteractionScope scope = router.RegisterScope(
				new CursesInteractionScopeOptions {
					Parent = parent
				}
			);
			scopes.Add( scope );
			parent = scope;
		}
		return scopes;
	}

	private static long MeasureMinimumAllocatedBytes(
		Action operation,
		int iterations = AllocationIterations
	) {
		ArgumentNullException.ThrowIfNull( operation );
		if ( 1 > iterations ) {
			throw new ArgumentOutOfRangeException( nameof( iterations ) );
		}

		for ( int index = 0; index < WarmupIterations; index++ ) {
			operation();
		}

		long minimum = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < iterations; index++ ) {
				operation();
			}
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			minimum = Math.Min(
				minimum,
				allocated
			);
		}
		return minimum;
	}

	private static void ChurnMaximumScopes() {
		CursesScreen screen = new( 10, 10 );
		using CursesInteractionRouter router = new( screen );

		for ( int cycle = 0; cycle < 2; cycle++ ) {
			List<CursesInteractionScope> scopes = new( CursesInteractionRouter.MaximumScopes );
			for ( int index = 0; index < CursesInteractionRouter.MaximumScopes; index++ ) {
				scopes.Add( router.RegisterScope() );
			}
			for ( int index = scopes.Count - 1; 0 <= index; index-- ) {
				scopes[index].Dispose();
			}
		}
	}

	private static void ChurnMaximumTotalBindings() {
		CursesScreen screen = new( 100, 100 );
		using CursesInteractionRouter router = new( screen );
		CursesCommand command = new( "capacity" );

		const int fullyBoundRegionCount = 63;
		for ( int regionIndex = 0; regionIndex < fullyBoundRegionCount; regionIndex++ ) {
			CursesInteractionRegion region = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle(
						regionIndex,
						0,
						1,
						1
					)
				)
			);
			for ( int gestureIndex = 0; gestureIndex < CursesInteractionRouter.MaximumRegionGestureBindings; gestureIndex++ ) {
				region.BindGesture(
					CharacterGesture( gestureIndex ),
					command
				);
			}
		}

		using CursesInteractionScope scope = router.RegisterScope();
		for ( int index = 0; index < CursesInteractionRouter.MaximumScopeGestureBindings; index++ ) {
			scope.BindGesture(
				CharacterGesture( index ),
				command
			);
		}
		for ( int index = 0; index < CursesInteractionRouter.MaximumScopeGestureBindings; index++ ) {
			if ( !scope.UnbindGesture( CharacterGesture( index ) ) ) {
				throw new InvalidOperationException( "A maximum-capacity scope binding disappeared during churn." );
			}
		}
		for ( int index = 0; index < CursesInteractionRouter.MaximumScopeGestureBindings; index++ ) {
			scope.BindGesture(
				CharacterGesture( index ),
				command
			);
		}
	}

	private static void ChurnCaptureAndGestureState() {
		CursesScreen screen = new( 8, 4 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			)
		);
		CursesInputEvent press = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 0,
				row: 0
			)
		);
		CursesInputEvent move = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Move,
				CursesMouseButton.Primary,
				column: 1,
				row: 0
			)
		);
		CursesInputEvent release = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Release,
				CursesMouseButton.Primary,
				column: 1,
				row: 0
			)
		);

		for ( int index = 0; index < 2048; index++ ) {
			using CursesPointerCaptureLease capture = router.CapturePointer(
				region,
				CursesMouseButton.Primary
			);
			CursesInteractionResult pressed = router.Route( press );
			CursesInteractionResult moved = router.Route( move );
			CursesInteractionResult released = router.Route( release );
			if ( CursesPointerGestureKind.Press != pressed.PointerGesture?.Kind
				|| CursesPointerGestureKind.DragStart != moved.PointerGesture?.Kind
				|| CursesPointerGestureKind.DragEnd != released.PointerGesture?.Kind ) {
				throw new InvalidOperationException( "Pointer gesture state changed during capture churn." );
			}
		}
	}

	private static CursesKeyGesture CharacterGesture(
		int index
	) {
		return CursesKeyGesture.ForCharacter(
			new Rune( 0x3000 + index )
		);
	}
}
