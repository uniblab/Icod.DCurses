namespace Icod.DCurses;

using System.Runtime.ExceptionServices;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using TerminalCellSize = Icod.DCurses.Terminal.TerminalSize;

/// <summary>
/// Owns the live terminal state associated with one curses presentation.
/// </summary>
public sealed partial class CursesSession
	: IAsyncDisposable {
	private readonly object restoreSync = new();
	private readonly ITerminalSessionModeController sessionModes;

	private ITerminalModeState? capturedMode;
	private string? alternateScreenRestore;
	private string? keypadRestore;
	private string? cursorRestore;
	private bool modeRestoreRequired;
	private Task<Exception?>? restoreTask;

	private CursesSession(
		TerminalBackend backend,
		ITerminalSessionModeController sessionModes,
		ITerminalLifecycleSource? lifecycleSource,
		CursesSessionOptions options) {
		Backend = backend;
		this.sessionModes = sessionModes;
		this.lifecycleSource = lifecycleSource;
		Options = options;
	}

	/// <summary>
	/// Gets the terminal profile selected for this session.
	/// </summary>
	public TerminalDescription Terminal => Backend.Capabilities;

	/// <summary>
	/// Gets the input endpoint used by the session.
	/// </summary>
	public TerminalEndpoint InputEndpoint => Backend.InputEndpoint;

	/// <summary>
	/// Gets the output endpoint used by the session.
	/// </summary>
	public TerminalEndpoint OutputEndpoint => Backend.OutputEndpoint;

	/// <summary>
	/// Gets whether both session endpoints are interactive terminals.
	/// </summary>
	public bool IsInteractive => Backend.IsInteractive;

	/// <summary>
	/// Gets the immutable options with which this session was opened.
	/// </summary>
	public CursesSessionOptions Options {
		get;
	}

	internal TerminalBackend Backend {
		get;
	}

	/// <summary>
	/// Opens a curses session against process standard input and standard output.
	/// </summary>
	/// <param name="options">Optional session presentation policy.</param>
	/// <param name="cancellationToken">Cancellation for session initialization.</param>
	/// <returns>The initialized curses session.</returns>
	public static ValueTask<CursesSession> OpenAsync(
		CursesSessionOptions? options = null,
		CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		CursesSessionOptions resolvedOptions =
			options
			?? new CursesSessionOptions();

		resolvedOptions.Validate();

		TerminalBackend backend =
			SystemTerminalBackendFactory.Create();
		ITerminalLifecycleSource lifecycleSource =
			new SystemTerminalLifecycleSource();

		return OpenAsync(
			backend,
			resolvedOptions,
			lifecycleSource,
			cancellationToken);
	}

	/// <summary>
	/// Opens a curses session against an explicitly supplied terminal backend.
	/// </summary>
	/// <param name="backend">The terminal backend to own for this session.</param>
	/// <param name="options">Optional session presentation policy.</param>
	/// <param name="cancellationToken">Cancellation for session initialization.</param>
	/// <returns>The initialized curses session.</returns>
	public static ValueTask<CursesSession> OpenAsync(
		TerminalBackend backend,
		CursesSessionOptions? options = null,
		CancellationToken cancellationToken = default) {
		return OpenAsync(
			backend,
			options,
			lifecycleSource: null,
			cancellationToken);
	}

	internal static async ValueTask<CursesSession> OpenAsync(
		TerminalBackend backend,
		CursesSessionOptions? options,
		ITerminalLifecycleSource? lifecycleSource,
		CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(backend);
		cancellationToken.ThrowIfCancellationRequested();

		CursesSessionOptions resolvedOptions =
			options
			?? new CursesSessionOptions();

		resolvedOptions.Validate();

		if (!backend.IsInteractive) {
			lifecycleSource?.Dispose();
			throw new InvalidOperationException(
				$"Curses requires interactive terminal input and output. "
				+ $"Input '{backend.InputEndpoint.DisplayName}' interactive: "
				+ $"{backend.InputEndpoint.IsInteractive}; output "
				+ $"'{backend.OutputEndpoint.DisplayName}' interactive: "
				+ $"{backend.OutputEndpoint.IsInteractive}.");
		}

		if (backend.Modes is not ITerminalSessionModeController sessionModes) {
			lifecycleSource?.Dispose();
			throw new NotSupportedException(
				"The terminal backend does not support curses session-mode transitions.");
		}

		CursesSession session =
			new(
				backend,
				sessionModes,
				lifecycleSource,
				resolvedOptions);

		try {
			await session.InitializeAsync(
				cancellationToken).ConfigureAwait(false);

			session.StartLifecyclePump();
			return session;
		} catch (Exception exception) {
			await session.StopLifecycleAsync().ConfigureAwait(false);
			Exception? restorationException =
				await session.RestoreCoreAsync().ConfigureAwait(false);

			if (restorationException is not null) {
				throw new AggregateException(
					"Curses session initialization failed and terminal restoration also reported an error.",
					exception,
					restorationException);
			}

			throw;
		}
	}

	/// <summary>
	/// Queries the current live terminal dimensions.
	/// </summary>
	/// <returns>A controlled terminal-dimension result.</returns>
	public TerminalBackendResult<TerminalCellSize> GetDimensions() {
		return Backend.Dimensions.GetDimensions();
	}

	/// <summary>
	/// Restores terminal state. Repeated disposal is safe and does not replay restoration.
	/// </summary>
	public async ValueTask DisposeAsync() {
		await StopLifecycleAsync().ConfigureAwait(false);

		Exception? exception =
			await RestoreCoreAsync().ConfigureAwait(false);

		if (exception is not null) {
			ExceptionDispatchInfo.Capture(exception).Throw();
		}
	}

	private async ValueTask InitializeAsync(
		CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		TerminalBackendResult<ITerminalModeState> captureResult =
			sessionModes.CaptureMode();

		if (!captureResult.IsAvailable) {
			throw new InvalidOperationException(
				captureResult.Message
				?? "The host terminal mode could not be captured.");
		}

		capturedMode =
			captureResult.GetRequiredValue();

		modeRestoreRequired = true;

		TerminalBackendMutationResult modeResult =
			sessionModes.ApplySessionMode(
				capturedMode,
				Options.InputMode,
				Options.EchoInput);

		if (!modeResult.Succeeded) {
			throw new InvalidOperationException(
				modeResult.Message
				?? "The requested curses terminal mode could not be applied.");
		}

		await EnterAlternateScreenAsync(
			cancellationToken).ConfigureAwait(false);
		await EnterKeypadAsync(
			cancellationToken).ConfigureAwait(false);
		await HideCursorAsync(
			cancellationToken).ConfigureAwait(false);

		if (HasPresentationState) {
			await Backend.Output.FlushAsync(
				cancellationToken).ConfigureAwait(false);
		}
	}

	private async ValueTask EnterAlternateScreenAsync(
		CancellationToken cancellationToken) {
		if (!Options.UseAlternateScreen) {
			return;
		}

		string? enter =
			Terminal.GetString(
				StringCapability.EnterCursorAddressingMode);
		string? exit =
			Terminal.GetString(
				StringCapability.ExitCursorAddressingMode);

		if ((enter is null) || (exit is null)) {
			return;
		}

		alternateScreenRestore = exit;

		await TerminalCapabilityWriter.WriteAsync(
			Backend.Output,
			enter,
			cancellationToken).ConfigureAwait(false);
	}

	private async ValueTask EnterKeypadAsync(
		CancellationToken cancellationToken) {
		if (!Options.EnableKeypad) {
			return;
		}

		string? enter =
			Terminal.GetString(
				StringCapability.EnterKeypadMode);
		string? exit =
			Terminal.GetString(
				StringCapability.ExitKeypadMode);

		if ((enter is null) || (exit is null)) {
			return;
		}

		keypadRestore = exit;

		await TerminalCapabilityWriter.WriteAsync(
			Backend.Output,
			enter,
			cancellationToken).ConfigureAwait(false);
	}

	private async ValueTask HideCursorAsync(
		CancellationToken cancellationToken) {
		if (!Options.HideCursor) {
			return;
		}

		string? hide =
			Terminal.GetString(
				StringCapability.CursorInvisible);
		string? show =
			Terminal.GetString(
				StringCapability.CursorNormal)
			?? Terminal.GetString(
				StringCapability.CursorVeryVisible);

		if ((hide is null) || (show is null)) {
			return;
		}

		cursorRestore = show;

		await TerminalCapabilityWriter.WriteAsync(
			Backend.Output,
			hide,
			cancellationToken).ConfigureAwait(false);
	}

	private bool HasPresentationState =>
		(cursorRestore is not null)
		|| (keypadRestore is not null)
		|| (alternateScreenRestore is not null);

	private ValueTask<Exception?> RestoreCoreAsync() {
		lock (restoreSync) {
			restoreTask ??=
				RestoreOnceAsync();

			return new ValueTask<Exception?>(
				restoreTask);
		}
	}

	private async Task<Exception?> RestoreOnceAsync() {
		await Task.Yield();

		List<Exception> exceptions = [];

		await TryRestoreCapabilityAsync(
			cursorRestore,
			exceptions).ConfigureAwait(false);
		await TryRestoreCapabilityAsync(
			keypadRestore,
			exceptions).ConfigureAwait(false);
		await TryRestoreCapabilityAsync(
			alternateScreenRestore,
			exceptions).ConfigureAwait(false);

		if (HasPresentationState) {
			try {
				await Backend.Output.FlushAsync(
					CancellationToken.None).ConfigureAwait(false);
			} catch (Exception exception) {
				exceptions.Add(exception);
			}
		}

		if (modeRestoreRequired
			&& (capturedMode is not null)) {
			try {
				TerminalBackendMutationResult result =
					sessionModes.RestoreMode(
						capturedMode,
						TerminalModeApplyTiming.AfterOutputDrained);

				if (!result.Succeeded) {
					exceptions.Add(
						new InvalidOperationException(
							result.Message
								?? "The original host terminal mode could not be restored."));
				}
			} catch (Exception exception) {
				exceptions.Add(exception);
			}
		}

		return BuildRestorationException(
			exceptions);
	}

	private async ValueTask TryRestoreCapabilityAsync(
		string? capability,
		ICollection<Exception> exceptions) {
		ArgumentNullException.ThrowIfNull(exceptions);

		if (capability is null) {
			return;
		}

		try {
			await TerminalCapabilityWriter.WriteAsync(
				Backend.Output,
				capability,
				CancellationToken.None).ConfigureAwait(false);
		} catch (Exception exception) {
			exceptions.Add(exception);
		}
	}

	private static Exception? BuildRestorationException(
		IReadOnlyCollection<Exception> exceptions) {
		ArgumentNullException.ThrowIfNull(exceptions);

		return exceptions.Count switch {
			0 => null,
			1 => exceptions.First(),
			_ => new AggregateException(
				"Multiple errors occurred while restoring terminal state.",
				exceptions)
		};
	}
}
