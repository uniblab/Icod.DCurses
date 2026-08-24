namespace Icod.DCurses.Terminal;

using FrameworkTerminal = Icod.CommandFramework.Terminal;

/// <summary>Applies curses input-discipline policy to captured platform terminal modes.</summary>
internal static class SystemTerminalModeEditor
{
	private const ulong InputIgnoreBreak = 0x0001UL;
	private const ulong InputBreakInterrupt = 0x0002UL;
	private const ulong InputParityMark = 0x0008UL;
	private const ulong InputParityCheck = 0x0010UL;
	private const ulong InputStrip = 0x0020UL;
	private const ulong InputMapNewLineToCarriageReturn = 0x0040UL;
	private const ulong InputIgnoreCarriageReturn = 0x0080UL;
	private const ulong InputMapCarriageReturnToNewLine = 0x0100UL;
	private const ulong InputSoftwareFlowControl = 0x0400UL;
	private const ulong InputSoftwareFlowControlOutput = 0x1000UL;

	private const ulong OutputPostProcess = 0x0001UL;

	private const uint WindowsEnableProcessedInput = 0x0001U;
	private const uint WindowsEnableLineInput = 0x0002U;
	private const uint WindowsEnableEchoInput = 0x0004U;
	private const uint WindowsEnableVirtualTerminalInput = 0x0200U;

	/// <summary>Creates a terminal-mode snapshot configured for the requested curses input policy.</summary>
	/// <param name="baseline">The captured host terminal mode.</param>
	/// <param name="inputMode">The requested curses input discipline.</param>
	/// <param name="echoInput">Whether host input echo remains enabled.</param>
	/// <returns>The configured terminal-mode snapshot.</returns>
	internal static FrameworkTerminal.TerminalModeSnapshot Configure(
		FrameworkTerminal.TerminalModeSnapshot baseline,
		CursesInputMode inputMode,
		bool echoInput)
	{
		ArgumentNullException.ThrowIfNull(baseline);

		if (!Enum.IsDefined(inputMode))
		{
			throw new ArgumentOutOfRangeException(nameof(inputMode));
		}

		return baseline.Platform switch
		{
			FrameworkTerminal.TerminalPlatformKind.PosixTermios =>
				ConfigurePosix(
					baseline,
					inputMode,
					echoInput),
			FrameworkTerminal.TerminalPlatformKind.WindowsConsole =>
				ConfigureWindows(
					baseline,
					inputMode,
					echoInput),
			_ => throw new ArgumentOutOfRangeException(
				nameof(baseline),
				baseline.Platform,
				"The terminal platform is not recognized.")
		};
	}

	private static FrameworkTerminal.TerminalModeSnapshot ConfigurePosix(
		FrameworkTerminal.TerminalModeSnapshot baseline,
		CursesInputMode inputMode,
		bool echoInput)
	{
		bool macOsAbi = 64 == baseline.NativeFlagWidth;

		ulong inputFlags = baseline.InputFlags;
		ulong outputFlags = baseline.OutputFlags;
		ulong controlFlags = baseline.ControlFlags;
		ulong localFlags = baseline.LocalFlags;
		byte[] controlCharacters = baseline.ControlCharacters.ToArray();

		ulong echo = 0x0008UL;
		ulong echoNewLine = macOsAbi ? 0x0010UL : 0x0040UL;
		ulong signal = macOsAbi ? 0x0080UL : 0x0001UL;
		ulong canonical = macOsAbi ? 0x0100UL : 0x0002UL;
		ulong extended = macOsAbi ? 0x0400UL : 0x8000UL;
		ulong characterSize = macOsAbi ? 0x0300UL : 0x0030UL;
		ulong eightBitCharacters = characterSize;
		ulong parityEnable = macOsAbi ? 0x1000UL : 0x0100UL;

		switch (inputMode)
		{
			case CursesInputMode.Canonical:
				localFlags |= canonical;
				break;

			case CursesInputMode.CBreak:
				localFlags &= ~canonical;
				SetMinimumRead(
					controlCharacters,
					macOsAbi);
				break;

			case CursesInputMode.Raw:
				inputFlags &=
					~(
						InputIgnoreBreak
						| InputBreakInterrupt
						| InputParityMark
						| InputParityCheck
						| InputStrip
						| InputMapNewLineToCarriageReturn
						| InputIgnoreCarriageReturn
						| InputMapCarriageReturnToNewLine
						| InputSoftwareFlowControl
						| InputSoftwareFlowControlOutput);
				outputFlags &= ~OutputPostProcess;
				controlFlags &= ~(characterSize | parityEnable);
				controlFlags |= eightBitCharacters;
				localFlags &= ~(echo | echoNewLine | canonical | signal | extended);
				SetMinimumRead(
					controlCharacters,
					macOsAbi);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(inputMode));
		}

		if (echoInput)
		{
			localFlags |= echo;
		}
		else
		{
			localFlags &= ~(echo | echoNewLine);
		}

		return baseline.WithPosixSerializedState(
			inputFlags,
			outputFlags,
			controlFlags,
			localFlags,
			controlCharacters);
	}

	private static FrameworkTerminal.TerminalModeSnapshot ConfigureWindows(
		FrameworkTerminal.TerminalModeSnapshot baseline,
		CursesInputMode inputMode,
		bool echoInput)
	{
		if (FrameworkTerminal.TerminalConsoleDirection.Input != baseline.ConsoleDirection)
		{
			throw new InvalidOperationException(
				"A curses input mode requires a Windows console input snapshot.");
		}

		uint mode = baseline.ConsoleMode!.Value;

		switch (inputMode)
		{
			case CursesInputMode.Canonical:
				mode |= WindowsEnableProcessedInput | WindowsEnableLineInput;
				break;

			case CursesInputMode.CBreak:
				mode |= WindowsEnableProcessedInput | WindowsEnableVirtualTerminalInput;
				mode &= ~WindowsEnableLineInput;
				break;

			case CursesInputMode.Raw:
				mode |= WindowsEnableVirtualTerminalInput;
				mode &=
					~(
						WindowsEnableProcessedInput
						| WindowsEnableLineInput
						| WindowsEnableEchoInput);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(inputMode));
		}

		if (echoInput)
		{
			mode |= WindowsEnableEchoInput;
		}
		else
		{
			mode &= ~WindowsEnableEchoInput;
		}

		return FrameworkTerminal.TerminalModeSnapshot.CreateWindowsConsole(
			FrameworkTerminal.TerminalConsoleDirection.Input,
			mode);
	}

	private static void SetMinimumRead(
		byte[] controlCharacters,
		bool macOsAbi)
	{
		ArgumentNullException.ThrowIfNull(controlCharacters);

		int minimumIndex = macOsAbi ? 16 : 6;
		int timeoutIndex = macOsAbi ? 17 : 5;

		if ((controlCharacters.Length <= minimumIndex)
			|| (controlCharacters.Length <= timeoutIndex))
		{
			throw new InvalidOperationException(
				"The host terminal snapshot does not contain VMIN and VTIME control-character slots.");
		}

		controlCharacters[minimumIndex] = 1;
		controlCharacters[timeoutIndex] = 0;
	}
}
