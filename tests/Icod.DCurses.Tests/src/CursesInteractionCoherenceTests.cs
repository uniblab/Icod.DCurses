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

/// <summary>Proves T1408 resize, retained-surface, disposal, and lifecycle coherence.</summary>
public sealed class CursesInteractionCoherenceTests {
	[Fact]
	public void ScreenShrinkAndRegrowPreserveDeclaredRegionAndBindingsWithoutRestoringOldFocus() {
		CursesScreen screen = new( 20, 10 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion fallback = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 2, 2 )
			) {
				IsFocusable = true,
				TraversalOrder = 0
			}
		);
		CursesRectangle edgeBounds = new( 8, 18, 2, 2 );
		using CursesInteractionRegion edge = router.RegisterRegion(
			new CursesInteractionRegionOptions( edgeBounds ) {
				IsFocusable = true,
				TraversalOrder = 1
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Edge.Enter" );
		edge.BindGesture( enter, command );
		Assert.True( router.Focus( edge ) );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
		);

		screen.Resize( 10, 5 );

		Assert.Equal( edgeBounds, edge.Bounds );
		Assert.Same( fallback, router.FocusedRegion );

		screen.Resize( 20, 10 );

		Assert.Equal( edgeBounds, edge.Bounds );
		Assert.Same( fallback, router.FocusedRegion );
		Assert.True( router.Focus( edge ) );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
		);
	}

	[Fact]
	public void PanelMoveResizeHideAndShowUseCurrentGeometryWithoutLosingInteractionState() {
		CursesScreen screen = new( 30, 15 );
		using CursesPanel panel = screen.CreatePanel( 2, 2, 5, 5 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 5, 5 )
			) {
				Panel = panel,
				IsFocusable = true,
				PointerShape = CursesPointerShape.Pointer
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Panel.Enter" );
		region.BindGesture( enter, command );
		Assert.True( router.Focus( region ) );

		Assert.Same( region, router.HitTest( 3, 3 )!.Region );
		Assert.Equal( CursesPointerShape.Pointer, router.HitTest( 3, 3 )!.PointerShape );

		panel.MoveTo( 5, 6 );
		panel.Resize( 3, 3 );

		Assert.Null( router.HitTest( 3, 3 ) );
		Assert.Same( region, router.HitTest( 6, 7 )!.Region );

		panel.Hide();
		Assert.Null( router.HitTest( 6, 7 ) );
		Assert.Null( router.FocusedRegion );

		panel.Show();
		Assert.Same( region, router.HitTest( 6, 7 )!.Region );
		Assert.True( router.Focus( region ) );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
		);
		Assert.Equal( CursesPointerShape.Pointer, region.PointerShape );
	}

	[Fact]
	public void DisposedPanelMakesRegionPermanentlyIneligibleWithoutDisposingItsApplicationState() {
		CursesScreen screen = new( 20, 10 );
		CursesPanel panel = screen.CreatePanel( 1, 1, 4, 4 );
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 0, 0, 4, 4 )
			) {
				Panel = panel,
				IsFocusable = true,
				PointerShape = CursesPointerShape.Text
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		region.BindGesture(
			enter,
			new CursesCommand( "Panel.Enter" )
		);
		Assert.True( router.Focus( region ) );

		panel.Dispose();

		Assert.Null( router.HitTest( 2, 2 ) );
		Assert.Null( router.FocusedRegion );
		Assert.False( router.Focus( region ) );
		Assert.Equal( CursesPointerShape.Text, region.PointerShape );
		Assert.True( region.UnbindGesture( enter ) );
	}

	[Fact]
	public async Task DimensionSynchronizationReclipsApplicationRouterWithoutRelayoutOrRegistrationLoss() {
		RecordingOutput output = new();
		RecordingTerminalControlProvider provider = new() {
			Size = new TerminalSize( 20, 10 )
		};
		await using CursesSession session = await OpenSessionAsync(
			provider,
			output
		);
		CursesScreen screen = session.Screen;
		using CursesInteractionRouter router = new( screen );
		CursesRectangle bounds = new( 8, 18, 2, 2 );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions( bounds ) {
				IsFocusable = true
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Edge.Enter" );
		region.BindGesture( enter, command );
		Assert.True( router.Focus( region ) );

		provider.Size = new TerminalSize( 10, 5 );
		TerminalControlResult<TerminalDimensions> smaller = session.SynchronizeDimensions();

		Assert.True( smaller.IsAvailable );
		Assert.Equal( 10, screen.Columns );
		Assert.Equal( 5, screen.Rows );
		Assert.Equal( bounds, region.Bounds );
		Assert.Null( router.FocusedRegion );

		provider.Size = new TerminalSize( 20, 10 );
		TerminalControlResult<TerminalDimensions> restored = session.SynchronizeDimensions();

		Assert.True( restored.IsAvailable );
		Assert.Equal( bounds, region.Bounds );
		Assert.Null( router.FocusedRegion );
		Assert.True( router.Focus( region ) );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
		);
	}

	[Fact]
	public async Task RepeatedDCursesLifecycleCallbacksDoNotMutateApplicationOwnedRouterState() {
		RecordingOutput output = new();
		RecordingTerminalControlProvider provider = new() {
			Size = new TerminalSize( 20, 10 )
		};
		await using CursesSession session = await OpenSessionAsync(
			provider,
			output
		);
		using CursesInteractionRouter router = new( session.Screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 4, 4 )
			) {
				IsFocusable = true,
				PointerShape = CursesPointerShape.Crosshair
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Region.Enter" );
		region.BindGesture( enter, command );
		Assert.True( router.Focus( region ) );

		for ( int cycle = 0; cycle < 3; cycle++ ) {
			await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
			Assert.Same( region, router.FocusedRegion );
			Assert.Equal(
				command,
				router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
			);
			await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
			Assert.Same( region, router.FocusedRegion );
			Assert.Equal( CursesPointerShape.Crosshair, region.PointerShape );
		}
	}

	[Fact]
	public async Task SessionDisposalLetsTerminalClosePointerStateWithoutInvalidatingApplicationRouter() {
		RecordingOutput output = new();
		RecordingTerminalControlProvider provider = new() {
			Size = new TerminalSize( 20, 10 )
		};
		CursesSession session = await OpenSessionAsync(
			provider,
			output
		);
		CursesScreen screen = session.Screen;
		using CursesInteractionRouter router = new( screen );
		using CursesInteractionRegion region = router.RegisterRegion(
			new CursesInteractionRegionOptions(
				new CursesRectangle( 1, 1, 4, 4 )
			) {
				IsFocusable = true
			}
		);
		CursesKeyGesture enter = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesCommand command = new( "Region.Enter" );
		region.BindGesture( enter, command );
		Assert.True( router.Focus( region ) );
		CursesPointerShapeLease pointer = await session.AcquirePointerShapeAsync(
			CursesPointerShape.Pointer
		);
		Assert.Contains( "\u001b]22;pointer\u001b\\", output.Text );

		await session.DisposeAsync();

		Assert.Contains( "\u001b]22;\u001b\\", output.Text );
		await pointer.DisposeAsync();
		await pointer.DisposeAsync();
		Assert.Same( region, router.FocusedRegion );
		Assert.Equal(
			command,
			router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command
		);
	}

	private static async ValueTask<CursesSession> OpenSessionAsync(
		RecordingTerminalControlProvider provider,
		RecordingOutput output
	) {
		ArgumentNullException.ThrowIfNull( provider );
		ArgumentNullException.ThrowIfNull( output );
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			provider,
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
		return await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
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

		internal TerminalSize Size {
			get;
			set;
		} = new( 80, 24 );

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
			return TerminalControlResult<TerminalSize>.Available( this.Size );
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
