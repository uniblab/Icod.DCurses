using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesSessionTests
{
	[Fact]
	public async Task OpenAndDisposeEnterAndRestoreTerminalStateInOrder()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes = new(events);

		await using (
			CursesSession session =
				await CursesSession.OpenAsync(
					CreateBackend(
						output,
						modes)))
		{
			Assert.True(session.IsInteractive);
			Assert.Equal("test-terminal", session.Terminal.Name);
		}

		Assert.Equal(
			new[]
			{
				"mode:capture",
				"mode:apply:CBreak:False",
				"write:<alternate>",
				"write:<keypad>",
				"write:<hide>",
				"flush",
				"write:<show>",
				"write:</keypad>",
				"write:</alternate>",
				"flush",
				"mode:restore:AfterOutputDrained"
			},
			events);
	}

	[Fact]
	public async Task DisposeAsyncIsIdempotent()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes = new(events);

		CursesSession session =
			await CursesSession.OpenAsync(
				CreateBackend(
					output,
					modes));

		await session.DisposeAsync();
		await session.DisposeAsync();

		Assert.Equal(1, modes.RestoreCount);
		Assert.Equal(
			1,
			events.Count(
				value =>
					"write:</alternate>" == value));
	}

	[Fact]
	public async Task PresentationFailureRestoresAttemptedTransitionsAndHostMode()
	{
		List<string> events = [];
		FakeTerminalOutput output =
			new(
				events,
				throwOnWrite: 2);
		FakeSessionModeController modes = new(events);

		await Assert.ThrowsAsync<IOException>(
			async () =>
			{
				_ =
					await CursesSession.OpenAsync(
						CreateBackend(
							output,
							modes));
			});

		Assert.Contains(
			"write:</keypad>",
			events);
		Assert.Contains(
			"write:</alternate>",
			events);
		Assert.Equal(1, modes.RestoreCount);
	}

	[Fact]
	public async Task CancellationDuringPresentationRestoresHostMode()
	{
		using CancellationTokenSource cancellation = new();
		List<string> events = [];
		FakeTerminalOutput output =
			new(
				events,
				cancelOnWrite: 2,
				cancellation: cancellation);
		FakeSessionModeController modes = new(events);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			async () =>
			{
				_ =
					await CursesSession.OpenAsync(
						CreateBackend(
							output,
							modes),
						cancellationToken: cancellation.Token);
			});

		Assert.Contains(
			"write:</keypad>",
			events);
		Assert.Contains(
			"write:</alternate>",
			events);
		Assert.Equal(1, modes.RestoreCount);
	}

	[Fact]
	public async Task ModeConfigurationFailureRestoresCapturedBaseline()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes =
			new(
				events)
			{
				FailApply = true
			};

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
			{
				_ =
					await CursesSession.OpenAsync(
						CreateBackend(
							output,
							modes));
			});

		Assert.Equal(1, modes.RestoreCount);
		Assert.DoesNotContain(
			events,
			value => value.StartsWith(
				"write:",
				StringComparison.Ordinal));
	}


	[Fact]
	public async Task ExceptionDuringModeConfigurationRestoresCapturedBaseline()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes =
			new(
				events)
			{
				ThrowApply = true
			};

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
			{
				_ =
					await CursesSession.OpenAsync(
						CreateBackend(
							output,
							modes));
			});

		Assert.Equal(1, modes.RestoreCount);
	}

	[Fact]
	public async Task UnsafeOneWayPresentationCapabilitiesAreNotEntered()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes = new(events);

		TerminalDescription terminal =
			new TerminalDescriptionBuilder("one-way-terminal")
				.SetString(
					StringCapability.EnterCursorAddressingMode,
					"<alternate>")
				.SetString(
					StringCapability.EnterKeypadMode,
					"<keypad>")
				.SetString(
					StringCapability.CursorInvisible,
					"<hide>")
				.Build();

		await using CursesSession session =
			await CursesSession.OpenAsync(
				CreateBackend(
					output,
					modes,
					terminal));

		Assert.DoesNotContain(
			events,
			value => value.StartsWith(
				"write:",
				StringComparison.Ordinal));
	}

	[Fact]
	public async Task NonInteractiveBackendIsRejectedBeforeModeMutation()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes = new(events);

		TerminalBackend backend =
			CreateBackend(
				output,
				modes,
				inputInteractive: false);

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () =>
			{
				_ =
					await CursesSession.OpenAsync(
						backend);
			});

		Assert.Empty(events);
	}

	[Fact]
	public async Task OptionsControlInputPolicyAndPresentationTransitions()
	{
		List<string> events = [];
		FakeTerminalOutput output = new(events);
		FakeSessionModeController modes = new(events);

		CursesSessionOptions options = new()
		{
			InputMode = CursesInputMode.Raw,
			EchoInput = true,
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};

		await using CursesSession session =
			await CursesSession.OpenAsync(
				CreateBackend(
					output,
					modes),
				options);

		Assert.Contains(
			"mode:apply:Raw:True",
			events);
		Assert.DoesNotContain(
			events,
			value => value.StartsWith(
				"write:",
				StringComparison.Ordinal));
	}

	private static TerminalBackend CreateBackend(
		FakeTerminalOutput output,
		FakeSessionModeController modes,
		TerminalDescription? terminal = null,
		bool inputInteractive = true,
		bool outputInteractive = true)
	{
		ArgumentNullException.ThrowIfNull(output);
		ArgumentNullException.ThrowIfNull(modes);

		return new TerminalBackend(
			new TerminalEndpoint(
				"test input",
				inputInteractive),
			new TerminalEndpoint(
				"test output",
				outputInteractive),
			terminal
				?? CreateTerminal(),
			new FakeTerminalInput(),
			output,
			new FakeDimensionProvider(),
			modes);
	}

	private static TerminalDescription CreateTerminal()
	{
		return new TerminalDescriptionBuilder("test-terminal")
			.SetString(
				StringCapability.EnterCursorAddressingMode,
				"<alternate>")
			.SetString(
				StringCapability.ExitCursorAddressingMode,
				"</alternate>")
			.SetString(
				StringCapability.EnterKeypadMode,
				"<keypad>")
			.SetString(
				StringCapability.ExitKeypadMode,
				"</keypad>")
			.SetString(
				StringCapability.CursorInvisible,
				"<hide>")
			.SetString(
				StringCapability.CursorNormal,
				"<show>")
			.Build();
	}

	private sealed class FakeTerminalInput
		: ITerminalInput
	{
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult(0);
		}
	}

	private sealed class FakeTerminalOutput
		: ITerminalOutput
	{
		private readonly IList<string> events;
		private readonly int? throwOnWrite;
		private readonly int? cancelOnWrite;
		private readonly CancellationTokenSource? cancellation;
		private int writeCount;

		internal FakeTerminalOutput(
			IList<string> events,
			int? throwOnWrite = null,
			int? cancelOnWrite = null,
			CancellationTokenSource? cancellation = null)
		{
			ArgumentNullException.ThrowIfNull(events);

			this.events = events;
			this.throwOnWrite = throwOnWrite;
			this.cancelOnWrite = cancelOnWrite;
			this.cancellation = cancellation;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			writeCount++;

			events.Add(
				"write:"
				+ Encoding.Latin1.GetString(
					buffer.Span));

			if (throwOnWrite == writeCount)
			{
				throw new IOException(
					"Synthetic terminal output failure.");
			}

			if (cancelOnWrite == writeCount)
			{
				cancellation!.Cancel();
				cancellationToken.ThrowIfCancellationRequested();
			}

			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			events.Add("flush");
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeDimensionProvider
		: ITerminalDimensionProvider
	{
		public TerminalBackendResult<TerminalSize> GetDimensions()
		{
			return TerminalBackendResult<TerminalSize>.Available(
				new TerminalSize(
					80,
					24));
		}
	}

	private sealed class FakeSessionModeController
		: ITerminalSessionModeController
	{
		private readonly IList<string> events;

		internal FakeSessionModeController(
			IList<string> events)
		{
			ArgumentNullException.ThrowIfNull(events);
			this.events = events;
		}

		internal bool FailApply
		{
			get;
			init;
		}

		internal bool ThrowApply
		{
			get;
			init;
		}

		internal int RestoreCount
		{
			get;
			private set;
		}

		public TerminalBackendResult<ITerminalModeState> CaptureMode()
		{
			events.Add("mode:capture");

			return TerminalBackendResult<ITerminalModeState>.Available(
				new FakeTerminalModeState());
		}

		public TerminalBackendMutationResult ApplySessionMode(
			ITerminalModeState baseline,
			CursesInputMode inputMode,
			bool echoInput)
		{
			ArgumentNullException.ThrowIfNull(baseline);

			events.Add(
				$"mode:apply:{inputMode}:{echoInput}");

			if (ThrowApply)
			{
				throw new InvalidOperationException(
					"Synthetic mode exception.");
			}

			return FailApply
				? TerminalBackendMutationResult.Failed(
					"Synthetic mode failure.")
				: TerminalBackendMutationResult.Success();
		}

		public TerminalBackendMutationResult RestoreMode(
			ITerminalModeState state,
			TerminalModeApplyTiming timing)
		{
			ArgumentNullException.ThrowIfNull(state);

			if (!Enum.IsDefined(timing))
			{
				throw new ArgumentOutOfRangeException(nameof(timing));
			}

			RestoreCount++;

			events.Add(
				$"mode:restore:{timing}");

			return TerminalBackendMutationResult.Success();
		}
	}

	private sealed class FakeTerminalModeState
		: ITerminalModeState
	{
	}
}
