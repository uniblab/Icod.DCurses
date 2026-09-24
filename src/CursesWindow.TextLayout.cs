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

		// Compose only the intersection with the logical window. Building all
		// selected rows before mutation also keeps provider failures atomic.
		int firstRow = (int)Math.Min(
			visualLineCount,
			Math.Max( 0L, -(long)destinationRow )
		);
		int endRow = (int)Math.Max(
			firstRow,
			Math.Min( (long)visualLineCount, (long)Rows - destinationRow )
		);
		int firstColumn = (int)Math.Min(
			layout.Options.Columns,
			Math.Max( 0L, -(long)destinationColumn )
		);
		int endColumn = (int)Math.Max(
			firstColumn,
			Math.Min(
				(long)layout.Options.Columns,
				(long)Columns - destinationColumn
			)
		);
		if ( firstRow == endRow || firstColumn == endColumn ) {
			return;
		}

		int visibleRows = endRow - firstRow;
		int visibleColumns = endColumn - firstColumn;
		CursesCell[][] prepared = new CursesCell[ visibleRows ][];
		CursesCellMetadata?[][] metadata = new CursesCellMetadata?[ visibleRows ][];
		for ( int offset = firstRow; offset < endRow; offset++ ) {
			int row = destinationRow + offset;
			CursesCell[] cells = new CursesCell[ visibleColumns ];
			Array.Fill( cells, CursesCell.Blank( layout.Options.DefaultStyle ) );
			CursesCellMetadata?[] rowMetadata = new CursesCellMetadata?[ visibleColumns ];
			CursesTextVisualLine line = layout.Lines[ firstVisualLine + offset ];
			foreach ( CursesTextFragment fragment in line.Fragments ) {
				PrepareTextFragment(
					layout,
					fragment,
					row,
					destinationColumn,
					firstColumn,
					cells,
					rowMetadata
				);
			}
			prepared[ offset - firstRow ] = cells;
			metadata[ offset - firstRow ] = rowMetadata;
		}

		for ( int offset = firstRow; offset < endRow; offset++ ) {
			int row = destinationRow + offset;
			CursesCell[] cells = prepared[ offset - firstRow ];
			CursesCellMetadata?[] rowMetadata = metadata[ offset - firstRow ];
			for ( int column = 0; column < visibleColumns; column++ ) {
				int targetColumn = destinationColumn + firstColumn + column;
				if ( !TryMapToScreen(
					row,
					targetColumn,
					out int screenRow,
					out int screenColumn
				) ) {
					continue;
				}
				CursesVirtualScreen surface = screen.VirtualScreen;
				if ( surface[ screenRow, screenColumn ] == cells[ column ]
					&& Equals(
						surface.GetMetadata( screenRow, screenColumn ),
						rowMetadata[ column ]
					)
					&& surface.GetRasterCell( screenRow, screenColumn ) is null ) {
					continue;
				}
				surface[ screenRow, screenColumn ] = cells[ column ];
			}
			for ( int column = 0; column < visibleColumns; column++ ) {
				if ( rowMetadata[ column ] is CursesCellMetadata value ) {
					SetMetadataIfVisible(
						row,
						destinationColumn + firstColumn + column,
						value
					);
				}
			}
		}
	}

	private void PrepareTextFragment(
		CursesTextLayout layout,
		CursesTextFragment fragment,
		int row,
		int destinationColumn,
		int firstColumn,
		CursesCell[] cells,
		CursesCellMetadata?[] metadata
	) {
		int visualColumn = fragment.Column;
		int? previousLeaderIndex = null;
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
				if ( previousLeaderIndex is int leaderIndex ) {
					CursesCell leader = cells[ leaderIndex ];
					if ( !leader.IsBlank && !leader.IsContinuation ) {
						cells[ leaderIndex ] = new CursesCell(
							leader.Content + textElement,
							leader.Style,
							leader.DisplayWidth
						);
						metadata[ leaderIndex ] = fragment.Metadata;
					}
				}
				continue;
			}

			int column = destinationColumn
				+ ( visualColumn - layout.Options.StartingColumn );
			int index = visualColumn - layout.Options.StartingColumn
				- firstColumn;
			if ( isTab ) {
				for ( int tabColumn = 0; tabColumn < width; tabColumn++ ) {
					int currentColumn = column + tabColumn;
					int currentIndex = index + tabColumn;
					bool visible = 0 <= currentIndex
						&& currentIndex < cells.Length
						&& TryMapToScreen( row, currentColumn, out _, out _ );
					if ( visible ) {
						cells[ currentIndex ] = new CursesCell( " ", fragment.Style );
						metadata[ currentIndex ] = fragment.Metadata;
					}
					previousLeaderIndex = visible ? currentIndex : null;
				}
			} else if ( 1 == width ) {
				bool visible = 0 <= index
					&& index < cells.Length
					&& TryMapToScreen( row, column, out _, out _ );
				if ( visible ) {
					cells[ index ] = new CursesCell( textElement, fragment.Style );
					metadata[ index ] = fragment.Metadata;
				}
				previousLeaderIndex = visible ? index : null;
			} else {
				bool visible = 0 <= index
					&& index + 1 < cells.Length
					&& TryMapToScreen( row, column, out _, out _ )
					&& TryMapToScreen( row, column + 1, out _, out _ );
				if ( visible ) {
					cells[ index ] = new CursesCell(
						textElement,
						fragment.Style,
						2
					);
					cells[ index + 1 ] = CursesCell.Continuation( fragment.Style );
					metadata[ index ] = fragment.Metadata;
					metadata[ index + 1 ] = fragment.Metadata;
					previousLeaderIndex = index;
				} else {
					previousLeaderIndex = null;
				}
			}
			visualColumn += width;
		}
	}

	private void SetMetadataIfVisible(
		int row,
		int column,
		CursesCellMetadata metadata
	) {
		if ( TryMapToScreen( row, column, out int screenRow, out int screenColumn ) ) {
			screen.VirtualScreen.SetMetadata( screenRow, screenColumn, metadata );
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
