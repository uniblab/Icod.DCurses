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

/// <summary>Freezes T1604 raster propagation through pads and independent viewports.</summary>
public sealed class CursesRasterPadViewportTests {
	[Fact]
	public void PadPresentationCopiesRasterWithSelectedViewport() {
		CursesPad pad = new( 6, 2 );
		CursesScreen destinationScreen = new( 3, 1 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		pad.ContentWindow.SetRasterCell( 1, 3, token );

		pad.PresentTo(
			destinationScreen.StandardWindow,
			1,
			2,
			1,
			3,
			0,
			0
		);

		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
		AssertSameToken( token, destinationScreen.StandardWindow.GetRasterCell( 0, 1 ) );
		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void ViewportPanReprojectsRasterAndClearsOldCoordinate() {
		CursesPad pad = new( 5, 1 );
		CursesScreen destinationScreen = new( 2, 1 );
		CursesRasterCell first = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		CursesRasterCell second = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		pad.ContentWindow.SetRasterCell( 0, 0, first );
		pad.ContentWindow.SetRasterCell( 0, 2, second );
		CursesPadViewport viewport = pad.CreateViewport(
			destinationScreen.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);

		viewport.Present();
		AssertSameToken( first, destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 1 ) );

		viewport.PanBy( 0, 1 );
		viewport.Present();
		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
		AssertSameToken( second, destinationScreen.StandardWindow.GetRasterCell( 0, 1 ) );
	}

	[Fact]
	public void VisibleRasterMutationParticipatesInViewportChangeTracking() {
		CursesPad pad = new( 4, 1 );
		CursesScreen destinationScreen = new( 2, 1 );
		CursesPadViewport viewport = pad.CreateViewport(
			destinationScreen.StandardWindow,
			0,
			1,
			1,
			2,
			0,
			0
		);

		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );

		pad.ContentWindow.SetRasterCell(
			0,
			2,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		Assert.True( viewport.HasVisiblePadChanges );
		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );
	}

	[Fact]
	public void RasterOutsidePadViewportRemainsClipped() {
		CursesPad pad = new( 5, 2 );
		CursesScreen destinationScreen = new( 2, 1 );
		CursesRasterCell outside = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		pad.ContentWindow.SetRasterCell( 1, 4, outside );

		pad.PresentTo(
			destinationScreen.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);

		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 0 ) );
		Assert.Null( destinationScreen.StandardWindow.GetRasterCell( 0, 1 ) );
	}

	private static void AssertSameToken(
		CursesRasterCell expected,
		CursesRasterCell? actual
	) {
		Assert.True( actual.HasValue );
		Assert.True( actual.Value.IsValid );
		Assert.Same(
			expected.Placeholder,
			actual.Value.Placeholder
		);
	}
}
