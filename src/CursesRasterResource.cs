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

/// <summary>Owns one Terminal-backed persistent raster resource for a DCurses session.</summary>
public sealed class CursesRasterResource : IAsyncDisposable {
	private const int MaximumPlaceholderDimension = 256;

	private readonly CursesSession owner;
	private TerminalRasterResource? resource;

	internal CursesRasterResource(
		CursesSession owner,
		TerminalRasterResource resource
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( resource );
		this.owner = owner;
		this.resource = resource;
	}

	/// <summary>Gets a side-effect-free snapshot of current raster ownership certainty.</summary>
	public CursesRasterOwnershipState OwnershipState {
		get {
			TerminalRasterResource? current = Volatile.Read( ref this.resource );
			return current is null
				? DisposedState
				: CursesRasterOwnershipMapper.FromTerminal( current.OwnershipState )
			;
		}
	}

	/// <summary>Creates one Terminal-backed virtual raster placeholder.</summary>
	/// <param name="columns">The placeholder width in terminal cells, from 1 through 256.</param>
	/// <param name="rows">The placeholder height in terminal cells, from 1 through 256.</param>
	/// <param name="cancellationToken">Cancellation observed by the underlying Terminal transaction.</param>
	/// <returns>A controlled result containing the DCurses placeholder when available.</returns>
	public ValueTask<TerminalControlResult<CursesRasterPlaceholder>> CreatePlaceholderAsync(
		int columns,
		int rows,
		CancellationToken cancellationToken = default
	) {
		ValidatePlaceholderDimension(
			columns,
			nameof( columns )
		);
		ValidatePlaceholderDimension(
			rows,
			nameof( rows )
		);
		cancellationToken.ThrowIfCancellationRequested();

		TerminalRasterResource current = Volatile.Read( ref this.resource )
			?? throw new ObjectDisposedException( nameof( CursesRasterResource ) );
		return this.CreatePlaceholderCoreAsync(
			current,
			columns,
			rows,
			cancellationToken
		);
	}

	/// <summary>Releases this resource and its Terminal-owned persistent-raster lifetime.</summary>
	public ValueTask DisposeAsync() {
		TerminalRasterResource? current = Interlocked.Exchange(
			ref this.resource,
			null
		);
		return current is null
			? ValueTask.CompletedTask
			: current.DisposeAsync()
		;
	}

	internal bool BelongsTo( CursesSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		return ReferenceEquals(
			this.owner,
			session
		);
	}

	private async ValueTask<TerminalControlResult<CursesRasterPlaceholder>> CreatePlaceholderCoreAsync(
		TerminalRasterResource current,
		int columns,
		int rows,
		CancellationToken cancellationToken
	) {
		TerminalControlResult<TerminalRasterPlaceholder> result = await current.CreatePlaceholderAsync(
			new TerminalRasterPlaceholderOptions {
				Columns = columns,
				Rows = rows
			},
			cancellationToken
		).ConfigureAwait( false );
		return CursesTerminalControlResultMapper.Map(
			result,
			placeholder => new CursesRasterPlaceholder(
				this.owner,
				placeholder
			)
		);
	}

	private static void ValidatePlaceholderDimension(
		int value,
		string parameterName
	) {
		if ( 1 > value || MaximumPlaceholderDimension < value ) {
			throw new ArgumentOutOfRangeException(
				parameterName,
				value,
				$"A raster placeholder dimension must be between 1 and {MaximumPlaceholderDimension}."
			);
		}
	}

	private static CursesRasterOwnershipState DisposedState => new(
		CursesRasterOwnershipStatus.Disposed,
		CursesRasterOwnershipLossReason.ExplicitDisposal
	);
}
