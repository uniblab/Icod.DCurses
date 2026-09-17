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

/// <summary>Projects Terminal raster ownership observations into the DCurses public vocabulary.</summary>
internal static class CursesRasterOwnershipMapper {
	internal static CursesRasterOwnershipState FromTerminal(
		TerminalRasterOwnershipState state
	) {
		return new CursesRasterOwnershipState(
			ToCursesStatus( state.Status ),
			ToCursesLossReason( state.LossReason )
		);
	}

	private static CursesRasterOwnershipStatus ToCursesStatus(
		TerminalRasterOwnershipStatus status
	) {
		return status switch {
			TerminalRasterOwnershipStatus.Current => CursesRasterOwnershipStatus.Current,
			TerminalRasterOwnershipStatus.Stale => CursesRasterOwnershipStatus.Stale,
			TerminalRasterOwnershipStatus.Released => CursesRasterOwnershipStatus.Released,
			TerminalRasterOwnershipStatus.Disposed => CursesRasterOwnershipStatus.Disposed,
			_ => throw new ArgumentOutOfRangeException(
				nameof( status ),
				status,
				"The Terminal raster ownership status is not recognized by this DCurses release."
			)
		};
	}

	private static CursesRasterOwnershipLossReason ToCursesLossReason(
		TerminalRasterOwnershipLossReason reason
	) {
		return reason switch {
			TerminalRasterOwnershipLossReason.None => CursesRasterOwnershipLossReason.None,
			TerminalRasterOwnershipLossReason.SessionStateLost => CursesRasterOwnershipLossReason.SessionStateLost,
			TerminalRasterOwnershipLossReason.ResourceMissing => CursesRasterOwnershipLossReason.ResourceMissing,
			TerminalRasterOwnershipLossReason.ParentPlacementLost => CursesRasterOwnershipLossReason.ParentPlacementLost,
			TerminalRasterOwnershipLossReason.AncestorReleased => CursesRasterOwnershipLossReason.AncestorReleased,
			TerminalRasterOwnershipLossReason.ResourceReleased => CursesRasterOwnershipLossReason.ResourceReleased,
			TerminalRasterOwnershipLossReason.ExplicitDisposal => CursesRasterOwnershipLossReason.ExplicitDisposal,
			_ => throw new ArgumentOutOfRangeException(
				nameof( reason ),
				reason,
				"The Terminal raster ownership loss reason is not recognized by this DCurses release."
			)
		};
	}
}
