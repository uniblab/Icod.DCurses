/*
	Icod.DCurses
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.DCurses.Internal;

using Icod.Terminal;

/// <summary>One call's explicit frame; never stored in physical-screen state.</summary>
internal readonly record struct CursesRasterRefreshFrame( TerminalRasterImage Image, CursesRectangle Bounds );
