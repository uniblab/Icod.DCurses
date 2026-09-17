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

/// <summary>Exercises repeated retained-raster mutation paths for deterministic state convergence.</summary>
public sealed class CursesRasterAdversarialTests {
	[Fact]
	public void RepeatedPreservingScreenResizeRetainsExactlyOneRasterReference() {
		CursesScreen screen = new( 8, 4 );
		CursesRasterCell token = CreateToken();
		screen.StandardWindow.SetRasterCell( 1, 2, token );

		for ( int cycle = 0; cycle < 32; cycle++ ) {
			screen.Resize( 12, 6, preserveContents: true );
			AssertSingleRasterAt( screen.VirtualScreen, 1, 2, token );

			screen.Resize( 4, 2, preserveContents: true );
			AssertSingleRasterAt( screen.VirtualScreen, 1, 2, token );
		}

		screen.Resize( 8, 4, preserveContents: false );
		Assert.Empty( EnumerateRasterCells( screen.VirtualScreen ) );
		Assert.Equal( 0, screen.VirtualScreen.RasterCellCount );
		Assert.Equal( 0, screen.VirtualScreen.RasterAllocatedRowCount );
		Assert.False( screen.VirtualScreen.RasterStorageAllocated );
	}

	[Fact]
	public void RepeatedPanelMoveHideShowDoesNotAccumulateComposedRasterState() {
		CursesScreen screen = new( 20, 6 );
		CursesRasterCell token = CreateToken();
		CursesPanel panel = screen.CreatePanel( 0, 0, 1, 1 );
		panel.ContentWindow.SetRasterCell( 0, 0, token );

		for ( int cycle = 0; cycle < 64; cycle++ ) {
			int row = cycle % screen.Rows;
			int column = ( cycle * 7 ) % screen.Columns;
			panel.MoveTo( row, column );
			panel.Show();

			CursesVirtualScreen visible = screen.ComposePanels();
			AssertSingleRasterAt( visible, row, column, token );

			panel.Hide();
			CursesVirtualScreen hidden = screen.ComposePanels();
			Assert.Equal( 0, hidden.RasterCellCount );
			Assert.Equal( 0, hidden.RasterAllocatedRowCount );
			Assert.False( hidden.RasterStorageAllocated );
		}
	}

	[Fact]
	public void RepeatedPadViewportPanningConvergesToOneVisibleRasterCell() {
		CursesPad pad = new( 5, 1 );
		CursesScreen destination = new( 2, 1 );
		CursesRasterCell first = CreateToken();
		CursesRasterCell second = CreateToken();
		pad.ContentWindow.SetRasterCell( 0, 0, first );
		pad.ContentWindow.SetRasterCell( 0, 2, second );
		CursesPadViewport viewport = pad.CreateViewport(
			destination.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);

		for ( int cycle = 0; cycle < 64; cycle++ ) {
			viewport.Present();
			AssertSingleRasterAt( destination.VirtualScreen, 0, 0, first );

			viewport.PanBy( 0, 1 );
			viewport.Present();
			AssertSingleRasterAt( destination.VirtualScreen, 0, 1, second );

			viewport.PanBy( 0, -1 );
		}
	}

	[Fact]
	public void RepeatedCopyOverlayAndClearReturnDestinationRasterStorageToZero() {
		CursesScreen source = new( 4, 2 );
		CursesScreen destination = new( 4, 2 );
		CursesRasterCell first = CreateToken();
		CursesRasterCell second = CreateToken();
		source.StandardWindow.SetRasterCell( 0, 1, first );
		source.StandardWindow.SetRasterCell( 1, 3, second );

		for ( int cycle = 0; cycle < 64; cycle++ ) {
			source.StandardWindow.CopyRectangleTo(
				destination.StandardWindow,
				0,
				0,
				2,
				4,
				0,
				0
			);
			Assert.Equal( 2, destination.VirtualScreen.RasterCellCount );
			AssertSameToken( first, destination.VirtualScreen.GetRasterCell( 0, 1 ) );
			AssertSameToken( second, destination.VirtualScreen.GetRasterCell( 1, 3 ) );

			destination.VirtualScreen.Clear();
			AssertRasterStorageReleased( destination.VirtualScreen );

			source.StandardWindow.OverlayRectangleTo(
				destination.StandardWindow,
				0,
				0,
				2,
				4,
				0,
				0
			);
			Assert.Equal( 2, destination.VirtualScreen.RasterCellCount );
			AssertSameToken( first, destination.VirtualScreen.GetRasterCell( 0, 1 ) );
			AssertSameToken( second, destination.VirtualScreen.GetRasterCell( 1, 3 ) );

			destination.VirtualScreen.Clear();
			AssertRasterStorageReleased( destination.VirtualScreen );
		}
	}

	private static CursesRasterCell CreateToken() {
		return CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
	}

	private static void AssertSingleRasterAt(
		CursesVirtualScreen screen,
		int row,
		int column,
		CursesRasterCell expected
	) {
		Assert.Equal( 1, screen.RasterCellCount );
		Assert.Equal( 1, screen.RasterAllocatedRowCount );
		Assert.True( screen.RasterStorageAllocated );
		AssertSameToken( expected, screen.GetRasterCell( row, column ) );
		Assert.Single( EnumerateRasterCells( screen ) );
	}

	private static void AssertRasterStorageReleased( CursesVirtualScreen screen ) {
		Assert.Equal( 0, screen.RasterCellCount );
		Assert.Equal( 0, screen.RasterAllocatedRowCount );
		Assert.False( screen.RasterStorageAllocated );
		Assert.Empty( EnumerateRasterCells( screen ) );
	}

	private static List<CursesRasterCell> EnumerateRasterCells( CursesVirtualScreen screen ) {
		List<CursesRasterCell> result = [];
		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				CursesRasterCell? cell = screen.GetRasterCell( row, column );
				if ( cell.HasValue ) {
					result.Add( cell.Value );
				}
			}
		}
		return result;
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
