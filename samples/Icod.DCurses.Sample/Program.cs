using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

DrawQuickStart( screen );
await session.RefreshAsync();

while ( true ) {
	CursesEvent input = await session.ReadEventAsync();

	if ( input.RequiresRepaint ) {
		session.Invalidate();
		DrawQuickStart( screen );
		await session.RefreshAsync();
		continue;
	}

	if ( CursesEventKind.Lifecycle == input.Kind
		&& null != input.Lifecycle
		&& ( CursesLifecycleEventKind.Interrupt == input.Lifecycle.Kind
			|| CursesLifecycleEventKind.Termination == input.Lifecycle.Kind ) ) {
		break;
	}

	if ( CursesEventKind.Input == input.Kind
		&& null != input.Input ) {
		break;
	}
}

return 0;

static void DrawQuickStart( CursesWindow screen ) {
	ArgumentNullException.ThrowIfNull( screen );

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	WriteLine(
		screen,
		0,
		"Icod.DCurses quick start"
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
}

static void WriteLine(
	CursesWindow screen,
	int row,
	string text ) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( text );

	if ( row < 0 || row >= screen.Rows ) {
		return;
	}

	screen.Move(
		row,
		0
	);
	screen.Write( text );
}
