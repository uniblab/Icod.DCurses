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

/// <summary>Adapts curses rendition semantics to Terminal-owned screen planning.</summary>
internal sealed class CursesPresentationResolver {
	private readonly TerminalScreenPlanner planner;

	internal CursesPresentationResolver( TerminalScreenPlanner planner ) {
		ArgumentNullException.ThrowIfNull( planner );
		this.planner = planner;
	}

	internal CursesStyle Normalize( CursesStyle requested ) {
		return CursesTerminalScreenMapper.ToCurses(
			this.planner.NormalizeRendition(
				CursesTerminalScreenMapper.ToTerminal( requested )
			)
		);
	}

	internal TerminalScreenOperationPlan? PlanBaseline() {
		return this.planner.PlanRenditionBaseline();
	}

	internal TerminalScreenOperationPlan? PlanTransition(
		CursesStyle current,
		CursesStyle target
	) {
		return this.planner.PlanRenditionTransition(
			CursesTerminalScreenMapper.ToTerminal( current ),
			CursesTerminalScreenMapper.ToTerminal( target )
		);
	}

	internal TerminalScreenOperationPlan? PlanReset( CursesStyle current ) {
		return this.planner.PlanRenditionReset(
			CursesTerminalScreenMapper.ToTerminal( current )
		);
	}
}
