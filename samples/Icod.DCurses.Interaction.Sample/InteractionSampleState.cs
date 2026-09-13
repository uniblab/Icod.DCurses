/*
	Icod.DCurses.Interaction.Sample
	Interactive 1.4 interaction-routing acceptance sample for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Interaction.Sample;

using Icod.DCurses;

internal sealed class InteractionSampleState {
	internal bool Running {
		get;
		set;
	} = true;

	internal bool PopupVisible {
		get;
		set;
	}

	internal string Status {
		get;
		set;
	} = "Ready";

	internal CursesFocusState? TerminalFocus {
		get;
		set;
	}

	internal string RoutedTarget {
		get;
		set;
	} = "-";

	internal CursesPointerShape? AppliedPointerShape {
		get;
		set;
	}
}
