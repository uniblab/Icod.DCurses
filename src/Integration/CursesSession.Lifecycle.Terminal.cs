namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>Adapts Terminal lifecycle events and coordinates DCurses rendition around suspension.</summary>
public sealed partial class CursesSession {
	/// <summary>Gets whether the underlying Terminal session observes host lifecycle events.</summary>
	public bool SupportsLifecycleEvents {
		get {
			return this.terminalSession.SupportsLifecycleEvents;
		}
	}

	/// <summary>Gets the underlying Terminal session termination token.</summary>
	public CancellationToken TerminationToken {
		get {
			return this.terminalSession.TerminationToken;
		}
	}

	/// <summary>Waits for the next normalized lifecycle event.</summary>
	public async ValueTask<CursesLifecycleEvent> ReadLifecycleEventAsync(
		CancellationToken cancellationToken = default
	) {
		TerminalLifecycleEvent terminalEvent = await this.terminalSession.ReadLifecycleEventAsync(
			cancellationToken
		).ConfigureAwait( false );
		return this.ConvertLifecycleEvent( terminalEvent );
	}

	private CursesLifecycleEvent ConvertLifecycleEvent(
		TerminalLifecycleEvent terminalEvent
	) {
		ArgumentNullException.ThrowIfNull( terminalEvent );

		CursesLifecycleEventKind kind = terminalEvent.Kind switch {
			TerminalLifecycleEventKind.Resize => CursesLifecycleEventKind.Resize,
			TerminalLifecycleEventKind.Interrupt => CursesLifecycleEventKind.Interrupt,
			TerminalLifecycleEventKind.Termination => CursesLifecycleEventKind.Termination,
			TerminalLifecycleEventKind.Suspending => CursesLifecycleEventKind.Suspending,
			TerminalLifecycleEventKind.Resumed => CursesLifecycleEventKind.Resumed,
			_ => throw new ArgumentOutOfRangeException(
				nameof( terminalEvent ),
				terminalEvent.Kind,
				"The Terminal lifecycle-event kind is not recognized."
			)
		};

		if (
			terminalEvent.Kind is TerminalLifecycleEventKind.Resize or TerminalLifecycleEventKind.Resumed
			&& terminalEvent.Size.HasValue
		) {
			_ = this.ResizeLogicalScreen(
				terminalEvent.Size.Value.Columns,
				terminalEvent.Size.Value.Rows
			);
		}
		if (
			terminalEvent.Kind is TerminalLifecycleEventKind.Resize or TerminalLifecycleEventKind.Resumed
		) {
			this.InvalidatePhysicalScreen();
		}

		return new CursesLifecycleEvent( kind, terminalEvent.Size );
	}

	private sealed class CursesTerminalLifecycleParticipant
		: ITerminalSessionLifecycleParticipant {
		private readonly CursesSession owner;
		private readonly SemaphoreSlim callbackGate = new( 1, 1 );

		private IDisposable? suspendedActivity;
		private bool closed;

		internal CursesTerminalLifecycleParticipant(
			CursesSession owner
		) {
			ArgumentNullException.ThrowIfNull( owner );
			this.owner = owner;
		}

		public async ValueTask PrepareForTerminalSuspendAsync(
			CancellationToken cancellationToken = default
		) {
			await this.callbackGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
			try {
				if ( this.closed || this.suspendedActivity is not null ) {
					return;
				}

				await this.owner.terminalActivityGate.WaitAsync(
					cancellationToken
				).ConfigureAwait( false );
				this.suspendedActivity = new TerminalActivityLease(
					this.owner.terminalActivityGate
				);

				try {
					await this.owner.ResetRefreshRenditionAsync().ConfigureAwait( false );
				} catch {
					this.suspendedActivity.Dispose();
					this.suspendedActivity = null;
					throw;
				}
			} finally {
				this.callbackGate.Release();
			}
		}

		public async ValueTask ResumeAfterTerminalSuspendAsync(
			CancellationToken cancellationToken = default
		) {
			await this.callbackGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
			try {
				this.owner.InvalidatePhysicalScreen();
				this.suspendedActivity?.Dispose();
				this.suspendedActivity = null;
			} finally {
				this.callbackGate.Release();
			}
		}

		internal async ValueTask CloseAsync() {
			await this.callbackGate.WaitAsync( CancellationToken.None ).ConfigureAwait( false );
			try {
				if ( this.closed ) {
					return;
				}

				this.closed = true;
				this.suspendedActivity?.Dispose();
				this.suspendedActivity = null;
			} finally {
				this.callbackGate.Release();
			}
		}
	}
}
