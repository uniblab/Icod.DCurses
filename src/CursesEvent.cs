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
/// Identifies the high-level event returned by <see cref="CursesSession.ReadEventAsync(CancellationToken)"/>.
/// </summary>
public enum CursesEventKind {
	/// <summary>A decoded keyboard or end-of-input event.</summary>
	Input,

	/// <summary>A terminal or process lifecycle event.</summary>
	Lifecycle,

	/// <summary>The requested wait interval or deadline expired.</summary>
	Timeout
}

/// <summary>
/// Represents one event consumed by a curses application event loop.
/// </summary>
public sealed class CursesEvent {
	private CursesEvent(
		CursesEventKind kind,
		CursesInputEvent? input,
		CursesLifecycleEvent? lifecycle) {
		Kind = kind;
		Input = input;
		Lifecycle = lifecycle;
	}

	/// <summary>Gets the high-level event kind.</summary>
	public CursesEventKind Kind {
		get;
	}

	/// <summary>Gets the decoded input event when <see cref="Kind"/> is <see cref="CursesEventKind.Input"/>.</summary>
	public CursesInputEvent? Input {
		get;
	}

	/// <summary>
	/// Gets the lifecycle event when <see cref="Kind"/> is <see cref="CursesEventKind.Lifecycle"/>.
	/// </summary>
	public CursesLifecycleEvent? Lifecycle {
		get;
	}

	/// <summary>Gets whether the event requires the physical screen to be repainted.</summary>
	public bool RequiresRepaint =>
		null != Lifecycle
		&& Lifecycle.RequiresRepaint;

	/// <summary>Creates a unified event carrying decoded terminal input.</summary>
	/// <param name="input">The decoded input event.</param>
	/// <returns>The unified input event.</returns>
	internal static CursesEvent FromInput( CursesInputEvent input ) {
		ArgumentNullException.ThrowIfNull( input );
		return new CursesEvent(
			CursesEventKind.Input,
			input,
			null
		);
	}

	/// <summary>Creates a unified event carrying a lifecycle notification.</summary>
	/// <param name="lifecycle">The lifecycle notification.</param>
	/// <returns>The unified lifecycle event.</returns>
	internal static CursesEvent FromLifecycle( CursesLifecycleEvent lifecycle ) {
		ArgumentNullException.ThrowIfNull( lifecycle );
		return new CursesEvent(
			CursesEventKind.Lifecycle,
			null,
			lifecycle
		);
	}

	/// <summary>Creates a unified timeout event.</summary>
	/// <returns>The timeout event.</returns>
	internal static CursesEvent TimedOut() {
		return new CursesEvent(
			CursesEventKind.Timeout,
			null,
			null
		);
	}
}