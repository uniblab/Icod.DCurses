namespace Icod.DCurses.Terminal;

using Icod.TermInfo;
using FrameworkTerminal = Icod.CommandFramework.Terminal;
using TermInfoTerminalSize = Icod.TermInfo.TerminalSize;

/// <summary>Creates the platform-backed terminal services used by a standard curses session.</summary>
internal static class SystemTerminalBackendFactory
{
	/// <summary>Creates a backend bound to process standard input and output.</summary>
	/// <returns>The system terminal backend.</returns>
	internal static TerminalBackend Create()
	{
		FrameworkTerminal.SystemTerminalControlProvider controlProvider =
			FrameworkTerminal.SystemTerminalControlProvider.Instance;

		bool inputInteractive =
			IsInteractive(
				controlProvider,
				FrameworkTerminal.TerminalEndpoint.StandardInput);
		bool outputInteractive =
			IsInteractive(
				controlProvider,
				FrameworkTerminal.TerminalEndpoint.StandardOutput);

		return new TerminalBackend(
			new TerminalEndpoint(
				"standard input",
				inputInteractive),
			new TerminalEndpoint(
				"standard output",
				outputInteractive),
			ResolveTerminalDescription(),
			new StreamTerminalInput(
				Console.OpenStandardInput()),
			new StreamTerminalOutput(
				Console.OpenStandardOutput()),
			new SystemTerminalDimensionProvider(),
			new SystemTerminalModeController(
				controlProvider));
	}

	private static bool IsInteractive(
		FrameworkTerminal.ITerminalControlProvider controlProvider,
		FrameworkTerminal.TerminalEndpoint endpoint)
	{
		ArgumentNullException.ThrowIfNull(controlProvider);
		ArgumentNullException.ThrowIfNull(endpoint);

		FrameworkTerminal.TerminalControlResult<FrameworkTerminal.TerminalEndpointObservation> result =
			controlProvider.Observe(endpoint);

		return result.IsAvailable
			&& result.GetRequiredValue().IsTerminal;
	}

	private static TerminalDescription ResolveTerminalDescription()
	{
		TerminalDatabase database = new(
			new ITerminalDescriptionProvider[]
			{
				new SystemTerminalDescriptionProvider(),
				TerminalDatabase.BuiltIn
			});

		return TerminalEnvironment.Resolve(
			database,
			GetFallbackTerminalDescription());
	}

	private static TerminalDescription GetFallbackTerminalDescription()
	{
		if (!OperatingSystem.IsWindows())
		{
			return TerminalProfiles.Dumb;
		}

		string? windowsTerminalSession =
			Environment.GetEnvironmentVariable("WT_SESSION");

		return string.IsNullOrWhiteSpace(windowsTerminalSession)
			? TerminalProfiles.WinConsole
			: TerminalProfiles.MsTerminalDirect;
	}

	private sealed class StreamTerminalInput
		: ITerminalInput
	{
		private readonly Stream stream;

		/// <summary>Initializes a stream-backed terminal input service.</summary>
		/// <param name="stream">The readable standard-input stream.</param>
		internal StreamTerminalInput(
			Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);
			this.stream = stream;
		}

		/// <inheritdoc />
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default)
		{
			return stream.ReadAsync(
				buffer,
				cancellationToken);
		}
	}

	private sealed class StreamTerminalOutput
		: ITerminalOutput
	{
		private readonly Stream stream;

		/// <summary>Initializes a stream-backed terminal output service.</summary>
		/// <param name="stream">The writable standard-output stream.</param>
		internal StreamTerminalOutput(
			Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);
			this.stream = stream;
		}

		/// <inheritdoc />
		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default)
		{
			return stream.WriteAsync(
				buffer,
				cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default)
		{
			return new ValueTask(
				stream.FlushAsync(
					cancellationToken));
		}
	}

	private sealed class SystemTerminalDimensionProvider
		: ITerminalDimensionProvider
	{
		/// <inheritdoc />
		public TerminalBackendResult<TerminalSize> GetDimensions()
		{
			if (TerminalEnvironment.TryGetLiveSize(
				out TermInfoTerminalSize size))
			{
				return TerminalBackendResult<TerminalSize>.Available(
					new TerminalSize(
						size.Columns,
						size.Rows));
			}

			return TerminalBackendResult<TerminalSize>.Unavailable(
				"Live terminal dimensions are unavailable.");
		}
	}
}

