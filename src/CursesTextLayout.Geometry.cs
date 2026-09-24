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

public sealed partial class CursesTextLayout {
	private readonly int[] legalOffsets;
	private readonly ulong[] legalBoundaryBits;
	private readonly int[] legalBoundaryRanks;
	private readonly GeometryLine[] geometryLines;

	/// <summary>Maps a legal source position to its visual caret position.</summary>
	/// <param name="position">A legal source position in this layout.</param>
	/// <param name="affinity">The preferred edge at an ambiguous wrap boundary.</param>
	/// <returns>The corresponding visual caret position.</returns>
	public CursesTextVisualPosition GetVisualPosition(
		CursesTextPosition position,
		CursesTextAffinity affinity = CursesTextAffinity.Leading
	) {
		if ( 0 > FindLegalOffsetIndex( position.Offset ) ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		if ( !Enum.IsDefined( affinity ) ) {
			throw new ArgumentOutOfRangeException( nameof( affinity ) );
		}
		if ( 0 == lines.Count ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}

		int lineIndex = CursesTextAffinity.Leading == affinity
			? FindLeadingLine( position.Offset )
			: FindTrailingLine( position.Offset )
		;
		if ( 0 > lineIndex || lines.Count <= lineIndex ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}

		GeometryLine geometry = geometryLines[ lineIndex ];
		int column = FindSourceColumn(
			geometry,
			position.Offset,
			affinity
		);
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		return new CursesTextVisualPosition(
			lineIndex,
			column,
			affinity
		);
	}

	/// <summary>Maps an absolute visual column on one line to the nearest legal source position.</summary>
	public CursesTextHitTestResult HitTest( int line, int column ) {
		if ( 0 > line || lines.Count <= line ) {
			throw new ArgumentOutOfRangeException( nameof( line ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}

		CursesTextVisualLine visualLine = lines[ line ];
		GeometryLine geometry = geometryLines[ line ];
		if ( column < visualLine.Column ) {
			return new CursesTextHitTestResult(
				visualLine.SourceStart,
				CursesTextAffinity.Leading,
				false
			);
		}
		int elementIndex = FindColumnElementIndex(
			geometry.Elements,
			column
		);
		if ( 0 <= elementIndex ) {
			GeometryElement element = geometry.Elements[ elementIndex ];
			if ( 0 != element.Columns
				&& column < element.Column + element.Columns ) {
				if ( element.IsEllipsis || column == element.Column ) {
					return new CursesTextHitTestResult(
						new CursesTextPosition( element.SourceStart ),
						CursesTextAffinity.Leading,
						true
					);
				}
				return new CursesTextHitTestResult(
					new CursesTextPosition( element.SourceEnd ),
					CursesTextAffinity.Trailing,
					true
				);
			}
		}

		return new CursesTextHitTestResult(
			visualLine.SourceEnd,
			CursesTextAffinity.Trailing,
			false
		);
	}

	/// <summary>Gets the previous legal source position, clamped at the source start.</summary>
	public CursesTextPosition GetPreviousPosition( CursesTextPosition position ) {
		int index = FindLegalOffsetIndex( position.Offset );
		if ( 0 > index ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		return new CursesTextPosition(
			legalOffsets[ Math.Max( 0, index - 1 ) ]
		);
	}

	/// <summary>Gets the next legal source position, clamped at the source end.</summary>
	public CursesTextPosition GetNextPosition( CursesTextPosition position ) {
		int index = FindLegalOffsetIndex( position.Offset );
		if ( 0 > index ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		return new CursesTextPosition(
			legalOffsets[ Math.Min( legalOffsets.Length - 1, index + 1 ) ]
		);
	}

	/// <summary>Moves a visual position vertically while preserving a preferred column.</summary>
	public CursesTextVisualPosition MoveVertically(
		CursesTextVisualPosition position,
		int lineDelta,
		int preferredColumn
	) {
		if ( lines.Count <= position.Line ) {
			throw new ArgumentOutOfRangeException( nameof( position ) );
		}
		if ( 0 > preferredColumn ) {
			throw new ArgumentOutOfRangeException( nameof( preferredColumn ) );
		}

		long requestedLine = (long)position.Line + lineDelta;
		int targetLine = (int)Math.Clamp(
			requestedLine,
			0L,
			lines.Count - 1L
		);
		CursesTextHitTestResult hit = HitTest(
			targetLine,
			preferredColumn
		);
		return GetVisualPosition(
			hit.Position,
			hit.Affinity
		);
	}

	/// <summary>Gets the legal source position at the start of one visual line.</summary>
	public CursesTextPosition GetLineStart( int line ) {
		if ( 0 > line || lines.Count <= line ) {
			throw new ArgumentOutOfRangeException( nameof( line ) );
		}
		return lines[ line ].SourceStart;
	}

	/// <summary>Gets the legal source position at the end of one visual line.</summary>
	public CursesTextPosition GetLineEnd( int line ) {
		if ( 0 > line || lines.Count <= line ) {
			throw new ArgumentOutOfRangeException( nameof( line ) );
		}
		return lines[ line ].SourceEnd;
	}

	/// <summary>Gets owned visual rectangles for the selected source cells in a visual-line range.</summary>
	public CursesRectangle[] GetSelectionRectangles(
		CursesTextSelection selection,
		int firstLine,
		int lineCount
	) {
		if ( 0 > FindLegalOffsetIndex( selection.Start.Offset )
			|| 0 > FindLegalOffsetIndex( selection.End.Offset ) ) {
			throw new ArgumentOutOfRangeException( nameof( selection ) );
		}
		if ( 0 > firstLine || lines.Count < firstLine ) {
			throw new ArgumentOutOfRangeException( nameof( firstLine ) );
		}
		if ( 0 > lineCount || lines.Count - firstLine < lineCount ) {
			throw new ArgumentOutOfRangeException( nameof( lineCount ) );
		}
		if ( selection.IsEmpty || 0 == lineCount ) {
			return [];
		}

		int endLine = firstLine + lineCount;
		int rectangleCount = 0;
		for ( int line = firstLine; line < endLine; line++ ) {
			if ( TryGetSelectionRectangle(
				geometryLines[ line ],
				line,
				selection.Start.Offset,
				selection.End.Offset,
				out _
			) ) {
				rectangleCount++;
			}
		}
		if ( 0 == rectangleCount ) {
			return [];
		}

		CursesRectangle[] rectangles = new CursesRectangle[ rectangleCount ];
		int rectangleIndex = 0;
		for ( int line = firstLine; line < endLine; line++ ) {
			if ( TryGetSelectionRectangle(
				geometryLines[ line ],
				line,
				selection.Start.Offset,
				selection.End.Offset,
				out CursesRectangle rectangle
			) ) {
				rectangles[ rectangleIndex++ ] = rectangle;
			}
		}
		return rectangles;
	}

	private static bool TryGetSelectionRectangle(
		GeometryLine line,
		int lineIndex,
		int selectionStart,
		int selectionEnd,
		out CursesRectangle rectangle
	) {
		int firstIndex = FindFirstSelectedCellIndex(
			line,
			selectionStart
		);
		if ( line.CellElementIndexes.Length <= firstIndex ) {
			rectangle = default;
			return false;
		}
		GeometryElement first = line.Elements[
			line.CellElementIndexes[ firstIndex ]
		];
		if ( selectionEnd <= first.SourceStart ) {
			rectangle = default;
			return false;
		}

		int lastIndex = FindLastSelectedCellIndex(
			line,
			selectionEnd
		);
		if ( lastIndex < firstIndex ) {
			rectangle = default;
			return false;
		}
		GeometryElement last = line.Elements[
			line.CellElementIndexes[ lastIndex ]
		];
		rectangle = new CursesRectangle(
			lineIndex,
			first.Column,
			1,
			last.Column + last.Columns - first.Column
		);
		return true;
	}

	private int FindLegalOffsetIndex( int offset ) {
		if ( 0 > offset || legalOffsets[ ^1 ] < offset ) {
			return -1;
		}

		int wordIndex = offset / 64;
		int bitIndex = offset % 64;
		ulong bit = 1UL << bitIndex;
		ulong word = legalBoundaryBits[ wordIndex ];
		if ( 0 == ( word & bit ) ) {
			return -1;
		}

		ulong precedingMask = bit - 1UL;
		return legalBoundaryRanks[ wordIndex ]
			+ System.Numerics.BitOperations.PopCount(
				word & precedingMask
			);
	}

	private static int FindColumnElementIndex(
		GeometryElement[] elements,
		int column
	) {
		int lower = 0;
		int upper = elements.Length;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			if ( elements[ middle ].Column <= column ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		return lower - 1;
	}

	private static int FindFirstSelectedCellIndex(
		GeometryLine line,
		int selectionStart
	) {
		int lower = 0;
		int upper = line.CellElementIndexes.Length;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			GeometryElement element = line.Elements[
				line.CellElementIndexes[ middle ]
			];
			if ( element.SourceEnd <= selectionStart ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		return lower;
	}

	private static int FindLastSelectedCellIndex(
		GeometryLine line,
		int selectionEnd
	) {
		int lower = 0;
		int upper = line.CellElementIndexes.Length;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			GeometryElement element = line.Elements[
				line.CellElementIndexes[ middle ]
			];
			if ( element.SourceStart < selectionEnd ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		return lower - 1;
	}

	private int FindLeadingLine( int sourceOffset ) {
		int lower = 0;
		int upper = lines.Count;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			if ( lines[ middle ].SourceStart.Offset <= sourceOffset ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		return lower - 1;
	}

	private int FindTrailingLine( int sourceOffset ) {
		int lower = 0;
		int upper = lines.Count;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			if ( lines[ middle ].SourceEnd.Offset < sourceOffset ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		return lower;
	}

	private static int FindSourceColumn(
		GeometryLine line,
		int sourceOffset,
		CursesTextAffinity affinity
	) {
		int lower = 0;
		int upper = line.Elements.Length;
		while ( lower < upper ) {
			int middle = lower + ( ( upper - lower ) / 2 );
			int edge = CursesTextAffinity.Leading == affinity
				? line.Elements[ middle ].SourceStart
				: line.Elements[ middle ].SourceEnd
			;
			if ( edge < sourceOffset ) {
				lower = middle + 1;
			} else {
				upper = middle;
			}
		}
		if ( lower < line.Elements.Length ) {
			GeometryElement element = line.Elements[ lower ];
			int edge = CursesTextAffinity.Leading == affinity
				? element.SourceStart
				: element.SourceEnd
			;
			if ( edge == sourceOffset
				&& ( CursesTextAffinity.Leading == affinity
					|| !element.IsEllipsis ) ) {
				return CursesTextAffinity.Leading == affinity
					? element.Column
					: element.Column + element.Columns
				;
			}
		}

		if ( line.SourceStart == sourceOffset ) {
			return line.Column;
		}
		if ( line.SourceEnd == sourceOffset ) {
			return line.Column + line.Columns;
		}
		return -1;
	}

	private static (
		int[] LegalOffsets,
		ulong[] LegalBoundaryBits,
		int[] LegalBoundaryRanks,
		GeometryLine[] Lines
	) CreateGeometryIndexes(
		CursesTextElement[] elements,
		CursesTextVisualLine[] lines,
		CursesTextLayoutOptions options
	) {
		int[] legal = new int[ elements.Length + 1 ];
		for ( int index = 0; index < elements.Length; index++ ) {
			legal[ index + 1 ] = elements[ index ].SourceEnd;
		}
		int boundaryWordCount = ( legal[ ^1 ] + 64 ) / 64;
		ulong[] boundaryBits = new ulong[ boundaryWordCount ];
		foreach ( int offset in legal ) {
			boundaryBits[ offset / 64 ] |= 1UL << ( offset % 64 );
		}
		int[] boundaryRanks = new int[ boundaryWordCount ];
		int boundaryCount = 0;
		for ( int index = 0; index < boundaryBits.Length; index++ ) {
			boundaryRanks[ index ] = boundaryCount;
			boundaryCount += System.Numerics.BitOperations.PopCount(
				boundaryBits[ index ]
			);
		}

		GeometryLine[] geometry = new GeometryLine[ lines.Length ];
		int elementIndex = 0;
		for ( int lineIndex = 0; lineIndex < lines.Length; lineIndex++ ) {
			CursesTextVisualLine line = lines[ lineIndex ];
			while ( elementIndex < elements.Length
				&& elements[ elementIndex ].SourceStart < line.SourceStart.Offset ) {
				elementIndex++;
			}

			List<GeometryElement> lineElements = [];
			List<int> cellElementIndexes = [];
			int currentColumn = line.Column;
			int currentIndex = elementIndex;
			while ( currentIndex < elements.Length ) {
				CursesTextElement element = elements[ currentIndex ];
				if ( element.IsHardBreak
					|| line.SourceEnd.Offset < element.SourceEnd ) {
					break;
				}
				int width = element.IsTab
					? options.TabInterval - ( currentColumn % options.TabInterval )
					: element.Width
				;
				GeometryElement geometryElement = new(
					element.SourceStart,
					element.SourceEnd,
					currentColumn,
					width,
					false
				);
				if ( 0 != width ) {
					cellElementIndexes.Add( lineElements.Count );
				}
				lineElements.Add( geometryElement );
				currentColumn += width;
				currentIndex++;
			}
			elementIndex = currentIndex;

			foreach ( CursesTextFragment fragment in line.Fragments ) {
				if ( fragment.IsEllipsis ) {
					lineElements.Add(
						new GeometryElement(
							fragment.SourceStart.Offset,
							fragment.SourceEnd.Offset,
							fragment.Column,
							fragment.Columns,
							true
						)
					);
				}
			}
			geometry[ lineIndex ] = new GeometryLine(
				line.SourceStart.Offset,
				line.SourceEnd.Offset,
				line.Column,
				line.Columns,
				[ .. lineElements ],
				[ .. cellElementIndexes ]
			);
		}
		return (
			legal,
			boundaryBits,
			boundaryRanks,
			geometry
		);
	}

	private readonly record struct GeometryElement(
		int SourceStart,
		int SourceEnd,
		int Column,
		int Columns,
		bool IsEllipsis
	);

	private readonly record struct GeometryLine(
		int SourceStart,
		int SourceEnd,
		int Column,
		int Columns,
		GeometryElement[] Elements,
		int[] CellElementIndexes
	);
}
