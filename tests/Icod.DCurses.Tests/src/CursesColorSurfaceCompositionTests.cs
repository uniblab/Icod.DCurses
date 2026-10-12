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

namespace Icod.DCurses.Tests;

using Icod.DCurses.Internal;
using Xunit;

/// <summary>Qualifies complete-style retention through pads and panel composition.</summary>
public sealed class CursesColorSurfaceCompositionTests {
	private static readonly CursesStyle LowerStyle = new(
		CursesColor.Indexed( 24 ),
		CursesColor.Rgb( 15, 30, 45 ),
		CursesTextAttributes.Bold
	);

	private static readonly CursesStyle UpperStyle = new(
		CursesColor.Rgb( 220, 90, 30 ),
		CursesColor.Indexed( 53 ),
		CursesTextAttributes.Underline | CursesTextAttributes.Italic
	);

	[Fact]
	public void PadViewportRetainsCompleteStylesAcrossProjection() {
		CursesPad pad = new( 5, 2 );
		pad.ContentWindow.FillRectangle( 0, 0, 1, 5, new CursesCell( "L", LowerStyle ) );
		pad.ContentWindow.FillRectangle( 1, 0, 1, 5, new CursesCell( "U", UpperStyle ) );
		CursesScreen screen = new( 3, 2 );
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow, 0, 1, 2, 3, 0, 0
		);

		viewport.Present();

		for ( int column = 0; column < 3; column++ ) {
			Assert.Equal( LowerStyle, screen.VirtualScreen[ 0, column ].Style );
			Assert.Equal( UpperStyle, screen.VirtualScreen[ 1, column ].Style );
		}
	}

	[Fact]
	public void RepeatedViewportProjectionDoesNotLoseBackground() {
		CursesPad pad = new( 3, 1 );
		pad.ContentWindow.FillRectangle(
			0, 0, 1, 3, CursesCell.Blank( UpperStyle )
		);
		CursesScreen screen = new( 3, 1 );
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow, 0, 0, 1, 3, 0, 0
		);
		viewport.Present();
		screen.StandardWindow.FillRectangle(
			0, 1, 1, 1, CursesCell.Blank( LowerStyle )
		);

		viewport.Present();

		for ( int column = 0; column < 3; column++ ) {
			Assert.Equal( UpperStyle, screen.VirtualScreen[ 0, column ].Style );
		}
	}

	[Fact]
	public void ViewportBoundaryRepairsWideStyleFootprint() {
		CursesPad pad = new( 4, 1 );
		pad.ContentWindow.BackgroundCell = CursesCell.Blank( UpperStyle );
		pad.ContentWindow.Write( "\u754CX", LowerStyle );
		CursesScreen screen = new( 2, 1 );
		screen.StandardWindow.Write( "\u754C", LowerStyle );
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow, 0, 1, 1, 2, 0, 0
		);

		viewport.Present();

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( UpperStyle, screen.VirtualScreen[ 0, 0 ].Style );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( LowerStyle, screen.VirtualScreen[ 0, 1 ].Style );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void OpaquePanelStyledBlankCoversLowerCell() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );

		CursesCell composed = screen.ComposePanels()[ 0, 1 ];

		Assert.True( composed.IsBlank );
		Assert.Equal( UpperStyle, composed.Style );
	}

	[Fact]
	public void OpaquePanelMoveRestoresLowerStyle() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.MoveTo( 0, 3 );
		CursesVirtualScreen moved = screen.ComposePanels();

		Assert.Same( composed, moved );
		Assert.Equal( LowerStyle, moved[ 0, 1 ].Style );
		Assert.Equal( UpperStyle, moved[ 0, 3 ].Style );
	}

	[Fact]
	public void OpaquePanelHideAndShowRestoresCorrectStyles() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 2, 1, 1 );
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );
		CursesVirtualScreen composed = screen.ComposePanels();
		panel.Hide();

		CursesVirtualScreen hidden = screen.ComposePanels();
		Assert.Equal( LowerStyle, hidden[ 0, 2 ].Style );
		panel.Show();
		CursesVirtualScreen shown = screen.ComposePanels();
		Assert.Same( composed, shown );
		Assert.Equal( UpperStyle, shown[ 0, 2 ].Style );
	}

	[Fact]
	public void PanelResizeLeavesNoStaleColoredCoordinates() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 3 );
		panel.ContentWindow.FillRectangle(
			0, 0, 1, 3, CursesCell.Blank( UpperStyle )
		);
		CursesVirtualScreen composed = screen.ComposePanels();

		panel.Resize( 1, 1 );
		CursesVirtualScreen resized = screen.ComposePanels();

		Assert.Same( composed, resized );
		Assert.Equal( UpperStyle, resized[ 0, 1 ].Style );
		Assert.Equal( LowerStyle, resized[ 0, 2 ].Style );
		Assert.Equal( LowerStyle, resized[ 0, 3 ].Style );
	}

	[Fact]
	public void BlankTransparentPanelStyledBlankRemainsTransparent() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );

		CursesCell composed = screen.ComposePanels()[ 0, 1 ];

		Assert.Equal( "B", composed.Content );
		Assert.Equal( LowerStyle, composed.Style );
	}

	[Fact]
	public void BlankTransparentPanelMetadataOnlyBlankRemainsTransparent() {
		CursesScreen screen = CreateLowerScreen();
		CursesCellMetadata lowerMetadata = new(
			new CursesHyperlink( "https://example.test/lower" )
		);
		screen.VirtualScreen.SetMetadata( 0, 1, lowerMetadata );
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );
		panel.VirtualScreen.SetMetadata(
			0,
			0,
			new CursesCellMetadata( new CursesHyperlink( "https://example.test/upper" ) )
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed[ 0, 1 ].Content );
		Assert.Equal( LowerStyle, composed[ 0, 1 ].Style );
		Assert.Equal( lowerMetadata, composed.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void BlankTransparentPanelRasterBlankRemainsOpaque() {
		CursesScreen screen = CreateLowerScreen();
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		panel.ContentWindow.SetRasterCell( 0, 0, raster );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.True( composed[ 0, 1 ].IsBlank );
		Assert.Equal( UpperStyle, composed[ 0, 1 ].Style );
		Assert.True( composed.GetRasterCell( 0, 1 ).HasValue );
	}

	[Fact]
	public void BlankTransparentPanelPreservesLowerWideFootprintAtBoundary() {
		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.Move( 0, 1 );
		screen.StandardWindow.Write( "\u754C", LowerStyle );
		CursesPanel panel = screen.CreatePanel( 0, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( UpperStyle ) );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "\u754C", composed[ 0, 1 ].Content );
		Assert.True( composed[ 0, 2 ].IsContinuation );
		Assert.Equal( LowerStyle, composed[ 0, 1 ].Style );
		Assert.Equal( LowerStyle, composed[ 0, 2 ].Style );
		CursesCellFootprint.Validate( composed );
	}

	private static CursesScreen CreateLowerScreen() {
		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.FillRectangle(
			0,
			0,
			1,
			5,
			new CursesCell( "B", LowerStyle )
		);
		return screen;
	}
}
