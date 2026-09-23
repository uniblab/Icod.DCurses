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

using Icod.Terminal;

/// <summary>Adapts curses cursor intent to Terminal-owned screen planning.</summary>
internal sealed class CursesCursorMotionResolver {
	private readonly TerminalScreenPlanner planner;

	internal CursesCursorMotionResolver(
		TerminalScreenPlanner planner
	) {
		ArgumentNullException.ThrowIfNull( planner );
		this.planner = planner;
	}

	internal TerminalScreenOperationPlan Resolve(
		int? currentRow,
		int? currentColumn,
		int targetRow,
		int targetColumn
	) {
		if ( 0 > targetRow ) {
			throw new ArgumentOutOfRangeException( nameof( targetRow ) );
		}
		if ( 0 > targetColumn ) {
			throw new ArgumentOutOfRangeException( nameof( targetColumn ) );
		}

		TerminalScreenPosition? current =
			CursesTerminalScreenMapper.ToTerminalPosition(
				currentRow,
				currentColumn
			);
		TerminalScreenPosition target = new(
			targetRow,
			targetColumn
		);
		return this.planner.PlanCursorMove( current, target )
			?? throw new NotSupportedException(
				$"Terminal '{this.planner.Profile.Name}' does not provide a safe cursor-motion plan for the requested position."
			);
	}
}
