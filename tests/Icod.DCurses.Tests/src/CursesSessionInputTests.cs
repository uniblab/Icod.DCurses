using System.Text;
using System.Threading.Channels;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesSessionInputTests {
	[Fact]
	public async Task ResizeLifecycleNotificationWakesUnifiedEventWait() {
		ControllableTerminalInput input = new();
		FakeLifecycleSource lifecycle = new();
		TerminalBackend backend = CreateBackend(
			input,
			new TerminalSize( 132, 43 )
		);

		await using CursesSession session = await CursesSession.OpenAsync(
			backend,
			options: null,
			lifecycleSource: lifecycle,
			cancellationToken: CancellationToken.None
		);

		Task<CursesEvent> waiting = session.ReadEventAsync(
			TimeSpan.FromSeconds( 2 )
		).AsTask();

		lifecycle.Publish( TerminalLifecycleSignalKind.Resize );

		CursesEvent result = await waiting;

		Assert.Equal( CursesEventKind.Lifecycle, result.Kind );
		CursesLifecycleEvent lifecycleEvent =
			Assert.IsType<CursesLifecycleEvent>( result.Lifecycle );
		Assert.Equal( CursesLifecycleEventKind.Resize, lifecycleEvent.Kind );
		Assert.Equal( 132, lifecycleEvent.Dimensions!.Value.Columns );
		Assert.Equal( 43, lifecycleEvent.Dimensions!.Value.Rows );
		Assert.True( result.RequiresRepaint );
	}

	[Fact]
	public async Task TimeoutDoesNotDiscardPendingTerminalRead() {
		ControllableTerminalInput input = new();
		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend( input )
		);

		CursesEvent timedOut = await session.ReadEventAsync( TimeSpan.Zero );

		input.Supply( [ (byte)'q' ] );

		CursesEvent inputEvent = await session.ReadEventAsync(
			TimeSpan.FromSeconds( 1 )
		);

		Assert.Equal( CursesEventKind.Timeout, timedOut.Kind );
		Assert.Equal( CursesEventKind.Input, inputEvent.Kind );
		Assert.Equal( new Rune( 'q' ), inputEvent.Input!.Character );
		Assert.Equal( 1, input.ReadCount );
	}

	[Fact]
	public async Task CallerCancellationDoesNotCancelOrDuplicatePendingTerminalRead() {
		ControllableTerminalInput input = new();
		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend( input )
		);
		using CancellationTokenSource cancellation = new();
		Task<CursesEvent> waiting = session.ReadEventAsync(
			cancellation.Token
		).AsTask();

		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			async () => {
				_ = await waiting;
			}
		);

		input.Supply( [ (byte)'x' ] );

		CursesEvent inputEvent = await session.ReadEventAsync(
			TimeSpan.FromSeconds( 1 )
		);

		Assert.Equal( new Rune( 'x' ), inputEvent.Input!.Character );
		Assert.Equal( 1, input.ReadCount );
	}

	[Fact]
	public async Task PastDeadlineReturnsTimeoutWithoutBlocking() {
		await using CursesSession session = await CursesSession.OpenAsync(
			CreateBackend(
				new ControllableTerminalInput()
			)
		);

		CursesEvent inputEvent = await session.ReadEventAsync(
			DateTimeOffset.UtcNow - TimeSpan.FromSeconds( 1 )
		);

		Assert.Equal( CursesEventKind.Timeout, inputEvent.Kind );
	}

	private static TerminalBackend CreateBackend(
		ITerminalInput input,
		TerminalSize? size = null) {
		ArgumentNullException.ThrowIfNull( input );

		return new TerminalBackend(
			new TerminalEndpoint( "input", true ),
			new TerminalEndpoint( "output", true ),
			new TerminalDescriptionBuilder( "event-test" ).Build(),
			input,
			new FakeTerminalOutput(),
			new FakeDimensionProvider(
				size
					?? new TerminalSize( 80, 24 )
			),
			new FakeSessionModeController()
		);
	}

	private sealed class ControllableTerminalInput
		: ITerminalInput {
		private readonly Channel<byte[]> chunks =
			Channel.CreateUnbounded<byte[]>();
		private int readCount;

		internal int ReadCount =>
			Volatile.Read( ref readCount );

		internal void Supply( byte[] bytes ) {
			ArgumentNullException.ThrowIfNull( bytes );
			if ( !chunks.Writer.TryWrite( bytes.ToArray() ) ) {
				throw new InvalidOperationException(
					"The input channel is closed."
				);
			}
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default) {
			Interlocked.Increment( ref readCount );

			byte[] bytes = await chunks.Reader.ReadAsync(
				cancellationToken
			).ConfigureAwait( false );

			if ( bytes.Length > buffer.Length ) {
				throw new InvalidOperationException(
					"The supplied input exceeds the decoder read buffer."
				);
			}

			bytes.AsSpan().CopyTo( buffer.Span );
			return bytes.Length;
		}
	}

	private sealed class FakeLifecycleSource
		: ITerminalLifecycleSource {
		private readonly Channel<TerminalLifecycleSignal> signals =
			Channel.CreateUnbounded<TerminalLifecycleSignal>();

		internal void Publish( TerminalLifecycleSignalKind kind ) {
			if ( !signals.Writer.TryWrite(
				new TerminalLifecycleSignal( kind )
			) ) {
				throw new InvalidOperationException(
					"The lifecycle source is closed."
				);
			}
		}

		public ValueTask<TerminalLifecycleSignal> ReadAsync(
			CancellationToken cancellationToken = default) {
			return signals.Reader.ReadAsync( cancellationToken );
		}

		public void Dispose() {
			signals.Writer.TryComplete();
		}
	}

	private sealed class FakeTerminalOutput
		: ITerminalOutput {
		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeDimensionProvider
		: ITerminalDimensionProvider {
		private readonly TerminalSize size;

		internal FakeDimensionProvider( TerminalSize size ) {
			this.size = size;
		}

		public TerminalBackendResult<TerminalSize> GetDimensions() {
			return TerminalBackendResult<TerminalSize>.Available( size );
		}
	}

	private sealed class FakeSessionModeController
		: ITerminalSessionModeController {
		public TerminalBackendResult<ITerminalModeState> CaptureMode() {
			return TerminalBackendResult<ITerminalModeState>.Available(
				new FakeModeState()
			);
		}

		public TerminalBackendMutationResult ApplySessionMode(
			ITerminalModeState baseline,
			CursesInputMode inputMode,
			bool echoInput) {
			ArgumentNullException.ThrowIfNull( baseline );
			return TerminalBackendMutationResult.Success();
		}

		public TerminalBackendMutationResult RestoreMode(
			ITerminalModeState state,
			TerminalModeApplyTiming timing) {
			ArgumentNullException.ThrowIfNull( state );
			return TerminalBackendMutationResult.Success();
		}
	}

	private sealed class FakeModeState
		: ITerminalModeState {
	}
}