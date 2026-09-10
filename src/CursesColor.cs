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

/// <summary>
/// Identifies how a curses color is represented semantically.
/// </summary>
public enum CursesColorKind {
	/// <summary>Use the terminal's default color.</summary>
	Default,

	/// <summary>Use an indexed terminal color.</summary>
	Indexed,

	/// <summary>Use an explicit RGB color.</summary>
	Rgb
}

/// <summary>
/// Represents a terminal-independent color request.
/// </summary>
/// <remarks>
/// A color carries semantic color information only. It never contains a terminal escape sequence.
/// Capability mapping is owned by the refresh/output layer.
/// </remarks>
public readonly record struct CursesColor {
	private CursesColor(
		CursesColorKind kind,
		int? index,
		byte? red,
		byte? green,
		byte? blue ) {
		Kind = kind;
		Index = index;
		Red = red;
		Green = green;
		Blue = blue;
	}

	/// <summary>Gets the terminal-default color.</summary>
	public static CursesColor Default => default;

	/// <summary>Gets the semantic representation kind.</summary>
	public CursesColorKind Kind {
		get;
	}

	/// <summary>Gets the indexed color number when <see cref="Kind"/> is <see cref="CursesColorKind.Indexed"/>.</summary>
	public int? Index {
		get;
	}

	/// <summary>Gets the red component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Red {
		get;
	}

	/// <summary>Gets the green component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Green {
		get;
	}

	/// <summary>Gets the blue component when <see cref="Kind"/> is <see cref="CursesColorKind.Rgb"/>.</summary>
	public byte? Blue {
		get;
	}

	/// <summary>Gets whether this color requests the terminal default.</summary>
	public bool IsDefault => CursesColorKind.Default == Kind;

	/// <summary>Creates an indexed-color request.</summary>
	/// <param name="index">The non-negative terminal color index.</param>
	/// <returns>The semantic indexed color.</returns>
	public static CursesColor Indexed( int index ) {
		if ( 0 > index ) {
			throw new ArgumentOutOfRangeException(
				nameof( index ),
				index,
				"A terminal color index cannot be negative."
			);
		}

		return new CursesColor(
			CursesColorKind.Indexed,
			index,
			null,
			null,
			null
		);
	}

	/// <summary>Creates a direct RGB color request.</summary>
	/// <param name="red">The red component.</param>
	/// <param name="green">The green component.</param>
	/// <param name="blue">The blue component.</param>
	/// <returns>The semantic RGB color.</returns>
	public static CursesColor Rgb(
		byte red,
		byte green,
		byte blue ) {
		return new CursesColor(
			CursesColorKind.Rgb,
			null,
			red,
			green,
			blue
		);
	}
}
