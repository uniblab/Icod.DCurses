namespace Icod.DCurses.Terminal;

using System.Runtime.InteropServices;
using System.Threading.Channels;

/// <summary>
/// Observes process and terminal lifecycle events for the supported desktop hosts.
/// </summary>
internal sealed partial class SystemTerminalLifecycleSource
	: ITerminalLifecycleSource,
	  ITerminalSuspendController {
	private const int LinuxSigTstp = 20;
	private const int MacOsSigTstp = 18;

	private readonly Channel<TerminalLifecycleSignal> signals;
	private readonly List<IDisposable> registrations = [];

	private ConsoleCancelEventHandler? consoleCancelHandler;
	private int allowSuspendDelivery;
	private int disposed;

	/// <summary>Initializes lifecycle observation for the current supported host.</summary>
	internal SystemTerminalLifecycleSource() {
		signals = Channel.CreateUnbounded<TerminalLifecycleSignal>(
			new UnboundedChannelOptions {
				SingleReader = true,
				SingleWriter = false,
				AllowSynchronousContinuations = false
			}
		);

		if ( OperatingSystem.IsWindows() ) {
			consoleCancelHandler = HandleConsoleCancel;
			Console.CancelKeyPress += consoleCancelHandler;
			return;
		}

		if ( !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() ) {
			return;
		}

		registrations.Add(
			PosixSignalRegistration.Create(
				PosixSignal.SIGWINCH,
				context => {
					context.Cancel = true;
					Publish( TerminalLifecycleSignalKind.Resize );
				}
			)
		);
		registrations.Add(
			PosixSignalRegistration.Create(
				PosixSignal.SIGCONT,
				context => {
					context.Cancel = false;
					Publish( TerminalLifecycleSignalKind.Resume );
				}
			)
		);
		registrations.Add(
			PosixSignalRegistration.Create(
				PosixSignal.SIGTSTP,
				HandleSuspendSignal
			)
		);

		RegisterTerminationSignal(
			PosixSignal.SIGINT,
			TerminalLifecycleSignalKind.Interrupt
		);
		RegisterTerminationSignal(
			PosixSignal.SIGTERM,
			TerminalLifecycleSignalKind.Termination
		);
		RegisterTerminationSignal(
			PosixSignal.SIGQUIT,
			TerminalLifecycleSignalKind.Termination
		);
		RegisterTerminationSignal(
			PosixSignal.SIGHUP,
			TerminalLifecycleSignalKind.Termination
		);
	}

	/// <inheritdoc />
	public ValueTask<TerminalLifecycleSignal> ReadAsync(
		CancellationToken cancellationToken = default
	) {
		return signals.Reader.ReadAsync( cancellationToken );
	}

	/// <inheritdoc />
	public TerminalBackendMutationResult SuspendCurrentProcess() {
		if ( 0 != Volatile.Read( ref disposed ) ) {
			return TerminalBackendMutationResult.Unavailable(
				"The terminal lifecycle source has already been disposed."
			);
		}

		int signalNumber;
		if ( OperatingSystem.IsLinux() ) {
			signalNumber = LinuxSigTstp;
		} else if ( OperatingSystem.IsMacOS() ) {
			signalNumber = MacOsSigTstp;
		} else {
			return TerminalBackendMutationResult.Unsupported(
				"Process suspension is only supported by the POSIX lifecycle source."
			);
		}

		Interlocked.Exchange( ref allowSuspendDelivery, 1 );

		try {
			int result = NativeRaise( signalNumber );
			if ( 0 == result ) {
				return TerminalBackendMutationResult.Success();
			}

			Interlocked.Exchange( ref allowSuspendDelivery, 0 );
			return TerminalBackendMutationResult.Failed(
				$"The host rejected the suspend signal with error code {result}."
			);
		} catch ( DllNotFoundException exception ) {
			Interlocked.Exchange( ref allowSuspendDelivery, 0 );
			return TerminalBackendMutationResult.Unsupported( exception.Message );
		} catch ( EntryPointNotFoundException exception ) {
			Interlocked.Exchange( ref allowSuspendDelivery, 0 );
			return TerminalBackendMutationResult.Unsupported( exception.Message );
		}
	}

	/// <inheritdoc />
	public void Dispose() {
		if ( 0 != Interlocked.Exchange( ref disposed, 1 ) ) {
			return;
		}

		if ( null != consoleCancelHandler ) {
			Console.CancelKeyPress -= consoleCancelHandler;
			consoleCancelHandler = null;
		}

		foreach ( IDisposable registration in registrations ) {
			registration.Dispose();
		}
		registrations.Clear();

		signals.Writer.TryComplete();
	}

	private void HandleConsoleCancel(
		object? sender,
		ConsoleCancelEventArgs eventArgs
	) {
		ArgumentNullException.ThrowIfNull( eventArgs );

		eventArgs.Cancel = true;
		Publish(
			ConsoleSpecialKey.ControlC == eventArgs.SpecialKey
				? TerminalLifecycleSignalKind.Interrupt
				: TerminalLifecycleSignalKind.Termination
		);
	}

	private void HandleSuspendSignal( PosixSignalContext context ) {
		if ( 0 != Interlocked.Exchange( ref allowSuspendDelivery, 0 ) ) {
			context.Cancel = false;
			return;
		}

		context.Cancel = true;
		Publish( TerminalLifecycleSignalKind.Suspend );
	}

	private void RegisterTerminationSignal(
		PosixSignal signal,
		TerminalLifecycleSignalKind kind
	) {
		registrations.Add(
			PosixSignalRegistration.Create(
				signal,
				context => {
					context.Cancel = true;
					Publish( kind );
				}
			)
		);
	}

	private void Publish( TerminalLifecycleSignalKind kind ) {
		if ( 0 != Volatile.Read( ref disposed ) ) {
			return;
		}

		signals.Writer.TryWrite(
			new TerminalLifecycleSignal( kind )
		);
	}

	[LibraryImport(
		"libc",
		EntryPoint = "raise"
	)]
	private static partial int NativeRaise( int signal );
}
