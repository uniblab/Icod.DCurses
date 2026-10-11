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

/// <summary>Owns one reversible session-managed terminal text-cursor style request.</summary>
public sealed class CursesCursorStyleLease : IAsyncDisposable {
	private readonly TerminalCursorStyleLease terminalLease;

	internal CursesCursorStyleLease(
		CursesCursorStyle style,
		TerminalCursorStyleLease terminalLease
	) {
		if ( !Enum.IsDefined( style ) ) {
			throw new ArgumentOutOfRangeException( nameof( style ) );
		}
		ArgumentNullException.ThrowIfNull( terminalLease );
		this.Style = style;
		this.terminalLease = terminalLease;
	}

	/// <summary>Gets the semantic cursor style owned by this lease.</summary>
	public CursesCursorStyle Style { get; }

	/// <summary>Releases this style request and restores the exact preceding style.</summary>
	public ValueTask DisposeAsync() {
		return this.terminalLease.DisposeAsync();
	}
}
