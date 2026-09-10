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

using System.Runtime.CompilerServices;
using Icod.DCurses.Internal;

/// <summary>Provides the internal logical-frame composition operation used while the 1.2 contract is under development.</summary>
internal static class CursesPanelCompositionExtensions {
	private static readonly ConditionalWeakTable<CursesScreen, CursesPanelCompositionState> compositionStates = new();

	/// <summary>Creates or incrementally updates the retained composed logical frame for one screen.</summary>
	/// <param name="screen">The screen whose base frame and panels should be composed.</param>
	/// <returns>The retained logical frame containing the base screen plus visible panels in bottom-to-top order.</returns>
	internal static CursesVirtualScreen ComposePanels( this CursesScreen screen ) {
		ArgumentNullException.ThrowIfNull( screen );

		CursesPanelCompositionState state = compositionStates.GetValue(
			screen,
			static _ => new CursesPanelCompositionState()
		);
		return state.Compose( screen );
	}
}
