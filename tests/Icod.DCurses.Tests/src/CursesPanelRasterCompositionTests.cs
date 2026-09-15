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

/// <summary>Freezes T1605 retained-raster panel composition and resize behavior.</summary>
public sealed class CursesPanelRasterCompositionTests {
	[Fact]
	public void CompositionWithoutPanelsCopiesBaseRasterState() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell token = CreateToken();
		screen.StandardWindow.SetRasterCell( 1, 2, token );

		CursesVirtualScreen composed = screen.ComposePanels();

		AssertSameToken( token, composed.GetRasterCell( 1, 2 ) );
		AssertSameToken( token, screen.VirtualScreen.GetRasterCell( 1, 2 ) );
	}

	[Fact]
	public void OpaqueBlankPanelClearsCoveredBaseRaster() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell baseRaster = CreateToken();
		screen.StandardWindow.SetRasterCell( 0, 2, baseRaster );
		_ = screen.CreatePanel( 0, 2, 1, 1 );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Null( composed.GetRasterCell( 0, 2 ) );
		AssertSameToken( baseRaster, screen.VirtualScreen.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void TransparentBlankPanelWithoutRasterRevealsBaseRaster() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell baseRaster = CreateToken();
		screen.StandardWindow.SetRasterCell( 0, 2, baseRaster );
		CursesPanel panel = screen.CreatePanel( 0, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;

		CursesVirtualScreen composed = screen.ComposePanels();

		AssertSameToken( baseRaster, composed.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void TransparentBlankPanelWithRasterIsVisuallyPresent() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell baseRaster = CreateToken();
		CursesRasterCell panelRaster = CreateToken();
		screen.StandardWindow.SetRasterCell( 0, 2, baseRaster );
		CursesPanel panel = screen.CreatePanel( 0, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.SetRasterCell( 0, 0, panelRaster );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.True( composed.GetCell( 0, 2 ).IsBlank );
		AssertSameToken( panelRaster, composed.GetRasterCell( 0, 2 ) );
	}

	[Fact]
	public void TopmostVisiblePanelRasterWinsAndReorderingChangesWinner() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell lowerRaster = CreateToken();
		CursesRasterCell upperRaster = CreateToken();
		CursesPanel lower = screen.CreatePanel( 0, 1, 1, 1 );
		CursesPanel upper = screen.CreatePanel( 0, 1, 1, 1 );
		lower.ContentWindow.SetRasterCell( 0, 0, lowerRaster );
		upper.ContentWindow.SetRasterCell( 0, 0, upperRaster );

		CursesVirtualScreen composed = screen.ComposePanels();
		AssertSameToken( upperRaster, composed.GetRasterCell( 0, 1 ) );

		composed.MarkClean();
		lower.MoveToTop();
		CursesVirtualScreen reordered = screen.ComposePanels();

		Assert.Same( composed, reordered );
		AssertSameToken( lowerRaster, reordered.GetRasterCell( 0, 1 ) );
		Assert.True( reordered.IsDirty( 0, 1 ) );
	}

	[Fact]
	public void HideShowAndDisposeRestoreUnderlyingRasterDeterministically() {
		CursesScreen screen = new( 5, 2 );
		CursesRasterCell baseRaster = CreateToken();
		CursesRasterCell panelRaster = CreateToken();
		screen.StandardWindow.SetRasterCell( 0, 1, baseRaster );
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.ContentWindow.SetRasterCell( 0, 0, panelRaster );

		CursesVirtualScreen composed = screen.ComposePanels();
		AssertSameToken( panelRaster, composed.GetRasterCell( 0, 1 ) );

		composed.MarkClean();
		panel.Hide();
		CursesVirtualScreen hidden = screen.ComposePanels();
		AssertSameToken( baseRaster, hidden.GetRasterCell( 0, 1 ) );
		Assert.True( hidden.IsDirty( 0, 1 ) );

		hidden.MarkClean();
		panel.Show();
		CursesVirtualScreen shown = screen.ComposePanels();
		AssertSameToken( panelRaster, shown.GetRasterCell( 0, 1 ) );
		Assert.True( shown.IsDirty( 0, 1 ) );

		shown.MarkClean();
		panel.Dispose();
		CursesVirtualScreen disposed = screen.ComposePanels();
		AssertSameToken( baseRaster, disposed.GetRasterCell( 0, 1 ) );
		Assert.True( disposed.IsDirty( 0, 1 ) );
	}

	[Fact]
	public void MovingPanelMovesRasterAndClearsOldCoordinate() {
		CursesScreen screen = new( 7, 3 );
		CursesRasterCell token = CreateToken();
		CursesPanel panel = screen.CreatePanel( 0, 0, 1, 1 );
		panel.ContentWindow.SetRasterCell( 0, 0, token );
		CursesVirtualScreen composed = screen.ComposePanels();
		AssertSameToken( token, composed.GetRasterCell( 0, 0 ) );
		composed.MarkClean();

		panel.MoveTo( 1, 4 );
		CursesVirtualScreen moved = screen.ComposePanels();

		Assert.Same( composed, moved );
		Assert.Null( moved.GetRasterCell( 0, 0 ) );
		AssertSameToken( token, moved.GetRasterCell( 1, 4 ) );
		Assert.True( moved.IsDirty( 0, 0 ) );
		Assert.True( moved.IsDirty( 1, 4 ) );
	}

	[Fact]
	public void PanelRasterMutationRecomposesOnlyChangedCoordinate() {
		CursesScreen screen = new( 6, 2 );
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 3 );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		CursesRasterCell token = CreateToken();

		panel.ContentWindow.SetRasterCell( 0, 1, token );
		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		AssertSameToken( token, updated.GetRasterCell( 0, 2 ) );
		Assert.Equal( 1, updated.DirtyCellCount );
		Assert.True( updated.IsDirty( 0, 2 ) );
	}

	[Fact]
	public void PanelResizePreservesRasterInOverlappingUpperLeftRegion() {
		CursesScreen screen = new( 8, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 2, 2 );
		CursesRasterCell token = CreateToken();
		panel.ContentWindow.SetRasterCell( 1, 1, token );

		panel.Resize( 3, 3 );

		AssertSameToken( token, panel.ContentWindow.GetRasterCell( 1, 1 ) );
		CursesVirtualScreen composed = screen.ComposePanels();
		AssertSameToken( token, composed.GetRasterCell( 2, 3 ) );
	}

	[Fact]
	public void PanelShrinkDiscardsRasterOutsideNewBounds() {
		CursesScreen screen = new( 8, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 2, 3 );
		CursesRasterCell kept = CreateToken();
		CursesRasterCell discarded = CreateToken();
		panel.ContentWindow.SetRasterCell( 0, 0, kept );
		panel.ContentWindow.SetRasterCell( 1, 2, discarded );

		panel.Resize( 1, 2 );

		AssertSameToken( kept, panel.ContentWindow.GetRasterCell( 0, 0 ) );
		Assert.Equal( 1, panel.Rows );
		Assert.Equal( 2, panel.Columns );
		CursesVirtualScreen composed = screen.ComposePanels();
		AssertSameToken( kept, composed.GetRasterCell( 1, 2 ) );
		Assert.Null( composed.GetRasterCell( 2, 4 ) );
	}

	[Fact]
	public void RasterPanelCompositionClipsAfterDestinationShrink() {
		CursesScreen screen = new( 6, 3 );
		CursesPanel panel = screen.CreatePanel( 1, 4, 2, 2 );
		CursesRasterCell visible = CreateToken();
		CursesRasterCell clipped = CreateToken();
		panel.ContentWindow.SetRasterCell( 0, 0, visible );
		panel.ContentWindow.SetRasterCell( 1, 1, clipped );

		screen.Resize( 5, 2, preserveContents: true );
		CursesVirtualScreen composed = screen.ComposePanels();

		AssertSameToken( visible, composed.GetRasterCell( 1, 4 ) );
		AssertSameToken( clipped, panel.ContentWindow.GetRasterCell( 1, 1 ) );
	}

	private static CursesRasterCell CreateToken() {
		return CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
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
