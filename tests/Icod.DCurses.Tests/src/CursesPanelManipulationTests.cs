using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies public panel visibility, placement, and z-order operations.</summary>
public sealed class CursesPanelManipulationTests {
	[Fact]
	public void CreatedPanelsBeginVisibleInBottomToTopCreationOrder() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel first = screen.CreatePanel(
			0,
			0,
			2,
			3
		);
		CursesPanel second = screen.CreatePanel(
			1,
			1,
			2,
			3
		);
		CursesPanel third = screen.CreatePanel(
			2,
			2,
			2,
			3
		);

		Assert.True( first.IsVisible );
		Assert.True( second.IsVisible );
		Assert.True( third.IsVisible );
		Assert.Equal(
			new[] {
				first,
				second,
				third
			},
			screen.SnapshotPanelsBottomToTop()
		);
	}

	[Fact]
	public void HideAndShowPreservePanelStackPosition() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel first = screen.CreatePanel( 0, 0, 2, 3 );
		CursesPanel second = screen.CreatePanel( 1, 1, 2, 3 );
		CursesPanel third = screen.CreatePanel( 2, 2, 2, 3 );

		second.Hide();
		Assert.False( second.IsVisible );
		Assert.Equal(
			new[] {
				first,
				second,
				third
			},
			screen.SnapshotPanelsBottomToTop()
		);

		second.Show();
		Assert.True( second.IsVisible );
		Assert.Equal(
			new[] {
				first,
				second,
				third
			},
			screen.SnapshotPanelsBottomToTop()
		);
	}

	[Fact]
	public void MoveToChangesOnlyDestinationOrigin() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel panel = screen.CreatePanel(
			1,
			2,
			3,
			4
		);
		panel.ContentWindow.Write( "keep" );

		panel.MoveTo(
			5,
			12
		);

		Assert.Equal( 5, panel.Row );
		Assert.Equal( 12, panel.Column );
		Assert.Equal( "k", panel.ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( 3, panel.Rows );
		Assert.Equal( 4, panel.Columns );
	}

	[Fact]
	public void MoveToRejectsOriginsWhichCannotContainThePanel() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel panel = screen.CreatePanel(
			1,
			2,
			3,
			4
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.MoveTo(
				-1,
				2
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.MoveTo(
				8,
				2
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => panel.MoveTo(
				1,
				17
			)
		);

		Assert.Equal( 1, panel.Row );
		Assert.Equal( 2, panel.Column );
	}

	[Fact]
	public void MoveToTopAndBottomPreserveOtherRelativeOrder() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel first = screen.CreatePanel( 0, 0, 2, 3 );
		CursesPanel second = screen.CreatePanel( 1, 1, 2, 3 );
		CursesPanel third = screen.CreatePanel( 2, 2, 2, 3 );

		first.MoveToTop();
		Assert.Equal(
			new[] {
				second,
				third,
				first
			},
			screen.SnapshotPanelsBottomToTop()
		);

		third.MoveToBottom();
		Assert.Equal(
			new[] {
				third,
				second,
				first
			},
			screen.SnapshotPanelsBottomToTop()
		);
	}

	[Fact]
	public void MoveAboveAndBelowUsePanelIdentity() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel first = screen.CreatePanel( 0, 0, 2, 3 );
		CursesPanel second = screen.CreatePanel( 1, 1, 2, 3 );
		CursesPanel third = screen.CreatePanel( 2, 2, 2, 3 );
		CursesPanel fourth = screen.CreatePanel( 3, 3, 2, 3 );

		first.MoveAbove( third );
		Assert.Equal(
			new[] {
				second,
				third,
				first,
				fourth
			},
			screen.SnapshotPanelsBottomToTop()
		);

		fourth.MoveBelow( second );
		Assert.Equal(
			new[] {
				fourth,
				second,
				third,
				first
			},
			screen.SnapshotPanelsBottomToTop()
		);
	}

	[Fact]
	public void RelativeOrderingRejectsCrossScreenPanels() {
		CursesScreen firstScreen = new(
			20,
			10
		);
		CursesScreen secondScreen = new(
			20,
			10
		);
		CursesPanel first = firstScreen.CreatePanel( 0, 0, 2, 3 );
		CursesPanel sibling = secondScreen.CreatePanel( 1, 1, 2, 3 );

		Assert.Throws<ArgumentException>(
			() => first.MoveAbove( sibling )
		);
		Assert.Throws<ArgumentException>(
			() => first.MoveBelow( sibling )
		);
		Assert.Same(
			first,
			Assert.Single( firstScreen.SnapshotPanelsBottomToTop() )
		);
		Assert.Same(
			sibling,
			Assert.Single( secondScreen.SnapshotPanelsBottomToTop() )
		);
	}

	[Fact]
	public void SelfRelativeOrderingIsRejected() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel panel = screen.CreatePanel( 0, 0, 2, 3 );

		Assert.Throws<ArgumentException>(
			() => panel.MoveAbove( panel )
		);
		Assert.Throws<ArgumentException>(
			() => panel.MoveBelow( panel )
		);
	}

	[Fact]
	public void RepeatingNoOpManipulationsPreservesStateAndOrder() {
		CursesScreen screen = new(
			20,
			10
		);
		CursesPanel first = screen.CreatePanel( 0, 0, 2, 3 );
		CursesPanel second = screen.CreatePanel( 1, 1, 2, 3 );

		first.Hide();
		first.Hide();
		first.Show();
		first.Show();
		first.MoveTo(
			0,
			0
		);
		second.MoveToTop();
		second.MoveToTop();
		first.MoveToBottom();
		first.MoveToBottom();

		Assert.True( first.IsVisible );
		Assert.Equal( 0, first.Row );
		Assert.Equal( 0, first.Column );
		Assert.Equal(
			new[] {
				first,
				second
			},
			screen.SnapshotPanelsBottomToTop()
		);
	}
}
