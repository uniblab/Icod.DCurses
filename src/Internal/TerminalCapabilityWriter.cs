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

namespace Icod.DCurses.Internal;

using Icod.DCurses.Terminal;

/// <summary>Writes expanded terminfo capability data to a terminal output service.</summary>
internal static class TerminalCapabilityWriter {
	/// <summary>Writes one terminal capability using terminfo output semantics.</summary>
	/// <param name="output">The destination terminal output service.</param>
	/// <param name="capability">The expanded capability data.</param>
	/// <param name="cancellationToken">Cancellation for the write operation.</param>
	/// <returns>A value task representing the asynchronous write.</returns>
	internal static ValueTask WriteAsync(
		ITerminalOutput output,
		string capability,
		CancellationToken cancellationToken
	) {
		return WriteAsync(
			output,
			capability,
			affectedLines: 1,
			cancellationToken
		);
	}

	/// <summary>Writes one terminal capability using terminfo output semantics.</summary>
	/// <param name="output">The destination terminal output service.</param>
	/// <param name="capability">The expanded capability data.</param>
	/// <param name="affectedLines">The positive number of lines affected for padding semantics.</param>
	/// <param name="cancellationToken">Cancellation for the write operation.</param>
	/// <returns>A value task representing the asynchronous write.</returns>
	internal static ValueTask WriteAsync(
		ITerminalOutput output,
		string capability,
		int affectedLines,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( output );
		ArgumentNullException.ThrowIfNull( capability );
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		return output.WriteTerminalStringAsync(
			capability,
			affectedLines,
			cancellationToken
		);
	}
}
