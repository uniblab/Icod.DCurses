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

/// <summary>Window-local geometric line and border drawing operations.</summary>
public sealed partial class CursesWindow {
	/// <summary>Draws one horizontal semantic line using the default style.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="length">The positive number of columns to draw.</param>
	/// <remarks>
	/// The logical cells retain <see cref="CursesLineGlyph.Horizontal"/> identity so terminal presentation
	/// may select ACS, Unicode, or ASCII without rewriting the logical screen. The cursor is preserved.
	/// </remarks>
	public void DrawHorizontalLine(
		int row,
		int column,
		int length
	) {
		DrawHorizontalLine(
			row,
			column,
			length,
			CursesCell.Line( CursesLineGlyph.Horizontal )
		);
	}

	/// <summary>Draws one horizontal line using a one-column logical cell.</summary>
	/// <param name="row">The zero-based local row.</param>
	/// <param name="column">The zero-based first local column.</param>
	/// <param name="length">The positive number of columns to draw.</param>
	/// <param name="cell">The one-column non-continuation cell used for the line.</param>
	/// <remarks>The cursor is preserved.</remarks>
	public void DrawHorizontalLine(
		int row,
		int column,
		int length,
		CursesCell cell
	) {
		ValidateLineLength( length );
		ValidateRegion(
			row,
			column,
			1,
			length
		);
		ValidateFillCell( cell );

		FillRegion(
			row,
			column,
			1,
			length,
			cell
		);
	}

	/// <summary>Draws one vertical semantic line using the default style.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <param name="length">The positive number of rows to draw.</param>
	/// <remarks>
	/// The logical cells retain <see cref="CursesLineGlyph.Vertical"/> identity so terminal presentation
	/// may select ACS, Unicode, or ASCII without rewriting the logical screen. The cursor is preserved.
	/// </remarks>
	public void DrawVerticalLine(
		int row,
		int column,
		int length
	) {
		DrawVerticalLine(
			row,
			column,
			length,
			CursesCell.Line( CursesLineGlyph.Vertical )
		);
	}

	/// <summary>Draws one vertical line using a one-column logical cell.</summary>
	/// <param name="row">The zero-based first local row.</param>
	/// <param name="column">The zero-based local column.</param>
	/// <param name="length">The positive number of rows to draw.</param>
	/// <param name="cell">The one-column non-continuation cell used for the line.</param>
	/// <remarks>The cursor is preserved.</remarks>
	public void DrawVerticalLine(
		int row,
		int column,
		int length,
		CursesCell cell
	) {
		ValidateLineLength( length );
		ValidateRegion(
			row,
			column,
			length,
			1
		);
		ValidateFillCell( cell );

		FillRegion(
			row,
			column,
			length,
			1,
			cell
		);
	}

	/// <summary>Draws a semantic single-line border around the complete window using the default style.</summary>
	/// <remarks>The cursor is preserved.</remarks>
	public void DrawBorder() {
		DrawBorder( CursesStyle.Default );
	}

	/// <summary>Draws a semantic single-line border around the complete window.</summary>
	/// <param name="style">The semantic style assigned to every border cell.</param>
	/// <remarks>
	/// The window must be at least two rows by two columns. Each logical border cell retains its semantic
	/// line-junction identity so physical presentation may select ACS, Unicode, or ASCII. The cursor is preserved.
	/// </remarks>
	public void DrawBorder( CursesStyle style ) {
		DrawBorder(
			CursesCell.Line( CursesLineGlyph.Horizontal, style ),
			CursesCell.Line( CursesLineGlyph.Vertical, style ),
			CursesCell.Line( CursesLineGlyph.UpperLeftCorner, style ),
			CursesCell.Line( CursesLineGlyph.UpperRightCorner, style ),
			CursesCell.Line( CursesLineGlyph.LowerLeftCorner, style ),
			CursesCell.Line( CursesLineGlyph.LowerRightCorner, style )
		);
	}

	/// <summary>Draws a box-shaped border around the complete window.</summary>
	/// <param name="horizontal">The one-column cell used for top and bottom edges.</param>
	/// <param name="vertical">The one-column cell used for left and right edges.</param>
	/// <param name="topLeft">The upper-left corner cell.</param>
	/// <param name="topRight">The upper-right corner cell.</param>
	/// <param name="bottomLeft">The lower-left corner cell.</param>
	/// <param name="bottomRight">The lower-right corner cell.</param>
	/// <remarks>
	/// The window must be at least two rows by two columns. The cursor is preserved. This overload remains
	/// the exact caller-supplied-cell path; use the semantic overloads when the terminal presentation layer
	/// should select a line representation.
	/// </remarks>
	public void DrawBorder(
		CursesCell horizontal,
		CursesCell vertical,
		CursesCell topLeft,
		CursesCell topRight,
		CursesCell bottomLeft,
		CursesCell bottomRight
	) {
		if ( Rows < 2 || Columns < 2 ) {
			throw new InvalidOperationException(
				"A window must be at least two rows by two columns to draw a border."
			);
		}

		ValidateFillCell( horizontal );
		ValidateFillCell( vertical );
		ValidateFillCell( topLeft );
		ValidateFillCell( topRight );
		ValidateFillCell( bottomLeft );
		ValidateFillCell( bottomRight );

		if ( 2 < Columns ) {
			DrawHorizontalLine(
				0,
				1,
				Columns - 2,
				horizontal
			);
			DrawHorizontalLine(
				Rows - 1,
				1,
				Columns - 2,
				horizontal
			);
		}
		if ( 2 < Rows ) {
			DrawVerticalLine(
				1,
				0,
				Rows - 2,
				vertical
			);
			DrawVerticalLine(
				1,
				Columns - 1,
				Rows - 2,
				vertical
			);
		}

		SetCellIfVisible(
			0,
			0,
			topLeft
		);
		SetCellIfVisible(
			0,
			Columns - 1,
			topRight
		);
		SetCellIfVisible(
			Rows - 1,
			0,
			bottomLeft
		);
		SetCellIfVisible(
			Rows - 1,
			Columns - 1,
			bottomRight
		);
	}

	private static void ValidateLineLength( int length ) {
		if ( 0 >= length ) {
			throw new ArgumentOutOfRangeException(
				nameof( length ),
				length,
				"The line length must be positive."
			);
		}
	}
}
