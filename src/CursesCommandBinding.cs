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

/// <summary>Describes one effective semantic key binding without application behavior.</summary>
public sealed class CursesCommandBinding {
	/// <summary>Initializes an immutable effective-binding snapshot entry.</summary>
	/// <param name="gesture">The factory-created semantic keyboard gesture.</param>
	/// <param name="command">The application command identity.</param>
	public CursesCommandBinding(
		CursesKeyGesture gesture,
		CursesCommand command
	) {
		if ( !gesture.IsBindable ) {
			throw new ArgumentException(
				"The gesture must be created by a CursesKeyGesture factory.",
				nameof( gesture )
			);
		}
		ArgumentNullException.ThrowIfNull( command );
		this.Gesture = gesture;
		this.Command = command;
	}

	/// <summary>Gets the effective semantic keyboard gesture.</summary>
	public CursesKeyGesture Gesture {
		get;
	}

	/// <summary>Gets the application-owned command identity.</summary>
	public CursesCommand Command {
		get;
	}
}
