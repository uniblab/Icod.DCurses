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

/// <summary>Configures one immutable rich-text layout operation.</summary>
public sealed class CursesTextLayoutOptions {
	internal const int MaximumExtent = 1_048_576;

	/// <summary>Initializes layout options for a positive terminal-column extent.</summary>
	/// <param name="columns">The available terminal columns.</param>
	public CursesTextLayoutOptions( int columns ) {
		if ( 1 > columns || MaximumExtent < columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		Columns = columns;
	}

	/// <summary>Gets the available terminal columns.</summary>
	public int Columns { get; }

	/// <summary>Gets the optional maximum number of produced visual lines.</summary>
	public int? MaximumRows { get; init; }

	/// <summary>Gets the text wrapping policy.</summary>
	public CursesTextWrapMode WrapMode { get; init; }

	/// <summary>Gets the horizontal alignment policy.</summary>
	public CursesTextAlignment Alignment { get; init; }

	/// <summary>Gets the hidden-content policy.</summary>
	public CursesTextOverflow Overflow { get; init; }

	/// <summary>Gets the absolute first available terminal column.</summary>
	public int StartingColumn { get; init; }

	/// <summary>Gets the positive absolute-column tab interval.</summary>
	public int TabInterval { get; init; } = 8;

	/// <summary>Gets the style used by uncovered source text.</summary>
	public CursesStyle DefaultStyle { get; init; } = CursesStyle.Default;

	/// <summary>Gets the optional metadata used by uncovered source text.</summary>
	public CursesCellMetadata? DefaultMetadata { get; init; }

	/// <summary>Gets the text-element display-width provider.</summary>
	public ICursesTextWidthProvider WidthProvider { get; init; }
		= UnicodeCursesTextWidthProvider.Instance;
}
