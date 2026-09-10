/*
	Icod.DCurses.Panel.Sample
	Demonstrates retained panels, layering, and z-order composition.
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

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow baseWindow = session.StandardScreen;
baseWindow.WrapMode = CursesWrapMode.Clip;
baseWindow.Clear();
WriteLine(
	baseWindow,
	0,
	"Icod.DCurses 1.2 retained panel sample"
);
WriteLine(
	baseWindow,
	1,
	"The base screen remains retained while panels overlap, move, hide, and dispose."
);

if ( screen.Rows < 4 || screen.Columns < 12 ) {
	WriteLine(
		baseWindow,
		2,
		"Resize to at least 12 columns x 4 rows, then rerun this sample."
	);
	await session.RefreshAsync();
	_ = await session.ReadEventAsync();
	return 0;
}

int panelRows = Math.Min(
	7,
	screen.Rows - 2
);
int panelColumns = Math.Min(
	38,
	screen.Columns - 2
);
using CursesPanel panel = screen.CreatePanel(
	1,
	1,
	panelRows,
	panelColumns
);
CursesWindow panelWindow = panel.ContentWindow;
panelWindow.WrapMode = CursesWrapMode.Clip;
panelWindow.Clear();
WriteLine(
	panelWindow,
	0,
	"Independent retained panel"
);
WriteLine(
	panelWindow,
	2,
	"Press a key: show overlap"
);

int overlayColumns = Math.Min(
	24,
	panelColumns - 1
);
using CursesPanel overlay = screen.CreatePanel(
	1,
	2,
	1,
	overlayColumns
);
CursesWindow overlayWindow = overlay.ContentWindow;
overlayWindow.WrapMode = CursesWrapMode.Clip;
overlayWindow.Clear();
WriteLine(
	overlayWindow,
	0,
	"Second panel starts on top"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

panel.MoveToTop();
WriteLine(
	panelWindow,
	0,
	"Primary panel moved to top"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

overlay.MoveAbove( panel );
overlayWindow.Clear();
WriteLine(
	overlayWindow,
	0,
	"Second panel moved above"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

overlay.Dispose();
WriteLine(
	panelWindow,
	0,
	"Independent retained panel"
);
WriteLine(
	panelWindow,
	2,
	"Press a key: hide"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

panel.Hide();
await session.RefreshAsync();
await WaitForInputAsync( session );

panel.Show();
if ( screen.Rows >= panel.Rows
	&& screen.Columns >= panel.Columns ) {
	panel.MoveTo(
		Math.Max(
			0,
			screen.Rows - panel.Rows
		),
		Math.Max(
			0,
			screen.Columns - panel.Columns
		)
	);
}
WriteLine(
	panelWindow,
	2,
	"Retained after show/move"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
panelWindow.Clear();
WriteLine(
	panelWindow,
	0,
	"Blank cells are transparent"
);
WriteLine(
	panelWindow,
	2,
	"Press a key: dispose"
);
await session.RefreshAsync();
await WaitForInputAsync( session );

panel.Dispose();
await session.RefreshAsync();
_ = await session.ReadEventAsync();
return 0;

static async Task WaitForInputAsync( CursesSession session ) {
	ArgumentNullException.ThrowIfNull( session );

	while ( true ) {
		CursesEvent current = await session.ReadEventAsync();
		if ( current.RequiresRepaint ) {
			session.Invalidate();
			await session.RefreshAsync();
			continue;
		}
		if ( CursesEventKind.Input == current.Kind ) {
			return;
		}
		if ( CursesEventKind.Lifecycle == current.Kind
			&& null != current.Lifecycle
			&& current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
				or CursesLifecycleEventKind.Termination ) {
			return;
		}
	}
}

static void WriteLine(
	CursesWindow window,
	int row,
	string text
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( text );
	if ( row < 0 || row >= window.Rows ) {
		return;
	}

	window.Move(
		row,
		0
	);
	window.Write( text );
}
