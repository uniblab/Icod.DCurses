namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>
/// Keyboard and unified event-loop input for <see cref="CursesSession"/>.
/// </summary>
public sealed partial class CursesSession {
	private static readonly TimeSpan DefaultEscapeDelay =
		TimeSpan.FromMilliseconds( 100 );

	private readonly SemaphoreSlim eventReadGate = new( 1, 1 );

	private CursesInputDecoder? inputDecoder;
	private Task<CursesInputEvent>? pendingInputEvent;
	private Task<CursesLifecycleEvent>? pendingLifecycleEvent;

	/// <summary>
	/// Gets the bounded delay used to distinguish an isolated Escape key from a fragmented
	/// escape-prefixed terminfo key sequence.
	/// </summary>
	/// <remarks>
	/// If bytes already buffered form a complete key sequence, they are decoded immediately.
	/// Otherwise an Escape byte which is also a prefix of a configured terminfo key sequence
	/// waits up to 100 milliseconds for continuation bytes before becoming an isolated Escape key.
	/// </remarks>
	public static TimeSpan DefaultEscapeSequenceTimeout => DefaultEscapeDelay;

	/// <summary>
	/// Waits indefinitely for keyboard input or a managed lifecycle notification.
	/// </summary>
	/// <remarks>
	/// This unified reader and <see cref="ReadLifecycleEventAsync(CancellationToken)"/> consume the
	/// same lifecycle queue and therefore should not be used concurrently by different consumers.
	/// Caller cancellation ends only the wait; a terminal read already in progress is retained for
	/// the next call so fragmented input is not discarded and concurrent terminal reads are avoided.
	/// </remarks>
	/// <param name="cancellationToken">Cancellation for this wait only.</param>
	/// <returns>The next input or lifecycle event.</returns>
	public ValueTask<CursesEvent> ReadEventAsync(
		CancellationToken cancellationToken = default) {
		return ReadEventCoreAsync(
			timeout: null,
			cancellationToken
		);
	}

	/// <summary>
	/// Waits for keyboard input or a managed lifecycle notification for at most the supplied interval.
	/// </summary>
	/// <param name="timeout">
	/// A nonnegative timeout, or <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
	/// </param>
	/// <param name="cancellationToken">Cancellation for this wait only.</param>
	/// <returns>An input, lifecycle, or timeout event.</returns>
	public ValueTask<CursesEvent> ReadEventAsync(
		TimeSpan timeout,
		CancellationToken cancellationToken = default) {
		if ( Timeout.InfiniteTimeSpan == timeout ) {
			return ReadEventCoreAsync(
				timeout: null,
				cancellationToken
			);
		}

		if ( TimeSpan.Zero > timeout ) {
			throw new ArgumentOutOfRangeException( nameof( timeout ) );
		}

		return ReadEventCoreAsync(
			timeout,
			cancellationToken
		);
	}

	/// <summary>
	/// Waits for keyboard input or a managed lifecycle notification until the supplied UTC-aware deadline.
	/// </summary>
	/// <param name="deadline">The absolute deadline.</param>
	/// <param name="cancellationToken">Cancellation for this wait only.</param>
	/// <returns>An input, lifecycle, or timeout event.</returns>
	public ValueTask<CursesEvent> ReadEventAsync(
		DateTimeOffset deadline,
		CancellationToken cancellationToken = default) {
		TimeSpan remaining =
			deadline - DateTimeOffset.UtcNow;

		if ( TimeSpan.Zero > remaining ) {
			remaining = TimeSpan.Zero;
		}

		return ReadEventAsync(
			remaining,
			cancellationToken
		);
	}

	private async ValueTask<CursesEvent> ReadEventCoreAsync(
		TimeSpan? timeout,
		CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		await eventReadGate.WaitAsync(
			cancellationToken
		).ConfigureAwait( false );

		try {
			Task<CursesInputEvent> inputTask =
				GetPendingInputEvent();
			Task<CursesLifecycleEvent> lifecycleTask =
				GetPendingLifecycleEvent();

			if ( lifecycleTask.IsCompleted ) {
				return CursesEvent.FromLifecycle(
					await CompleteLifecycleEventAsync(
						lifecycleTask
					).ConfigureAwait( false )
				);
			}

			if ( inputTask.IsCompleted ) {
				return CursesEvent.FromInput(
					await CompleteInputEventAsync(
						inputTask
					).ConfigureAwait( false )
				);
			}

			if ( TimeSpan.Zero == timeout ) {
				return CursesEvent.TimedOut();
			}

			using CancellationTokenSource waitCancellation =
				CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken
				);

			Task waitTask = Task.Delay(
				timeout
					?? Timeout.InfiniteTimeSpan,
				waitCancellation.Token
			);

			Task completed = await Task.WhenAny(
				lifecycleTask,
				inputTask,
				waitTask
			).ConfigureAwait( false );

			if ( ReferenceEquals( completed, lifecycleTask ) ) {
				waitCancellation.Cancel();
				return CursesEvent.FromLifecycle(
					await CompleteLifecycleEventAsync(
						lifecycleTask
					).ConfigureAwait( false )
				);
			}

			if ( ReferenceEquals( completed, inputTask ) ) {
				waitCancellation.Cancel();
				return CursesEvent.FromInput(
					await CompleteInputEventAsync(
						inputTask
					).ConfigureAwait( false )
				);
			}

			cancellationToken.ThrowIfCancellationRequested();
			return CursesEvent.TimedOut();
		} finally {
			eventReadGate.Release();
		}
	}

	private Task<CursesInputEvent> GetPendingInputEvent() {
		inputDecoder ??= new CursesInputDecoder(
			Backend.Input,
			Terminal,
			DefaultEscapeDelay
		);

		pendingInputEvent ??= inputDecoder.ReadAsync(
			lifecycleStop.Token
		).AsTask();

		return pendingInputEvent;
	}

	private Task<CursesLifecycleEvent> GetPendingLifecycleEvent() {
		pendingLifecycleEvent ??= lifecycleEvents.Reader.ReadAsync(
			lifecycleStop.Token
		).AsTask();

		return pendingLifecycleEvent;
	}

	private async ValueTask<CursesInputEvent> CompleteInputEventAsync(
		Task<CursesInputEvent> inputTask) {
		try {
			return await inputTask.ConfigureAwait( false );
		} finally {
			if ( ReferenceEquals( pendingInputEvent, inputTask ) ) {
				pendingInputEvent = null;
			}
		}
	}

	private async ValueTask<CursesLifecycleEvent> CompleteLifecycleEventAsync(
		Task<CursesLifecycleEvent> lifecycleTask) {
		try {
			return await lifecycleTask.ConfigureAwait( false );
		} finally {
			if ( ReferenceEquals( pendingLifecycleEvent, lifecycleTask ) ) {
				pendingLifecycleEvent = null;
			}
		}
	}
}