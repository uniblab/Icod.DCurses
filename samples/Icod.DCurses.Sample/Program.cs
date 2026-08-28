using Icod.DCurses;

TimeSpan updateInterval = TimeSpan.FromMilliseconds( 250 );

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;
int tick = 0;
bool running = true;
bool dirty = true;

while ( running ) {
	if ( dirty ) {
		DrawQuickStart(
			screen,
			tick
		);
		await session.RefreshAsync();
		dirty = false;
	}

	CursesEvent current = await session.ReadEventAsync( updateInterval );

	if ( CursesEventKind.Timeout == current.Kind ) {
		tick++;
		dirty = true;
		continue;
	}

	if ( current.RequiresRepaint ) {
		session.Invalidate();
		dirty = true;
		continue;
	}

	if ( CursesEventKind.Lifecycle == current.Kind
		&& null != current.Lifecycle
		&& current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
		running = false;
		continue;
	}

	if ( CursesEventKind.Input == current.Kind
		&& null != current.Input ) {
		running = false;
	}
}

return 0;

static void DrawQuickStart(
	CursesWindow screen,
	int tick
) {
	ArgumentNullException.ThrowIfNull( screen );

	CursesStyle titleStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Bold
	);
	CursesStyle markerStyle = new(
		CursesColor.Indexed( 2 ),
		CursesColor.Default,
		CursesTextAttributes.Reverse
	);

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	WriteLine(
		screen,
		0,
		"Icod.DCurses quick start",
		titleStyle
	);
	WriteLine(
		screen,
		2,
		"This text is drawn through CursesSession.StandardScreen."
	);
	WriteLine(
		screen,
		3,
		"Resize the terminal to exercise repaint handling."
	);
	WriteLine(
		screen,
		5,
		"Press any key to exit."
	);

	if ( 7 >= screen.Rows || 0 >= screen.Columns ) {
		return;
	}

	int availableColumns = Math.Max(
		1,
		screen.Columns - 2
	);
	int markerColumn = tick % availableColumns;
	screen.Move(
		7,
		markerColumn
	);
	screen.Write(
		"@",
		markerStyle
	);
}

static void WriteLine(
	CursesWindow screen,
	int row,
	string text,
	CursesStyle style = default
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( text );

	if ( row < 0 || row >= screen.Rows ) {
		return;
	}

	screen.Move(
		row,
		0
	);
	screen.Write(
		text,
		style
	);
}
