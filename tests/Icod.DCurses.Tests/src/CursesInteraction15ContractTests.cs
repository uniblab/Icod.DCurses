/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the additive public interaction contract selected for Icod.DCurses 1.5.</summary>
public sealed class CursesInteraction15ContractTests {
	[Fact]
	public void FocusDirectionNumericsPreserve14AndAppendSpatialDirections() {
		Assert.Equal( 0, (int)CursesFocusDirection.Forward );
		Assert.Equal( 1, (int)CursesFocusDirection.Backward );
		Assert.Equal( 2, (int)CursesFocusDirection.Up );
		Assert.Equal( 3, (int)CursesFocusDirection.Down );
		Assert.Equal( 4, (int)CursesFocusDirection.Left );
		Assert.Equal( 5, (int)CursesFocusDirection.Right );
	}

	[Fact]
	public void Interaction15BoundsAreFrozen() {
		Assert.Equal( 4096, CursesInteractionRouter.MaximumRegions );
		Assert.Equal( 256, CursesInteractionRouter.MaximumScopes );
		Assert.Equal( 32, CursesInteractionRouter.MaximumScopeDepth );
		Assert.Equal( 256, CursesInteractionRouter.MaximumScopeGestureBindings );
		Assert.Equal( 16384, CursesInteractionRouter.MaximumGestureBindings );
	}

	[Fact]
	public void InteractionResultKindNumericsRemainFrozen() {
		Assert.Equal( 0, (int)CursesInteractionResultKind.Unrouted );
		Assert.Equal( 1, (int)CursesInteractionResultKind.Targeted );
		Assert.Equal( 2, (int)CursesInteractionResultKind.Command );
	}
}
