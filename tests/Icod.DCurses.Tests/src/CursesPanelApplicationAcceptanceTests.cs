using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises representative panel applications and steady-state resource behavior.</summary>
public sealed class CursesPanelApplicationAcceptanceTests {
	[Fact]
	public void ModalHelpPanelOccludesAndRestoresBaseApplication() {
		CursesScreen screen = new( 24, 6 );
		screen.StandardWindow.Write( "application" );
		CursesPanel help = screen.CreatePanel( 1, 4, 3, 12 );
		help.ContentWindow.Write( "HELP" );

		CursesVirtualScreen modal = screen.ComposePanels();

		Assert.Equal( "HELP", ReadText( modal, 1, 4, 4 ) );
		help.Hide();
		CursesVirtualScreen restored = screen.ComposePanels();
		Assert.Same( modal, restored );
		Assert.Equal( "application", ReadText( restored, 0, 0, 11 ) );
	}

	[Fact]
	public void CommandPaletteCanMoveWithoutRebuildingItsRetainedContent() {
		CursesScreen screen = new( 30, 8 );
		CursesPanel palette = screen.CreatePanel( 1, 3, 2, 14 );
		palette.ContentWindow.Write( "> open file" );

		CursesVirtualScreen initial = screen.ComposePanels();
		Assert.Equal( "> open file", ReadText( initial, 1, 3, 11 ) );
		initial.MarkClean();

		palette.MoveTo( 4, 8 );
		CursesVirtualScreen moved = screen.ComposePanels();

		Assert.Same( initial, moved );
		Assert.Equal( "> open file", ReadText( moved, 4, 8, 11 ) );
		Assert.Equal( "> open file", ReadText( palette.VirtualScreen, 0, 0, 11 ) );
	}

	[Fact]
	public void CompletionPopupTransparentBlanksRevealEditorText() {
		CursesScreen screen = new( 20, 4 );
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "abcdef" );
		CursesPanel completion = screen.CreatePanel( 1, 2, 1, 6 );
		completion.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		completion.ContentWindow.Write( "XY" );

		CursesVirtualScreen composed = screen.ComposePanels();

		Assert.Equal( "XYcdef", ReadText( composed, 1, 2, 6 ) );
	}

	[Fact]
	public void ContextMenuCanReorderAboveAnotherTransientPanel() {
		CursesScreen screen = new( 18, 5 );
		CursesPanel status = screen.CreatePanel( 2, 4, 1, 6 );
		status.ContentWindow.Write( "STATUS" );
		CursesPanel menu = screen.CreatePanel( 2, 4, 1, 6 );
		menu.ContentWindow.Write( "MENU  " );

		CursesVirtualScreen first = screen.ComposePanels();
		Assert.Equal( "MENU  ", ReadText( first, 2, 4, 6 ) );

		status.MoveToTop();
		CursesVirtualScreen reordered = screen.ComposePanels();

		Assert.Same( first, reordered );
		Assert.Equal( "STATUS", ReadText( reordered, 2, 4, 6 ) );
	}

	[Fact]
	public void HiddenTransientStatusRetainsUpdatesUntilShown() {
		CursesScreen screen = new( 22, 4 );
		CursesPanel status = screen.CreatePanel( 3, 0, 1, 12 );
		status.ContentWindow.Write( "ready" );
		_ = screen.ComposePanels();
		status.Hide();
		CursesVirtualScreen hidden = screen.ComposePanels();
		hidden.MarkClean();
		status.ContentWindow.Move( 0, 0 );
		status.ContentWindow.Write( "complete" );

		CursesVirtualScreen stillHidden = screen.ComposePanels();
		Assert.Same( hidden, stillHidden );
		Assert.Equal( 0, stillHidden.DirtyCellCount );

		status.Show();
		CursesVirtualScreen shown = screen.ComposePanels();
		Assert.Equal( "complete", ReadText( shown, 3, 0, 8 ) );
	}

	[Fact]
	public void SparseVisibleEditKeepsDamageBounded() {
		CursesScreen screen = new( 160, 48 );
		CursesPanel panel = screen.CreatePanel( 10, 20, 4, 30 );
		panel.ContentWindow.Write( "A" );
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.ContentWindow.Move( 2, 12 );
		panel.ContentWindow.Write( "B" );
		CursesVirtualScreen changed = screen.ComposePanels();

		Assert.Same( composed, changed );
		Assert.InRange( changed.DirtyCellCount, 1, 3 );
		Assert.True( changed.IsDirty( 12, 32 ) );
	}

	[Fact]
	public void NoPanelPresenceCheckIsAllocationFree() {
		CursesScreen screen = new( 80, 24 );
		Assert.False( screen.HasPanels );
		_ = screen.HasPanels;

		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int index = 0; index < 10000; index++ ) {
			if ( screen.HasPanels ) {
				throw new InvalidOperationException( "An empty screen unexpectedly reported panels." );
			}
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.Equal( 0, allocated );
	}

	[Fact]
	public void SteadyStateSparseCompositionReusesFrameWithinAllocationCeiling() {
		CursesScreen screen = new( 120, 40 );
		CursesPanel panel = screen.CreatePanel( 5, 9, 3, 20 );
		panel.ContentWindow.Write( "steady" );
		CursesVirtualScreen retained = screen.ComposePanels();
		retained.MarkClean();
		for ( int index = 0; index < 32; index++ ) {
			_ = screen.ComposePanels();
		}

		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int index = 0; index < 1024; index++ ) {
			CursesVirtualScreen current = screen.ComposePanels();
			if ( !ReferenceEquals( retained, current ) ) {
				throw new InvalidOperationException( "Steady-state composition replaced the retained frame." );
			}
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.InRange( allocated, 0, 131072 );
		Assert.Equal( 0, retained.DirtyCellCount );
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

		System.Text.StringBuilder result = new();
		for ( int index = 0; index < length; index++ ) {
			result.Append( screen.GetCell( row, column + index ).Content );
		}
		return result.ToString();
	}
}
