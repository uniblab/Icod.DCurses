namespace Icod.DCurses;

/// <summary>
/// Selects the host input discipline used by a <see cref="CursesSession"/>.
/// </summary>
public enum CursesInputMode
{
	/// <summary>
	/// Preserve line-oriented canonical input while applying the requested echo policy.
	/// </summary>
	Canonical,

	/// <summary>
	/// Disable canonical line buffering while retaining host signal processing.
	/// </summary>
	CBreak,

	/// <summary>
	/// Disable canonical processing, host signal processing, and ordinary input translations.
	/// </summary>
	Raw
}

/// <summary>
/// Configures terminal state entered by a <see cref="CursesSession"/>.
/// </summary>
public sealed class CursesSessionOptions
{
	/// <summary>
	/// Gets or initializes the input discipline.
	/// </summary>
	public CursesInputMode InputMode
	{
		get;
		init;
	} = CursesInputMode.CBreak;

	/// <summary>
	/// Gets or initializes whether typed input is echoed by the host terminal.
	/// </summary>
	public bool EchoInput
	{
		get;
		init;
	}

	/// <summary>
	/// Gets or initializes whether cursor-addressing presentation mode should be entered when the
	/// terminal provides both entry and restoration capabilities.
	/// </summary>
	public bool UseAlternateScreen
	{
		get;
		init;
	} = true;

	/// <summary>
	/// Gets or initializes whether keypad/application transmit mode should be entered when the
	/// terminal provides both entry and restoration capabilities.
	/// </summary>
	public bool EnableKeypad
	{
		get;
		init;
	} = true;

	/// <summary>
	/// Gets or initializes whether the physical cursor should be hidden while the session is active
	/// when the terminal also provides a restoration capability.
	/// </summary>
	public bool HideCursor
	{
		get;
		init;
	} = true;

	internal void Validate()
	{
		if (!Enum.IsDefined(InputMode))
		{
			throw new ArgumentOutOfRangeException(
				nameof(InputMode),
				InputMode,
				"The curses input mode is not recognized.");
		}
	}
}
