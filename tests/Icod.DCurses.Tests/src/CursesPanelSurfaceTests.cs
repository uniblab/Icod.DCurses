using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesPanelSurfaceTests {
	[Fact]
	public void CreatePanelExposesIndependentRetainedSurface() {
		CursesScreen screen = new(
			12,
			8
		);
		screen.StandardWindow.Move(
			2,
			3
		);
		screen.StandardWindow.Write( "B" );

		CursesPanel panel = screen.CreatePanel(
			2,
			3,
			3,
			5
		);

		Assert.Equal( 2, panel.Row );
		Assert.Equal( 3, panel.Column );
		Assert.Equal( 3, panel.Rows );
		Assert.Equal( 5, panel.Columns );
		Assert.True( panel.IsVisible );
		Assert.Equal( 3, panel.ContentWindow.Rows );
		Assert.Equal( 5, panel.ContentWindow.Columns );
		Assert.True( panel.ContentWindow.IsStandardWindow );
		Assert.True( panel.ContentWindow.GetCell( 0, 0 ).IsBlank );
		Assert.Null( panel.ContentWindow.GetMetadata( 0, 0 ) );

		panel.ContentWindow.Write( "P" );

		Assert.Equal( "P", panel.ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "B", screen.StandardWindow.GetCell( 2, 3 ).Content );
		Assert.Equal( "B", screen.VirtualScreen[ 2, 3 ].Content );
	}

	[Fact]
	public void PanelContentUsesDestinationTextWidthPolicy() {
		FixedWidthProvider provider = new( 2 );
		CursesScreen screen = new(
			10,
			6,
			provider
		);
		CursesPanel panel = screen.CreatePanel(
			1,
			2,
			3,
			4
		);

		panel.ContentWindow.Write( "x" );

		Assert.Equal( "x", panel.ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( 2, panel.ContentWindow.GetCell( 0, 0 ).DisplayWidth );
		Assert.True( panel.ContentWindow.GetCell( 0, 1 ).IsContinuation );
	}

	[Fact]
	public void PanelSubwindowsShareOnlyThePanelBackingSurface() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel panel = screen.CreatePanel(
			2,
			4,
			5,
			8
		);
		CursesWindow child = panel.ContentWindow.CreateSubwindow(
			1,
			2,
			2,
			4
		);

		child.Write( "OK" );

		Assert.Equal( "O", panel.ContentWindow.GetCell( 1, 2 ).Content );
		Assert.Equal( "K", panel.ContentWindow.GetCell( 1, 3 ).Content );
		Assert.True( screen.StandardWindow.GetCell( 3, 6 ).IsBlank );
	}

	[Fact]
	public void CreatePanelRejectsInvalidOrOutOfRangeRectangle() {
		CursesScreen screen = new(
			10,
			6
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				-1,
				0,
				1,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				0,
				-1,
				1,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				0,
				0,
				0,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				0,
				0,
				1,
				0
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				5,
				0,
				2,
				1
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => screen.CreatePanel(
				0,
				9,
				1,
				2
			)
		);
	}

	private sealed class FixedWidthProvider
		: ICursesTextWidthProvider {
		private readonly int width;

		internal FixedWidthProvider( int width ) {
			if ( 0 > width || 2 < width ) {
				throw new ArgumentOutOfRangeException( nameof( width ) );
			}

			this.width = width;
		}

		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return width;
		}
	}
}
