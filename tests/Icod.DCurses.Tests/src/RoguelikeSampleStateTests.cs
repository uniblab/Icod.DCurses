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

using Icod.DCurses.Roguelike.Sample;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class RoguelikeSampleStateTests {
	[Fact]
	public void PlayerMovesAcrossAViewportWithoutAllocatingTheWorld() {
		RoguelikeSampleState state = new( 12, 20 );
		Assert.Equal( 10_000_000, state.Viewport.ContentRows );
		Assert.Equal( 2048, state.Viewport.ContentColumns );
		Assert.True( state.TryGetPlayerViewportPosition( out CursesCellPosition local ) );
		Assert.Equal( 11, local.Row );
		Assert.Equal( 19, local.Column );
		CursesCell[] first = state.CreateVisibleFrame();
		Assert.Equal( 240, first.Length );
		Assert.Equal( new CursesCell( "@" ), first[ local.Row * 20 + local.Column ] );
		Assert.True( state.Move( 1, 0 ) );
		Assert.Equal( 101, state.PlayerRow );
		Assert.Equal( 90, state.Viewport.OriginRow );
		Assert.False( state.Move( 0, 0 ) );

		state.ResizeViewport( 24, 80 );
		Assert.Equal( 24 * 80, state.CreateVisibleFrame().Length );
		Assert.True( state.TryGetPlayerViewportPosition( out local ) );
		Assert.Equal( new CursesCell( "@" ),
			state.CreateVisibleFrame()[ local.Row * state.Viewport.Columns + local.Column ] );
	}

	[Fact]
	public void ExtremeMovementClampsAndOnlyVisibleCellsAreMaterialized() {
		RoguelikeSampleState state = new( 24, 80 );
		Assert.True( state.Move( int.MaxValue, int.MaxValue ) );
		Assert.Equal( RoguelikeSampleState.WorldRows - 1, state.PlayerRow );
		Assert.Equal( RoguelikeSampleState.WorldColumns - 1, state.PlayerColumn );
		Assert.True( state.TryGetPlayerViewportPosition( out _ ) );
		Assert.True( state.Move( int.MinValue, int.MinValue ) );
		Assert.Equal( 0, state.PlayerRow );
		Assert.Equal( 0, state.PlayerColumn );
		Assert.Equal( new CursesCell( "." ), state.TerrainCellAt( 1, 1 ) );
		Assert.Equal( state.TerrainCellAt( 100, 200 ), state.TerrainCellAt( 100, 200 ) );
		Assert.Equal( 24 * 80, state.CreateVisibleFrame().Length );
	}

	[Fact]
	public void ApplicationRegionsRecomputeAndRemainDisjoint() {
		Assert.False( RoguelikeSampleLayout.TryArrange(
			new CursesRectangle( 0, 0, 4, 20 ), out _ ) );
		foreach ( CursesRectangle bounds in new[] {
			new CursesRectangle( 0, 0, 24, 80 ),
			new CursesRectangle( 0, 0, 48, 160 ),
			new CursesRectangle( 0, 0, 6, 30 )
		} ) {
			Assert.True( RoguelikeSampleLayout.TryArrange( bounds, out RoguelikeRegions regions ) );
			CursesRectangle[] parts = [ regions.Map, regions.Sidebar, regions.Messages, regions.Status ];
			Assert.All( parts, part => Assert.True( bounds.Contains( part ) ) );
			for ( int first = 0; first < parts.Length; first++ ) {
				for ( int second = first + 1; second < parts.Length; second++ ) {
					Assert.True( parts[ first ].Intersect( parts[ second ] ).IsEmpty );
				}
			}
			Assert.True( bounds.Contains( regions.Overlay ) );
		}
	}

	[Fact]
	public void MessagesRemainBoundedAfterLongMovement() {
		RoguelikeSampleState state = new( 12, 20 );
		for ( int index = 0; index < 100; index++ ) {
			state.AddMessage( $"Turn {index}" );
		}
		Assert.Equal( 4, state.Messages.Count );
		Assert.Equal( "Turn 96", state.Messages[ 0 ] );
		Assert.Equal( "Turn 99", state.Messages[ 3 ] );
	}

	[Fact]
	public void LocalMovementDamagesTwoCellsAndHelpRestoresTheBaseMap() {
		CursesScreen screen = new( 80, 24 );
		Assert.True( RoguelikeSampleLayout.TryArrange( screen.Bounds, out RoguelikeRegions regions ) );
		RoguelikeSampleState state = new( regions.Map.Rows, regions.Map.Columns );
		CursesWindow map = screen.CreateWindow( regions.Map.Row, regions.Map.Column,
			regions.Map.Rows, regions.Map.Columns );
		CursesCell[] fullFrame = state.CreateVisibleFrame();
		map.WriteCells( 0, 0, map.Rows, map.Columns, fullFrame, map.Columns );
		Assert.Equal( map.Rows * map.Columns, fullFrame.Length );
		screen.VirtualScreen.MarkClean();

		CursesViewport previous = state.Viewport;
		int oldRow = state.PlayerRow;
		int oldColumn = state.PlayerColumn;
		Assert.True( state.Move( 0, -1 ) );
		Assert.Equal( previous, state.Viewport );
		Assert.True( previous.TryContentToViewport(
			new CursesCellPosition( oldRow, oldColumn ), out CursesCellPosition oldLocal ) );
		Assert.True( state.TryGetPlayerViewportPosition( out CursesCellPosition newLocal ) );
		map.WriteCells( oldLocal.Row, oldLocal.Column,
			[ state.TerrainCellAt( oldRow, oldColumn ) ] );
		map.WriteCells( newLocal.Row, newLocal.Column, [ new CursesCell( "@" ) ] );
		Assert.Equal( 2, screen.VirtualScreen.DirtyCellCount );

		CursesCell underneath = screen.VirtualScreen.GetCell(
			regions.Overlay.Row, regions.Overlay.Column );
		using CursesPanel help = screen.CreatePanel( regions.Overlay.Row,
			regions.Overlay.Column, regions.Overlay.Rows, regions.Overlay.Columns );
		help.ContentWindow.Write( "HELP" );
		Assert.Equal( "H", screen.ComposePanels().GetCell(
			regions.Overlay.Row, regions.Overlay.Column ).Content );
		help.Hide();
		Assert.Equal( underneath, screen.ComposePanels().GetCell(
			regions.Overlay.Row, regions.Overlay.Column ) );
	}
}
