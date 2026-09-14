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

/// <summary>Owns one explicit pointer-capture lifetime.</summary>
public sealed class CursesPointerCaptureLease : IDisposable {
	private readonly CursesInteractionRouter owner;
	private bool released;

	internal CursesPointerCaptureLease(
		CursesInteractionRouter owner,
		long generation
	) {
		ArgumentNullException.ThrowIfNull( owner );
		if ( 0 > generation ) {
			throw new ArgumentOutOfRangeException( nameof( generation ) );
		}

		this.owner = owner;
		this.Generation = generation;
	}

	/// <summary>Releases this capture when it is still current.</summary>
	public void Dispose() {
		if ( this.released ) {
			return;
		}

		this.owner.ReleasePointerCapture( this );
	}

	internal long Generation {
		get;
	}

	internal bool IsReleased => this.released;

	internal void MarkReleased() {
		this.released = true;
	}
}
