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

/// <summary>Retains one immutable ordered bundle of Terminal-owned operation plans.</summary>
internal sealed class CursesTerminalPlanSequence {
	internal CursesTerminalPlanSequence(
		IEnumerable<TerminalScreenOperationPlan> plans,
		bool usesTemporaryScrollRegion = false
	) {
		ArgumentNullException.ThrowIfNull( plans );
		TerminalScreenOperationPlan[] materialized = plans.ToArray();
		if ( 0 == materialized.Length ) {
			throw new ArgumentException(
				"At least one Terminal screen-operation plan is required.",
				nameof( plans )
			);
		}

		int byteCount = 0;
		foreach ( TerminalScreenOperationPlan plan in materialized ) {
			if ( 0 >= plan.AffectedLines ) {
				throw new ArgumentException(
					"Every Terminal screen-operation plan must be initialized.",
					nameof( plans )
				);
			}
			byteCount = checked( byteCount + plan.ByteCount );
		}

		Plans = Array.AsReadOnly( materialized );
		ByteCount = byteCount;
		UsesTemporaryScrollRegion = usesTemporaryScrollRegion;
	}

	internal IReadOnlyList<TerminalScreenOperationPlan> Plans {
		get;
	}

	internal int ByteCount {
		get;
	}

	internal bool UsesTemporaryScrollRegion {
		get;
	}
}
