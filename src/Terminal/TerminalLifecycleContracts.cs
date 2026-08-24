namespace Icod.DCurses.Terminal;

/// <summary>
/// Identifies one low-level lifecycle notification delivered to the curses session owner.
/// </summary>
internal enum TerminalLifecycleSignalKind {
	/// <summary>The terminal dimensions may have changed.</summary>
	Resize,

	/// <summary>An interactive interrupt request was observed.</summary>
	Interrupt,

	/// <summary>A process termination request was observed.</summary>
	Termination,

	/// <summary>A process suspension request was observed.</summary>
	Suspend,

	/// <summary>The process resumed after suspension.</summary>
	Resume
}

/// <summary>
/// Represents one low-level lifecycle notification.
/// </summary>
internal readonly record struct TerminalLifecycleSignal {
	/// <summary>Initializes one low-level lifecycle signal.</summary>
	/// <param name="kind">The lifecycle signal kind.</param>
	internal TerminalLifecycleSignal( TerminalLifecycleSignalKind kind ) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		Kind = kind;
	}

	/// <summary>Gets the lifecycle signal kind.</summary>
	internal TerminalLifecycleSignalKind Kind {
		get;
	}
}

/// <summary>
/// Supplies queued host lifecycle notifications without exposing operating-system signal APIs.
/// </summary>
internal interface ITerminalLifecycleSource
	: IDisposable {
	/// <summary>Waits for the next queued host lifecycle signal.</summary>
	/// <param name="cancellationToken">Cancellation for this wait.</param>
	/// <returns>The next lifecycle signal.</returns>
	ValueTask<TerminalLifecycleSignal> ReadAsync(
		CancellationToken cancellationToken = default
	);
}

/// <summary>
/// Completes a previously intercepted suspend request after curses has restored terminal state.
/// </summary>
internal interface ITerminalSuspendController {
	/// <summary>Completes suspension of the current process after terminal restoration.</summary>
	/// <returns>The controlled suspension result.</returns>
	TerminalBackendMutationResult SuspendCurrentProcess();
}
