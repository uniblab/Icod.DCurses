namespace Icod.DCurses;

using System.Runtime.ExceptionServices;
using Icod.DCurses.Internal;
using CursesTerminalOutput = Icod.DCurses.Terminal.ITerminalOutput;
using Icod.Terminal;
using Icod.TermInfo;

/// <summary>
/// Owns one curses presentation over a canonical <see cref="TerminalSession"/>.
/// </summary>
public sealed partial class CursesSession : IAsyncDisposable {
	private readonly object disposeSync = new();
	private readonly SemaphoreSlim terminalActivityGate = new( 1, 1 );
	private readonly TerminalSession terminalSession;
	private readonly CursesTerminalOutput refreshOutput;
	private readonly CursesTerminalLifecycleParticipant lifecycleParticipant;
	private readonly IDisposable lifecycleParticipantRegistration;

	private Task? disposeTask;
	private int disposeStarted;

	private CursesSession(
		TerminalSession terminalSession,
		CursesSessionOptions options
	) {
		ArgumentNullException.ThrowIfNull( terminalSession );
		ArgumentNullException.ThrowIfNull( options );

		this.terminalSession = terminalSession;
		this.refreshOutput = new Icod.DCurses.Terminal.TerminalSessionCursesOutput( terminalSession );
		this.Options = options;
		this.lifecycleParticipant = new CursesTerminalLifecycleParticipant( this );
		this.lifecycleParticipantRegistration = terminalSession.RegisterLifecycleParticipant(
			this.lifecycleParticipant
		);
	}

	/// <summary>Gets the terminal profile selected for this session.</summary>
	public TerminalDescription Terminal {
		get {
			return this.terminalSession.Terminal;
		}
	}

	/// <summary>Gets the terminal input endpoint used by this session.</summary>
	public TerminalEndpoint InputEndpoint {
		get {
			return this.terminalSession.InputEndpoint;
		}
	}

	/// <summary>Gets the terminal output endpoint used by this session.</summary>
	public TerminalEndpoint OutputEndpoint {
		get {
			return this.terminalSession.OutputEndpoint;
		}
	}

	/// <summary>Gets whether both session endpoints are interactive terminals.</summary>
	public bool IsInteractive {
		get {
			return this.terminalSession.IsInteractive;
		}
	}

	/// <summary>Gets the immutable options with which this curses session was opened.</summary>
	public CursesSessionOptions Options {
		get;
	}

	/// <summary>Gets the canonical Terminal session owned by this curses session.</summary>
	internal TerminalSession HostSession {
		get {
			return this.terminalSession;
		}
	}

	/// <summary>Gets the registered lifecycle participant for deterministic integration tests.</summary>
	internal ITerminalSessionLifecycleParticipant LifecycleParticipant {
		get {
			return this.lifecycleParticipant;
		}
	}

	/// <summary>
	/// Opens a curses session against process standard input and standard output.
	/// </summary>
	/// <param name="options">Optional curses presentation policy.</param>
	/// <param name="cancellationToken">Cancellation for session initialization.</param>
	/// <returns>The initialized curses session.</returns>
	public static async ValueTask<CursesSession> OpenAsync(
		CursesSessionOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();

		CursesSessionOptions resolvedOptions = options ?? new CursesSessionOptions();
		resolvedOptions.Validate();

		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new TerminalSessionOptions {
				InputMode = ToTerminalInputMode( resolvedOptions.InputMode ),
				EchoInput = resolvedOptions.EchoInput,
				RequireInteractiveOutput = true
			},
			cancellationToken
		).ConfigureAwait( false );

