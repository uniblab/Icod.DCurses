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

/// <summary>Specifies retained projection of immutable text layouts.</summary>
public sealed class CursesTextPresentationTests {
	[Fact]
	public void PresentTextLayoutWritesOnlyTheSelectedVisualLines() {
		CursesScreen screen = new( 8, 4 );
		CursesTextLayout layout = CursesTextLayout.Create(
			"one\ntwo",
			new CursesTextLayoutOptions( 3 )
		);

		screen.StandardWindow.PresentTextLayout(
			layout,
			1,
			1,
			2,
			2
		);

		Assert.Equal( "t", screen.VirtualScreen[ 2, 2 ].Content );
		Assert.Equal( "w", screen.VirtualScreen[ 2, 3 ].Content );
		Assert.Equal( "o", screen.VirtualScreen[ 2, 4 ].Content );
		Assert.True( screen.VirtualScreen[ 1, 2 ].IsBlank );
	}
}
