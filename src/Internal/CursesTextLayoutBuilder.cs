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
	internal static CursesTextLayout Build(
		string text,
		CursesTextLayoutOptions options,
		IReadOnlyList<CursesTextSpan>? spans
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( options );
		CursesTextSpan[] spanCopy = spans?.ToArray() ?? [];
		CursesTextLayoutOptions optionCopy = new( options.Columns ) {
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
		CursesTextElement[] elements = CursesTextElementScanner.Scan(
			text,
			optionCopy.WidthProvider
		);
		int columns = elements.Sum( static current => current.Width );
		CursesTextFragment[] fragments = 0 == text.Length
			? []
			: [
				new CursesTextFragment(
					new CursesTextPosition( 0 ),
					new CursesTextPosition( text.Length ),
					optionCopy.StartingColumn,
					columns,
					CursesUnicodeText.NormalizeMalformedUtf16( text ),
					optionCopy.DefaultStyle,
					optionCopy.DefaultMetadata,
					false
				)
			]
		;
		CursesTextVisualLine line = new(
			0,
			new CursesTextPosition( 0 ),
			new CursesTextPosition( text.Length ),
			optionCopy.StartingColumn,
			columns,
			false,
			false,
			false,
			fragments
		);

		return new CursesTextLayout(
			text,
			optionCopy,
			spanCopy,
			[ line ],
			columns,
			false
		);
	}
}
