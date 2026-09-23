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
	MERCHANTIBILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

/// <summary>Represents one immutable styled span over UTF-16 source text.</summary>
public sealed record CursesTextSpan {
	/// <summary>Initializes one immutable styled source span.</summary>
	/// <param name="start">The span's zero-based UTF-16 source start.</param>
	/// <param name="length">The positive UTF-16 source length.</param>
	/// <param name="style">The presentation style.</param>
	/// <param name="metadata">Optional semantic metadata.</param>
	public CursesTextSpan(
		CursesTextPosition start,
		int length,
		CursesStyle style,
		CursesCellMetadata? metadata = null
	) {
		if ( 0 >= length ) {
			throw new ArgumentOutOfRangeException( nameof( length ) );
		}
		if ( start.Offset > int.MaxValue - length ) {
			throw new ArgumentOutOfRangeException( nameof( length ) );
		}

		Start = start;
		Length = length;
		End = new CursesTextPosition( start.Offset + length );
		Style = style;
		Metadata = metadata;
	}

	/// <summary>Gets the span's zero-based UTF-16 source start.</summary>
	public CursesTextPosition Start {
		get;
	}

	/// <summary>Gets the positive UTF-16 source length.</summary>
	public int Length {
		get;
	}

	/// <summary>Gets the first UTF-16 source position after the span.</summary>
	public CursesTextPosition End {
		get;
	}

	/// <summary>Gets the span's presentation style.</summary>
	public CursesStyle Style {
		get;
	}

	/// <summary>Gets the span's optional semantic metadata.</summary>
	public CursesCellMetadata? Metadata {
		get;
	}
}
