using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies that per-cell logical change revisions are opt-in rather than a universal screen cost.</summary>
public sealed class CursesVirtualScreenChangeTrackingTests {
	[Fact]
	public void ChangeTrackingIsDisabledByDefaultAndCanBeEnabledExplicitly() {
		CursesVirtualScreen screen = new(
			3,
			2
		);

		Assert.False( screen.ChangeTrackingEnabled );
		Assert.Equal( 0UL, screen.ChangeRevision );
		Assert.Throws<InvalidOperationException>(
			() => screen.GetCellChangeRevision(
				0,
				0
			)
		);

		screen[ 0, 0 ] = new CursesCell( "A" );
		Assert.Equal( 0UL, screen.ChangeRevision );

		screen.EnableChangeTracking();
		Assert.True( screen.ChangeTrackingEnabled );
		Assert.Equal( 0UL, screen.GetCellChangeRevision( 0, 0 ) );

		screen[ 0, 0 ] = new CursesCell( "B" );
		ulong valueRevision = screen.GetCellChangeRevision(
			0,
			0
		);
		Assert.NotEqual( 0UL, valueRevision );
		Assert.Equal( valueRevision, screen.ChangeRevision );

		screen.TouchCell(
			0,
			0
		);
		ulong touchRevision = screen.GetCellChangeRevision(
			0,
			0
		);
		Assert.True( touchRevision > valueRevision );
		Assert.Equal( touchRevision, screen.ChangeRevision );
	}

	[Fact]
	public void OrdinaryCursesScreenDoesNotEnablePadChangeTracking() {
		CursesScreen screen = new(
			80,
			24
		);

		Assert.False( screen.VirtualScreen.ChangeTrackingEnabled );
		Assert.Equal( 0UL, screen.VirtualScreen.ChangeRevision );
	}

	[Fact]
	public void CursesPadEnablesChangeTrackingForItsBackingSurface() {
		CursesPad pad = new(
			4,
			2
		);
		CursesScreen destination = new(
			2,
			1
		);
		CursesPadViewport viewport = pad.CreateViewport(
			destination.StandardWindow,
			0,
			0,
			1,
			2,
			0,
			0
		);

		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );

		pad.ContentWindow.FillRectangle(
			0,
			0,
			1,
			1,
			new CursesCell( "X" )
		);
		Assert.True( viewport.HasVisiblePadChanges );
	}
}
