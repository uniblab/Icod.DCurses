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

public sealed partial class CursesTextLayout {
	/// <summary>Maps a legal source position to its visual caret position.</summary>
	/// <param name="position">A legal source position in this layout.</param>
	/// <param name="affinity">The preferred edge at an ambiguous wrap boundary.</param>
	/// <returns>The corresponding visual caret position.</returns>
	public CursesTextVisualPosition GetVisualPosition(
		CursesTextPosition position,
		CursesTextAffinity affinity = CursesTextAffinity.Leading
	) {
		if ( Text.Length < position.Offset ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		if ( !Enum.IsDefined( affinity ) ) {
			throw new ArgumentOutOfRangeException( nameof( affinity ) );
		}
		if ( 0 == lines.Count ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}

		if ( CursesTextAffinity.Leading == affinity ) {
			for ( int index = 0; index < lines.Count; index++ ) {
				CursesTextVisualLine line = lines[ index ];
				if ( line.SourceStart.Offset == position.Offset ) {
					return new CursesTextVisualPosition(
						index,
						line.Column,
						affinity
					);
				}
			}
		} else {
			for ( int index = lines.Count - 1; index >= 0; index-- ) {
				CursesTextVisualLine line = lines[ index ];
				if ( line.SourceEnd.Offset == position.Offset ) {
					return new CursesTextVisualPosition(
						index,
						line.Column + line.Columns,
						affinity
					);
				}
			}
		}

		throw new ArgumentOutOfRangeException( nameof( position ) );
	}

	/// <summary>Maps an absolute visual column on one line to the nearest legal source position.</summary>
	public CursesTextHitTestResult HitTest( int line, int column ) {
		throw new NotImplementedException();
	}

	/// <summary>Gets the previous legal source position, clamped at the source start.</summary>
	public CursesTextPosition GetPreviousPosition( CursesTextPosition position ) {
		throw new NotImplementedException();
	}

	/// <summary>Gets the next legal source position, clamped at the source end.</summary>
	public CursesTextPosition GetNextPosition( CursesTextPosition position ) {
		throw new NotImplementedException();
	}

	/// <summary>Moves a visual position vertically while preserving a preferred column.</summary>
	public CursesTextVisualPosition MoveVertically(
		CursesTextVisualPosition position,
		int lineDelta,
		int preferredColumn
	) {
		throw new NotImplementedException();
	}

	/// <summary>Gets the legal source position at the start of one visual line.</summary>
	public CursesTextPosition GetLineStart( int line ) {
		throw new NotImplementedException();
	}

	/// <summary>Gets the legal source position at the end of one visual line.</summary>
	public CursesTextPosition GetLineEnd( int line ) {
		throw new NotImplementedException();
	}

	/// <summary>Gets owned visual rectangles for the selected source cells in a visual-line range.</summary>
	public CursesRectangle[] GetSelectionRectangles(
		CursesTextSelection selection,
		int firstLine,
		int lineCount
	) {
		throw new NotImplementedException();
	}
}
