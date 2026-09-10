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

using System.Buffers;
using System.Globalization;
using System.Text;

/// <summary>
/// Centralizes malformed UTF-16 normalization and Unicode text-element segmentation for DCurses.
/// </summary>
internal static class CursesUnicodeText {
	/// <summary>
	/// Replaces malformed UTF-16 code units deterministically with U+FFFD while preserving valid scalar values.
	/// </summary>
	/// <param name="text">The source text.</param>
	/// <returns>Well-formed UTF-16 text.</returns>
	internal static string NormalizeMalformedUtf16( string text ) {
		ArgumentNullException.ThrowIfNull( text );
		if ( 0 == text.Length ) {
			return string.Empty;
		}

		StringBuilder normalized = new( text.Length );
		ReadOnlySpan<char> remaining = text.AsSpan();
		while ( !remaining.IsEmpty ) {
			OperationStatus status = Rune.DecodeFromUtf16(
				remaining,
				out Rune rune,
				out int consumed
			);
			if ( OperationStatus.Done == status ) {
				normalized.Append( rune.ToString() );
				remaining = remaining[ consumed.. ];
				continue;
			}

			normalized.Append( Rune.ReplacementChar.ToString() );
			remaining = remaining[ 1.. ];
		}

		return normalized.ToString();
	}

	/// <summary>
	/// Enumerates normalized Unicode text elements using the runtime Unicode grapheme segmentation contract.
	/// </summary>
	/// <param name="text">The source text.</param>
	/// <returns>The normalized text elements in source order.</returns>
	internal static IEnumerable<string> EnumerateTextElements( string text ) {
		ArgumentNullException.ThrowIfNull( text );
		string normalized = NormalizeMalformedUtf16( text );
		TextElementEnumerator elements = StringInfo.GetTextElementEnumerator( normalized );
		while ( elements.MoveNext() ) {
			yield return (string)elements.Current;
		}
	}
}
