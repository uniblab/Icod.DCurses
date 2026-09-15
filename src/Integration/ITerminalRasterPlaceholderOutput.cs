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

namespace Icod.DCurses.Terminal;

/// <summary>Optional typed raster-placeholder output implemented by Terminal-backed refresh output.</summary>
internal interface ITerminalRasterPlaceholderOutput {
	/// <summary>Writes one retained raster-placeholder cell at the current terminal cursor.</summary>
	/// <param name="cell">The opaque retained raster cell to emit.</param>
	/// <param name="cancellationToken">Cancellation observed before Terminal commits placeholder output.</param>
	ValueTask WriteRasterPlaceholderCellAsync(
		CursesRasterCell cell,
		CancellationToken cancellationToken = default
	);
}
