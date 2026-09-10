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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies public panel visibility, placement, z-order, and lifetime operations.</summary>
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

	[Fact]
	public void DisposeRemovesPanelAndRevealsUnderlyingComposition() {
		CursesScreen screen = new( 12, 4 );
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "BASE" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 4 );
		panel.ContentWindow.Write( "TOP!" );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.Equal( "TOP!", ReadText( composed, 1, 2, 4 ) );
		composed.MarkClean();

		panel.Dispose();
		CursesVirtualScreen revealed = screen.ComposePanels();

		Assert.Same( composed, revealed );
		Assert.False( panel.IsVisible );
		Assert.Empty( screen.SnapshotPanelsBottomToTop() );
		Assert.False( screen.HasPanels );
		Assert.Equal( "BASE", ReadText( revealed, 1, 2, 4 ) );
	}

	[Fact]
	public void DisposeIsIdempotent() {
		CursesScreen screen = new( 12, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 4 );
		IDisposable disposable = panel;

		disposable.Dispose();
		disposable.Dispose();

		Assert.False( panel.IsVisible );
		Assert.False( screen.HasPanels );
		Assert.Empty( screen.SnapshotPanelsBottomToTop() );
	}

	[Fact]
	public void DisposedPanelRejectsFurtherManipulation() {
		CursesScreen screen = new( 12, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 4 );
		CursesPanel sibling = screen.CreatePanel( 0, 0, 1, 1 );
		panel.Dispose();

		Assert.Throws<ObjectDisposedException>( () => panel.Show() );
		Assert.Throws<ObjectDisposedException>( () => panel.Hide() );
		Assert.Throws<ObjectDisposedException>( () => panel.MoveTo( 1, 2 ) );
		Assert.Throws<ObjectDisposedException>( () => panel.MoveToTop() );
		Assert.Throws<ObjectDisposedException>( () => panel.MoveToBottom() );
		Assert.Throws<ObjectDisposedException>( () => panel.MoveAbove( sibling ) );
		Assert.Throws<ObjectDisposedException>( () => panel.MoveBelow( sibling ) );
		Assert.Throws<ObjectDisposedException>(
			() => panel.Transparency = CursesPanelTransparency.BlankCellsTransparent
		);
		Assert.Throws<ArgumentException>( () => sibling.MoveAbove( panel ) );
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
