using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies damage-bounded retained panel recomposition.</summary>
public sealed class CursesPanelIncrementalCompositionTests {
	[Fact]
	public void RepeatedCompositionWithoutChangesReusesCleanFrame() {
		CursesScreen screen = new(
			12,
			6
		);
		CursesPanel panel = screen.CreatePanel( 2, 3, 2, 4 );
		panel.ContentWindow.Write( "TEST" );
		CursesVirtualScreen initial = screen.ComposePanels();
		initial.MarkClean();

		CursesVirtualScreen repeated = screen.ComposePanels();

		Assert.Same( initial, repeated );
		Assert.Equal( 0, repeated.DirtyCellCount );
	}

	[Fact]
	public void VisiblePanelCellChangeDirtiesOnlyChangedDestinationCell() {
		CursesScreen screen = new(
			12,
			6
		);
		CursesPanel panel = screen.CreatePanel( 2, 3, 2, 4 );
		panel.ContentWindow.Write( "ABCD" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		panel.ContentWindow.Move( 0, 1 );
		panel.ContentWindow.Write( "Z" );

		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "Z", updated.GetCell( 2, 4 ).Content );
		Assert.Equal( 1, updated.DirtyCellCount );
		Assert.True( updated.IsDirty( 2, 4 ) );
	}

	[Fact]
	public void HiddenPanelContentChangeDoesNotDirtyUntilPanelIsShown() {
		CursesScreen screen = new(
			10,
			4
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		panel.ContentWindow.Write( "AB" );
		panel.Hide();
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		panel.ContentWindow.Move( 0, 0 );
		panel.ContentWindow.Write( "XY" );

		CursesVirtualScreen hiddenUpdate = screen.ComposePanels();

		Assert.Same( composed, hiddenUpdate );
		Assert.Equal( 0, hiddenUpdate.DirtyCellCount );
		panel.Show();

		CursesVirtualScreen shown = screen.ComposePanels();

		Assert.Same( composed, shown );
		Assert.Equal( "X", shown.GetCell( 1, 2 ).Content );
		Assert.Equal( "Y", shown.GetCell( 1, 3 ).Content );
		Assert.Equal( 2, shown.DirtyCellCount );
	}

	[Fact]
	public void FullyOccludedPanelChangeDoesNotDirtyComposedFrame() {
		CursesScreen screen = new(
			10,
			4
		);
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 3 );
		CursesPanel upper = screen.CreatePanel( 1, 2, 1, 3 );
		lower.ContentWindow.Write( "LOW" );
		upper.ContentWindow.Write( "TOP" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		lower.ContentWindow.Move( 0, 1 );
		lower.ContentWindow.Write( "X" );

		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "TOP", ReadText( updated, 1, 2, 3 ) );
		Assert.Equal( 0, updated.DirtyCellCount );
	}

	[Fact]
	public void BaseChangeUnderOpaquePanelDoesNotDirtyComposedFrame() {
		CursesScreen screen = new(
			10,
			4
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.ContentWindow.Write( "P" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "B" );

		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "P", updated.GetCell( 1, 2 ).Content );
		Assert.Equal( 0, updated.DirtyCellCount );
	}

	[Fact]
	public void MovingPanelDirtiesOnlyCoordinatesWhoseComposedValuesChange() {
		CursesScreen screen = new(
			12,
			5
		);
		CursesPanel panel = screen.CreatePanel( 1, 1, 1, 1 );
		panel.ContentWindow.Write( "P" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		panel.MoveTo( 3, 8 );

		CursesVirtualScreen moved = screen.ComposePanels();

		Assert.Same( composed, moved );
		Assert.True( moved.GetCell( 1, 1 ).IsBlank );
		Assert.Equal( "P", moved.GetCell( 3, 8 ).Content );
		Assert.Equal( 2, moved.DirtyCellCount );
		Assert.True( moved.IsDirty( 1, 1 ) );
		Assert.True( moved.IsDirty( 3, 8 ) );
	}

	[Fact]
	public void TransparencyChangeRevealsLowerCellWithBoundedDamage() {
		CursesScreen screen = new(
			8,
			3
		);
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "B" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.True( composed.GetCell( 1, 2 ).IsBlank );
		composed.MarkClean();
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;

		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "B", updated.GetCell( 1, 2 ).Content );
		Assert.Equal( 1, updated.DirtyCellCount );
	}

	[Fact]
	public void SemanticOnlyPanelChangeDirtiesOnlyAnnotatedCoordinate() {
		CursesScreen screen = new(
			8,
			3
		);
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		panel.ContentWindow.Write( "AB" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1205" )
		);
		panel.VirtualScreen.SetMetadata(
			0,
			1,
			metadata
		);

		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( metadata, updated.GetMetadata( 1, 3 ) );
		Assert.Equal( 1, updated.DirtyCellCount );
		Assert.True( updated.IsDirty( 1, 3 ) );
	}

	[Fact]
	public void ZOrderChangeDirtiesOnlyChangedOverlap() {
		CursesScreen screen = new(
			10,
			4
		);
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 1 );
		CursesPanel upper = screen.CreatePanel( 1, 2, 1, 1 );
		lower.ContentWindow.Write( "L" );
		upper.ContentWindow.Write( "U" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();
		lower.MoveToTop();

		CursesVirtualScreen reordered = screen.ComposePanels();

		Assert.Same( composed, reordered );
		Assert.Equal( "L", reordered.GetCell( 1, 2 ).Content );
		Assert.Equal( 1, reordered.DirtyCellCount );
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
