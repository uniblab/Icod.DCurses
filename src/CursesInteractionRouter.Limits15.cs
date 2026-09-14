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

public sealed partial class CursesInteractionRouter {
	/// <summary>Gets the maximum number of live explicit interaction scopes owned by one router.</summary>
	public const int MaximumScopes = 256;

	/// <summary>Gets the maximum explicit interaction-scope depth below the implicit root scope.</summary>
	public const int MaximumScopeDepth = 32;

	/// <summary>Gets the maximum number of gesture bindings owned by one explicit interaction scope.</summary>
	public const int MaximumScopeGestureBindings = 256;
}
