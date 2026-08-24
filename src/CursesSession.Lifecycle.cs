namespace Icod.DCurses;

using System.Threading.Channels;
using Icod.DCurses.Terminal;

/// <summary>
/// Terminal and process lifecycle handling for <see cref="CursesSession"/>.
/// </summary>
public sealed partial class CursesSession {
	private readonly object lifecycleSync = new();
	private readonly CancellationTokenSource lifecycleStop = new();
	private readonly CancellationTokenSource termination = new();
	private readonly Channel<CursesLifecycleEvent> lifecycleEvents =
		Channel.CreateUnbounded<CursesLifecycleEvent>(
			new UnboundedChannelOptions {
				SingleReader = false,
				SingleWriter = true,
				AllowSynchronousContinuations = false
			}
		);

	private readonly ITerminalLifecycleSource? lifecycleSource;
	private Task? lifecyclePumpTask;
	private Task? lifecycleStopTask;
	private TerminalSize? lastResizeNotificationDimensions;
	private int presentationSuspended;

	/// <summary>
	/// Gets a token canceled after an interactive interrupt or termination request is observed.
	/// </summary>
	public CancellationToken TerminationToken => termination.Token;

	/// <summary>
	/// Waits for the next managed terminal or process lifecycle event.
	/// </summary>
	/// <param name="cancellationToken">Cancellation for this wait only.</param>
	/// <returns>The next lifecycle event.</returns>
	public ValueTask<CursesLifecycleEvent> ReadLifecycleEventAsync(
		CancellationToken cancellationToken = default
	) {
		return lifecycleEvents.Reader.ReadAsync( cancellationToken );
	}

	private void StartLifecyclePump() {
		if ( null == lifecycleSource ) {
			return;
		}

		lock ( lifecycleSync ) {
			lifecyclePumpTask ??= Task.Run( RunLifecyclePumpAsync );
		}
	}

	private ValueTask StopLifecycleAsync() {
		lock ( lifecycleSync ) {
			lifecycleStopTask ??= StopLifecycleOnceAsync();
			return new ValueTask( lifecycleStopTask );
		}
	}

	private async Task StopLifecycleOnceAsync() {
		lifecycleStop.Cancel();
		lifecycleSource?.Dispose();

		Task? pumpTask;
		lock ( lifecycleSync ) {
			pumpTask = lifecyclePumpTask;
		}

		if ( null != pumpTask ) {
			await pumpTask.ConfigureAwait( false );
		}

		lifecycleEvents.Writer.TryComplete();
	}

	private async Task RunLifecyclePumpAsync() {
		try {
			while ( true ) {
				lifecycleStop.Token.ThrowIfCancellationRequested();

				TerminalLifecycleSignal signal =
					await lifecycleSource!.ReadAsync(
						lifecycleStop.Token
					).ConfigureAwait( false );

				await HandleLifecycleSignalAsync(
					signal
				).ConfigureAwait( false );
			}
		} catch ( OperationCanceledException ) when ( lifecycleStop.IsCancellationRequested ) {
		} catch ( ChannelClosedException ) when ( lifecycleStop.IsCancellationRequested ) {
		} catch ( Exception exception ) {
			TryCancelTermination();
			lifecycleEvents.Writer.TryComplete( exception );
		}
	}

	private async ValueTask HandleLifecycleSignalAsync(
		TerminalLifecycleSignal signal
	) {
		switch ( signal.Kind ) {
			case TerminalLifecycleSignalKind.Resize:
				HandleResizeSignal();
				break;

			case TerminalLifecycleSignalKind.Interrupt:
				TryCancelTermination();
				PublishLifecycleEvent( CursesLifecycleEventKind.Interrupt );
				break;

			case TerminalLifecycleSignalKind.Termination:
				TryCancelTermination();
				PublishLifecycleEvent( CursesLifecycleEventKind.Termination );
				break;

			case TerminalLifecycleSignalKind.Suspend:
				await HandleSuspendAsync().ConfigureAwait( false );
				break;

			case TerminalLifecycleSignalKind.Resume:
				await HandleResumeAsync().ConfigureAwait( false );
				break;

			default:
				throw new ArgumentOutOfRangeException(
					nameof( signal ),
					signal.Kind,
					"Unknown terminal lifecycle signal."
				);
		}
	}

	private async ValueTask HandleSuspendAsync() {
		await SuspendPresentationAsync().ConfigureAwait( false );
		PublishLifecycleEvent( CursesLifecycleEventKind.Suspending );

		if ( lifecycleSource is not ITerminalSuspendController suspendController ) {
			await ResumePresentationAsync().ConfigureAwait( false );
			throw new NotSupportedException(
				"The terminal lifecycle source cannot complete a process suspension."
			);
		}

		TerminalBackendMutationResult result =
			suspendController.SuspendCurrentProcess();

		if ( result.Succeeded ) {
			return;
		}

		await ResumePresentationAsync().ConfigureAwait( false );
		throw new InvalidOperationException(
			result.Message
				?? "The process could not be suspended after terminal restoration."
		);
	}

