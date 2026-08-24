using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesScreenTests {
	[Fact]
	public void StandardWindowAlwaysTracksScreenDimensions() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow standard = screen.StandardWindow;

		standard.Move( 1, 3 );
		screen.Resize( 2, 1 );

		Assert.Equal( 2, standard.Columns );
		Assert.Equal( 1, standard.Rows );
		Assert.Equal( 0, standard.CursorRow );
		Assert.Equal( 1, standard.CursorColumn );
		Assert.True( standard.IsStandardWindow );
		Assert.Equal( 0, standard.OriginRow );
		Assert.Equal( 0, standard.OriginColumn );
	}

	[Fact]
	public void ResizePreservesOverlappingCellsAndRaisesHook() {
		CursesScreen screen = new( 3, 2 );
		screen.VirtualScreen[ 1, 2 ] = new CursesCell( "x" );
		CursesScreenResizedEventArgs? observed = null;
		screen.Resized += ( _, args ) => observed = args;

		screen.Resize( 5, 4 );

		Assert.Equal( "x", screen.VirtualScreen[ 1, 2 ].Content );
		CursesScreenResizedEventArgs resized = Assert.IsType<CursesScreenResizedEventArgs>( observed );
		Assert.Equal( 3, resized.OldColumns );
		Assert.Equal( 2, resized.OldRows );
		Assert.Equal( 5, resized.Columns );
		Assert.Equal( 4, resized.Rows );
	}

	[Fact]
	public void ResizeCanDiscardPreviousLogicalContents() {
		CursesScreen screen = new( 2, 2 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "x" );

		screen.Resize(
			2,
			2,
			preserveContents: false
		);

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
	}

	[Fact]
	public void ExistingViewClipsSafelyAfterOwningScreenShrinks() {
		CursesScreen screen = new( 6, 3 );
		CursesWindow window = screen.CreateWindow( 1, 3, 2, 3 );
		window.Write( "abc" );

		screen.Resize( 4, 2 );
		window.Move( 0, 0 );
		window.Write( "XYZ" );

		Assert.Equal( "X", screen.VirtualScreen[ 1, 3 ].Content );
		Assert.Equal( 4, screen.Columns );
		Assert.Equal( 2, screen.Rows );
	}

	[Fact]
	public void NonStandardWindowCanResizeWithinItsContainer() {
		CursesScreen screen = new( 8, 4 );
		CursesWindow window = screen.CreateWindow( 1, 2, 2, 3 );

		window.Move( 1, 2 );
		window.Resize( 3, 5 );

		Assert.Equal( 3, window.Rows );
		Assert.Equal( 5, window.Columns );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		Assert.Throws<InvalidOperationException>(
			() => screen.StandardWindow.Resize( 1, 1 )
		);
	}
}
