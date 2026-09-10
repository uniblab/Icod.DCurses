using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies retained logical panel composition independently of terminal rendering.</summary>
public sealed class CursesPanelCompositionTests {
	[Fact]
	public void CompositionWithoutPanelsCopiesBaseCellAndMetadataState() {
		CursesScreen screen = new(
			8,
			4
		);
		CursesCellMetadata metadata = CreateMetadata( "https://example.test/base" );
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.WriteWithMetadata(
			"B",
			metadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.NotSame( screen.VirtualScreen, composed );
		Assert.Equal( "B", composed.GetCell( 1, 2 ).Content );
		Assert.Equal( metadata, composed.GetMetadata( 1, 2 ) );
		Assert.Equal( "B", screen.VirtualScreen.GetCell( 1, 2 ).Content );
		Assert.Equal( metadata, screen.VirtualScreen.GetMetadata( 1, 2 ) );
	}

	[Fact]
	public void OpaquePanelContentAndBlankCellsCoverBaseState() {
		CursesScreen screen = new(
			8,
			4
		);
		CursesCellMetadata baseMetadata = CreateMetadata( "https://example.test/base" );
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.WriteWithMetadata(
			"BASE",
			baseMetadata
		);
		CursesPanel panel = screen.CreatePanel(
			1,
			2,
			2,
			3
		);
		panel.ContentWindow.Move( 0, 0 );
		panel.ContentWindow.Write( "P" );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed.GetCell( 1, 1 ).Content );
		Assert.Equal( "P", composed.GetCell( 1, 2 ).Content );
		Assert.Null( composed.GetMetadata( 1, 2 ) );
		Assert.True( composed.GetCell( 1, 3 ).IsBlank );
		Assert.Null( composed.GetMetadata( 1, 3 ) );
		Assert.True( composed.GetCell( 1, 4 ).IsBlank );
		Assert.Null( composed.GetMetadata( 1, 4 ) );
	}

	[Fact]
	public void VisiblePanelsComposeBottomToTopAndHonorReordering() {
		CursesScreen screen = new(
			8,
			4
		);
		CursesPanel lower = screen.CreatePanel( 1, 1, 1, 3 );
		CursesPanel upper = screen.CreatePanel( 1, 1, 1, 3 );
		lower.ContentWindow.Write( "LOW" );
		upper.ContentWindow.Write( "TOP" );

		CursesVirtualScreen initiallyComposed = screen.ComposePanels();
		Assert.Equal( "TOP", ReadText( initiallyComposed, 1, 1, 3 ) );

		lower.MoveToTop();
		CursesVirtualScreen reordered = screen.ComposePanels();
		Assert.Equal( "LOW", ReadText( reordered, 1, 1, 3 ) );
	}

