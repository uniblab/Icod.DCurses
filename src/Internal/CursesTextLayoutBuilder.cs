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

namespace Icod.DCurses.Internal;

internal static class CursesTextLayoutBuilder {
	private const int MaximumSourceLength = 16_777_216;
	private const int MaximumSpanCount = 1_048_576;
	private const int MaximumFragmentCount = 4_194_304;
	private const int MaximumCellCount = 16_777_216;

	internal static CursesTextLayout Build(
		string text,
		CursesTextLayoutOptions options,
		IReadOnlyList<CursesTextSpan>? spans
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( options );
		if ( MaximumSourceLength < text.Length ) {
			throw new ArgumentOutOfRangeException( nameof( text ) );
		}
		ValidateOptions( options );
		CursesTextSpan[] spanCopy = CopySpans( spans );
		CursesTextLayoutOptions optionCopy = CopyOptions( options );
		CursesTextElement[] elements = CursesTextElementScanner.Scan(
			text,
			optionCopy.WidthProvider
		);
		ValidateSpans(
			spanCopy,
			text.Length,
			elements
		);

		return BuildLines(
			text,
			optionCopy,
			spanCopy,
			elements
		);
	}

	private static CursesTextLayout BuildLines(
		string text,
		CursesTextLayoutOptions options,
		CursesTextSpan[] spans,
		CursesTextElement[] elements
	) {
		if ( 0 == options.MaximumRows ) {
			return new CursesTextLayout(
				text,
				options,
				spans,
				[],
				0,
				0 != text.Length
			);
		}

		string normalized = CursesUnicodeText.NormalizeMalformedUtf16( text );
		List<CursesTextVisualLine> lines = [];
		int logicalElementStart = 0;
		int logicalSourceStart = 0;
		int cellCount = 0;
		int fragmentCount = 0;
		bool isTruncated = false;
		while ( true ) {
			int logicalElementEnd = logicalElementStart;
			while ( logicalElementEnd < elements.Length
				&& !elements[ logicalElementEnd ].IsHardBreak ) {
				logicalElementEnd++;
			}
			bool hasHardBreak = logicalElementEnd < elements.Length;
			int nextLogicalSourceStart = hasHardBreak
				? elements[ logicalElementEnd ].SourceEnd
				: text.Length
			;
			int visualElementStart = logicalElementStart;
			bool producedLine = false;
			while ( !producedLine || visualElementStart < logicalElementEnd ) {
				if ( options.MaximumRows is int maximumRows
					&& maximumRows <= lines.Count ) {
					isTruncated |= logicalSourceStart < text.Length;
					return CreateLayout(
						text,
						options,
						spans,
						lines,
						cellCount,
						isTruncated
					);
				}

				int fitEnd = FindFitEnd(
					elements,
					visualElementStart,
					logicalElementEnd,
					options,
					options.StartingColumn
				);
				bool noWrap = CursesTextWrapMode.NoWrap == options.WrapMode;
				bool tooWide = visualElementStart < logicalElementEnd
					&& fitEnd == visualElementStart;
				int advanceEnd;
				int visibleEnd;
				bool isClipped;
				if ( noWrap ) {
					advanceEnd = logicalElementEnd;
					visibleEnd = fitEnd;
					isClipped = fitEnd < logicalElementEnd;
				} else if ( tooWide ) {
					advanceEnd = visualElementStart + 1;
					visibleEnd = visualElementStart;
					isClipped = true;
				} else {
					advanceEnd = fitEnd;
					if ( CursesTextWrapMode.Word == options.WrapMode
						&& fitEnd < logicalElementEnd ) {
						int wordEnd = FindWordEnd(
							normalized,
							elements,
							visualElementStart,
							fitEnd
						);
						if ( visualElementStart < wordEnd ) {
							advanceEnd = wordEnd;
						}
					}
					visibleEnd = advanceEnd;
					isClipped = false;
				}

				int sourceStart = visualElementStart < logicalElementEnd
					? elements[ visualElementStart ].SourceStart
					: logicalSourceStart
				;
				int sourceEnd = sourceStart;
				if ( visibleEnd > visualElementStart ) {
					sourceEnd = elements[ visibleEnd - 1 ].SourceEnd;
				}
				int contentColumns = MeasureColumns(
					elements,
					visualElementStart,
					visibleEnd,
					options,
					options.StartingColumn
				);
				int lineColumn = GetAlignedColumn(
					options,
					contentColumns
				);
				List<MutableFragment> fragments = [];
				int fragmentColumn = lineColumn;
				for ( int index = visualElementStart; index < visibleEnd; index++ ) {
					CursesTextElement element = elements[ index ];
					int width = GetElementWidth(
						element,
						fragmentColumn,
						options
					);
					AppendElement(
						fragments,
						element,
						fragmentColumn,
						width,
						options,
						spans
					);
					fragmentColumn += width;
				}
				CursesTextFragment[] publishedFragments = fragments
					.Select(
						current => current.Publish( normalized )
					)
					.ToArray();
				fragmentCount = checked( fragmentCount + publishedFragments.Length );
				if ( MaximumFragmentCount < fragmentCount ) {
					throw new InvalidOperationException(
						"Text layout exceeded the supported fragment capacity."
					);
				}
				cellCount = checked( cellCount + contentColumns );
				if ( MaximumCellCount < cellCount ) {
					throw new InvalidOperationException(
						"Text layout exceeded the supported cell capacity."
					);
				}
				bool endsWithSoftWrap = !noWrap && advanceEnd < logicalElementEnd;
				bool endsWithHardBreak = hasHardBreak
					&& advanceEnd >= logicalElementEnd;
				lines.Add(
					new CursesTextVisualLine(
						lines.Count,
						new CursesTextPosition( sourceStart ),
						new CursesTextPosition( sourceEnd ),
						lineColumn,
						contentColumns,
						endsWithHardBreak,
						endsWithSoftWrap,
						isClipped,
						publishedFragments
					)
				);
				producedLine = true;
				isTruncated |= isClipped;
				visualElementStart = advanceEnd;
				if ( noWrap ) {
					break;
				}
			}

			if ( !hasHardBreak ) {
				break;
			}
			logicalElementStart = logicalElementEnd + 1;
			logicalSourceStart = nextLogicalSourceStart;
		}

		return CreateLayout(
			text,
			options,
			spans,
			lines,
			cellCount,
			isTruncated
		);
	}

