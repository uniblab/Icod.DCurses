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

		CursesVirtualScreen revealed = screen.ComposePanels();

		Assert.Same( occluded, revealed );
		Assert.Equal( WideText, revealed.GetCell( 1, 2 ).Content );
		Assert.Equal( 2, revealed.GetCell( 1, 2 ).DisplayWidth );
		Assert.True( revealed.GetCell( 1, 3 ).IsContinuation );
		Assert.True( revealed.IsDirty( 1, 2 ) );
		Assert.True( revealed.IsDirty( 1, 3 ) );
		CursesCellFootprint.Validate( revealed );
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

		CursesVirtualScreen revealed = screen.ComposePanels();

		Assert.Same( occluded, revealed );
		Assert.Equal( WideText, revealed.GetCell( 1, 2 ).Content );
		Assert.Equal( 2, revealed.GetCell( 1, 2 ).DisplayWidth );
		Assert.True( revealed.GetCell( 1, 3 ).IsContinuation );
		Assert.True( revealed.IsDirty( 1, 2 ) );
		Assert.True( revealed.IsDirty( 1, 3 ) );
		CursesCellFootprint.Validate( revealed );
	}

	[Fact]
	public void DestinationResizeClippingWideContinuationNeverExposesOrphanedLeader() {
		CursesScreen screen = new(
			6,
			2
		);
		CursesPanel panel = screen.CreatePanel( 0, 4, 1, 2 );
		panel.ContentWindow.Write( WideText );
		CursesVirtualScreen beforeResize = screen.ComposePanels();
		Assert.Equal( WideText, beforeResize.GetCell( 0, 4 ).Content );
		Assert.True( beforeResize.GetCell( 0, 5 ).IsContinuation );
		CursesCellFootprint.Validate( beforeResize );

		screen.Resize(
			5,
			2
		);
		CursesVirtualScreen clipped = screen.ComposePanels();

		Assert.True( clipped.GetCell( 0, 4 ).IsBlank );
		Assert.Equal( WideText, panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.True( panel.VirtualScreen.GetCell( 0, 1 ).IsContinuation );
		CursesCellFootprint.Validate( clipped );
	}

	[Fact]
	public void SemanticLineGlyphComposesWithoutLosingIdentityOrStyle() {
		CursesScreen screen = new(
			6,
			3
		);
		CursesStyle style = new(
			CursesColor.Indexed( 3 ),
			CursesColor.Default
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.ContentWindow.WriteCell(
			CursesCell.Line(
				CursesLineGlyph.Crossing,
				style
			)
		);

		CursesVirtualScreen composed = screen.ComposePanels();
		CursesCell cell = composed.GetCell( 1, 2 );

		Assert.True( cell.IsLineGlyph );
		Assert.Equal( CursesLineGlyph.Crossing, cell.LineGlyph );
		Assert.Equal( style, cell.Style );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void OpaqueStyledBlankComposesAsStyledContent() {
		CursesScreen screen = new(
			6,
			3
		);
		CursesStyle baseStyle = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default
		);
		CursesStyle panelStyle = new(
			CursesColor.Indexed( 5 ),
			CursesColor.Default
		);
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write(
			"B",
			baseStyle
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.ContentWindow.WriteCell( CursesCell.Blank( panelStyle ) );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.True( composed.GetCell( 1, 2 ).IsBlank );
		Assert.Equal( panelStyle, composed.GetCell( 1, 2 ).Style );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void WideHyperlinkMetadataRemainsPairedAcrossLeaderAndContinuation() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.Write( WideText );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1206/wide" )
		);
		panel.VirtualScreen.SetMetadata(
			0,
			0,
			metadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( metadata, composed.GetMetadata( 1, 2 ) );
		Assert.Equal( metadata, composed.GetMetadata( 1, 3 ) );
		CursesCellFootprint.Validate( composed );
	}

	[Fact]
	public void SemanticOnlyWideMetadataReplacementUpdatesEntireFootprintIncrementally() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.Write( WideText );
		CursesCellMetadata first = new(
			new CursesHyperlink( "https://example.test/t1206/first" )
		);
		CursesCellMetadata second = new(
			new CursesHyperlink( "https://example.test/t1206/second" )
		);
		panel.VirtualScreen.SetMetadata(
			0,
			0,
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
		Assert.Equal( WideText, updated.GetCell( 1, 2 ).Content );
		Assert.True( updated.GetCell( 1, 3 ).IsContinuation );
		Assert.Equal( second, updated.GetMetadata( 1, 2 ) );
		Assert.Equal( second, updated.GetMetadata( 1, 3 ) );
		Assert.True( updated.IsDirty( 1, 2 ) );
		Assert.True( updated.IsDirty( 1, 3 ) );
		CursesCellFootprint.Validate( updated );
	}

	[Fact]
	public void TransparentBlankContributesNeitherStyleNorSourceMetadata() {
		CursesScreen screen = new(
			6,
			3
		);
		CursesStyle baseStyle = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default
		);
		CursesStyle transparentStyle = new(
			CursesColor.Indexed( 5 ),
			CursesColor.Default
		);
		CursesCellMetadata baseMetadata = new(
			new CursesHyperlink( "https://example.test/t1206/base" )
		);
		CursesCellMetadata transparentMetadata = new(
			new CursesHyperlink( "https://example.test/t1206/transparent" )
		);
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write(
			"B",
			baseStyle,
			baseMetadata
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( transparentStyle ) );
		panel.VirtualScreen.SetMetadata(
			0,
			0,
			transparentMetadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed.GetCell( 1, 2 ).Content );
		Assert.Equal( baseStyle, composed.GetCell( 1, 2 ).Style );
		Assert.Equal( baseMetadata, composed.GetMetadata( 1, 2 ) );
		CursesCellFootprint.Validate( composed );
	}
}
