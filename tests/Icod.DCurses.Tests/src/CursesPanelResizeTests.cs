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

/// <summary>Qualifies retained panel resizing and incremental recomposition.</summary>
public sealed class CursesPanelResizeTests {
	private const string WideText = "\u4E00";

	[Fact]
	public void GrowPreservesContentMetadataAndContentWindowIdentity() {
		CursesScreen screen = new( 10, 6 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 2, 2 );
		CursesWindow content = panel.ContentWindow;
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1305-grow" )
		);
		content.WriteWithMetadata(
			"A",
			metadata
		);
		content.Move( 1, 1 );
		content.Write( "B" );

		panel.Resize(
			3,
			4
		);

		Assert.Equal( 3, panel.Rows );
		Assert.Equal( 4, panel.Columns );
		Assert.Same( content, panel.ContentWindow );
		Assert.Equal( "A", content.GetCell( 0, 0 ).Content );
		Assert.Equal( metadata, content.GetMetadata( 0, 0 ) );
		Assert.Equal( "B", content.GetCell( 1, 1 ).Content );
		Assert.True( content.GetCell( 2, 3 ).IsBlank );
		CursesCellFootprint.Validate( panel.VirtualScreen );
	}

	[Fact]
	public void ShrinkPreservesUpperLeftContentAndClampsCursor() {
		CursesScreen screen = new( 10, 6 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 3, 4 );
		CursesWindow content = panel.ContentWindow;
		content.Write( "AB" );
		content.Move( 2, 3 );

		panel.Resize(
			2,
			2
		);

		Assert.Equal( 2, panel.Rows );
		Assert.Equal( 2, panel.Columns );
		Assert.Equal( "A", content.GetCell( 0, 0 ).Content );
		Assert.Equal( "B", content.GetCell( 0, 1 ).Content );
		Assert.Equal( 1, content.CursorRow );
		Assert.Equal( 1, content.CursorColumn );
		CursesCellFootprint.Validate( panel.VirtualScreen );
	}

	[Fact]
	public void ShrinkRepairsWideFootprintClippedByNewBoundary() {
		CursesScreen screen = new( 8, 3 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1305-wide" )
		);
		panel.ContentWindow.Move( 0, 1 );
		panel.ContentWindow.WriteWithMetadata(
			WideText,
			metadata
		);
		Assert.Equal( WideText, panel.VirtualScreen.GetCell( 0, 1 ).Content );
		Assert.True( panel.VirtualScreen.GetCell( 0, 2 ).IsContinuation );

		panel.Resize(
			1,
			2
		);

		Assert.True( panel.VirtualScreen.GetCell( 0, 1 ).IsBlank );
		Assert.Null( panel.VirtualScreen.GetMetadata( 0, 1 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
	}

	[Fact]
	public void ResizePreservesVisibilityTransparencyAndRememberedZOrder() {
		CursesScreen screen = new( 12, 6 );
		CursesPanel first = screen.CreatePanel( 0, 0, 1, 1 );
		CursesPanel target = screen.CreatePanel( 1, 2, 2, 2 );
		CursesPanel last = screen.CreatePanel( 3, 6, 1, 1 );
		target.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		target.Hide();
		CursesPanel[] before = screen.SnapshotPanelsBottomToTop();

		target.Resize(
			3,
			3
		);

		CursesPanel[] after = screen.SnapshotPanelsBottomToTop();
		Assert.Equal( before.Length, after.Length );
		for ( int index = 0; index < before.Length; index++ ) {
			Assert.Same( before[ index ], after[ index ] );
		}
		Assert.Same( first, after[ 0 ] );
		Assert.Same( target, after[ 1 ] );
		Assert.Same( last, after[ 2 ] );
		Assert.False( target.IsVisible );
		Assert.Equal(
			CursesPanelTransparency.BlankCellsTransparent,
			target.Transparency
		);
	}

	[Fact]
	public void ResizeRejectsInvalidDimensionsAndContainmentBeforeMutation() {
		CursesScreen screen = new( 8, 6 );
		CursesPanel panel = screen.CreatePanel( 3, 4, 2, 3 );
		CursesWindow content = panel.ContentWindow;
		content.Write( "ABC" );
		content.Move( 1, 2 );

		ArgumentOutOfRangeException rows = Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.Resize(
				0,
				3
			)
		);
		ArgumentOutOfRangeException columns = Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.Resize(
				2,
				0
			)
		);
		ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.Resize(
				4,
				3
			)
		);
		ArgumentOutOfRangeException beyond = Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.Resize(
				2,
				5
			)
		);

		Assert.Equal( "rows", rows.ParamName );
		Assert.Equal( "columns", columns.ParamName );
		Assert.Equal( "rows", below.ParamName );
		Assert.Equal( "columns", beyond.ParamName );
		Assert.Equal( 2, panel.Rows );
		Assert.Equal( 3, panel.Columns );
		Assert.Equal( "ABC", ReadText( content, 0, 0, 3 ) );
		Assert.Equal( 1, content.CursorRow );
		Assert.Equal( 2, content.CursorColumn );
	}

	[Fact]
	public void ResizeRejectsDisposedPanel() {
		CursesScreen screen = new( 8, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		panel.Dispose();

		Assert.Throws<ObjectDisposedException>(
			() => panel.Resize(
				1,
				1
			)
		);
	}

	[Fact]
	public void VisibleShrinkIncrementallyRemovesStaleComposedCells() {
		CursesScreen screen = new( 8, 3 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.Write( "ABC" );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.Equal( "ABC", ReadText( composed, 1, 2, 3 ) );
		composed.MarkClean();

		panel.Resize(
			1,
			1
		);
		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "A", updated.GetCell( 1, 2 ).Content );
		Assert.True( updated.GetCell( 1, 3 ).IsBlank );
		Assert.True( updated.GetCell( 1, 4 ).IsBlank );
		Assert.Equal( 2, updated.DirtyCellCount );
		Assert.True( updated.IsDirty( 1, 3 ) );
		Assert.True( updated.IsDirty( 1, 4 ) );
		CursesCellFootprint.Validate( updated );
	}

	[Fact]
	public void VisibleGrowIncrementallyCoversNewAreaWithOpaqueBlankCells() {
		CursesScreen screen = new( 8, 3 );
		screen.StandardWindow.Move( 1, 3 );
		screen.StandardWindow.Write( "B" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.ContentWindow.Write( "P" );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.Equal( "P", composed.GetCell( 1, 2 ).Content );
		Assert.Equal( "B", composed.GetCell( 1, 3 ).Content );
		composed.MarkClean();

		panel.Resize(
			1,
			2
		);
		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( "P", updated.GetCell( 1, 2 ).Content );
		Assert.True( updated.GetCell( 1, 3 ).IsBlank );
		Assert.Equal( 1, updated.DirtyCellCount );
		Assert.True( updated.IsDirty( 1, 3 ) );
		CursesCellFootprint.Validate( updated );
	}

	[Fact]
	public void HiddenResizeDoesNotDirtyCompositionUntilPanelIsShown() {
		CursesScreen screen = new( 8, 3 );
		screen.StandardWindow.Move( 1, 3 );
		screen.StandardWindow.Write( "B" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.ContentWindow.Write( "P" );
		panel.Hide();
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.Resize(
			1,
			2
		);
		CursesVirtualScreen hidden = screen.ComposePanels();

		Assert.Same( composed, hidden );
		Assert.Equal( 0, hidden.DirtyCellCount );
		Assert.Equal( "B", hidden.GetCell( 1, 3 ).Content );

		panel.Show();
		CursesVirtualScreen shown = screen.ComposePanels();

		Assert.Same( composed, shown );
		Assert.Equal( "P", shown.GetCell( 1, 2 ).Content );
		Assert.True( shown.GetCell( 1, 3 ).IsBlank );
		Assert.Equal( 2, shown.DirtyCellCount );
		CursesCellFootprint.Validate( shown );
	}

	[Fact]
	public void SameSizeResizeIsCompositionNoOp() {
		CursesScreen screen = new( 8, 3 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 2 );
		panel.ContentWindow.Write( "AB" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.Resize(
			1,
			2
		);
		CursesVirtualScreen repeated = screen.ComposePanels();

		Assert.Same( composed, repeated );
		Assert.Equal( 0, repeated.DirtyCellCount );
	}

	private static string ReadText(
		CursesWindow window,
		int row,
		int column,
		int length
	) {
		ArgumentNullException.ThrowIfNull( window );
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
				current => window.GetCell( row, current ).Content
			)
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