		try {
			return await OpenAsync(
				terminalSession,
				resolvedOptions,
				cancellationToken
			).ConfigureAwait( false );
		} catch {
			await terminalSession.DisposeAsync().ConfigureAwait( false );
			throw;
		}
	}

	/// <summary>
	/// Opens a curses presentation over an already-open Terminal session.
	/// </summary>
	/// <remarks>
	/// Ownership transfers to the returned <see cref="CursesSession"/> only after successful
	/// initialization. On success, disposing the curses session also disposes the supplied
	/// <paramref name="terminalSession"/>. When initialization fails before ownership transfer,
	/// the caller retains responsibility for the supplied Terminal session.
	/// </remarks>
	/// <param name="terminalSession">The live Terminal session.</param>
	/// <param name="options">Optional curses presentation policy.</param>
	/// <param name="cancellationToken">Cancellation for curses initialization.</param>
	/// <returns>The initialized curses session.</returns>
	public static async ValueTask<CursesSession> OpenAsync(
		TerminalSession terminalSession,
		CursesSessionOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( terminalSession );
		cancellationToken.ThrowIfCancellationRequested();

		CursesSessionOptions resolvedOptions = options ?? new CursesSessionOptions {
			InputMode = FromTerminalInputMode( terminalSession.Options.InputMode ),
			EchoInput = terminalSession.Options.EchoInput
		};
		resolvedOptions.Validate();

		if ( !terminalSession.IsInteractive ) {
			throw new InvalidOperationException(
				"Curses requires interactive Terminal input and output endpoints."
			);
		}
		if (
			ToTerminalInputMode( resolvedOptions.InputMode ) != terminalSession.Options.InputMode
			|| resolvedOptions.EchoInput != terminalSession.Options.EchoInput
		) {
			throw new InvalidOperationException(
				"Curses input-mode and echo options must match the supplied TerminalSession."
			);
		}

		CursesSession session = new( terminalSession, resolvedOptions );
		try {
			await session.InitializePresentationAsync( cancellationToken ).ConfigureAwait( false );
			return session;
		} catch ( Exception exception ) {
			Interlocked.Exchange( ref session.disposeStarted, 1 );
			try {
				await session.RestoreOwnedStateAsync(
					disposeTerminalSession: false
				).ConfigureAwait( false );
			} catch ( Exception restorationException ) {
				throw new AggregateException(
					"Curses initialization failed and restoration also reported an error.",
					exception,
					restorationException
				);
			}
			throw;
		}
	}

	/// <summary>Queries the current live terminal dimensions.</summary>
	/// <returns>The canonical Terminal live-size result.</returns>
	public TerminalControlResult<TerminalSize> GetDimensions() {
		return this.terminalSession.GetSize();
	}

	/// <summary>Restores curses and Terminal-owned state exactly once.</summary>
	/// <returns>A value task representing asynchronous restoration.</returns>
	public ValueTask DisposeAsync() {
		lock ( this.disposeSync ) {
			if ( this.disposeTask is null ) {
				Interlocked.Exchange( ref this.disposeStarted, 1 );
				this.disposeTask = this.DisposeOnceAsync();
			}
			return new ValueTask( this.disposeTask );
		}
	}

	private Task DisposeOnceAsync() {
		return this.RestoreOwnedStateAsync( disposeTerminalSession: true );
	}

	private async Task RestoreOwnedStateAsync(
		bool disposeTerminalSession
	) {
		List<Exception> exceptions = [];

		this.lifecycleParticipantRegistration.Dispose();
		try {
			await this.lifecycleParticipant.CloseAsync().ConfigureAwait( false );
		} catch ( Exception exception ) {
			exceptions.Add( exception );
		}

		await this.terminalActivityGate.WaitAsync( CancellationToken.None ).ConfigureAwait( false );
		try {
			try {
				await this.ResetRefreshRenditionAsync().ConfigureAwait( false );
			} catch ( Exception exception ) {
				exceptions.Add( exception );
			}

			await this.ReleasePresentationLeasesForDisposalAsync(
				exceptions
			).ConfigureAwait( false );
		} finally {
			this.terminalActivityGate.Release();
		}

		if ( disposeTerminalSession ) {
			try {
				await this.terminalSession.DisposeAsync().ConfigureAwait( false );
			} catch ( Exception exception ) {
				exceptions.Add( exception );
			}
		}

		Exception? failure = BuildDisposalException( exceptions );
		if ( failure is not null ) {
			ExceptionDispatchInfo.Capture( failure ).Throw();
		}
	}

	private async ValueTask<IDisposable> AcquireTerminalActivityAsync(
		CancellationToken cancellationToken
	) {
		cancellationToken.ThrowIfCancellationRequested();
		if ( 0 != Volatile.Read( ref this.disposeStarted ) ) {
			throw new ObjectDisposedException( nameof( CursesSession ) );
		}

		await this.terminalActivityGate.WaitAsync( cancellationToken ).ConfigureAwait( false );
		if ( 0 != Volatile.Read( ref this.disposeStarted ) ) {
			this.terminalActivityGate.Release();
			throw new ObjectDisposedException( nameof( CursesSession ) );
		}

		return new TerminalActivityLease( this.terminalActivityGate );
	}

	private static TerminalInputMode ToTerminalInputMode(
		CursesInputMode inputMode
	) {
		return inputMode switch {
			CursesInputMode.Canonical => TerminalInputMode.Canonical,
			CursesInputMode.CBreak => TerminalInputMode.CBreak,
			CursesInputMode.Raw => TerminalInputMode.Raw,
			_ => throw new ArgumentOutOfRangeException( nameof( inputMode ) )
		};
	}

	private static CursesInputMode FromTerminalInputMode(
		TerminalInputMode inputMode
	) {
		return inputMode switch {
			TerminalInputMode.Canonical => CursesInputMode.Canonical,
			TerminalInputMode.CBreak => CursesInputMode.CBreak,
			TerminalInputMode.Raw => CursesInputMode.Raw,
			_ => throw new ArgumentOutOfRangeException( nameof( inputMode ) )
		};
	}

	private static Exception? BuildDisposalException(
		IReadOnlyCollection<Exception> exceptions
	) {
		ArgumentNullException.ThrowIfNull( exceptions );
		return exceptions.Count switch {
			0 => null,
			1 => exceptions.First(),
			_ => new AggregateException(
				"Multiple errors occurred while restoring the curses terminal session.",
				exceptions
			)
		};
	}

	private sealed class TerminalActivityLease : IDisposable {
		private SemaphoreSlim? gate;

		internal TerminalActivityLease( SemaphoreSlim gate ) {
			ArgumentNullException.ThrowIfNull( gate );
			this.gate = gate;
		}

		public void Dispose() {
			SemaphoreSlim? current = Interlocked.Exchange( ref this.gate, null );
			current?.Release();
		}
	}
}
