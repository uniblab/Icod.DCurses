using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies stateful pad viewport positioning and independent panning.</summary>
public sealed class CursesPadViewportTests {
	[Fact]
	public void ViewportPresentsAndPansVertically() {
		CursesPad pad = new(
			8,
			5
		);
		for ( int row = 0; row < 5; row++ ) {
			pad.ContentWindow.FillRectangle(
				row,
				0,
				1,
				8,
				new CursesCell( ((char)( 'A' + row )).ToString() )
			);
		}

		CursesScreen screen = new(
			4,
			2
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			2,
			2,
			4,
			0,
			0
		);

		viewport.Present();
		AssertRowContent( screen.StandardWindow, 0, "AAAA" );
		AssertRowContent( screen.StandardWindow, 1, "BBBB" );

		viewport.PanBy(
			2,
			0
		);
		viewport.Present();

		Assert.Equal( 2, viewport.PadRow );
		Assert.Equal( 2, viewport.PadColumn );
		AssertRowContent( screen.StandardWindow, 0, "CCCC" );
		AssertRowContent( screen.StandardWindow, 1, "DDDD" );
	}

	[Fact]
	public void ViewportPansHorizontallyAndSupportsAbsoluteSourcePosition() {
		CursesPad pad = new(
			8,
			2
		);
		for ( int column = 0; column < 8; column++ ) {
			pad.ContentWindow.FillRectangle(
				0,
				column,
				2,
				1,
				new CursesCell( column.ToString() )
			);
		}

		CursesScreen screen = new(
			3,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			1,
			1,
			3,
			0,
			0
		);

		viewport.Present();
		AssertRowContent( screen.StandardWindow, 0, "123" );

		viewport.PanBy(
			0,
			2
		);
		viewport.Present();
		AssertRowContent( screen.StandardWindow, 0, "345" );

		viewport.SetSource(
			1,
			5
		);
		viewport.Present();
		Assert.Equal( 1, viewport.PadRow );
		Assert.Equal( 5, viewport.PadColumn );
		AssertRowContent( screen.StandardWindow, 0, "567" );
	}

	[Fact]
	public void MultipleViewportsPanIndependentlyOverOnePad() {
		CursesPad pad = new(
			5,
			4
		);
		for ( int row = 0; row < 4; row++ ) {
			pad.ContentWindow.FillRectangle(
				row,
				0,
				1,
				5,
				new CursesCell( ((char)( 'A' + row )).ToString() )
			);
		}

		CursesScreen firstScreen = new(
			5,
			2
		);
		CursesScreen secondScreen = new(
			5,
			2
		);
		CursesPadViewport first = pad.CreateViewport(
			firstScreen.StandardWindow,
			0,
			0,
			2,
			5,
			0,
			0
		);
		CursesPadViewport second = pad.CreateViewport(
			secondScreen.StandardWindow,
			2,
			0,
			2,
			5,
			0,
			0
		);

		first.Present();
		second.Present();
		first.PanBy(
			1,
			0
		);
		first.Present();

		Assert.Equal( 1, first.PadRow );
		Assert.Equal( 2, second.PadRow );
		AssertRowContent( firstScreen.StandardWindow, 0, "BBBBB" );
		AssertRowContent( firstScreen.StandardWindow, 1, "CCCCC" );
		AssertRowContent( secondScreen.StandardWindow, 0, "CCCCC" );
		AssertRowContent( secondScreen.StandardWindow, 1, "DDDDD" );
	}

	[Fact]
	public void InvalidPanLeavesViewportStateUnchanged() {
		CursesPad pad = new(
			10,
			6
		);
		CursesScreen screen = new(
			4,
			3
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			1,
			2,
			3,
			4,
			0,
			0
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => viewport.PanBy(
				3,
				0
			)
		);
		Assert.Equal( 1, viewport.PadRow );
		Assert.Equal( 2, viewport.PadColumn );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => viewport.SetSource(
				0,
				7
			)
		);
		Assert.Equal( 1, viewport.PadRow );
		Assert.Equal( 2, viewport.PadColumn );
	}

	[Fact]
	public void ViewportExposesFrozenGeometry() {
		CursesPad pad = new(
			20,
			10
		);
		CursesScreen screen = new(
			12,
			6
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			3,
			4,
			5,
			7,
			1,
			2
		);

		Assert.Equal( 3, viewport.PadRow );
		Assert.Equal( 4, viewport.PadColumn );
		Assert.Equal( 5, viewport.Rows );
		Assert.Equal( 7, viewport.Columns );
		Assert.Equal( 1, viewport.DestinationRow );
		Assert.Equal( 2, viewport.DestinationColumn );
	}

	private static void AssertRowContent(
		CursesWindow window,
		int row,
		string expected
	) {
		ArgumentNullException.ThrowIfNull( window );
		ArgumentNullException.ThrowIfNull( expected );
		string actual = string.Concat(
			Enumerable.Range( 0, window.Columns )
				.Select( column => window.GetCell( row, column ).Content )
		);
		Assert.Equal( expected, actual );
	}
}
