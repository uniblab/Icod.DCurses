using System.Text;
using System.Threading.Channels;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesSessionLifecycleTests {
	[Fact]
	public async Task ResizePublishesFreshDimensionsAndRequestsRepaint() {
		List<string> events = [];
		FakeLifecycleSource lifecycle = new( events );
		FakeDimensionProvider dimensions = new(
			new TerminalSize( 80, 24 )
		);

		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend(
				events,
				dimensions
			),
			options: null,
			lifecycleSource: lifecycle,
			cancellationToken: CancellationToken.None
		);

		dimensions.Current = new TerminalSize( 132, 43 );
		lifecycle.Publish( TerminalLifecycleSignalKind.Resize );

		CursesLifecycleEvent lifecycleEvent =
			await session.ReadLifecycleEventAsync();

		Assert.Equal(
			CursesLifecycleEventKind.Resize,
			lifecycleEvent.Kind
		);
		Assert.True( lifecycleEvent.RequiresRepaint );
		Assert.Equal(
			new TerminalSize( 132, 43 ),
			lifecycleEvent.Dimensions
		);
		Assert.Equal( 1, dimensions.QueryCount );
	}

	[Fact]
	public async Task InterruptPublishesEventAndCancelsTerminationToken() {
		List<string> events = [];
		FakeLifecycleSource lifecycle = new( events );

		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend(
				events,
				new FakeDimensionProvider(
					new TerminalSize( 80, 24 )
				)
			),
			options: null,
			lifecycleSource: lifecycle,
			cancellationToken: CancellationToken.None
		);

		lifecycle.Publish( TerminalLifecycleSignalKind.Interrupt );

		CursesLifecycleEvent lifecycleEvent =
			await session.ReadLifecycleEventAsync();

		Assert.Equal(
			CursesLifecycleEventKind.Interrupt,
			lifecycleEvent.Kind
		);
		Assert.False( lifecycleEvent.RequiresRepaint );
		Assert.True( session.TerminationToken.IsCancellationRequested );
	}

	[Fact]
	public async Task SuspendRestoresBeforeStopAndResumeReentersPresentation() {
		List<string> events = [];
		FakeLifecycleSource lifecycle = new(
			events,
			autoResume: true
		);
		FakeDimensionProvider dimensions = new(
			new TerminalSize( 80, 24 )
		);
		FakeSessionModeController modes = new( events );
		FakeTerminalOutput output = new( events );

		CursesSession session = await CursesSession.OpenAsync(
			CreateBackend(
				events,
				dimensions,
				modes,
				output
			),
			options: null,
			lifecycleSource: lifecycle,
			cancellationToken: CancellationToken.None
		);

		events.Clear();
		dimensions.Current = new TerminalSize( 100, 31 );
		lifecycle.Publish( TerminalLifecycleSignalKind.Suspend );

		CursesLifecycleEvent suspending =
			await session.ReadLifecycleEventAsync();
		CursesLifecycleEvent resumed =
			await session.ReadLifecycleEventAsync();

		Assert.Equal(
			CursesLifecycleEventKind.Suspending,
			suspending.Kind
		);
		Assert.Equal(
			CursesLifecycleEventKind.Resumed,
			resumed.Kind
		);
		Assert.True( resumed.RequiresRepaint );
		Assert.Equal(
			new TerminalSize( 100, 31 ),
			resumed.Dimensions
		);

		Assert.Equal(
			new[] {
				"write:<show>",
				"write:</keypad>",
				"write:</alternate>",
				"flush",
				"mode:restore:AfterOutputDrained",
				"process:suspend",
				"mode:apply:CBreak:False",
				"write:<alternate>",
				"write:<keypad>",
				"write:<hide>",
				"flush"
			},
			events
		);

		Assert.Equal( 1, dimensions.QueryCount );
		Assert.Equal( 2, modes.ApplyCount );
		Assert.Equal( 1, modes.RestoreCount );

		await session.DisposeAsync();

		Assert.Equal( 2, modes.RestoreCount );
		Assert.True( lifecycle.Disposed );
	}

	[Fact]
	public async Task TerminationPublishesEventAndCancelsTerminationToken() {
		List<string> events = [];
		FakeLifecycleSource lifecycle = new( events );

		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend(
				events,
				new FakeDimensionProvider(
					new TerminalSize( 80, 24 )
				)
			),
			options: null,
			lifecycleSource: lifecycle,
			cancellationToken: CancellationToken.None
		);

		lifecycle.Publish( TerminalLifecycleSignalKind.Termination );

		CursesLifecycleEvent lifecycleEvent =
			await session.ReadLifecycleEventAsync();

		Assert.Equal(
			CursesLifecycleEventKind.Termination,
			lifecycleEvent.Kind
		);
		Assert.True( session.TerminationToken.IsCancellationRequested );
	}

	private static TerminalBackend CreateBackend(
		IList<string> events,
		FakeDimensionProvider dimensions,
		FakeSessionModeController? modes = null,
		FakeTerminalOutput? output = null
	) {
		ArgumentNullException.ThrowIfNull( events );
		ArgumentNullException.ThrowIfNull( dimensions );

		return new TerminalBackend(
			new TerminalEndpoint( "test input", true ),
			new TerminalEndpoint( "test output", true ),
			CreateTerminal(),
			new FakeTerminalInput(),
			output
				?? new FakeTerminalOutput( events ),
			dimensions,
			modes
				?? new FakeSessionModeController( events )
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "test-terminal" )
			.SetString(
				StringCapability.EnterCursorAddressingMode,
				"<alternate>"
			)
			.SetString(
				StringCapability.ExitCursorAddressingMode,
				"</alternate>"
			)
			.SetString(
				StringCapability.EnterKeypadMode,
				"<keypad>"
			)
			.SetString(
				StringCapability.ExitKeypadMode,
				"</keypad>"
			)
			.SetString(
				StringCapability.CursorInvisible,
				"<hide>"
			)
			.SetString(
				StringCapability.CursorNormal,
				"<show>"
			)
			.Build();
	}

	private sealed class FakeLifecycleSource
		: ITerminalLifecycleSource,
		  ITerminalSuspendController {
		private readonly IList<string> events;
		private readonly Channel<TerminalLifecycleSignal> signals =
			Channel.CreateUnbounded<TerminalLifecycleSignal>();
		private readonly bool autoResume;
		private int disposed;

		internal FakeLifecycleSource(
			IList<string> events,
			bool autoResume = false
		) {
			ArgumentNullException.ThrowIfNull( events );
			this.events = events;
			this.autoResume = autoResume;
		}

		internal bool Disposed =>
			0 != Volatile.Read( ref disposed );

		internal void Publish( TerminalLifecycleSignalKind kind ) {
			signals.Writer.TryWrite(
				new TerminalLifecycleSignal( kind )
			);
		}

		public ValueTask<TerminalLifecycleSignal> ReadAsync(
			CancellationToken cancellationToken = default
		) {
			return signals.Reader.ReadAsync( cancellationToken );
		}

		public TerminalBackendMutationResult SuspendCurrentProcess() {
			events.Add( "process:suspend" );

			if ( autoResume ) {
				Publish( TerminalLifecycleSignalKind.Resume );
			}

			return TerminalBackendMutationResult.Success();
		}

		public void Dispose() {
			if ( 0 != Interlocked.Exchange( ref disposed, 1 ) ) {
				return;
			}

			signals.Writer.TryComplete();
		}
	}

	private sealed class FakeTerminalInput
		: ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class FakeTerminalOutput
		: ITerminalOutput {
		private readonly IList<string> events;

		internal FakeTerminalOutput( IList<string> events ) {
			ArgumentNullException.ThrowIfNull( events );
			this.events = events;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			events.Add(
				"write:"
					+ Encoding.Latin1.GetString( buffer.Span )
			);
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			events.Add( "flush" );
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeDimensionProvider
		: ITerminalDimensionProvider {
		internal FakeDimensionProvider( TerminalSize current ) {
			Current = current;
		}

		internal TerminalSize Current {
			get;
			set;
		}

		internal int QueryCount {
			get;
			private set;
		}

		public TerminalBackendResult<TerminalSize> GetDimensions() {
			QueryCount++;
			return TerminalBackendResult<TerminalSize>.Available( Current );
		}
	}

	private sealed class FakeSessionModeController
		: ITerminalSessionModeController {
		private readonly IList<string> events;

		internal FakeSessionModeController( IList<string> events ) {
			ArgumentNullException.ThrowIfNull( events );
			this.events = events;
		}

		internal int ApplyCount {
			get;
			private set;
		}

		internal int RestoreCount {
			get;
			private set;
		}

		public TerminalBackendResult<ITerminalModeState> CaptureMode() {
			events.Add( "mode:capture" );
			return TerminalBackendResult<ITerminalModeState>.Available(
				new FakeTerminalModeState()
			);
		}

		public TerminalBackendMutationResult ApplySessionMode(
			ITerminalModeState baseline,
			CursesInputMode inputMode,
			bool echoInput
		) {
			ArgumentNullException.ThrowIfNull( baseline );
			ApplyCount++;
			events.Add(
				$"mode:apply:{inputMode}:{echoInput}"
			);
			return TerminalBackendMutationResult.Success();
		}

		public TerminalBackendMutationResult RestoreMode(
			ITerminalModeState state,
			TerminalModeApplyTiming timing
		) {
			ArgumentNullException.ThrowIfNull( state );
			if ( !Enum.IsDefined( timing ) ) {
				throw new ArgumentOutOfRangeException( nameof( timing ) );
			}

			RestoreCount++;
			events.Add(
				$"mode:restore:{timing}"
			);
			return TerminalBackendMutationResult.Success();
		}
	}

	private sealed class FakeTerminalModeState
		: ITerminalModeState {
	}
}
