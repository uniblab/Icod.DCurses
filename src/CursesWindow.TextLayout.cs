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

/// <summary>Retained immutable-text-layout presentation for logical windows.</summary>
public sealed partial class CursesWindow {
	/// <summary>Projects selected visual lines into retained window cells without changing the cursor.</summary>
	/// <param name="layout">The immutable text layout to project.</param>
	/// <param name="firstVisualLine">The first visual line to project.</param>
	/// <param name="visualLineCount">The number of visual lines to project.</param>
	/// <param name="destinationRow">The window-relative row for the first selected line.</param>
	/// <param name="destinationColumn">The window-relative column corresponding to the layout starting column.</param>
	public void PresentTextLayout(
		CursesTextLayout layout,
		int firstVisualLine,
		int visualLineCount,
		int destinationRow,
		int destinationColumn
	) {
		ArgumentNullException.ThrowIfNull( layout );
		if ( 0 > firstVisualLine
			|| layout.Lines.Count < firstVisualLine ) {
			throw new ArgumentOutOfRangeException(
				nameof( firstVisualLine )
			);
		}
		if ( 0 > visualLineCount
			|| layout.Lines.Count - firstVisualLine < visualLineCount ) {
			throw new ArgumentOutOfRangeException(
				nameof( visualLineCount )
			);
		}
		ValidatePresentationExtent(
			destinationRow,
			visualLineCount,
			nameof( destinationRow )
		);
		ValidatePresentationExtent(
			destinationColumn,
			layout.Options.Columns,
			nameof( destinationColumn )
		);

		CursesCell blank = CursesCell.Blank(
			layout.Options.DefaultStyle
		);
		for ( int offset = 0; offset < visualLineCount; offset++ ) {
			int row = destinationRow + offset;
			for ( int column = 0; column < layout.Options.Columns; column++ ) {
				SetCellIfVisible(
					row,
					destinationColumn + column,
					blank
				);
			}

			CursesTextVisualLine line = layout.Lines[
				firstVisualLine + offset
			];
			foreach ( CursesTextFragment fragment in line.Fragments ) {
				PresentTextFragment(
					layout,
					fragment,
					row,
					destinationColumn
				);
			}
		}
	}

	private void PresentTextFragment(
		CursesTextLayout layout,
		CursesTextFragment fragment,
		int row,
		int destinationColumn
	) {
		int visualColumn = fragment.Column;
		int? previousLeaderColumn = null;
		foreach ( string textElement in CursesUnicodeText.EnumerateTextElements(
			fragment.Text
		) ) {
			int width;
			bool isTab = "\t" == textElement;
			if ( isTab ) {
				width = layout.Options.TabInterval
					- ( visualColumn % layout.Options.TabInterval );
			} else {
				width = layout.Options.WidthProvider.GetWidth( textElement );
				if ( 0 > width || 2 < width ) {
					throw new InvalidOperationException(
						"The layout width provider returned a width outside the supported range."
					);
				}
			}

			if ( 0 == width ) {
				if ( previousLeaderColumn is int leaderColumn ) {
					CursesCell leader = GetCellOrBackground(
						row,
						leaderColumn
					);
					if ( !leader.IsBlank && !leader.IsContinuation ) {
						SetProjectedCell(
							row,
							leaderColumn,
							new CursesCell(
								leader.Content + textElement,
								leader.Style,
								leader.DisplayWidth
							),
							fragment.Metadata
						);
					}
				}
				continue;
			}

			int column = destinationColumn
				+ ( visualColumn - layout.Options.StartingColumn );
			if ( isTab ) {
				for ( int tabColumn = 0; tabColumn < width; tabColumn++ ) {
					int currentColumn = column + tabColumn;
					bool visible = TryMapToScreen(
						row,
						currentColumn,
						out _,
						out _
					);
					SetProjectedCell(
						row,
						currentColumn,
						new CursesCell( " ", fragment.Style ),
						fragment.Metadata
					);
					previousLeaderColumn = visible
						? currentColumn
						: null
					;
				}
			} else if ( 1 == width ) {
				bool visible = TryMapToScreen(
					row,
					column,
					out _,
					out _
				);
				SetProjectedCell(
					row,
					column,
					new CursesCell( textElement, fragment.Style ),
					fragment.Metadata
				);
				previousLeaderColumn = visible ? column : null;
			} else {
				bool leaderVisible = TryMapToScreen(
					row,
					column,
					out _,
					out _
				);
				bool continuationVisible = TryMapToScreen(
					row,
					column + 1,
					out _,
					out _
				);
				if ( leaderVisible && continuationVisible ) {
					SetProjectedCell(
						row,
						column,
						new CursesCell(
							textElement,
							fragment.Style,
							2
						),
						fragment.Metadata
					);
					SetProjectedCell(
						row,
						column + 1,
						CursesCell.Continuation( fragment.Style ),
						fragment.Metadata
					);
					previousLeaderColumn = column;
				} else {
					previousLeaderColumn = null;
				}
			}
			visualColumn += width;
		}
	}

	private void SetProjectedCell(
		int row,
		int column,
		CursesCell cell,
		CursesCellMetadata? metadata
	) {
		if ( metadata is null ) {
			SetCellIfVisible( row, column, cell );
		} else {
			SetCellAndMetadataIfVisible(
				row,
				column,
				cell,
				metadata
			);
		}
	}

	private static void ValidatePresentationExtent(
		int origin,
		int extent,
		string parameterName
	) {
		long end = (long)origin + extent;
		if ( int.MinValue > end || int.MaxValue < end ) {
			throw new ArgumentOutOfRangeException( parameterName );
		}
	}
}
