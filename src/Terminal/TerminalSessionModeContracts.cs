namespace Icod.DCurses.Terminal;

/// <summary>
/// Extends host terminal-mode capture/restoration with the semantic input-mode transition required
/// by a live curses session.
/// </summary>
public interface ITerminalSessionModeController
	: ITerminalModeController
{
	/// <summary>
	/// Applies the requested curses input discipline and echo policy relative to a captured host
	/// baseline.
	/// </summary>
	/// <param name="baseline">The host mode captured before session mutation.</param>
	/// <param name="inputMode">The requested curses input discipline.</param>
	/// <param name="echoInput">Whether host input echo should remain enabled.</param>
	/// <returns>The controlled mutation result.</returns>
	TerminalBackendMutationResult ApplySessionMode(
		ITerminalModeState baseline,
		CursesInputMode inputMode,
		bool echoInput);
}
