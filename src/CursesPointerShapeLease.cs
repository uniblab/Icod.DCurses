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

using Icod.Terminal;

/// <summary>Owns one session-managed terminal mouse-pointer shape request.</summary>
public sealed class CursesPointerShapeLease : IAsyncDisposable {
	private readonly TerminalPointerShapeLease terminalLease;

	internal CursesPointerShapeLease(
		CursesPointerShape shape,
		TerminalPointerShapeLease terminalLease
	) {
		if ( !Enum.IsDefined( shape ) ) {
			throw new ArgumentOutOfRangeException( nameof( shape ) );
		}
		ArgumentNullException.ThrowIfNull( terminalLease );

		this.Shape = shape;
		this.terminalLease = terminalLease;
	}

	/// <summary>Gets the semantic pointer shape owned by this lease.</summary>
	public CursesPointerShape Shape {
		get;
	}

	/// <summary>Releases this pointer-shape request through the canonical Terminal owner.</summary>
	/// <returns>A value task representing asynchronous restoration or final reset.</returns>
	public ValueTask DisposeAsync() {
		return this.terminalLease.DisposeAsync();
	}
}