	[Fact]
	public void HiddenPanelsDoNotContributeButRetainTheirContent() {
		CursesScreen screen = new(
			8,
			4
		);
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.Write( "BASE" );
		CursesPanel panel = screen.CreatePanel( 1, 1, 1, 4 );
		panel.ContentWindow.Write( "OVER" );
		panel.Hide();

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "BASE", ReadText( composed, 1, 1, 4 ) );
		Assert.Equal( "OVER", ReadText( panel.VirtualScreen, 0, 0, 4 ) );
	}

	[Fact]
	public void MovingPanelChangesCompositionPlacementWithoutChangingPanelContent() {
		CursesScreen screen = new(
			10,
			5
		);
		CursesPanel panel = screen.CreatePanel( 0, 0, 1, 2 );
		panel.ContentWindow.Write( "XY" );
		panel.MoveTo(
			3,
			6
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.True( composed.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "XY", ReadText( composed, 3, 6, 2 ) );
		Assert.Equal( "XY", ReadText( panel.VirtualScreen, 0, 0, 2 ) );
	}

	[Fact]
	public void CompositionClipsPanelsAfterDestinationScreenShrinks() {
		CursesScreen screen = new(
			8,
			4
		);
		CursesPanel panel = screen.CreatePanel( 2, 5, 2, 3 );
		panel.ContentWindow.Move( 0, 0 );
		panel.ContentWindow.Write( "ABC" );
		panel.ContentWindow.Move( 1, 0 );
		panel.ContentWindow.Write( "DEF" );

		screen.Resize(
			6,
			3,
			preserveContents: true
		);
		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( 6, composed.Columns );
		Assert.Equal( 3, composed.Rows );
		Assert.Equal( "A", composed.GetCell( 2, 5 ).Content );
		Assert.Equal( "A", panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.Equal( "D", panel.VirtualScreen.GetCell( 1, 0 ).Content );
	}

	[Fact]
	public void TransparentBlankCellsContributeNeitherCellNorMetadata() {
		CursesScreen screen = new(
			8,
			4
		);
		CursesCellMetadata baseMetadata = CreateMetadata( "https://example.test/base" );
		CursesCellMetadata panelMetadata = CreateMetadata( "https://example.test/panel" );
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.WriteWithMetadata(
			"BASE",
			baseMetadata
		);
		CursesPanel panel = screen.CreatePanel( 1, 1, 1, 4 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.Move( 0, 1 );
		panel.ContentWindow.WriteWithMetadata(
			"P",
			panelMetadata
		);

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed.GetCell( 1, 1 ).Content );
		Assert.Equal( baseMetadata, composed.GetMetadata( 1, 1 ) );
		Assert.Equal( "P", composed.GetCell( 1, 2 ).Content );
		Assert.Equal( panelMetadata, composed.GetMetadata( 1, 2 ) );
		Assert.Equal( "S", composed.GetCell( 1, 3 ).Content );
		Assert.Equal( baseMetadata, composed.GetMetadata( 1, 3 ) );
		Assert.Equal( "E", composed.GetCell( 1, 4 ).Content );
		Assert.Equal( baseMetadata, composed.GetMetadata( 1, 4 ) );
	}

	[Fact]
	public void StyledBlankCellsAreTransparentInBlankCellTransparencyMode() {
		CursesScreen screen = new(
			5,
			2
		);
		CursesStyle baseStyle = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default
		);
		CursesStyle panelStyle = new(
			CursesColor.Indexed( 4 ),
			CursesColor.Default
		);
		screen.StandardWindow.Move( 0, 1 );
		screen.StandardWindow.Write(
			"B",
			baseStyle
		);
		CursesPanel panel = screen.CreatePanel( 0, 1, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.WriteCell( CursesCell.Blank( panelStyle ) );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "B", composed.GetCell( 0, 1 ).Content );
		Assert.Equal( baseStyle, composed.GetCell( 0, 1 ).Style );
		Assert.True( panel.VirtualScreen.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( panelStyle, panel.VirtualScreen.GetCell( 0, 0 ).Style );
	}

	[Fact]
	public void TransparencyRejectsUndefinedEnumValues() {
		CursesScreen screen = new(
			5,
			2
		);
		CursesPanel panel = screen.CreatePanel( 0, 0, 1, 1 );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.Transparency = (CursesPanelTransparency)int.MaxValue
		);
		Assert.Equal( CursesPanelTransparency.Opaque, panel.Transparency );
	}

	private static CursesCellMetadata CreateMetadata( string uri ) {
		ArgumentException.ThrowIfNullOrEmpty( uri );
		return new CursesCellMetadata(
			new CursesHyperlink( uri )
		);
	}

	private static string ReadText(
		CursesVirtualScreen screen,
		int row,
		int column,
		int length
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > length ) {
			throw new ArgumentOutOfRangeException( nameof( length ) );
		}

		return string.Concat(
			Enumerable.Range(
				column,
				length
			).Select(
				current => screen.GetCell( row, current ).Content
			)
		);
	}
}
