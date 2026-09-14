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
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Contract and integration coverage for the T1407 semantic pointer-shape surface.</summary>
public sealed class CursesPointerShapeTests {
	private const string ResetFrame = "\u001b]22;\u001b\\";

	[Fact]
	public void PointerShapeVocabularyHasFrozenNamesAndNumericValues() {
		string[] expectedNames = [
			"Alias",
			"Cell",
			"Copy",
			"Crosshair",
			"Default",
			"EastResize",
			"EastWestResize",
			"Grab",
			"Grabbing",
			"Help",
			"Move",
			"NorthResize",
			"NorthEastResize",
			"NorthEastSouthWestResize",
			"NoDrop",
			"NotAllowed",
			"NorthSouthResize",
			"NorthWestResize",
			"NorthWestSouthEastResize",
			"Pointer",
			"Progress",
			"SouthResize",
			"SouthEastResize",
			"SouthWestResize",
			"Text",
			"VerticalText",
			"WestResize",
			"Wait",
			"ZoomIn",
			"ZoomOut"
		];
		CursesPointerShape[] values = Enum.GetValues<CursesPointerShape>();

		Assert.Equal( expectedNames.Length, values.Length );
		for ( int index = 0; index < expectedNames.Length; index++ ) {
			Assert.Equal( index, (int)values[ index ] );
			Assert.Equal( expectedNames[ index ], values[ index ].ToString() );
		}
	}

