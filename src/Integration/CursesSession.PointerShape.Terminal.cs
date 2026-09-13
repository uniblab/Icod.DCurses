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

public sealed partial class CursesSession {
	/// <summary>Acquires a scoped semantic terminal mouse-pointer shape.</summary>
	/// <param name="shape">The DCurses semantic pointer shape to request.</param>
	/// <param name="cancellationToken">Cancellation for acquisition only.</param>
	/// <returns>A DCurses lease forwarding ownership to the canonical Terminal session.</returns>
	public async ValueTask<CursesPointerShapeLease> AcquirePointerShapeAsync(
		CursesPointerShape shape,
		CancellationToken cancellationToken = default
	) {
		TerminalPointerShape terminalShape = ToTerminalPointerShape( shape );
		cancellationToken.ThrowIfCancellationRequested();

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		TerminalPointerShapeLease terminalLease = await this.terminalSession.AcquirePointerShapeAsync(
			terminalShape,
			cancellationToken
		).ConfigureAwait( false );
		return new CursesPointerShapeLease(
			shape,
			terminalLease
		);
	}

	private static TerminalPointerShape ToTerminalPointerShape(
		CursesPointerShape shape
	) {
		return shape switch {
			CursesPointerShape.Alias => TerminalPointerShape.Alias,
			CursesPointerShape.Cell => TerminalPointerShape.Cell,
			CursesPointerShape.Copy => TerminalPointerShape.Copy,
			CursesPointerShape.Crosshair => TerminalPointerShape.Crosshair,
			CursesPointerShape.Default => TerminalPointerShape.Default,
			CursesPointerShape.EastResize => TerminalPointerShape.EastResize,
			CursesPointerShape.EastWestResize => TerminalPointerShape.EastWestResize,
			CursesPointerShape.Grab => TerminalPointerShape.Grab,
			CursesPointerShape.Grabbing => TerminalPointerShape.Grabbing,
			CursesPointerShape.Help => TerminalPointerShape.Help,
			CursesPointerShape.Move => TerminalPointerShape.Move,
			CursesPointerShape.NorthResize => TerminalPointerShape.NorthResize,
			CursesPointerShape.NorthEastResize => TerminalPointerShape.NorthEastResize,
			CursesPointerShape.NorthEastSouthWestResize => TerminalPointerShape.NorthEastSouthWestResize,
			CursesPointerShape.NoDrop => TerminalPointerShape.NoDrop,
			CursesPointerShape.NotAllowed => TerminalPointerShape.NotAllowed,
			CursesPointerShape.NorthSouthResize => TerminalPointerShape.NorthSouthResize,
			CursesPointerShape.NorthWestResize => TerminalPointerShape.NorthWestResize,
			CursesPointerShape.NorthWestSouthEastResize => TerminalPointerShape.NorthWestSouthEastResize,
			CursesPointerShape.Pointer => TerminalPointerShape.Pointer,
			CursesPointerShape.Progress => TerminalPointerShape.Progress,
			CursesPointerShape.SouthResize => TerminalPointerShape.SouthResize,
			CursesPointerShape.SouthEastResize => TerminalPointerShape.SouthEastResize,
			CursesPointerShape.SouthWestResize => TerminalPointerShape.SouthWestResize,
			CursesPointerShape.Text => TerminalPointerShape.Text,
			CursesPointerShape.VerticalText => TerminalPointerShape.VerticalText,
			CursesPointerShape.WestResize => TerminalPointerShape.WestResize,
			CursesPointerShape.Wait => TerminalPointerShape.Wait,
			CursesPointerShape.ZoomIn => TerminalPointerShape.ZoomIn,
			CursesPointerShape.ZoomOut => TerminalPointerShape.ZoomOut,
			_ => throw new ArgumentOutOfRangeException( nameof( shape ) )
		};
	}
}
