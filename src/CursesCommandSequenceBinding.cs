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

/// <summary>Describes one immutable bounded command-sequence binding.</summary>
public sealed class CursesCommandSequenceBinding {
	/// <summary>Initializes a detached command-sequence binding.</summary>
	/// <param name="gestures">The ordered bindable semantic gestures.</param>
	/// <param name="command">The application command identity.</param>
	public CursesCommandSequenceBinding(
		IReadOnlyList<CursesKeyGesture> gestures,
		CursesCommand command
	) {
		ArgumentNullException.ThrowIfNull( command );
		this.Gestures = Array.AsReadOnly(
			CursesCommandSequenceRegistration.ValidateAndCopy( gestures )
		);
		this.Command = command;
	}

	/// <summary>Gets the detached ordered gesture sequence.</summary>
	public IReadOnlyList<CursesKeyGesture> Gestures {
		get;
	}

	/// <summary>Gets the application command identity.</summary>
	public CursesCommand Command {
		get;
	}
}
