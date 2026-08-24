using Icod.CommandFramework.Terminal;
using Xunit;
using DCursesTerminal = Icod.DCurses.Terminal;

namespace Icod.DCurses.Tests;

public sealed class SystemTerminalModeEditorTests {

	[Fact]
	public void LinuxCanonicalEnablesCanonicalModeAndHonorsEchoPolicy() {
		TerminalModeSnapshot baseline = CreateLinuxMode(
			localFlags: 0
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.Canonical,
			echoInput: true
		);

		Assert.NotEqual(
			0UL,
			configured.LocalFlags & 0x0002UL
		);
		Assert.NotEqual(
			0UL,
			configured.LocalFlags & 0x0008UL
		);
	}

	[Fact]
	public void LinuxCBreakDisablesCanonicalModeAndEchoButKeepsSignals() {
		byte[] controlCharacters = new byte[ 32 ];
		TerminalModeSnapshot baseline = CreateLinuxMode(
			localFlags: 0xFFFFUL,
			controlCharacters: controlCharacters
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.CBreak,
			echoInput: false
		);

		Assert.Equal(
			0UL,
			configured.LocalFlags & 0x0002UL
		);
		Assert.Equal(
			0UL,
			configured.LocalFlags & 0x0008UL
		);
		Assert.NotEqual(
			0UL,
			configured.LocalFlags & 0x0001UL
		);
		Assert.Equal(
			1,
			configured.ControlCharacters[ 6 ]
		);
		Assert.Equal(
			0,
			configured.ControlCharacters[ 5 ]
		);
	}

	[Fact]
	public void LinuxRawAppliesEightBitUnprocessedMode() {
		byte[] controlCharacters = Enumerable.Repeat(
			(byte)0x7F,
			32
		).ToArray();

		TerminalModeSnapshot baseline = CreateLinuxMode(
			inputFlags: 0xFFFFFFFFUL,
			outputFlags: 0xFFFFFFFFUL,
			controlFlags: 0xFFFFFFFFUL,
			localFlags: 0xFFFFFFFFUL,
			controlCharacters
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.Raw,
			echoInput: false
		);

		const ulong clearedInput =
			0x0001UL
			| 0x0002UL
			| 0x0008UL
			| 0x0010UL
			| 0x0020UL
			| 0x0040UL
			| 0x0080UL
			| 0x0100UL
			| 0x0400UL
			| 0x1000UL
		;

		Assert.Equal(
			0UL,
			configured.InputFlags & clearedInput
		);
		Assert.Equal(
			0UL,
			configured.OutputFlags & 0x0001UL
		);
		Assert.Equal(
			0x0030UL,
			configured.ControlFlags & 0x0030UL
		);
		Assert.Equal(
			0UL,
			configured.ControlFlags & 0x0100UL
		);
		Assert.Equal(
			0UL,
			configured.LocalFlags
				& ( 0x0001UL | 0x0002UL | 0x0008UL | 0x0040UL | 0x8000UL )
		);
		Assert.Equal(
			1,
			configured.ControlCharacters[ 6 ]
		);
		Assert.Equal(
			0,
			configured.ControlCharacters[ 5 ]
		);
	}

	[Fact]
	public void MacOsCBreakUsesDarwinControlCharacterSlots() {
		byte[] controlCharacters = new byte[ 20 ];

		TerminalModeSnapshot baseline = TerminalModeSnapshot.CreatePosix(
			0,
			0,
			0,
			0xFFFFUL,
			controlCharacters,
			0xFF,
			64,
			null,
			new TerminalSpeed( 9600, 9600 ),
			new TerminalSpeed( 9600, 9600 )
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.CBreak,
			echoInput: false
		);

		Assert.Equal(
			0UL,
			configured.LocalFlags & 0x0100UL
		);
		Assert.NotEqual(
			0UL,
			configured.LocalFlags & 0x0080UL
		);
		Assert.Equal(
			1,
			configured.ControlCharacters[ 16 ]
		);
		Assert.Equal(
			0,
			configured.ControlCharacters[ 17 ]
		);
	}

	[Fact]
	public void WindowsCBreakRetainsProcessedInputAndEnablesVirtualTerminalInput() {
		TerminalModeSnapshot baseline = TerminalModeSnapshot.CreateWindowsConsole(
			TerminalConsoleDirection.Input,
			0x0007U
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.CBreak,
			echoInput: false
		);

		uint mode = configured.ConsoleMode!.Value;

		Assert.NotEqual(
			0U,
			mode & 0x0001U
		);
		Assert.Equal(
			0U,
			mode & 0x0002U
		);
		Assert.Equal(
			0U,
			mode & 0x0004U
		);
		Assert.NotEqual(
			0U,
			mode & 0x0200U
		);
	}

	[Fact]
	public void WindowsRawDisablesProcessedLineAndEchoInput() {
		TerminalModeSnapshot baseline = TerminalModeSnapshot.CreateWindowsConsole(
			TerminalConsoleDirection.Input,
			0x0007U
		);

		TerminalModeSnapshot configured = DCursesTerminal.SystemTerminalModeEditor.Configure(
			baseline,
			CursesInputMode.Raw,
			echoInput: false
		);

		uint mode = configured.ConsoleMode!.Value;

		Assert.Equal(
			0U,
			mode & 0x0007U
		);
		Assert.NotEqual(
			0U,
			mode & 0x0200U
		);
	}

	private static TerminalModeSnapshot CreateLinuxMode(
		ulong inputFlags = 0,
		ulong outputFlags = 0,
		ulong controlFlags = 0,
		ulong localFlags = 0,
		byte[]? controlCharacters = null ) {
		return TerminalModeSnapshot.CreatePosix(
			inputFlags,
			outputFlags,
			controlFlags,
			localFlags,
			controlCharacters
				?? new byte[ 32 ],
			0,
			32,
			0,
			new TerminalSpeed( 13, 9600 ),
			new TerminalSpeed( 13, 9600 )
		);
	}
}
