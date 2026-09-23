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

/// <summary>Specifies source/visual mapping, hit testing, and selection geometry.</summary>
public sealed class CursesTextGeometryTests {
	[Fact]
	public void SoftWrapBoundaryHonorsAffinity() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abcd",
			new CursesTextLayoutOptions( 2 ) {
				WrapMode = CursesTextWrapMode.TextElement
			}
		);

		Assert.Equal(
			new CursesTextVisualPosition(
				1,
				0,
				CursesTextAffinity.Leading
			),
			layout.GetVisualPosition( new CursesTextPosition( 2 ) )
		);
		Assert.Equal(
			new CursesTextVisualPosition(
				0,
				2,
				CursesTextAffinity.Trailing
			),
			layout.GetVisualPosition(
				new CursesTextPosition( 2 ),
				CursesTextAffinity.Trailing
			)
		);
	}
}
