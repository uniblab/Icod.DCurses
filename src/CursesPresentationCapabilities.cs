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

using Icod.DCurses.Internal;
using Icod.Terminal;

/// <summary>
/// Describes terminal presentation capabilities through curses-shaped semantic observations.
/// </summary>
/// <remarks>
/// This value contains no terminal escape sequences. It reports immutable facts derived from the
/// session's Terminal-owned profile so ordinary applications do not need to inspect raw
/// terminal capability strings merely to make common presentation decisions.
/// </remarks>
public readonly record struct CursesPresentationCapabilities {
	internal CursesPresentationCapabilities(
		int indexedColorCount,
		bool supportsDirectRgb,
		bool supportsForegroundColor,
		bool supportsBackgroundColor,
		bool supportsDefaultColorRestoration,
		CursesTextAttributes supportedAttributes,
		CursesTextAttributes colorRestrictedAttributes,
		bool supportsAlternateCharacterSet,
		bool supportsCursorHidden,
		bool supportsCursorNormal,
		bool supportsCursorVeryVisible
	) {
		IndexedColorCount = indexedColorCount;
		SupportsDirectRgb = supportsDirectRgb;
		SupportsForegroundColor = supportsForegroundColor;
		SupportsBackgroundColor = supportsBackgroundColor;
		SupportsDefaultColorRestoration = supportsDefaultColorRestoration;
		SupportedAttributes = supportedAttributes;
		ColorRestrictedAttributes = colorRestrictedAttributes;
		SupportsAlternateCharacterSet = supportsAlternateCharacterSet;
		SupportsCursorHidden = supportsCursorHidden;
		SupportsCursorNormal = supportsCursorNormal;
		SupportsCursorVeryVisible = supportsCursorVeryVisible;
	}

	/// <summary>Gets the number of safely addressable indexed terminal colors.</summary>
	public int IndexedColorCount {
		get;
	}

	/// <summary>Gets whether the terminal advertises direct RGB color semantics.</summary>
	public bool SupportsDirectRgb {
		get;
	}

	/// <summary>Gets whether a usable foreground-color selector is available.</summary>
	public bool SupportsForegroundColor {
		get;
	}

	/// <summary>Gets whether a usable background-color selector is available.</summary>
	public bool SupportsBackgroundColor {
		get;
	}

	/// <summary>Gets whether rendition can restore terminal-default colors after explicit color use.</summary>
	public bool SupportsDefaultColorRestoration {
		get;
	}

	/// <summary>Gets the text attributes with a directly advertised terminal representation.</summary>
	public CursesTextAttributes SupportedAttributes {
		get;
	}

	/// <summary>
	/// Gets the supported text attributes that the terminal reports as unavailable while color is active.
	/// </summary>
	public CursesTextAttributes ColorRestrictedAttributes {
		get;
	}

	/// <summary>Gets whether a complete alternate-character-set mapping/enter/exit contract is available.</summary>
	public bool SupportsAlternateCharacterSet {
		get;
	}

	/// <summary>Gets whether the physical cursor can be hidden through the selected terminal profile.</summary>
	public bool SupportsCursorHidden {
		get;
	}

	/// <summary>Gets whether the normal physical cursor presentation is explicitly available.</summary>
	public bool SupportsCursorNormal {
		get;
	}

	/// <summary>Gets whether the terminal advertises a very-visible cursor presentation.</summary>
	public bool SupportsCursorVeryVisible {
		get;
	}

	/// <summary>Gets whether at least one indexed or direct color representation is available.</summary>
	public bool SupportsColor => 0 < IndexedColorCount || SupportsDirectRgb;

	/// <summary>Gets whether bold rendition has a directly advertised representation.</summary>
	public bool SupportsBold => 0 != ( SupportedAttributes & CursesTextAttributes.Bold );

	/// <summary>Gets whether dim rendition has a directly advertised representation.</summary>
	public bool SupportsDim => 0 != ( SupportedAttributes & CursesTextAttributes.Dim );

	/// <summary>Gets whether underline rendition has a directly advertised representation.</summary>
	public bool SupportsUnderline => 0 != ( SupportedAttributes & CursesTextAttributes.Underline );

	/// <summary>Gets whether reverse-video rendition has a directly advertised representation.</summary>
	public bool SupportsReverse => 0 != ( SupportedAttributes & CursesTextAttributes.Reverse );

	/// <summary>Gets whether standout rendition has a directly advertised representation.</summary>
	public bool SupportsStandout => 0 != ( SupportedAttributes & CursesTextAttributes.Standout );

	/// <summary>Gets whether italic rendition has a directly advertised representation.</summary>
	public bool SupportsItalic => 0 != ( SupportedAttributes & CursesTextAttributes.Italic );

	/// <summary>Gets whether blink rendition has a directly advertised representation.</summary>
	public bool SupportsBlink => 0 != ( SupportedAttributes & CursesTextAttributes.Blink );

	/// <summary>Gets whether conceal/invisible rendition has a directly advertised representation.</summary>
	public bool SupportsConceal => 0 != ( SupportedAttributes & CursesTextAttributes.Conceal );

	/// <summary>Gets whether strikeout rendition has a directly advertised representation.</summary>
	public bool SupportsStrikeout => 0 != ( SupportedAttributes & CursesTextAttributes.Strikeout );

	/// <summary>Derives a curses-shaped presentation view from Terminal-owned capabilities.</summary>
	internal static CursesPresentationCapabilities Create(
		TerminalScreenCapabilities capabilities
	) {
		return new CursesPresentationCapabilities(
			capabilities.IndexedColorCount,
			capabilities.SupportsDirectRgb,
			capabilities.SupportsForegroundColor,
			capabilities.SupportsBackgroundColor,
			capabilities.SupportsDefaultColorRestoration,
			CursesTerminalScreenMapper.ToCurses( capabilities.SupportedAttributes ),
			CursesTerminalScreenMapper.ToCurses( capabilities.ColorRestrictedAttributes ),
			capabilities.SupportsAlternateCharacterSet,
			capabilities.SupportsCursorHidden,
			capabilities.SupportsCursorNormal,
			capabilities.SupportsCursorVeryVisible
		);
	}
}
