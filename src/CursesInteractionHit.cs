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

/// <summary>Represents one resolved interaction-region hit in region-local coordinates.</summary>
public sealed class CursesInteractionHit {
	internal CursesInteractionHit(
		CursesInteractionRegion region,
		int localRow,
		int localColumn
	) {
		ArgumentNullException.ThrowIfNull( region );
		if ( 0 > localRow ) {
			throw new ArgumentOutOfRangeException( nameof( localRow ) );
		}
		if ( 0 > localColumn ) {
			throw new ArgumentOutOfRangeException( nameof( localColumn ) );
		}

		this.Region = region;
		this.LocalRow = localRow;
		this.LocalColumn = localColumn;
	}

	/// <summary>Gets the interaction region selected by the hit test.</summary>
	public CursesInteractionRegion Region {
		get;
	}

	/// <summary>Gets the zero-based row relative to the declared region origin.</summary>
	public int LocalRow {
		get;
	}

	/// <summary>Gets the zero-based column relative to the declared region origin.</summary>
	public int LocalColumn {
		get;
	}
}
