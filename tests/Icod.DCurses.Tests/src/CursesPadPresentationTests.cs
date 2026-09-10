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

using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies destructive logical presentation of pad viewports.</summary>
public sealed class CursesPadPresentationTests {
	[Fact]
	public void PresentToCopiesViewportAndPreservesCursors() {
		CursesPad pad = new(
			20,
			10
		);
		CursesWindow content = pad.ContentWindow;
		content.Move(
			4,
			6
		);
		content.Write( "ABCD" );
		content.Move(
			7,
			9
		);

		CursesScreen screen = new(
			12,
			6
		);
		CursesWindow destination = screen.CreateWindow(
			1,
			2,
			4,
			8
		);
		destination.Move(
			3,
			7
		);

		pad.PresentTo(
			destination,
			4,
			6,
			1,
			4,
			1,
			2
		);

		Assert.Equal( 7, content.CursorRow );
		Assert.Equal( 9, content.CursorColumn );
		Assert.Equal( 3, destination.CursorRow );
		Assert.Equal( 7, destination.CursorColumn );
		Assert.Equal( "A", destination.GetCell( 1, 2 ).Content );
		Assert.Equal( "B", destination.GetCell( 1, 3 ).Content );
		Assert.Equal( "C", destination.GetCell( 1, 4 ).Content );
		Assert.Equal( "D", destination.GetCell( 1, 5 ).Content );
	}

	[Fact]
	public void PresentToCopiesOrdinaryBlankCellsDestructively() {
		CursesPad pad = new(
			4,
			2
		);
		CursesScreen screen = new(
			4,
			2
		);
		CursesWindow destination = screen.StandardWindow;
		destination.FillRectangle(
			0,
			0,
			2,
			4,
			new CursesCell( "X" )
		);

		pad.PresentTo(
			destination,
			0,
			0,
			1,
			3,
			0,
			1
		);

		Assert.Equal( "X", destination.GetCell( 0, 0 ).Content );
		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.True( destination.GetCell( 0, 2 ).IsBlank );
		Assert.True( destination.GetCell( 0, 3 ).IsBlank );
	}

	[Fact]
	public void PresentToNormalizesWideGlyphCutAtSourceBoundary() {
		CursesPad pad = new(
			8,
			2
		);
		pad.ContentWindow.Write( "A\u754CB" );

		CursesScreen screen = new(
			6,
			2
		);
		CursesWindow destination = screen.StandardWindow;
		destination.FillRectangle(
			0,
			0,
			1,
			6,
			new CursesCell( "." )
		);

		pad.PresentTo(
			destination,
			0,
			2,
			1,
			2,
			0,
			1
		);

		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( "B", destination.GetCell( 0, 2 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void PresentToRepairsWideDestinationFootprint() {
		CursesPad pad = new(
			3,
			1
		);
		pad.ContentWindow.Write( "Z" );

		CursesScreen screen = new(
			6,
			1
		);
		CursesWindow destination = screen.StandardWindow;
		destination.Move(
			0,
			1
		);
		destination.Write( "\u754C" );

		pad.PresentTo(
			destination,
			0,
			0,
			1,
			1,
			0,
			2
		);

		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( "Z", destination.GetCell( 0, 2 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void PresentToRejectsOutOfRangeSourceOrDestinationRectangles() {
		CursesPad pad = new(
			8,
			4
		);
		CursesScreen screen = new(
			6,
			3
		);
		CursesWindow destination = screen.StandardWindow;

		Assert.Throws<ArgumentOutOfRangeException>(
			() => pad.PresentTo(
				destination,
				3,
				0,
				2,
				1,
				0,
				0
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => pad.PresentTo(
				destination,
				0,
				0,
				1,
				2,
				0,
				5
			)
		);
	}
}
