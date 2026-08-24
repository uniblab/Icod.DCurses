namespace Icod.DCurses.Terminal;

/// <summary>
/// Identifies one low-level lifecycle notification delivered to the curses session owner.
/// </summary>
internal enum TerminalLifecycleSignalKind {
	Resize,
	Interrupt,
	Termination,
	Suspend,
	Resume
}

/// <summary>
/// Represents one low-level lifecycle notification.
/// </summary>
internal readonly record struct TerminalLifecycleSignal {
	internal TerminalLifecycleSignal( TerminalLifecycleSignalKind kind ) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		Kind = kind;
	}

	internal TerminalLifecycleSignalKind Kind {
		get;
	}
}

/// <summary>
/// Supplies queued host lifecycle notifications without exposing operating-system signal APIs.
/// </summary>
internal interface ITerminalLifecycleSource
	: IDisposable {
	ValueTask<TerminalLifecycleSignal> ReadAsync(
		CancellationToken cancellationToken = default
	);
}

/// <summary>
/// Completes a previously intercepted suspend request after curses has restored terminal state.
/// </summary>
internal interface ITerminalSuspendController {
	TerminalBackendMutationResult SuspendCurrentProcess();
}
