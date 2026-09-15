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

using Icod.DCurses.Internal;
using Icod.Terminal;

/// <summary>Terminal-backed retained-raster ownership operations.</summary>
public sealed partial class CursesSession {
	/// <summary>Uploads one backend-neutral raster as a session-owned persistent resource.</summary>
	/// <param name="image">The caller-owned immutable raster image.</param>
	/// <param name="cancellationToken">Cancellation observed by the underlying Terminal transaction.</param>
	/// <returns>A controlled result containing the DCurses resource when available.</returns>
	public ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceAsync(
		TerminalRasterImage image,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( image );
		cancellationToken.ThrowIfCancellationRequested();
		return this.CreateRasterResourceCoreAsync(
			image,
			cancellationToken
		);
	}

	private async ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceCoreAsync(
		TerminalRasterImage image,
		CancellationToken cancellationToken
	) {
		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		TerminalControlResult<TerminalRasterResource> result =
			await this.terminalSession.CreateRasterResourceAsync(
				image,
				cancellationToken
			).ConfigureAwait( false );
		return CursesTerminalControlResultMapper.Map(
			result,
			resource => new CursesRasterResource(
				this,
				resource
			)
		);
	}
}
