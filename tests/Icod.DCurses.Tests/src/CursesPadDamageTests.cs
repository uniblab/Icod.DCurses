using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies independent pad viewport content/damage observation.</summary>
public sealed class CursesPadDamageTests {
	[Fact]
	public void ViewportReportsInitialAndVisibleContentChanges() {
		CursesPad pad = new(
			6,
			2
		);
		pad.ContentWindow.FillRectangle(
			0,
			0,
			2,
			6,
			new CursesCell( "A" )
		);
		CursesScreen screen = new(
			3,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			3,
			0,
			0
		);

		Assert.True( viewport.HasVisiblePadChanges );
		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );

		pad.ContentWindow.FillRectangle(
			0,
			1,
			1,
			1,
			new CursesCell( "B" )
		);
		Assert.True( viewport.HasVisiblePadChanges );

		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );
		Assert.Equal( "B", screen.StandardWindow.GetCell( 0, 1 ).Content );
	}

	[Fact]
	public void OffscreenPadChangesDoNotReportAsVisibleChanges() {
		CursesPad pad = new(
			8,
			2
		);
		CursesScreen screen = new(
			3,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			3,
			0,
			0
		);
		viewport.Present();

		pad.ContentWindow.FillRectangle(
			0,
			7,
			1,
			1,
			new CursesCell( "X" )
		);

		Assert.False( viewport.HasVisiblePadChanges );

		pad.ContentWindow.FillRectangle(
			0,
			2,
			1,
			1,
			new CursesCell( "Y" )
		);
		Assert.True( viewport.HasVisiblePadChanges );
	}

	[Fact]
	public void OneViewportPresentationDoesNotAcknowledgeAnotherViewport() {
		CursesPad pad = new(
			5,
			2
		);
		CursesScreen firstScreen = new(
			3,
			1
		);
		CursesScreen secondScreen = new(
			3,
			1
		);
		CursesPadViewport first = pad.CreateViewport(
			firstScreen.StandardWindow,
			0,
			1,
			1,
			3,
			0,
			0
		);
		CursesPadViewport second = pad.CreateViewport(
			secondScreen.StandardWindow,
			0,
			1,
			1,
			3,
			0,
			0
		);
		first.Present();
		second.Present();

		pad.ContentWindow.FillRectangle(
			0,
			2,
			1,
			1,
			new CursesCell( "Q" )
		);
		Assert.True( first.HasVisiblePadChanges );
		Assert.True( second.HasVisiblePadChanges );

		first.Present();
		Assert.False( first.HasVisiblePadChanges );
		Assert.True( second.HasVisiblePadChanges );

		second.Present();
		Assert.False( second.HasVisiblePadChanges );
	}

	[Fact]
	public void ExplicitPadTouchPropagatesDamageWithoutValueChange() {
		CursesPad pad = new(
			4,
			1
		);
		pad.ContentWindow.Write( "ABCD" );
		CursesScreen screen = new(
			4,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			4,
			0,
			0
		);
		viewport.Present();
		screen.VirtualScreen.MarkClean();

		pad.ContentWindow.TouchRegion(
			0,
			1,
			1,
			1
		);
		Assert.True( viewport.HasVisiblePadChanges );

		viewport.Present();

		Assert.False( viewport.HasVisiblePadChanges );
		Assert.Equal( 1, screen.VirtualScreen.DirtyCellCount );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 0 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 1 ) );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 2 ) );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 3 ) );
	}

	[Fact]
	public void PanningInvalidatesDestinationEvenWhenCellValuesMatch() {
		CursesPad pad = new(
			5,
			1
		);
		pad.ContentWindow.FillRectangle(
			0,
			0,
			1,
			5,
			new CursesCell( "X" )
		);
		CursesScreen screen = new(
			2,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);
		viewport.Present();
		screen.VirtualScreen.MarkClean();

		viewport.PanBy(
			0,
			1
		);
		Assert.True( viewport.HasVisiblePadChanges );
		viewport.Present();

		Assert.Equal( 2, screen.VirtualScreen.DirtyCellCount );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 0 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 1 ) );
		Assert.False( viewport.HasVisiblePadChanges );
	}

	[Fact]
	public void AuthoritativePresentationRepairsDestinationDriftWithoutPadChange() {
		CursesPad pad = new(
			3,
			1
		);
		pad.ContentWindow.Write( "ABC" );
		CursesScreen screen = new(
			3,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			1,
			3,
			0,
			0
		);
		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );

		screen.StandardWindow.FillRectangle(
			0,
			0,
			1,
			1,
			new CursesCell( "Z" )
		);
		Assert.False( viewport.HasVisiblePadChanges );

		viewport.Present();

		Assert.Equal( "A", screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "B", screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.Equal( "C", screen.StandardWindow.GetCell( 0, 2 ).Content );
	}
}
