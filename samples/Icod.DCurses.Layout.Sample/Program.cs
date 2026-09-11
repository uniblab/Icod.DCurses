/*
	Icod.DCurses.Layout.Sample
	Demonstrates explicit 1.3 layout recomputation after terminal resize.
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

const int MinimumColumns = 12;
const int MinimumRows = 4;

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
standard.WrapMode = CursesWrapMode.Clip;

if ( screen.Columns < MinimumColumns
	|| screen.Rows < MinimumRows ) {
	standard.Clear();
	WriteLine(
		standard,
		0,
		$"Resize to at least {MinimumColumns} columns x {MinimumRows} rows, then rerun."
	);
	await session.RefreshAsync();
	_ = await session.ReadEventAsync();
	return 0;
}

ComputeLayout(
	screen.Bounds,
	out CursesRectangle headerBounds,
	out CursesRectangle bodyBounds,
	out CursesRectangle panelBounds
);
CursesWindow header = screen.CreateWindow(
	headerBounds.Row,
	headerBounds.Column,
	headerBounds.Rows,
	headerBounds.Columns
);
CursesWindow body = screen.CreateWindow(
	bodyBounds.Row,
	bodyBounds.Column,
	bodyBounds.Rows,
	bodyBounds.Columns
);
using CursesPanel panel = screen.CreatePanel(
	panelBounds.Row,
	panelBounds.Column,
	panelBounds.Rows,
	panelBounds.Columns
);
header.WrapMode = CursesWrapMode.Clip;
body.WrapMode = CursesWrapMode.Clip;
panel.ContentWindow.WrapMode = CursesWrapMode.Clip;

bool running = true;
while ( running ) {
	Draw(
		screen,
		header,
		body,
		panel
	);
	await session.RefreshAsync();

	CursesEvent current = await session.ReadEventAsync();
	if ( CursesEventKind.Input == current.Kind
		&& current.Input is not null ) {
		CursesInputEvent input = current.Input;
		if ( CursesInputEventKind.Key == input.Kind
			&& input.Key is not null
			&& ( input.Key.Key is CursesKey.Escape
				|| input.Key.Text?.Equals(
					"q",
					StringComparison.OrdinalIgnoreCase
				) == true ) ) {
			running = false;
			continue;
		}
	}
	if ( CursesEventKind.Lifecycle == current.Kind
		&& current.Lifecycle is not null ) {
		if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
			break;
		}
	}
	if ( !current.RequiresRepaint ) {
		continue;
	}

	_ = session.SynchronizeDimensions();
	if ( screen.Columns < MinimumColumns
		|| screen.Rows < MinimumRows ) {
		panel.Hide();
		standard.Clear();
		WriteLine(
			standard,
			0,
			$"Too small: {screen.Columns} x {screen.Rows}; need {MinimumColumns} x {MinimumRows}."
		);
		session.Invalidate();
		continue;
	}

	ComputeLayout(
		screen.Bounds,
		out headerBounds,
		out bodyBounds,
		out panelBounds
	);
	header.SetBounds( headerBounds );
	body.SetBounds( bodyBounds );
	panel.SetBounds( panelBounds );
	panel.Show();
	session.Invalidate();
}

return 0;

static void ComputeLayout(
	CursesRectangle bounds,
	out CursesRectangle header,
	out CursesRectangle body,
	out CursesRectangle panel
) {
	CursesLayout.Dock(
		bounds,
		CursesDockEdge.Top,
		1,
		out header,
		out CursesRectangle content
	);
	int panelColumns = Math.Min(
		20,
		Math.Max(
			4,
			content.Columns / 3
		)
	);
	CursesLayout.SplitRight(
		content,
		panelColumns,
		out body,
		out panel
	);
}

static void Draw(
	CursesScreen screen,
	CursesWindow header,
	CursesWindow body,
	CursesPanel panel
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( header );
	ArgumentNullException.ThrowIfNull( body );
	ArgumentNullException.ThrowIfNull( panel );

	header.Clear();
	body.Clear();
	panel.ContentWindow.Clear();
	WriteLine(
		header,
		0,
		$"Icod.DCurses 1.3 explicit layout sample — {screen.Columns} x {screen.Rows}"
	);
	WriteLine(
		body,
		0,
		"Resize the terminal: the application recomputes rectangles explicitly."
	);
	WriteLine(
		body,
		2,
		"The screen owns no retained layout tree."
	);
	WriteLine(
		body,
		4,
		"Q / Escape exits."
	);
	WriteLine(
		panel.ContentWindow,
		0,
		"Retained panel"
	);
	WriteLine(
		panel.ContentWindow,
		2,
		$"{panel.Rows} x {panel.Columns}"
	);
}

static void WriteLine(
	CursesWindow window,
	int row,
	string text
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( text );
	if ( row < 0
		|| row >= window.Rows ) {
		return;
	}

	window.Move(
		row,
		0
	);
	window.Write( text );
}
