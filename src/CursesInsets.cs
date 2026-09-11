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

/// <summary>Represents non-negative terminal-cell insets from four rectangle edges.</summary>
public readonly record struct CursesInsets {
	/// <summary>Initializes immutable terminal-cell insets.</summary>
	/// <param name="top">The non-negative top inset.</param>
	/// <param name="right">The non-negative right inset.</param>
	/// <param name="bottom">The non-negative bottom inset.</param>
	/// <param name="left">The non-negative left inset.</param>
	public CursesInsets(
		int top,
		int right,
		int bottom,
		int left
	) {
		if ( 0 > top ) {
			throw new ArgumentOutOfRangeException( nameof( top ) );
		}
		if ( 0 > right ) {
			throw new ArgumentOutOfRangeException( nameof( right ) );
		}
		if ( 0 > bottom ) {
			throw new ArgumentOutOfRangeException( nameof( bottom ) );
		}
		if ( 0 > left ) {
			throw new ArgumentOutOfRangeException( nameof( left ) );
		}
		if ( top > int.MaxValue - bottom ) {
			throw new ArgumentOutOfRangeException( nameof( bottom ) );
		}
		if ( left > int.MaxValue - right ) {
			throw new ArgumentOutOfRangeException( nameof( right ) );
		}

		Top = top;
		Right = right;
		Bottom = bottom;
		Left = left;
	}

	/// <summary>Gets the top inset.</summary>
	public int Top {
		get;
	}

	/// <summary>Gets the right inset.</summary>
	public int Right {
		get;
	}

	/// <summary>Gets the bottom inset.</summary>
	public int Bottom {
		get;
	}

	/// <summary>Gets the left inset.</summary>
	public int Left {
		get;
	}

	/// <summary>Gets the combined left and right inset.</summary>
	public int Horizontal => Left + Right;

	/// <summary>Gets the combined top and bottom inset.</summary>
	public int Vertical => Top + Bottom;
}