/// <summary>Adapts the command-framework terminal-control provider to curses session-mode semantics.</summary>
internal sealed class SystemTerminalModeController
	: ITerminalSessionModeController
{
	private readonly FrameworkTerminal.ITerminalControlProvider controlProvider;

	/// <summary>Initializes a system terminal-mode controller.</summary>
	/// <param name="controlProvider">The underlying terminal-control provider.</param>
	internal SystemTerminalModeController(
		FrameworkTerminal.ITerminalControlProvider controlProvider)
	{
		ArgumentNullException.ThrowIfNull(controlProvider);
		this.controlProvider = controlProvider;
	}

	/// <inheritdoc />
	public TerminalBackendResult<ITerminalModeState> CaptureMode()
	{
		FrameworkTerminal.TerminalControlResult<FrameworkTerminal.TerminalModeSnapshot> result =
			controlProvider.GetMode(
				FrameworkTerminal.TerminalEndpoint.StandardInput);

		if (!result.IsAvailable)
		{
			return ConvertFailure<ITerminalModeState>(
				result.Status,
				result.Message);
		}

		return TerminalBackendResult<ITerminalModeState>.Available(
			new SystemTerminalModeState(
				result.GetRequiredValue()));
	}

	/// <inheritdoc />
	public TerminalBackendMutationResult ApplySessionMode(
		ITerminalModeState baseline,
		CursesInputMode inputMode,
		bool echoInput)
	{
		ArgumentNullException.ThrowIfNull(baseline);

		if (!Enum.IsDefined(inputMode))
		{
			throw new ArgumentOutOfRangeException(nameof(inputMode));
		}

		if (baseline is not SystemTerminalModeState state)
		{
			throw new ArgumentException(
				"The terminal mode state was not created by the system terminal backend.",
				nameof(baseline));
		}

		if (FrameworkTerminal.TerminalPlatformKind.WindowsConsole == state.Baseline.Platform)
		{
			state.VirtualTerminalLease ??=
				WindowsVirtualTerminal.TryEnableOutput();

			if (state.VirtualTerminalLease is null)
			{
				return TerminalBackendMutationResult.Unavailable(
					"Windows virtual-terminal output processing could not be enabled.");
			}
		}

		FrameworkTerminal.TerminalModeSnapshot configured;

		try
		{
			configured =
				SystemTerminalModeEditor.Configure(
					state.Baseline,
					inputMode,
					echoInput);
		}
		catch (Exception exception)
			when (exception is InvalidOperationException
				or ArgumentException)
		{
			return TerminalBackendMutationResult.Failed(
				exception.Message);
		}

		FrameworkTerminal.TerminalModeApplyTiming timing =
			FrameworkTerminal.TerminalPlatformKind.WindowsConsole == configured.Platform
				? FrameworkTerminal.TerminalModeApplyTiming.Immediately
				: FrameworkTerminal.TerminalModeApplyTiming.AfterOutputDrained;

		return ConvertMutation(
			controlProvider.SetMode(
				FrameworkTerminal.TerminalEndpoint.StandardInput,
				configured,
				timing));
	}

	/// <inheritdoc />
	public TerminalBackendMutationResult RestoreMode(
		ITerminalModeState state,
		TerminalModeApplyTiming timing)
	{
		ArgumentNullException.ThrowIfNull(state);

		if (!Enum.IsDefined(timing))
		{
			throw new ArgumentOutOfRangeException(nameof(timing));
		}

		if (state is not SystemTerminalModeState systemState)
		{
			throw new ArgumentException(
				"The terminal mode state was not created by the system terminal backend.",
				nameof(state));
		}

		TerminalBackendMutationResult result;

		try
		{
			FrameworkTerminal.TerminalModeApplyTiming nativeTiming =
				FrameworkTerminal.TerminalPlatformKind.WindowsConsole
					== systemState.Baseline.Platform
					? FrameworkTerminal.TerminalModeApplyTiming.Immediately
					: ConvertTiming(timing);

			result =
				ConvertMutation(
					controlProvider.SetMode(
						FrameworkTerminal.TerminalEndpoint.StandardInput,
						systemState.Baseline,
						nativeTiming));
		}
		finally
		{
			systemState.VirtualTerminalLease?.Dispose();
			systemState.VirtualTerminalLease = null;
		}

		return result;
	}

	private static FrameworkTerminal.TerminalModeApplyTiming ConvertTiming(
		TerminalModeApplyTiming timing)
	{
		return timing switch
		{
			TerminalModeApplyTiming.Immediately =>
				FrameworkTerminal.TerminalModeApplyTiming.Immediately,
			TerminalModeApplyTiming.AfterOutputDrained =>
				FrameworkTerminal.TerminalModeApplyTiming.AfterOutputDrained,
			TerminalModeApplyTiming.AfterOutputDrainedAndInputDiscarded =>
				FrameworkTerminal.TerminalModeApplyTiming.AfterOutputDrainedAndInputDiscarded,
			_ => throw new ArgumentOutOfRangeException(nameof(timing))
		};
	}

	private static TerminalBackendMutationResult ConvertMutation(
		FrameworkTerminal.TerminalControlMutationResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		return result.Status switch
		{
			FrameworkTerminal.TerminalControlStatus.Available =>
				TerminalBackendMutationResult.Success(),
			FrameworkTerminal.TerminalControlStatus.Unavailable =>
				TerminalBackendMutationResult.Unavailable(result.Message),
			FrameworkTerminal.TerminalControlStatus.Unsupported =>
				TerminalBackendMutationResult.Unsupported(result.Message),
			FrameworkTerminal.TerminalControlStatus.Failed =>
				TerminalBackendMutationResult.Failed(result.Message),
			_ => TerminalBackendMutationResult.Failed(
				"The terminal-control provider returned an unrecognized status.")
		};
	}

	private static TerminalBackendResult<T> ConvertFailure<T>(
		FrameworkTerminal.TerminalControlStatus status,
		string? message)
	{
		return status switch
		{
			FrameworkTerminal.TerminalControlStatus.Unavailable =>
				TerminalBackendResult<T>.Unavailable(message),
			FrameworkTerminal.TerminalControlStatus.Unsupported =>
				TerminalBackendResult<T>.Unsupported(message),
			FrameworkTerminal.TerminalControlStatus.Failed =>
				TerminalBackendResult<T>.Failed(message),
			_ => TerminalBackendResult<T>.Failed(
				"The terminal-control provider did not return an available value.")
		};
	}

	private sealed class SystemTerminalModeState
		: ITerminalModeState
	{
		/// <summary>Initializes captured system terminal-mode state.</summary>
		/// <param name="baseline">The captured command-framework mode snapshot.</param>
		internal SystemTerminalModeState(
			FrameworkTerminal.TerminalModeSnapshot baseline)
		{
			ArgumentNullException.ThrowIfNull(baseline);
			Baseline = baseline;
		}

		/// <summary>Gets the captured baseline terminal-mode snapshot.</summary>
		internal FrameworkTerminal.TerminalModeSnapshot Baseline
		{
			get;
		}

		/// <summary>Gets or sets the active Windows virtual-terminal output lease.</summary>
		internal IDisposable? VirtualTerminalLease
		{
			get;
			set;
		}
	}
}
