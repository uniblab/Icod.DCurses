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

/// <summary>Verifies that ordinary subwindows provide the shared derived-view contract for pads.</summary>
public sealed class CursesPadDerivedViewTests {
	[Fact]
	public void DerivedViewsSharePadStorageAndKeepIndependentCursors() {
		CursesPad pad = new(
			12,
			6
		);
		CursesWindow first = pad.ContentWindow.CreateSubwindow(
			1,
			2,
			3,
			6
		);
		CursesWindow second = pad.ContentWindow.CreateSubwindow(
			2,
			4,
			3,
			6
		);

		first.FillRectangle(
			1,
			2,
			1,
			3,
			new CursesCell( "X" )
		);

		Assert.Equal( "X", pad.ContentWindow.GetCell( 2, 4 ).Content );
		Assert.Equal( "X", second.GetCell( 0, 0 ).Content );
		Assert.Equal( "X", second.GetCell( 0, 1 ).Content );
		Assert.Equal( "X", second.GetCell( 0, 2 ).Content );

		first.Move(
			0,
			1
		);
		second.Move(
			2,
			3
		);
		Assert.Equal( 0, first.CursorRow );
		Assert.Equal( 1, first.CursorColumn );
		Assert.Equal( 2, second.CursorRow );
		Assert.Equal( 3, second.CursorColumn );
	}

	[Fact]
	public void DerivedViewUsesPadUnicodeAndWideCellContract() {
		CursesPad pad = new(
			10,
			4
		);
		CursesWindow view = pad.ContentWindow.CreateSubwindow(
			1,
			2,
			2,
			6
		);
		view.Move(
			0,
			1
		);

		view.Write( "A\u754CB" );

		Assert.Equal( "A", pad.ContentWindow.GetCell( 1, 3 ).Content );
		Assert.Equal( "\u754C", pad.ContentWindow.GetCell( 1, 4 ).Content );
		Assert.Equal( 2, pad.ContentWindow.GetCell( 1, 4 ).DisplayWidth );
		Assert.True( pad.ContentWindow.GetCell( 1, 5 ).IsContinuation );
		Assert.Equal( "B", pad.ContentWindow.GetCell( 1, 6 ).Content );
		CursesCellFootprint.Validate( GetBackingScreen( pad ) );
	}

	[Fact]
	public void ViewportPresentationObservesEditsMadeThroughDerivedView() {
		CursesPad pad = new(
			10,
			4
		);
		CursesWindow view = pad.ContentWindow.CreateSubwindow(
			1,
			2,
			2,
			5
		);
		view.Write( "A\u754CB" );

		CursesScreen destination = new(
			4,
			1
		);
		pad.PresentTo(
			destination.StandardWindow,
			1,
			2,
			1,
			4,
			0,
			0
		);

		Assert.Equal( "A", destination.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "\u754C", destination.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.True( destination.StandardWindow.GetCell( 0, 2 ).IsContinuation );
		Assert.Equal( "B", destination.StandardWindow.GetCell( 0, 3 ).Content );
		CursesCellFootprint.Validate( destination.VirtualScreen );
	}

	private static CursesVirtualScreen GetBackingScreen( CursesPad pad ) {
		ArgumentNullException.ThrowIfNull( pad );
		CursesScreen probe = new(
			pad.Columns,
			pad.Rows,
			pad.TextWidthProvider
		);
		pad.PresentTo(
			probe.StandardWindow,
			0,
			0,
			pad.Rows,
			pad.Columns,
			0,
			0
		);
		return probe.VirtualScreen;
	}
}
