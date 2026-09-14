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

/// <summary>Represents one immutable normalized pointer gesture snapshot.</summary>
public sealed class CursesPointerGesture {
	internal CursesPointerGesture(
		CursesPointerGestureKind kind,
		CursesMouseButton button,
		CursesKeyModifiers modifiers,
		CursesPointerTarget? target
	) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}
		if ( !Enum.IsDefined( button ) ) {
			throw new ArgumentOutOfRangeException( nameof( button ) );
		}
		this.Kind = kind;
		this.Button = button;
		this.Modifiers = modifiers;
		this.Target = target;
	}

	/// <summary>Gets the normalized gesture kind.</summary>
	public CursesPointerGestureKind Kind {
		get;
	}

	/// <summary>Gets the concrete button associated with the gesture, or None when no button applies.</summary>
	public CursesMouseButton Button {
		get;
	}

	/// <summary>Gets keyboard modifiers reported with the originating mouse event.</summary>
	public CursesKeyModifiers Modifiers {
		get;
	}

	/// <summary>Gets the current routed pointer target when one exists.</summary>
	public CursesPointerTarget? Target {
		get;
	}
}