	private async ValueTask HandleResumeAsync() {
		await ResumePresentationAsync().ConfigureAwait( false );
		TerminalSize? dimensions = SynchronizeLifecycleDimensions();
		lastResizeNotificationDimensions = dimensions;
		InvalidatePhysicalScreen();
		PublishLifecycleEvent(
			CursesLifecycleEventKind.Resumed,
			dimensions
		);
	}

	private async ValueTask SuspendPresentationAsync() {
		if ( 0 != Interlocked.CompareExchange(
			ref presentationSuspended,
			1,
			0
		) ) {
			return;
		}

		List<Exception> exceptions = [];

		try {
			await ResetRefreshRenditionAsync().ConfigureAwait( false );
		} catch ( Exception exception ) {
			exceptions.Add( exception );
		}

		await TryRestoreCapabilityAsync(
			cursorRestore,
			exceptions
		).ConfigureAwait( false );
		await TryRestoreCapabilityAsync(
			keypadRestore,
			exceptions
		).ConfigureAwait( false );
		await TryRestoreCapabilityAsync(
			alternateScreenRestore,
			exceptions
		).ConfigureAwait( false );

		if ( HasPresentationState ) {
			try {
				await Backend.Output.FlushAsync(
					CancellationToken.None
				).ConfigureAwait( false );
			} catch ( Exception exception ) {
				exceptions.Add( exception );
			}
		}

		if ( modeRestoreRequired && null != capturedMode ) {
			try {
				TerminalBackendMutationResult result =
					sessionModes.RestoreMode(
						capturedMode,
						TerminalModeApplyTiming.AfterOutputDrained
					);

				if ( !result.Succeeded ) {
					exceptions.Add(
						new InvalidOperationException(
							result.Message
								?? "The original host terminal mode could not be restored before suspension."
						)
					);
				}
			} catch ( Exception exception ) {
				exceptions.Add( exception );
			}
		}

		Exception? restorationException =
			BuildRestorationException( exceptions );

		if ( null == restorationException ) {
			return;
		}

		Interlocked.Exchange( ref presentationSuspended, 0 );
		throw restorationException;
	}

	private async ValueTask ResumePresentationAsync() {
		if ( 1 != Interlocked.CompareExchange(
			ref presentationSuspended,
			0,
			1
		) ) {
			return;
		}

		try {
			if ( null == capturedMode ) {
				throw new InvalidOperationException(
					"The captured host terminal mode is unavailable during resume."
				);
			}

			TerminalBackendMutationResult modeResult =
				sessionModes.ApplySessionMode(
					capturedMode,
					Options.InputMode,
					Options.EchoInput
				);

			if ( !modeResult.Succeeded ) {
				throw new InvalidOperationException(
					modeResult.Message
						?? "The curses terminal mode could not be restored after resume."
				);
			}

			await EnterAlternateScreenAsync(
				CancellationToken.None
			).ConfigureAwait( false );
			await EnterKeypadAsync(
				CancellationToken.None
			).ConfigureAwait( false );
			await HideCursorAsync(
				CancellationToken.None
			).ConfigureAwait( false );

			if ( HasPresentationState ) {
				await Backend.Output.FlushAsync(
					CancellationToken.None
				).ConfigureAwait( false );
			}
		} catch {
			Interlocked.Exchange( ref presentationSuspended, 1 );
			throw;
		}
	}

	private void HandleResizeSignal() {
		TerminalSize? dimensions = SynchronizeLifecycleDimensions();
		if ( dimensions.HasValue
			&& lastResizeNotificationDimensions.HasValue
			&& dimensions.Value == lastResizeNotificationDimensions.Value ) {
			return;
		}

		lastResizeNotificationDimensions = dimensions;
		InvalidatePhysicalScreen();
		PublishLifecycleEvent(
			CursesLifecycleEventKind.Resize,
			dimensions
		);
	}

	private TerminalSize? SynchronizeLifecycleDimensions() {
		TerminalBackendResult<TerminalSize> result = SynchronizeDimensions();
		return result.IsAvailable
			? result.GetRequiredValue()
			: null
		;
	}

	private void PublishLifecycleEvent(
		CursesLifecycleEventKind kind,
		TerminalSize? dimensions = null
	) {
		lifecycleEvents.Writer.TryWrite(
			new CursesLifecycleEvent(
				kind,
				dimensions
			)
		);
	}

	private void TryCancelTermination() {
		try {
			termination.Cancel();
		} catch ( ObjectDisposedException ) {
		}
	}
}