	private static CursesTextLayout CreateLayout(
		string text,
		CursesTextLayoutOptions options,
		CursesTextSpan[] spans,
		List<CursesTextVisualLine> lines,
		int cellCount,
		bool isTruncated
	) {
		return new CursesTextLayout(
			text,
			options,
			spans,
			[ .. lines ],
			cellCount,
			isTruncated
		);
	}

	private static int FindFitEnd(
		CursesTextElement[] elements,
		int start,
		int end,
		CursesTextLayoutOptions options,
		int column
	) {
		int currentColumn = column;
		for ( int index = start; index < end; index++ ) {
			int width = GetElementWidth(
				elements[ index ],
				currentColumn,
				options
			);
			if ( options.Columns - ( currentColumn - column ) < width ) {
				return index;
			}
			currentColumn += width;
		}
		return end;
	}

	private static int FindWordEnd(
		string normalized,
		CursesTextElement[] elements,
		int start,
		int fitEnd
	) {
		for ( int index = fitEnd - 1; index >= start; index-- ) {
			CursesTextElement element = elements[ index ];
			if ( string.IsNullOrWhiteSpace(
				normalized.Substring(
					element.SourceStart,
					element.SourceEnd - element.SourceStart
				)
			) ) {
				return index + 1;
			}
		}
		return start;
	}

	private static int MeasureColumns(
		CursesTextElement[] elements,
		int start,
		int end,
		CursesTextLayoutOptions options,
		int column
	) {
		int currentColumn = column;
		for ( int index = start; index < end; index++ ) {
			currentColumn += GetElementWidth(
				elements[ index ],
				currentColumn,
				options
			);
		}
		return currentColumn - column;
	}

	private static int GetElementWidth(
		CursesTextElement element,
		int column,
		CursesTextLayoutOptions options
	) {
		return element.IsTab
			? options.TabInterval - ( column % options.TabInterval )
			: element.Width
		;
	}

	private static int GetAlignedColumn(
		CursesTextLayoutOptions options,
		int contentColumns
	) {
		int unused = options.Columns - contentColumns;
		return options.Alignment switch {
			CursesTextAlignment.Start => options.StartingColumn,
			CursesTextAlignment.Center => options.StartingColumn + ( unused / 2 ),
			CursesTextAlignment.End => options.StartingColumn + unused,
			_ => throw new InvalidOperationException()
		};
	}

	private static void AppendElement(
		List<MutableFragment> fragments,
		CursesTextElement element,
		int column,
		int width,
		CursesTextLayoutOptions options,
		CursesTextSpan[] spans
	) {
		if ( 0 == width && 0 == fragments.Count ) {
			return;
		}

		CursesStyle style = options.DefaultStyle;
		CursesCellMetadata? metadata = options.DefaultMetadata;
		foreach ( CursesTextSpan span in spans ) {
			if ( span.End.Offset <= element.SourceStart ) {
				continue;
			}
			if ( span.Start.Offset <= element.SourceStart ) {
				style = span.Style;
				metadata = span.Metadata;
			}
			break;
		}

		if ( 0 != fragments.Count ) {
			MutableFragment previous = fragments[ ^1 ];
			if ( 0 == width
				|| ( previous.SourceEnd == element.SourceStart
					&& previous.Style == style
					&& Equals( previous.Metadata, metadata ) ) ) {
				previous.Append(
					element.SourceEnd,
					width
				);
				return;
			}
		}

		fragments.Add(
			new MutableFragment(
				element.SourceStart,
				element.SourceEnd,
				column,
				width,
				style,
				metadata
			)
		);
	}

