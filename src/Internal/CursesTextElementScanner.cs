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

using System.Text;

internal readonly record struct CursesTextElement(
	int SourceStart,
	int SourceEnd,
	int Width,
	bool IsHardBreak,
	bool IsTab
);

internal static class CursesTextElementScanner {
	internal static CursesTextElement[] Scan(
		string text,
		ICursesTextWidthProvider widthProvider
	) {
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( widthProvider );
		if ( 0 == text.Length ) {
			return [];
		}

		List<CursesTextElement> elements = [];
		int sourceStart = 0;
		foreach ( string textElement in CursesUnicodeText.EnumerateTextElements( text ) ) {
			bool isHardBreak = IsHardBreak( textElement );
			bool isTab = "\t" == textElement;
			ValidateControls( textElement );
			int width = isHardBreak || isTab
				? 0
				: GetValidatedWidth(
					widthProvider,
					textElement
				)
			;
			int sourceEnd = sourceStart + textElement.Length;
			elements.Add(
				new CursesTextElement(
					sourceStart,
					sourceEnd,
					width,
					isHardBreak,
					isTab
				)
			);
			sourceStart = sourceEnd;
		}

		return [ .. elements ];
	}

	private static bool IsHardBreak( string textElement ) {
		return "\r" == textElement
			|| "\n" == textElement
			|| "\r\n" == textElement;
	}

	private static void ValidateControls( string textElement ) {
		foreach ( Rune rune in textElement.EnumerateRunes() ) {
			if ( rune.Value is '\t' or '\r' or '\n' ) {
				continue;
			}
			if ( rune.Value <= 0x1F
				|| rune.Value is >= 0x7F and <= 0x9F ) {
				throw new ArgumentException(
					"Text layout cannot process this control character.",
					"text"
				);
			}
		}
	}

	private static int GetValidatedWidth(
		ICursesTextWidthProvider widthProvider,
		string textElement
	) {
		int width = widthProvider.GetWidth( textElement );
		if ( width < 0 || 2 < width ) {
			throw new InvalidOperationException(
				"The configured curses text-width provider returned a width outside the supported range."
			);
		}

		return width;
	}
}
