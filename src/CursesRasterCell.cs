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

/// <summary>Represents one opaque retained raster-placeholder cell.</summary>
/// <remarks>
/// Row and column are descriptive coordinates within the owning placeholder. Complete identity also
/// includes private placeholder/session ownership and cannot be reconstructed from those coordinates.
/// </remarks>
public readonly struct CursesRasterCell {
	private readonly CursesRasterPlaceholder? placeholder;
	private readonly TerminalRasterPlaceholderCell terminalCell;

	internal CursesRasterCell(
		CursesRasterPlaceholder placeholder,
		TerminalRasterPlaceholderCell terminalCell
	) {
		ArgumentNullException.ThrowIfNull( placeholder );
		this.placeholder = placeholder;
		this.terminalCell = terminalCell;
		this.Row = terminalCell.Row;
		this.Column = terminalCell.Column;
	}

	/// <summary>Gets the zero-based row within the owning raster placeholder.</summary>
	public int Row {
		get;
	}

	/// <summary>Gets the zero-based column within the owning raster placeholder.</summary>
	public int Column {
		get;
	}

	internal bool IsValid => this.placeholder is not null;

	internal CursesRasterPlaceholder Placeholder => this.placeholder
		?? throw new InvalidOperationException(
			"The default CursesRasterCell value is not associated with a live raster placeholder."
		);

	internal TerminalRasterPlaceholderCell TerminalCell {
		get {
			_ = this.Placeholder;
			return this.terminalCell;
		}
	}

	internal bool BelongsTo( CursesSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		return this.placeholder is not null
			&& this.placeholder.BelongsTo( session );
	}
}
