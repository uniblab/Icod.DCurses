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

/// <summary>Represents an immutable nonnegative cell coordinate.</summary>
public readonly record struct CursesCellPosition {
	/// <summary>Initializes one zero-based cell position.</summary>
	public CursesCellPosition( int row, int column ) {
		if ( row < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		Row = row;
		Column = column;
	}

	/// <summary>Gets the zero-based row.</summary>
	public int Row { get; }

	/// <summary>Gets the zero-based column.</summary>
	public int Column { get; }
}
