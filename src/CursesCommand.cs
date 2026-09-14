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

/// <summary>Identifies an application command without carrying executable behavior.</summary>
public sealed record CursesCommand {
	/// <summary>Gets the maximum command-name length in UTF-16 code units.</summary>
	public const int MaximumNameLength = 128;

	/// <summary>Initializes a command identity with its exact ordinal name.</summary>
	/// <param name="name">The non-whitespace command name.</param>
	public CursesCommand(
		string name
	) {
		ArgumentNullException.ThrowIfNull( name );
		if ( MaximumNameLength < name.Length ) {
			throw new ArgumentOutOfRangeException( nameof( name ) );
		}
		if ( string.IsNullOrWhiteSpace( name ) ) {
			throw new ArgumentException(
				"The command name must contain at least one non-whitespace character.",
				nameof( name )
			);
		}

		this.Name = name;
	}

	/// <summary>Gets the exact ordinal command name.</summary>
	public string Name {
		get;
	}

	/// <summary>Returns the exact command name.</summary>
	/// <returns>The exact command name.</returns>
	public override string ToString() {
		return this.Name;
	}
}
