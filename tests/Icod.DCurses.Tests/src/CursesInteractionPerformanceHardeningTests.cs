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

/// <summary>Freezes representative 1.4 interaction-routing allocation and throughput ceilings.</summary>
[Collection( AllocationMeasurementCollection.Name )]
public sealed class CursesInteractionPerformanceHardeningTests {
	private const int AllocationIterations = 10000;
	private const long AllocationMeasurementNoiseAllowance = 64L * 1024L;
	private const long AllocationMeasurementNoiseFloor = 1024L;
	private const int AllocationSamples = 8;
	private const int BindingCount = 64;
	private const int DiscoveryAllocationIterations = 128;
	private const int RegionCount = 256;
	private const int SequenceAllocationIterations = 512;
	private const int SequenceWarmupIterations = 256;
	private const int ThroughputIterations = 10000;
	private const int WarmupIterations = 4096;

	[Fact]
	public void HitTestMissIsAllocationFreeAfterWarmup() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out _,
			out _,
			out _,
			out _,
			out _,
			out _
		);

		Action operation = () => {
			CursesInteractionHit? hit = router.HitTest(
				59,
				159
			);
			if ( hit is not null ) {
				throw new InvalidOperationException( "Expected the representative point to miss every region." );
			}
		};

		Assert.Equal(
			0,
			MeasureMinimumAllocatedBytes( operation )
		);
	}

	[Fact]
	public void FocusTraversalStaysWithinMeasurementNoiseFloor() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out _,
			out _,
			out _,
			out _,
			out _,
			out _
		);

		Action operation = () => {
			CursesInteractionRegion? region = router.MoveFocus( CursesFocusDirection.Forward );
			if ( region is null ) {
				throw new InvalidOperationException( "Representative focus traversal unexpectedly found no eligible region." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			AllocationMeasurementNoiseFloor
		);
	}

	[Fact]
	public void SuccessfulHitTestStaysWithinSnapshotAllocationCeiling() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out CursesInteractionRegion winningRegion,
			out _,
			out _,
			out _,
			out _,
			out _
		);

		Action operation = () => {
			CursesInteractionHit hit = router.HitTest(
				0,
				0
			) ?? throw new InvalidOperationException( "Expected a representative hit." );
			if ( !ReferenceEquals(
				hit.Region,
				winningRegion
			) ) {
				throw new InvalidOperationException( "Representative hit precedence changed." );
			}
		};

		long allocated = MeasureMinimumAllocatedBytes( operation );
		Assert.InRange(
			allocated,
			0,
			96L * AllocationIterations
		);
	}

	[Fact]
	public void LocalAndGlobalCommandRoutesStayWithinSnapshotAllocationCeiling() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out CursesInteractionRegion winningRegion,
			out CursesInputEvent localInput,
			out CursesInputEvent globalInput,
			out _,
			out CursesCommand localCommand,
			out CursesCommand globalCommand
		);

		Action localOperation = () => {
			CursesInteractionResult result = router.Route( localInput );
			if ( CursesInteractionResultKind.Command != result.Kind
				|| !ReferenceEquals(
					result.Region,
					winningRegion
				)
				|| result.Command != localCommand ) {
				throw new InvalidOperationException( "Representative local-command routing changed." );
			}
		};
		Action globalOperation = () => {
			CursesInteractionResult result = router.Route( globalInput );
			if ( CursesInteractionResultKind.Command != result.Kind
				|| result.Region is not null
				|| result.Command != globalCommand ) {
				throw new InvalidOperationException( "Representative global-command routing changed." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( localOperation ),
			0,
			96L * AllocationIterations
		);
		Assert.InRange(
			MeasureMinimumAllocatedBytes( globalOperation ),
			0,
			96L * AllocationIterations
		);
	}

	[Fact]
	public void MouseRoutingStaysWithinHitAndResultAllocationCeiling() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out CursesInteractionRegion winningRegion,
			out _,
			out _,
			out CursesInputEvent mouseInput,
			out _,
			out _
		);

		Action operation = () => {
			CursesInteractionResult result = router.Route( mouseInput );
			CursesInteractionHit hit = result.Hit
				?? throw new InvalidOperationException( "Expected representative mouse input to produce a hit." );
			if ( CursesInteractionResultKind.Targeted != result.Kind
				|| !ReferenceEquals(
					result.Region,
					winningRegion
				)
				|| !ReferenceEquals(
					hit.Region,
					winningRegion
				)
				|| 0 != hit.LocalRow
				|| 0 != hit.LocalColumn ) {
				throw new InvalidOperationException( "Representative mouse routing changed." );
			}
		};

		long allocated = MeasureMinimumAllocatedBytes( operation );
		Assert.InRange(
			allocated,
			0,
			( 192L * AllocationIterations ) + AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void RegisteredCommandSequencesDoNotIncreaseOrdinaryRouteAllocationCeiling() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out CursesInteractionRegion winningRegion,
			out CursesInputEvent localInput,
			out _,
			out _,
			out CursesCommand localCommand,
			out _
		);
		BindMaximumRegionSequences( winningRegion );

		Action operation = () => {
			CursesInteractionResult result = router.Route( localInput );
			if ( CursesInteractionResultKind.Command != result.Kind
				|| !ReferenceEquals(
					result.Region,
					winningRegion
				)
				|| result.Command != localCommand ) {
				throw new InvalidOperationException( "Ordinary routing changed after sequence registration." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes( operation ),
			0,
			96L * AllocationIterations
		);
	}

	[Fact]
	public void StartingAndCancellingMaximumSharedPrefixStaysWithinBoundedAllocationCeiling() {
		using CursesInteractionRouter router = CreateSequenceRouter(
			out CursesInteractionRegion region
		);
		BindMaximumRegionSequences( region );
		CursesInputEvent prefix = CursesInputEvent.FromText( new Rune( 'g' ) );

		Action operation = () => {
			CursesCommandSequenceResult result = router.ProcessCommandSequence( prefix );
			if ( CursesCommandSequenceResultKind.Pending != result.Kind
				|| 1 != result.MatchedGestures.Count
				|| !router.CancelPendingCommandSequence() ) {
				throw new InvalidOperationException( "Maximum shared-prefix sequence state changed." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes(
				operation,
				SequenceWarmupIterations,
				SequenceAllocationIterations
			),
			0,
			( 16L * 1024L * SequenceAllocationIterations )
				+ AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void EffectiveBindingDiscoveryAtRegionCapacityStaysWithinSnapshotAllocationCeiling() {
		using CursesInteractionRouter router = CreateSequenceRouter(
			out CursesInteractionRegion region
		);
		for ( int index = 0;
			index < CursesInteractionRouter.MaximumRegionGestureBindings;
			++index ) {
			region.BindGesture(
				CursesKeyGesture.ForCharacter( new Rune( 0x2000 + index ) ),
				new CursesCommand( $"binding-{index}" )
			);
		}

		Action operation = () => {
			IReadOnlyList<CursesCommandBinding> bindings =
				router.GetEffectiveGestureBindings();
			if ( CursesInteractionRouter.MaximumRegionGestureBindings
				!= bindings.Count ) {
				throw new InvalidOperationException( "Effective binding discovery count changed." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes(
				operation,
				SequenceWarmupIterations,
				DiscoveryAllocationIterations
			),
			0,
			( 128L * 1024L * DiscoveryAllocationIterations )
				+ AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void EffectiveSequenceDiscoveryAtRegionCapacityStaysWithinSnapshotAllocationCeiling() {
		using CursesInteractionRouter router = CreateSequenceRouter(
			out CursesInteractionRegion region
		);
		BindMaximumRegionSequences( region );

		Action operation = () => {
			IReadOnlyList<CursesCommandSequenceBinding> bindings =
				router.GetEffectiveGestureSequenceBindings();
			if ( CursesInteractionRouter.MaximumRegionCommandSequenceBindings
				!= bindings.Count ) {
				throw new InvalidOperationException( "Effective sequence discovery count changed." );
			}
		};

		Assert.InRange(
			MeasureMinimumAllocatedBytes(
				operation,
				SequenceWarmupIterations,
				DiscoveryAllocationIterations
			),
			0,
			( 128L * 1024L * DiscoveryAllocationIterations )
				+ AllocationMeasurementNoiseAllowance
		);
	}

	[Fact]
	public void RepresentativeInteractionLoopStaysWithinBroadElapsedTimeGate() {
		using CursesInteractionRouter router = CreateRepresentativeRouter(
			out CursesInteractionRegion winningRegion,
			out CursesInputEvent localInput,
			out CursesInputEvent globalInput,
			out _,
			out CursesCommand localCommand,
			out CursesCommand globalCommand
		);

		for ( int index = 0; index < WarmupIterations; index++ ) {
			_ = router.HitTest(
				0,
				0
			);
			_ = router.MoveFocus( CursesFocusDirection.Forward );
			_ = router.Route( localInput );
			_ = router.Route( globalInput );
		}
		Assert.True( router.Focus( winningRegion ) );

		Stopwatch stopwatch = Stopwatch.StartNew();
		for ( int index = 0; index < ThroughputIterations; index++ ) {
			CursesInteractionHit hit = router.HitTest(
				0,
				0
			) ?? throw new InvalidOperationException( "Expected representative hit during throughput gate." );
			if ( !ReferenceEquals(
				hit.Region,
				winningRegion
			) ) {
				throw new InvalidOperationException( "Hit precedence changed during throughput gate." );
			}
		}
		for ( int index = 0; index < ThroughputIterations; index++ ) {
			if ( router.MoveFocus( CursesFocusDirection.Forward ) is null ) {
				throw new InvalidOperationException( "Focus traversal failed during throughput gate." );
			}
		}
		Assert.True( router.Focus( winningRegion ) );
		for ( int index = 0; index < ThroughputIterations; index++ ) {
			CursesInteractionResult result = router.Route( localInput );
			if ( result.Command != localCommand ) {
				throw new InvalidOperationException( "Local routing changed during throughput gate." );
			}
		}
		for ( int index = 0; index < ThroughputIterations; index++ ) {
			CursesInteractionResult result = router.Route( globalInput );
			if ( result.Command != globalCommand ) {
				throw new InvalidOperationException( "Global routing changed during throughput gate." );
			}
		}
		stopwatch.Stop();

		Assert.True(
			stopwatch.Elapsed <= TimeSpan.FromSeconds( 15 ),
			$"Representative interaction loop took {stopwatch.Elapsed}."
		);
	}

	private static CursesInteractionRouter CreateRepresentativeRouter(
		out CursesInteractionRegion winningRegion,
		out CursesInputEvent localInput,
		out CursesInputEvent globalInput,
		out CursesInputEvent mouseInput,
		out CursesCommand localCommand,
		out CursesCommand globalCommand
	) {
		CursesScreen screen = new(
			160,
			60
		);
		CursesInteractionRouter router = new( screen );
		CursesInteractionRegion? latest = null;
		for ( int index = 0; index < RegionCount; index++ ) {
			latest = router.RegisterRegion(
				new CursesInteractionRegionOptions(
					new CursesRectangle(
						0,
						0,
						1,
						1
					)
				) {
					IsFocusable = true,
					TraversalOrder = index
				}
			);
		}
		winningRegion = latest
			?? throw new InvalidOperationException( "Representative router did not register a winning region." );

		localCommand = new CursesCommand( $"local.{BindingCount - 1:D2}" );
		globalCommand = new CursesCommand( $"global.{BindingCount - 1:D2}" );
		for ( int index = 0; index < BindingCount; index++ ) {
			CursesCommand local = index == BindingCount - 1
				? localCommand
				: new CursesCommand( $"local.{index:D2}" )
			;
			CursesCommand global = index == BindingCount - 1
				? globalCommand
				: new CursesCommand( $"global.{index:D2}" )
			;
			winningRegion.BindGesture(
				CursesKeyGesture.ForFunctionKey( index ),
				local
			);
			router.BindGlobalGesture(
				CursesKeyGesture.ForFunctionKey(
					index,
					CursesKeyModifiers.Alt
				),
				global
			);
		}
		if ( !router.Focus( winningRegion ) ) {
			throw new InvalidOperationException( "Representative router could not establish initial focus." );
		}

		localInput = CursesInputEvent.FromKey(
			CursesKey.Function,
			functionKeyNumber: BindingCount - 1
		);
		globalInput = CursesInputEvent.FromKey(
			CursesKey.Function,
			modifiers: CursesKeyModifiers.Alt,
			functionKeyNumber: BindingCount - 1
		);
		mouseInput = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Move,
				CursesMouseButton.None,
				column: 0,
				row: 0
			)
		);
		return router;
	}

	private static CursesInteractionRouter CreateSequenceRouter(
		out CursesInteractionRegion region
	) {
		CursesScreen screen = new( 20, 10 );
		CursesInteractionRouter router = new( screen );
		region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 4, 4 )
			) {
				IsFocusable = true
			}
		);
		if ( !router.Focus( region ) ) {
			throw new InvalidOperationException( "Representative router could not establish sequence focus." );
		}
		return router;
	}

	private static void BindMaximumRegionSequences(
		CursesInteractionRegion region
	) {
		ArgumentNullException.ThrowIfNull( region );
		CursesKeyGesture prefix = CursesKeyGesture.ForCharacter( new Rune( 'g' ) );
		for ( int index = 0;
			index < CursesInteractionRouter.MaximumRegionCommandSequenceBindings;
			++index ) {
			region.BindGestureSequence(
				[
					prefix,
					CursesKeyGesture.ForCharacter( new Rune( 0x1000 + index ) )
				],
				new CursesCommand( $"sequence-{index}" )
			);
		}
	}

	private static long MeasureMinimumAllocatedBytes(
		Action operation
	) {
		return MeasureMinimumAllocatedBytes(
			operation,
			WarmupIterations,
			AllocationIterations
		);
	}

	private static long MeasureMinimumAllocatedBytes(
		Action operation,
		int warmupIterations,
		int allocationIterations
	) {
		ArgumentNullException.ThrowIfNull( operation );
		for ( int index = 0; index < warmupIterations; index++ ) {
			operation();
		}

		long minimum = long.MaxValue;
		for ( int sample = 0; sample < AllocationSamples; sample++ ) {
			long before = GC.GetAllocatedBytesForCurrentThread();
			for ( int index = 0; index < allocationIterations; index++ ) {
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
}
