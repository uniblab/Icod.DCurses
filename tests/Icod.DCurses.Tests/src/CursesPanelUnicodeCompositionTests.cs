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

/// <summary>Qualifies panel composition across Unicode, wide-cell, line-glyph, style, and metadata contracts.</summary>
public sealed class CursesPanelUnicodeCompositionTests {
	private const string WideText = "\u4E00";

	[Fact]
	public void WidePanelCellComposesAsCompleteLeaderContinuationFootprint() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.Write( WideText );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( WideText, composed.GetCell( 1, 2 ).Content );
		Assert.Equal( 2, composed.GetCell( 1, 2 ).DisplayWidth );
		Assert.True( composed.GetCell( 1, 3 ).IsContinuation );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void OccludingWideContinuationRepairsLeaderAndHideRestoresFootprint() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 3 );
		lower.ContentWindow.Write( WideText );
		CursesPanel upper = screen.CreatePanel( 1, 3, 1, 1 );
		upper.ContentWindow.Write( "X" );

		CursesVirtualScreen occluded = screen.ComposePanels();

		Assert.True( occluded.GetCell( 1, 2 ).IsBlank );
		Assert.Equal( "X", occluded.GetCell( 1, 3 ).Content );
		CursesCellFootprint.Validate( occluded );
		occluded.MarkClean();
		upper.Hide();

		CursesVirtualScreen restored = screen.ComposePanels();

		Assert.Same( occluded, restored );
		Assert.Equal( WideText, restored.GetCell( 1, 2 ).Content );
		Assert.True( restored.GetCell( 1, 3 ).IsContinuation );
		CursesCellFootprint.Validate( restored );
	}

	[Fact]
	public void OccludingWideLeaderRepairsContinuationAndHideRestoresFootprint() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 3 );
		lower.ContentWindow.Write( WideText );
		CursesPanel upper = screen.CreatePanel( 1, 2, 1, 1 );
		upper.ContentWindow.Write( "X" );

		CursesVirtualScreen occluded = screen.ComposePanels();

		Assert.Equal( "X", occluded.GetCell( 1, 2 ).Content );
		Assert.True( occluded.GetCell( 1, 3 ).IsBlank );
		CursesCellFootprint.Validate( occluded );
		occluded.MarkClean();
		upper.Hide();

		CursesVirtualScreen restored = screen.ComposePanels();

		Assert.Same( occluded, restored );
		Assert.Equal( WideText, restored.GetCell( 1, 2 ).Content );
		Assert.True( restored.GetCell( 1, 3 ).IsContinuation );
		CursesCellFootprint.Validate( restored );
	}

	[Fact]
	public void DestinationResizeClipsWideFootprintWithoutStrandingContinuation() {
		CursesScreen screen = new(
			6,
			2
		);
		CursesPanel panel = screen.CreatePanel( 0, 4, 1, 2 );
		panel.ContentWindow.Write( WideText );
		CursesVirtualScreen initial = screen.ComposePanels();
		Assert.Equal( WideText, initial.GetCell( 0, 4 ).Content );
		Assert.True( initial.GetCell( 0, 5 ).IsContinuation );
		CursesCellFootprint.Validate( initial );

		screen.Resize(
			5,
			2,
			preserveContents: true
		);
		CursesVirtualScreen clipped = screen.ComposePanels();

		Assert.True( clipped.GetCell( 0, 4 ).IsBlank );
		Assert.Equal( WideText, panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.True( panel.VirtualScreen.GetCell( 0, 1 ).IsContinuation );
		CursesCellFootprint.Validate( clipped );
	}

	[Fact]
	public void SemanticLineGlyphIdentityAndStyleSurviveComposition() {
		CursesScreen screen = new(
			6,
			2
		);
		CursesStyle style = new(
			CursesColor.Indexed( 3 ),
			CursesColor.Indexed( 4 )
		);
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.ContentWindow.WriteCell(
			CursesCell.Line(
				CursesLineGlyph.Crossing,
				style
			)
		);

		CursesVirtualScreen composed = screen.ComposePanels();
		CursesCell cell = composed.GetCell( 0, 1 );

		Assert.True( cell.IsLineGlyph );
		Assert.Equal( CursesLineGlyph.Crossing, cell.LineGlyph );
		Assert.Equal( style, cell.Style );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void OpaqueStyledBlankRetainsStyleAndOccludesLowerContent() {
		CursesScreen screen = new(
			6,
			2
		);
		CursesStyle style = new(
			CursesColor.Indexed( 5 ),
			CursesColor.Indexed( 6 )
		);
		screen.StandardWindow.Move( 0, 2 );
		screen.StandardWindow.Write( "B" );
		CursesPanel panel = screen.CreatePanel( 0, 2, 1, 1 );
		panel.ContentWindow.WriteCell( CursesCell.Blank( style ) );

		CursesVirtualScreen composed = screen.ComposePanels();
		CursesCell cell = composed.GetCell( 0, 2 );

		Assert.True( cell.IsBlank );
		Assert.Equal( style, cell.Style );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void WideHyperlinkMetadataCoversCompleteComposedFootprint() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/wide" )
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.WriteWithMetadata(
			WideText,
			metadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( WideText, composed.GetCell( 1, 2 ).Content );
		Assert.True( composed.GetCell( 1, 3 ).IsContinuation );
		Assert.Equal( metadata, composed.GetMetadata( 1, 2 ) );
		Assert.Equal( metadata, composed.GetMetadata( 1, 3 ) );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void SemanticOnlyMetadataReplacementRemainsIncrementalAndCoherent() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesCellMetadata first = new(
			new CursesHyperlink( "https://example.test/first" )
		);
		CursesCellMetadata second = new(
			new CursesHyperlink( "https://example.test/second" )
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		panel.ContentWindow.WriteWithMetadata(
			"A",
			first
		);
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.VirtualScreen.SetMetadata(
			0,
			0,
			second
		);
		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "A", updated.GetCell( 1, 2 ).Content );
		Assert.Equal( second, updated.GetMetadata( 1, 2 ) );
		Assert.Equal( 1, updated.DirtyCellCount );
		CursesCellFootprint.Validate( updated );
	}

	[Fact]
	public void TransparentBlankWithSourceMetadataContributesNeitherCellNorMetadata() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesCellMetadata baseMetadata = new(
			new CursesHyperlink( "https://example.test/base" )
		);
		CursesCellMetadata panelMetadata = new(
			new CursesHyperlink( "https://example.test/panel" )
		);
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.WriteWithMetadata(
			"B",
			baseMetadata
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.VirtualScreen.SetMetadata(
			0,
			0,
			panelMetadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed.GetCell( 1, 2 ).Content );
		Assert.Equal( baseMetadata, composed.GetMetadata( 1, 2 ) );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void WidePanelMetadataRemainsCoherentAfterPartialOcclusionIsRemovedIncrementally() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/reveal" )
		);
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 3 );
		lower.ContentWindow.WriteWithMetadata(
			WideText,
			metadata
		);
		CursesPanel upper = screen.CreatePanel( 1, 3, 1, 1 );
		upper.ContentWindow.Write( "X" );
		CursesVirtualScreen occluded = screen.ComposePanels();
		occluded.MarkClean();

		upper.Hide();
		CursesVirtualScreen restored = screen.ComposePanels();

		Assert.Same( occluded, restored );
		Assert.Equal( WideText, restored.GetCell( 1, 2 ).Content );
		Assert.True( restored.GetCell( 1, 3 ).IsContinuation );
		Assert.Equal( metadata, restored.GetMetadata( 1, 2 ) );
		Assert.Equal( metadata, restored.GetMetadata( 1, 3 ) );
		CursesCellFootprint.Validate( restored );
	}
}