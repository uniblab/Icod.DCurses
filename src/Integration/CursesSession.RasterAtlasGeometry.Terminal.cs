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

/// <summary>Terminal-backed raster-atlas geometry observation.</summary>
public sealed partial class CursesSession {
	/// <summary>Queries exact current cell pixels and projects them across an atlas grid.</summary>
	/// <param name="rows">Atlas height in terminal cells, from 1 through 256.</param>
	/// <param name="columns">Atlas width in terminal cells, from 1 through 256.</param>
	/// <param name="timeout">Positive timeout for each Terminal geometry query.</param>
	/// <param name="cancellationToken">Cancellation for activity acquisition and queries.</param>
	/// <returns>Fresh exact cell and atlas pixel geometry.</returns>
	/// <exception cref="ArgumentOutOfRangeException">An axis is outside its bound or <paramref name="timeout"/> is not positive.</exception>
	/// <exception cref="InvalidOperationException">Exact cell pixels cannot be observed or derived.</exception>
	/// <exception cref="TimeoutException">Both supported geometry query paths time out.</exception>
	/// <exception cref="OperationCanceledException">Cancellation is requested.</exception>
	public ValueTask<CursesRasterAtlasGeometry> QueryRasterAtlasGeometryAsync(
		int rows,
		int columns,
		TimeSpan timeout,
		CancellationToken cancellationToken = default
	) {
		CursesRasterAtlasGeometry.ValidateCellAxis( rows, nameof( rows ) );
		CursesRasterAtlasGeometry.ValidateCellAxis( columns, nameof( columns ) );
		if ( TimeSpan.Zero >= timeout ) {
			throw new ArgumentOutOfRangeException(
				nameof( timeout ),
				timeout,
				"A raster-atlas geometry query timeout must be positive."
			);
		}
		cancellationToken.ThrowIfCancellationRequested();
		return QueryRasterAtlasGeometryCoreAsync(
			rows,
			columns,
			timeout,
			cancellationToken
		);
	}

	private async ValueTask<CursesRasterAtlasGeometry> QueryRasterAtlasGeometryCoreAsync(
		int rows,
		int columns,
		TimeSpan timeout,
		CancellationToken cancellationToken
	) {
		using IDisposable activity = await AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );

		TerminalPixelDimensions cellPixels;
		try {
			cellPixels = await terminalSession.QueryCellPixelDimensionsAsync(
				timeout,
				cancellationToken
			).ConfigureAwait( false );
		} catch ( TimeoutException ) {
			TerminalPixelDimensions terminalPixels =
				await terminalSession.QueryTerminalPixelDimensionsAsync(
					timeout,
					cancellationToken
				).ConfigureAwait( false );
			TerminalControlResult<TerminalDimensions> dimensions = terminalSession.GetDimensions();
			if ( !dimensions.IsAvailable ) {
				throw new InvalidOperationException(
					dimensions.Message
						?? "Current terminal character dimensions are unavailable for exact pixel derivation."
				);
			}
			if ( !TerminalPixelGeometry.TryDeriveCellDimensions(
				dimensions.GetRequiredValue(),
				terminalPixels,
				out cellPixels
			) ) {
				throw new InvalidOperationException(
					"Terminal pixels are not exactly divisible by current character dimensions."
				);
			}
		}

		return new CursesRasterAtlasGeometry(
			rows,
			columns,
			cellPixels.Width,
			cellPixels.Height
		);
	}
}
