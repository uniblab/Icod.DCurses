/*
	Icod.DCurses.Roguelike.Sample
	Interactive 2.1 public-API roguelike acceptance sample.
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

using Icod.DCurses;
using Icod.DCurses.Roguelike.Sample;

await using CursesSession session = await CursesSession.OpenAsync(
	new CursesSessionOptions { EnableRefreshDiagnostics = true } );
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
CursesWindow map = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow sidebar = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow messages = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
using CursesPanel help = screen.CreatePanel( 0, 0, 1, 1 );
foreach ( CursesWindow window in new[] { standard, map, sidebar, messages, status, help.ContentWindow } ) {
	window.WrapMode = CursesWrapMode.Clip;
}
help.Hide();
RoguelikeSampleState state = new( 1, 1 );
state.AddMessage( "Arrows/WASD move, ? help, Q quits." );
bool running = true;
bool showHelp = false;
bool fullMap = true;

while ( running ) {
	if ( RoguelikeSampleLayout.TryArrange( screen.Bounds, out RoguelikeRegions regions ) ) {
		map.SetBounds( regions.Map );
		sidebar.SetBounds( regions.Sidebar );
		messages.SetBounds( regions.Messages );
		status.SetBounds( regions.Status );
		CursesViewport oldViewport = state.Viewport;
		state.ResizeViewport( map.Rows, map.Columns );
		if ( fullMap || oldViewport != state.Viewport ) {
			CursesCell[] frame = state.CreateVisibleFrame();
			map.WriteCells( 0, 0, map.Rows, map.Columns, frame, map.Columns );
		}
		DrawSidebar( sidebar, state );
		DrawMessages( messages, state );
		status.Clear();
		WriteLine( status, 0,
			$"World {state.PlayerRow},{state.PlayerColumn}  View {state.Viewport.OriginRow},{state.Viewport.OriginColumn}  Prepared {session.LatestRefreshDiagnostics?.PreparedOutputItemCount ?? 0}" );
		if ( showHelp ) {
			help.SetBounds( regions.Overlay );
			DrawHelp( help.ContentWindow );
			help.Show();
		} else {
			help.Hide();
		}
	} else {
		help.Hide();
		standard.Clear();
		WriteLine( standard, 0, "Resize to at least 30 columns x 6 rows; Q exits." );
	}
	await session.RefreshAsync();
	fullMap = false;

	CursesEvent current = await session.ReadEventAsync();
	if ( CursesEventKind.Lifecycle == current.Kind && current.Lifecycle is not null ) {
		if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
			break;
		}
	}
	if ( current.RequiresRepaint ) {
		_ = session.SynchronizeDimensions();
		session.Invalidate();
		fullMap = true;
	}
	if ( CursesEventKind.Input != current.Kind || current.Input is null ) {
		continue;
	}
	CursesInputEvent input = current.Input;
	if ( CursesInputEventKind.EndOfInput == input.Kind ) {
		break;
	}
	if ( CursesInputEventKind.Key == input.Kind && CursesKey.Escape == input.Key ) {
		break;
	}
	int rowDelta = 0;
	int columnDelta = 0;
	if ( CursesInputEventKind.Key == input.Kind ) {
		switch ( input.Key ) {
			case CursesKey.Up: rowDelta = -1; break;
			case CursesKey.Down: rowDelta = 1; break;
			case CursesKey.Left: columnDelta = -1; break;
			case CursesKey.Right: columnDelta = 1; break;
		}
	} else if ( CursesInputEventKind.Text == input.Kind && input.Character.HasValue ) {
		switch ( input.Character.Value.Value ) {
			case 'w': case 'W': rowDelta = -1; break;
			case 's': case 'S': rowDelta = 1; break;
			case 'a': case 'A': columnDelta = -1; break;
			case 'd': case 'D': columnDelta = 1; break;
			case '?': showHelp = !showHelp; break;
			case 'q': case 'Q': running = false; break;
		}
	}
	if ( !running || ( rowDelta == 0 && columnDelta == 0 ) ) {
		continue;
	}
	CursesViewport previous = state.Viewport;
	int oldRow = state.PlayerRow;
	int oldColumn = state.PlayerColumn;
	if ( !state.Move( rowDelta, columnDelta ) ) {
		continue;
	}
	state.AddMessage( $"Moved to {state.PlayerRow},{state.PlayerColumn}." );
	if ( previous != state.Viewport ) {
		fullMap = true;
		continue;
	}
	if ( previous.TryContentToViewport( new CursesCellPosition( oldRow, oldColumn ),
		out CursesCellPosition oldLocal )
		&& state.TryGetPlayerViewportPosition( out CursesCellPosition newLocal )
		&& map.Rows == state.Viewport.Rows && map.Columns == state.Viewport.Columns ) {
		map.WriteCells( oldLocal.Row, oldLocal.Column,
			[ state.TerrainCellAt( oldRow, oldColumn ) ] );
		map.WriteCells( newLocal.Row, newLocal.Column, [ new CursesCell( "@" ) ] );
	} else {
		fullMap = true;
	}
}

help.Hide();
standard.Clear();
await session.RefreshAsync();
return 0;

static void DrawSidebar( CursesWindow window, RoguelikeSampleState state ) {
	window.Clear();
	WriteLine( window, 0, "WORLD" );
	WriteLine( window, 1, $"Row {state.PlayerRow}" );
	WriteLine( window, 2, $"Col {state.PlayerColumn}" );
	WriteLine( window, 4, "@ you  + door" );
	WriteLine( window, 5, ". room # corridor" );
	WriteLine( window, 6, "-| wall ~ water" );
	WriteLine( window, 7, "? help" );
}

static void DrawMessages( CursesWindow window, RoguelikeSampleState state ) {
	window.Clear();
	int start = Math.Max( 0, state.Messages.Count - window.Rows );
	for ( int index = 0; index < window.Rows && start + index < state.Messages.Count; index++ ) {
		WriteLine( window, index, state.Messages[ start + index ] );
	}
}

static void DrawHelp( CursesWindow window ) {
	window.Clear();
	WriteLine( window, 0, " ROGUELIKE HELP" );
	WriteLine( window, 2, " Arrows or WASD: move through the world" );
	WriteLine( window, 3, " + doors and # corridors connect rooms." );
	WriteLine( window, 4, " -| walls, ~ water, and void block movement." );
	WriteLine( window, 5, " ?: close overlay   Q / Escape: quit" );
}

static void WriteLine( CursesWindow window, int row, string text ) {
	if ( row < 0 || row >= window.Rows ) {
		return;
	}
	window.Move( row, 0 );
	window.Write( text[ .. Math.Min( text.Length, window.Columns ) ] );
}
