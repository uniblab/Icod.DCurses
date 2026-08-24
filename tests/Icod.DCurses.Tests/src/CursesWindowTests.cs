using System.Text;
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesWindowTests {
	[Fact]
	public void MultipleRegionsComposeIntoOneSharedVirtualFrame() {
		CursesScreen screen = new( 12, 3 );
		CursesWindow heading = screen.CreateWindow( 0, 0, 1, 12 );
		CursesWindow body = screen.CreateWindow( 1, 0, 2, 12 );
		CursesStyle headingStyle = new(
			CursesColor.Indexed( 14 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);

		heading.CurrentStyle = headingStyle;
		heading.Write( "TOP" );
		body.Write( "pid  command" );

		Assert.Equal( "T", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( headingStyle, screen.VirtualScreen[ 0, 1 ].Style );
		Assert.Equal( "p", screen.VirtualScreen[ 1, 0 ].Content );
		Assert.Equal( "d", screen.VirtualScreen[ 1, 2 ].Content );
	}

	[Fact]
	public void SubwindowWritesShareParentAndScreenCells() {
		CursesScreen screen = new( 10, 4 );
		CursesWindow parent = screen.CreateWindow( 1, 2, 2, 6 );
		CursesWindow child = parent.CreateSubwindow( 0, 1, 1, 3 );

		child.Write( "abc" );

		Assert.Equal( "a", screen.VirtualScreen[ 1, 3 ].Content );
		Assert.Equal( "b", screen.VirtualScreen[ 1, 4 ].Content );
		Assert.Equal( "c", screen.VirtualScreen[ 1, 5 ].Content );
	}

	[Fact]
	public void StyledTextRuneAndExactCellWritesUseLogicalCursor() {
		CursesScreen screen = new( 6, 2 );
		CursesWindow window = screen.StandardWindow;
		CursesStyle style = new(
			CursesColor.Rgb( 220, 180, 40 ),
			CursesColor.Indexed( 0 ),
			CursesTextAttributes.Underline
		);

		window.Move( 1, 1 );
		window.Write( "A", style );
		window.Write( new Rune( 'B' ) );
		window.WriteCell( CursesCell.Continuation( style ) );

		Assert.Equal( "A", screen.VirtualScreen[ 1, 1 ].Content );
		Assert.Equal( style, screen.VirtualScreen[ 1, 1 ].Style );
		Assert.Equal( "B", screen.VirtualScreen[ 1, 2 ].Content );
		Assert.True( screen.VirtualScreen[ 1, 3 ].IsContinuation );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 4, window.CursorColumn );
	}

	[Fact]
	public void ClipModeStopsTextAtRightBoundary() {
		CursesScreen screen = new( 3, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WrapMode = CursesWrapMode.Clip;

		window.Write( "abcd" );

		Assert.Equal( "a", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( "b", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( "c", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
	}

	[Fact]
	public void WrappingAndScrollingKeepStreamingTextInsideWindow() {
		CursesScreen screen = new( 3, 2 );
		CursesWindow window = screen.StandardWindow;
		window.ScrollingEnabled = true;

		window.Write( "abcdefg" );

		Assert.Equal( "d", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( "e", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( "f", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( "g", screen.VirtualScreen[ 1, 0 ].Content );
		Assert.True( screen.VirtualScreen[ 1, 1 ].IsBlank );
		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 1, window.CursorColumn );
	}

	[Fact]
	public void BackgroundEraseAndClearOperationsUseConfiguredCell() {
		CursesScreen screen = new( 5, 2 );
		CursesWindow window = screen.StandardWindow;
		CursesStyle backgroundStyle = new(
			CursesColor.Default,
			CursesColor.Indexed( 4 ),
			CursesTextAttributes.None
		);
		window.BackgroundCell = new CursesCell( ".", backgroundStyle );
		window.Write( "abcdefgh" );
		window.Move( 0, 2 );

		window.ClearToEndOfWindow();

		Assert.Equal( "a", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( "b", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( ".", screen.VirtualScreen[ 0, 2 ].Content );
		Assert.Equal( ".", screen.VirtualScreen[ 1, 4 ].Content );
		Assert.Equal( backgroundStyle, screen.VirtualScreen[ 1, 4 ].Style );

		window.Clear();
		Assert.Equal( 0, window.CursorRow );
		Assert.Equal( 0, window.CursorColumn );
		Assert.Equal( ".", screen.VirtualScreen[ 0, 0 ].Content );
	}

	[Fact]
	public void ExplicitScrollDownAndUpOperateOnlyInsideWindowRectangle() {
		CursesScreen screen = new( 5, 4 );
		CursesWindow window = screen.CreateWindow( 1, 1, 2, 3 );
		window.WrapMode = CursesWrapMode.Clip;
		window.Move( 0, 0 );
		window.Write( "abc" );
		window.Move( 1, 0 );
		window.Write( "def" );

		window.ScrollUp();
		Assert.Equal( "d", screen.VirtualScreen[ 1, 1 ].Content );
		Assert.Equal( "f", screen.VirtualScreen[ 1, 3 ].Content );
		Assert.True( screen.VirtualScreen[ 2, 1 ].IsBlank );

		window.ScrollDown();
		Assert.True( screen.VirtualScreen[ 1, 1 ].IsBlank );
		Assert.Equal( "d", screen.VirtualScreen[ 2, 1 ].Content );
		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
	}

	[Fact]
	public void TouchAndInvalidateMarkUnchangedLogicalCellsDirty() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow window = screen.CreateWindow( 0, 1, 2, 2 );
		screen.VirtualScreen.MarkClean();

		window.TouchLine( 1 );

		Assert.Equal( 2, screen.VirtualScreen.DirtyCellCount );
		Assert.True( screen.VirtualScreen.IsDirty( 1, 1 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 1, 2 ) );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 0 ) );

		screen.VirtualScreen.MarkClean();
		window.Invalidate();
		Assert.Equal( 4, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public void CoordinatesDimensionsAndBackgroundAreValidated() {
		CursesScreen screen = new( 4, 3 );
		CursesWindow window = screen.CreateWindow( 1, 1, 2, 3 );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.Move( 2, 0 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreateWindow( 2, 0, 2, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.CreateSubwindow( 0, 2, 1, 2 )
		);
		Assert.Throws<ArgumentException>(
			() => window.BackgroundCell = CursesCell.Continuation()
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => window.ScrollUp( 0 )
		);
	}
}