	[Fact]
	public void RegionPointerPreferenceIsMutableAndHitIsSnapshot() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 2, 3, 4, 5 )
			) {
				PointerShape = CursesPointerShape.Pointer
			}
		);

		Assert.Equal( CursesPointerShape.Pointer, region.PointerShape );
		CursesInteractionHit first = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 3, 4 )
		);
		Assert.Equal( CursesPointerShape.Pointer, first.PointerShape );

		region.PointerShape = CursesPointerShape.Wait;
		Assert.Equal( CursesPointerShape.Wait, region.PointerShape );
		Assert.Equal( CursesPointerShape.Pointer, first.PointerShape );

		CursesInteractionHit second = Assert.IsType<CursesInteractionHit>(
			router.HitTest( 3, 4 )
		);
		Assert.Equal( CursesPointerShape.Wait, second.PointerShape );

		region.PointerShape = null;
		Assert.Null( router.HitTest( 3, 4 )!.PointerShape );
	}

	[Fact]
	public void RegionRejectsUndefinedPointerPreferenceAndMutationAfterDisposal() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = router.RegisterRegion(
					new CursesInteractionRegionOptions(
						new CursesRectangle( 0, 0, 1, 1 )
					) {
						PointerShape = (CursesPointerShape)30
					}
				);
			}
		);

		CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			)
		);
		region.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => {
				region.PointerShape = CursesPointerShape.Help;
			}
		);
	}

	[Fact]
	public void MouseRouteCarriesPointerPreferenceWithoutChangingFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion focused = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 1, 1 )
			) {
				IsFocusable = true
			}
		);
		using CursesInteractionRegion hitRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 4, 5, 3, 4 )
			) {
				PointerShape = CursesPointerShape.Crosshair
			}
		);
		Assert.True( router.Focus( focused ) );
		CursesInputEvent input = CursesInputEvent.FromMouse(
			new CursesMouseEvent(
				CursesMouseAction.Press,
				CursesMouseButton.Primary,
				column: 7,
				row: 5
			)
		);

		CursesInteractionResult result = router.Route( input );

		Assert.Equal( CursesInteractionResultKind.Targeted, result.Kind );
		Assert.Same( hitRegion, result.Region );
		Assert.NotNull( result.Hit );
		Assert.Equal( CursesPointerShape.Crosshair, result.Hit.PointerShape );
		Assert.Same( focused, router.FocusedRegion );

		hitRegion.PointerShape = CursesPointerShape.Move;
		Assert.Equal( CursesPointerShape.Crosshair, result.Hit.PointerShape );
	}

	[Fact]
	public async Task EveryPointerShapeMapsToCanonicalTerminalWireName() {
		string[] wireNames = [
			"alias",
			"cell",
			"copy",
			"crosshair",
			"default",
			"e-resize",
			"ew-resize",
			"grab",
			"grabbing",
			"help",
			"move",
			"n-resize",
			"ne-resize",
			"nesw-resize",
			"no-drop",
			"not-allowed",
			"ns-resize",
			"nw-resize",
			"nwse-resize",
			"pointer",
			"progress",
			"s-resize",
			"se-resize",
			"sw-resize",
			"text",
			"vertical-text",
			"w-resize",
			"wait",
			"zoom-in",
			"zoom-out"
		];
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync( output );
		CursesPointerShape[] shapes = Enum.GetValues<CursesPointerShape>();

		for ( int index = 0; index < shapes.Length; index++ ) {
			output.Clear();
			CursesPointerShapeLease lease = await session.AcquirePointerShapeAsync(
				shapes[ index ]
			);

			Assert.Equal( shapes[ index ], lease.Shape );
			Assert.Equal(
				$"\u001b]22;{wireNames[ index ]}\u001b\\",
				output.Text
			);

			await lease.DisposeAsync();
			Assert.Equal(
				$"\u001b]22;{wireNames[ index ]}\u001b\\{ResetFrame}",
				output.Text
			);
		}
	}

	[Fact]
	public async Task NestedCursesPointerLeasesDelegateRestorationToTerminal() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync( output );
		output.Clear();

		CursesPointerShapeLease outer = await session.AcquirePointerShapeAsync(
			CursesPointerShape.Pointer
		);
		CursesPointerShapeLease inner = await session.AcquirePointerShapeAsync(
			CursesPointerShape.Wait
		);
		await inner.DisposeAsync();
		await outer.DisposeAsync();

		Assert.Equal(
			"\u001b]22;pointer\u001b\\"
				+ "\u001b]22;wait\u001b\\"
				+ "\u001b]22;pointer\u001b\\"
				+ ResetFrame,
			output.Text
		);
	}

	[Fact]
	public async Task CursesPointerLeaseComposesWithOuterTerminalPointerLease() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		output.Clear();
		TerminalPointerShapeLease outer = await terminalSession.AcquirePointerShapeAsync(
			TerminalPointerShape.Pointer
		);
		CursesPointerShapeLease inner = await session.AcquirePointerShapeAsync(
			CursesPointerShape.Wait
		);

		await inner.DisposeAsync();
		await outer.DisposeAsync();

		Assert.Equal(
			"\u001b]22;pointer\u001b\\"
				+ "\u001b]22;wait\u001b\\"
				+ "\u001b]22;pointer\u001b\\"
				+ ResetFrame,
			output.Text
		);
	}

	[Fact]
	public async Task InvalidAndCanceledAcquisitionsFailBeforeTerminalOutput() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync( output );
		output.Clear();

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			async () => {
				_ = await session.AcquirePointerShapeAsync(
					(CursesPointerShape)30
				);
			}
		);
		Assert.Equal( string.Empty, output.Text );

		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			async () => {
				_ = await session.AcquirePointerShapeAsync(
					CursesPointerShape.Text,
					cancellation.Token
				);
			}
		);
		Assert.Equal( string.Empty, output.Text );
	}

	private static async ValueTask<CursesSession> OpenSessionAsync(
		RecordingOutput output
	) {
		ArgumentNullException.ThrowIfNull( output );
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		return await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		RecordingOutput output
	) {
		ArgumentNullException.ThrowIfNull( output );
		return TerminalSession.OpenAsync(
			new RecordingTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EmptyInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = TerminalProfiles.Dumb,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private sealed class EmptyInput : ITerminalInput {
		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			await Task.Delay(
				Timeout.InfiniteTimeSpan,
				cancellationToken
			).ConfigureAwait( false );
			return 0;
		}
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.ASCII.GetString( this.bytes.ToArray() );
				}
			}
		}

		internal void Clear() {
			lock ( this.sync ) {
				this.bytes.Clear();
			}
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class RecordingTerminalControlProvider : ITerminalControlProvider {
		private readonly TerminalModeSnapshot baseline = TerminalModeSnapshot.CreatePosix(
			0,
			0,
			0,
			0x0002UL,
			new byte[ 32 ],
			0,
			32,
			0,
			new TerminalSpeed( 13, 9600 ),
			new TerminalSpeed( 13, 9600 )
		);

		public TerminalControlResult<TerminalEndpointObservation> Observe(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalEndpointObservation>.Available(
				new TerminalEndpointObservation(
					true,
					null,
					TerminalPlatformKind.PosixTermios,
					TerminalControlCapabilities.Attachment
						| TerminalControlCapabilities.LiveSize
						| TerminalControlCapabilities.ModeRead
						| TerminalControlCapabilities.ModeWrite
				)
			);
		}

		public TerminalControlResult<TerminalSize> GetSize(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalSize>.Available(
				new TerminalSize( 80, 24 )
			);
		}

		public TerminalControlResult<TerminalModeSnapshot> GetMode(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalModeSnapshot>.Available(
				this.baseline
			);
		}

		public TerminalControlMutationResult SetMode(
			TerminalEndpoint endpoint,
			TerminalModeSnapshot mode,
			TerminalModeApplyTiming timing
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			ArgumentNullException.ThrowIfNull( mode );
			if ( !Enum.IsDefined( timing ) ) {
				throw new ArgumentOutOfRangeException( nameof( timing ) );
			}
			return TerminalControlMutationResult.Success();
		}
	}
}
