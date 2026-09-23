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

		return BuildNoWrap(
			text,
			optionCopy,
			spanCopy,
			elements
		);
	}

	private static CursesTextLayout BuildNoWrap(
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
		int elementIndex = 0;
		int logicalStart = 0;
		int cellCount = 0;
		int fragmentCount = 0;
		bool isTruncated = false;
		while ( true ) {
			if ( options.MaximumRows is int maximumRows
				&& maximumRows <= lines.Count ) {
				isTruncated |= logicalStart < text.Length;
				break;
			}

			List<MutableFragment> fragments = [];
			int absoluteColumn = options.StartingColumn;
			int sourceEnd = logicalStart;
			int nextLogicalStart = logicalStart;
			bool endsWithHardBreak = false;
			bool isClipped = false;
			while ( elementIndex < elements.Length ) {
				CursesTextElement element = elements[ elementIndex ];
				if ( element.IsHardBreak ) {
					endsWithHardBreak = true;
					nextLogicalStart = element.SourceEnd;
					elementIndex++;
					break;
				}

				if ( isClipped ) {
					elementIndex++;
					continue;
				}

				int width = element.IsTab
					? options.TabInterval - ( absoluteColumn % options.TabInterval )
					: element.Width
				;
				int usedColumns = absoluteColumn - options.StartingColumn;
				if ( options.Columns - usedColumns < width ) {
					isClipped = true;
					isTruncated = true;
					elementIndex++;
					continue;
				}

				AppendElement(
					fragments,
					element,
					absoluteColumn,
					width,
					options,
					spans
				);
				absoluteColumn += width;
				sourceEnd = element.SourceEnd;
				elementIndex++;
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
			int lineColumns = absoluteColumn - options.StartingColumn;
			cellCount = checked( cellCount + lineColumns );
			if ( MaximumCellCount < cellCount ) {
				throw new InvalidOperationException(
					"Text layout exceeded the supported cell capacity."
				);
			}
			lines.Add(
				new CursesTextVisualLine(
					lines.Count,
					new CursesTextPosition( logicalStart ),
					new CursesTextPosition( sourceEnd ),
					options.StartingColumn,
					lineColumns,
					endsWithHardBreak,
					false,
					isClipped,
					publishedFragments
				)
			);

			if ( !endsWithHardBreak ) {
				break;
			}
			logicalStart = nextLogicalStart;
		}

		return new CursesTextLayout(
			text,
			options,
			spans,
			[ .. lines ],
			cellCount,
			isTruncated
		);
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
