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

using Icod.TermInfo;

/// <summary>
/// Identifies a managed terminal or process lifecycle event observed by a curses session.
/// </summary>
public enum CursesLifecycleEventKind {
	/// <summary>The terminal dimensions may have changed.</summary>
	Resize,

	/// <summary>An interactive interrupt request was observed.</summary>
	Interrupt,

	/// <summary>A process termination request was observed.</summary>
	Termination,

	/// <summary>The session prepared higher-layer state immediately before POSIX suspension.</summary>
	Suspending,

	/// <summary>The process resumed and the curses presentation was re-entered.</summary>
	Resumed
}

/// <summary>Represents one lifecycle notification delivered by a <see cref="CursesSession"/>.</summary>
public sealed class CursesLifecycleEvent {
	internal CursesLifecycleEvent(
		CursesLifecycleEventKind kind,
		TerminalSize? dimensions = null
	) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		this.Kind = kind;
		this.Dimensions = dimensions;
	}

	/// <summary>Gets the lifecycle event kind.</summary>
	public CursesLifecycleEventKind Kind {
		get;
	}

	/// <summary>Gets fresh dimensions for resize/resume events when available.</summary>
	public TerminalSize? Dimensions {
		get;
	}

	/// <summary>Gets whether the physical-screen image should be treated as invalid.</summary>
	public bool RequiresRepaint =>
		this.Kind is CursesLifecycleEventKind.Resize
			or CursesLifecycleEventKind.Resumed;
}