	private static CursesTextSpan[] CopySpans(
		IReadOnlyList<CursesTextSpan>? spans
	) {
		if ( spans is null ) {
			return [];
		}
		if ( MaximumSpanCount < spans.Count ) {
			throw new ArgumentOutOfRangeException( nameof( spans ) );
		}

		CursesTextSpan[] copy = new CursesTextSpan[ spans.Count ];
		for ( int index = 0; index < spans.Count; index++ ) {
			copy[ index ] = spans[ index ] ?? throw CreateSpanException( index );
		}
		return copy;
	}

	private static CursesTextLayoutOptions CopyOptions(
		CursesTextLayoutOptions options
	) {
		return new CursesTextLayoutOptions( options.Columns ) {
			MaximumRows = options.MaximumRows,
			WrapMode = options.WrapMode,
			Alignment = options.Alignment,
			Overflow = options.Overflow,
			StartingColumn = options.StartingColumn,
			TabInterval = options.TabInterval,
			DefaultStyle = options.DefaultStyle,
			DefaultMetadata = options.DefaultMetadata,
			WidthProvider = options.WidthProvider
		};
	}

	private static void ValidateSpans(
		CursesTextSpan[] spans,
		int sourceLength,
		CursesTextElement[] elements
	) {
		HashSet<int> boundaries = [ 0, sourceLength ];
		foreach ( CursesTextElement element in elements ) {
			_ = boundaries.Add( element.SourceStart );
			_ = boundaries.Add( element.SourceEnd );
		}

		int previousEnd = 0;
		for ( int index = 0; index < spans.Length; index++ ) {
			CursesTextSpan span = spans[ index ];
			if ( span.Start.Offset < previousEnd
				|| sourceLength < span.End.Offset
				|| !boundaries.Contains( span.Start.Offset )
				|| !boundaries.Contains( span.End.Offset ) ) {
				throw CreateSpanException( index );
			}
			previousEnd = span.End.Offset;
		}
	}

	private static ArgumentException CreateSpanException( int index ) {
		return new ArgumentException(
			$"Text span at index {index} is not an ordered legal source range.",
			"spans"
		);
	}

	private static void ValidateOptions( CursesTextLayoutOptions options ) {
		if ( options.MaximumRows is < 0 or > CursesTextLayoutOptions.MaximumExtent ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.MaximumRows )
			);
		}
		if ( !Enum.IsDefined( options.WrapMode ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.WrapMode )
			);
		}
		if ( !Enum.IsDefined( options.Alignment ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.Alignment )
			);
		}
		if ( !Enum.IsDefined( options.Overflow ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.Overflow )
			);
		}
		if ( 0 > options.StartingColumn
			|| CursesTextLayoutOptions.MaximumExtent < options.StartingColumn
			|| int.MaxValue - options.Columns < options.StartingColumn ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.StartingColumn )
			);
		}
		if ( 1 > options.TabInterval
			|| CursesTextLayoutOptions.MaximumExtent < options.TabInterval ) {
			throw new ArgumentOutOfRangeException(
				nameof( CursesTextLayoutOptions.TabInterval )
			);
		}
		ArgumentNullException.ThrowIfNull(
			options.WidthProvider,
			nameof( CursesTextLayoutOptions.WidthProvider )
		);
	}

	private sealed class MutableFragment {
		internal MutableFragment(
			int sourceStart,
			int sourceEnd,
			int column,
			int columns,
			CursesStyle style,
			CursesCellMetadata? metadata
		) {
			SourceStart = sourceStart;
			SourceEnd = sourceEnd;
			Column = column;
			Columns = columns;
			Style = style;
			Metadata = metadata;
		}

		internal int SourceStart { get; }
		internal int SourceEnd { get; private set; }
		internal int Column { get; }
		internal int Columns { get; private set; }
		internal CursesStyle Style { get; }
		internal CursesCellMetadata? Metadata { get; }

		internal void Append(
			int sourceEnd,
			int columns
		) {
			SourceEnd = sourceEnd;
			Columns += columns;
		}

		internal CursesTextFragment Publish( string normalizedText ) {
			return new CursesTextFragment(
				new CursesTextPosition( SourceStart ),
				new CursesTextPosition( SourceEnd ),
				Column,
				Columns,
				normalizedText.Substring(
					SourceStart,
					SourceEnd - SourceStart
				),
				Style,
				Metadata,
				false
			);
		}
	}
}
