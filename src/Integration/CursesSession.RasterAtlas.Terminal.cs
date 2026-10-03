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

using Icod.Terminal;

/// <summary>Terminal-backed raster-atlas creation.</summary>
public sealed partial class CursesSession {
	/// <summary>Creates one retained raster atlas from an exactly divisible initial image.</summary>
	public ValueTask<TerminalControlResult<CursesRasterAtlas>> CreateRasterAtlasAsync(
		TerminalRasterImage initialImage,
		int rows,
		int columns,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( initialImage );
		CursesRasterAtlasGeometry.ValidateCellAxis( rows, nameof( rows ) );
		CursesRasterAtlasGeometry.ValidateCellAxis( columns, nameof( columns ) );
		if ( 0 != initialImage.Width % columns ) {
			throw new ArgumentException(
				"The initial image width must divide exactly across atlas columns.",
				nameof( initialImage )
			);
		}
		if ( 0 != initialImage.Height % rows ) {
			throw new ArgumentException(
				"The initial image height must divide exactly across atlas rows.",
				nameof( initialImage )
			);
		}
		cancellationToken.ThrowIfCancellationRequested();
		return ValueTask.FromResult(
			TerminalControlResult<CursesRasterAtlas>.Unavailable(
				"Raster-atlas creation is not yet initialized."
			)
		);
	}
}
