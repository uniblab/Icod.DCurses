/*
	Icod.DCurses.Roguelike.Sample
	Application-owned world and viewport for the 2.1 roguelike sample.
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

namespace Icod.DCurses.Roguelike.Sample;

using Icod.DCurses;

internal sealed class RoguelikeSampleState {
	internal const int WorldRows = 10_000_000;
	internal const int WorldColumns = 2048;
	private static readonly CursesCell Floor = new( "." );
	private static readonly CursesCell HorizontalWall = new( "-" );
	private static readonly CursesCell VerticalWall = new( "|" );
	private static readonly CursesCell Door = new( "+" );
	private static readonly CursesCell Corridor = new( "#" );
	private static readonly CursesCell Water = new( "~" );
	private static readonly CursesCell Void = new( " " );
	private static readonly CursesCell Player = new( "@" );
	private readonly List<string> messages = [];

	internal RoguelikeSampleState( int visibleRows, int visibleColumns ) {
		Viewport = new CursesViewport( WorldRows, WorldColumns,
			visibleRows, visibleColumns ).EnsureVisible( new CursesCellPosition( PlayerRow, PlayerColumn ) );
	}

	internal int PlayerRow { get; private set; } = 100;
	internal int PlayerColumn { get; private set; } = 100;
	internal CursesViewport Viewport { get; private set; }
	internal IReadOnlyList<string> Messages => messages;

	internal bool Move( int rowDelta, int columnDelta ) {
		int row = (int)Math.Clamp( (long)PlayerRow + rowDelta, 0, WorldRows - 1 );
		int column = (int)Math.Clamp( (long)PlayerColumn + columnDelta, 0, WorldColumns - 1 );
		if ( row == PlayerRow && column == PlayerColumn
			|| TerrainGlyphAt( row, column ) is not ( '.' or '+' or '#' ) ) {
			return false;
		}
		PlayerRow = row;
		PlayerColumn = column;
		Viewport = Viewport.EnsureVisible( new CursesCellPosition( row, column ) );
		return true;
	}

	internal void ResizeViewport( int rows, int columns ) {
		Viewport = Viewport.WithViewportExtent( rows, columns )
			.EnsureVisible( new CursesCellPosition( PlayerRow, PlayerColumn ) );
	}

	internal bool TryGetPlayerViewportPosition( out CursesCellPosition position ) =>
		Viewport.TryContentToViewport( new CursesCellPosition( PlayerRow, PlayerColumn ), out position );

	internal CursesCell TerrainCellAt( int row, int column ) {
		if ( row < 0 || row >= WorldRows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 || column >= WorldColumns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		return TerrainGlyphAt( row, column ) switch {
			'.' => Floor,
			'-' => HorizontalWall,
			'|' => VerticalWall,
			'+' => Door,
			'#' => Corridor,
			'~' => Water,
			_ => Void
		};
	}

	private static char TerrainGlyphAt( int row, int column ) {
		const int roomRows = 12;
		const int roomColumns = 24;
		int localRow = row % roomRows;
		int localColumn = column % roomColumns;
		int variant = ( row / roomRows + column / roomColumns ) & 1;
		int top = 1 + variant;
		int bottom = 9 - variant;
		int left = 2 + variant;
		int right = 20 - variant;

		if ( localRow < top || localRow > bottom
			|| localColumn < left || localColumn > right ) {
			return localRow == 5 || localColumn == 11 ? '#' : ' ';
		}
		if ( localRow == top || localRow == bottom ) {
			return localColumn == 11 ? '+' : '-';
		}
		if ( localColumn == left || localColumn == right ) {
			return localRow == 5 ? '+' : '|';
		}
		if ( ( localRow is 3 or 4 ) && ( localColumn is >= 15 and <= 17 ) ) {
			return '~';
		}
		return '.';
	}

	internal CursesCell[] CreateVisibleFrame() {
		CursesRectangle visible = Viewport.VisibleContent;
		CursesCell[] cells = new CursesCell[ visible.Rows * visible.Columns ];
		for ( int row = 0; row < visible.Rows; row++ ) {
			for ( int column = 0; column < visible.Columns; column++ ) {
				int worldRow = visible.Row + row;
				int worldColumn = visible.Column + column;
				cells[ row * visible.Columns + column ] =
					worldRow == PlayerRow && worldColumn == PlayerColumn
						? Player : TerrainCellAt( worldRow, worldColumn );
			}
		}
		return cells;
	}

	internal void AddMessage( string message ) {
		ArgumentNullException.ThrowIfNull( message );
		if ( messages.Count == 4 ) {
			messages.RemoveAt( 0 );
		}
		messages.Add( message );
	}
}

internal readonly record struct RoguelikeRegions(
	CursesRectangle Map,
	CursesRectangle Sidebar,
	CursesRectangle Messages,
	CursesRectangle Status,
	CursesRectangle Overlay
);

internal static class RoguelikeSampleLayout {
	internal static bool TryArrange( CursesRectangle bounds, out RoguelikeRegions regions ) {
		if ( bounds.Rows < 6 || bounds.Columns < 30 ) {
			regions = default;
			return false;
		}
		CursesRectangle[] rows = CursesLayout.ArrangeRows( bounds,
			[ CursesTrack.Weighted( minimum: 1 ), CursesTrack.Fixed( 2 ), CursesTrack.Fixed( 1 ) ] );
		CursesRectangle[] columns = CursesLayout.ArrangeColumns( rows[ 0 ],
			[ CursesTrack.Weighted( minimum: 1 ), CursesTrack.Fixed( 18 ) ] );
		int overlayRows = Math.Min( 9, bounds.Rows - 2 );
		int overlayColumns = Math.Min( 48, bounds.Columns - 2 );
		CursesRectangle overlay = new(
			bounds.Row + ( bounds.Rows - overlayRows ) / 2,
			bounds.Column + ( bounds.Columns - overlayColumns ) / 2,
			overlayRows,
			overlayColumns
		);
		regions = new RoguelikeRegions( columns[ 0 ], columns[ 1 ], rows[ 1 ], rows[ 2 ], overlay );
		return true;
	}
}
