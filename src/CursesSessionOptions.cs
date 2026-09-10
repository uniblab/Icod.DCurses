/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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

	/// <summary>
	/// Gets or initializes whether each refresh should be framed by the Terminal synchronized-output
	/// lease using DEC private mode 2026.
	/// </summary>
	/// <remarks>
	/// This option is disabled by default because framing adds fixed output overhead to every refresh.
	/// Enabling it requests optimistic synchronized-output framing; it does not prove that the physical
	/// terminal recognizes or continues honoring DEC private mode 2026.
	/// </remarks>
	public bool UseSynchronizedOutput
	{
		get;
		init;
	}

	/// <summary>Validates the configured session options.</summary>
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
