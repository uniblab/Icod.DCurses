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

/// <summary>Specifies the immutable 2.1 rich-text layout contract.</summary>
public sealed class CursesTextLayoutTests {
	[Fact]
	public void CreateProducesOneStyledVisualLine() {
		CursesTextLayout layout = CursesTextLayout.Create(
			"abc",
			new CursesTextLayoutOptions( 5 )
		);

		CursesTextVisualLine line = Assert.Single( layout.Lines );
		Assert.Equal( 3, line.Columns );
		Assert.Equal( "abc", Assert.Single( line.Fragments ).Text );
		Assert.Equal( 3, layout.CellCount );
		Assert.False( layout.IsTruncated );
	}
}
