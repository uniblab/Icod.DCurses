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

/// <summary>Owns one Terminal-backed virtual raster placeholder for retained DCurses presentation.</summary>
public sealed class CursesRasterPlaceholder : IAsyncDisposable {
	private readonly CursesSession owner;
	private readonly int columns;
	private readonly int rows;
	private TerminalRasterPlaceholder? placeholder;

	internal CursesRasterPlaceholder(
		CursesSession owner,
		TerminalRasterPlaceholder placeholder
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( placeholder );
		this.owner = owner;
		this.placeholder = placeholder;
		this.columns = placeholder.Columns;
		this.rows = placeholder.Rows;
	}

	/// <summary>Gets the placeholder width in terminal cells.</summary>
	public int Columns => this.columns;

	/// <summary>Gets the placeholder height in terminal cells.</summary>
	public int Rows => this.rows;

	/// <summary>Gets a side-effect-free snapshot of current raster ownership certainty.</summary>
	public CursesRasterOwnershipState OwnershipState {
		get {
			TerminalRasterPlaceholder? current = Volatile.Read( ref this.placeholder );
			return current is null
				? DisposedState
				: CursesRasterOwnershipMapper.FromTerminal( current.OwnershipState )
			;
		}
	}

	/// <summary>Creates one retained semantic cell token for a coordinate in this placeholder.</summary>
	/// <param name="row">The zero-based placeholder row.</param>
	/// <param name="column">The zero-based placeholder column.</param>
	/// <returns>The opaque DCurses raster cell.</returns>
	public CursesRasterCell GetCell(
		int row,
		int column
	) {
		TerminalRasterPlaceholder current = Volatile.Read( ref this.placeholder )
			?? throw new ObjectDisposedException( nameof( CursesRasterPlaceholder ) );
		TerminalRasterPlaceholderCell cell = current.GetCell(
			row,
			column
		);
		return new CursesRasterCell(
			this,
			cell
		);
	}

	/// <summary>Releases this placeholder and its Terminal-owned virtual-placement lifetime.</summary>
	public ValueTask DisposeAsync() {
		TerminalRasterPlaceholder? current = Interlocked.Exchange(
			ref this.placeholder,
			null
		);
		if ( current is null ) {
			return ValueTask.CompletedTask;
		}

		this.owner.InvalidatePhysicalScreen();
		return current.DisposeAsync();
	}

	internal bool BelongsTo( CursesSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		return ReferenceEquals(
			this.owner,
			session
		);
	}

	private static CursesRasterOwnershipState DisposedState => new(
		CursesRasterOwnershipStatus.Disposed,
		CursesRasterOwnershipLossReason.ExplicitDisposal
	);
}
