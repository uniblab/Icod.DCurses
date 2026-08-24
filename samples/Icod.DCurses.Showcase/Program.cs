using System.Text;
using Icod.DCurses;

string[] spinnerFrames = [
	"|",
	"/",
	"-",
	"\\"
];

await using CursesSession session = await CursesSession.OpenAsync();

CursesWindow screen = session.StandardScreen;
CursesStyle titleStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);

int spinnerIndex = 0;
int markerRow = 9;
int markerColumn = 2;
CursesCursorVisibility cursorVisibility = CursesCursorVisibility.Hidden;
string status = "Ready.";
bool running = true;
bool immediateRefresh = true;

while ( running ) {
	ClampMarker(
		screen,
		ref markerRow,
		ref markerColumn
	);

	DrawShowcase(
		screen,
		titleStyle,
		spinnerFrames[ spinnerIndex ],
		markerRow,
		markerColumn,
		cursorVisibility,
		status
	);

	await session.RefreshAsync();

	CursesEvent input = await session.ReadEventAsync(
		immediateRefresh
			? TimeSpan.Zero
			: TimeSpan.FromMilliseconds( 150 )
	);
	immediateRefresh = false;

	switch ( input.Kind ) {
		case CursesEventKind.Timeout:
			spinnerIndex = ( spinnerIndex + 1 ) % spinnerFrames.Length;
			break;

		case CursesEventKind.Lifecycle:
			if ( null == input.Lifecycle ) {
				break;
			}

			if ( input.RequiresRepaint ) {
				session.Invalidate();
			}

			switch ( input.Lifecycle.Kind ) {
				case CursesLifecycleEventKind.Resize:
					status = $"Resize observed: {screen.Columns} x {screen.Rows}.";
					break;

				case CursesLifecycleEventKind.Resumed:
					status = "Presentation resumed; physical screen invalidated.";
					break;

				case CursesLifecycleEventKind.Suspending:
					status = "Presentation is suspending.";
					break;

				case CursesLifecycleEventKind.Interrupt:
				case CursesLifecycleEventKind.Termination:
					running = false;
					break;
			}
			break;

		case CursesEventKind.Input:
			if ( null == input.Input ) {
				break;
			}

			if ( CursesInputEventKind.EndOfInput == input.Input.Kind ) {
				running = false;
				break;
			}

			switch ( input.Input.Key ) {
				case CursesKey.Up:
					markerRow--;
					status = "Marker moved up.";
					break;

				case CursesKey.Down:
					markerRow++;
					status = "Marker moved down.";
					break;

				case CursesKey.Left:
					markerColumn--;
					status = "Marker moved left.";
					break;

				case CursesKey.Right:
					markerColumn++;
					status = "Marker moved right.";
					break;

				case CursesKey.Space:
					status = "Immediate refresh requested.";
					immediateRefresh = true;
					break;

				case CursesKey.Escape:
					running = false;
					break;
			}

			if ( CursesInputEventKind.Text == input.Input.Kind
				&& input.Input.Character.HasValue ) {
				Rune character = input.Input.Character.Value;

				switch ( character.Value ) {
					case 'b':
					case 'B':
						bool alerted = await session.AlertAsync();
						status = alerted
							? "Alert capability emitted."
							: "No audible or visual alert capability is available."
						;
						break;

					case 'c':
					case 'C':
						cursorVisibility = NextCursorVisibility( cursorVisibility );

						bool cursorChanged = await session.SetCursorVisibilityAsync(
							cursorVisibility
						);

						status = cursorChanged
							? $"Cursor visibility: {cursorVisibility}."
							: $"Cursor visibility '{cursorVisibility}' is unavailable."
						;
						break;

					case 'i':
					case 'I':
						session.Invalidate();
						status = "Retained physical-screen knowledge invalidated.";
						break;

					case 'q':
					case 'Q':
						running = false;
						break;
				}
			}
			break;
	}
}

return 0;

static void DrawShowcase(
	CursesWindow screen,
	CursesStyle titleStyle,
	string spinner,
	int markerRow,
	int markerColumn,
	CursesCursorVisibility cursorVisibility,
	string status ) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( spinner );
	ArgumentNullException.ThrowIfNull( status );

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	WriteLine(
		screen,
		0,
		"Icod.DCurses interactive showcase",
		titleStyle
	);
	WriteLine(
		screen,
		1,
		$"Terminal: {screen.Columns} x {screen.Rows}   Spinner: {spinner}"
	);
	WriteLine(
		screen,
		2,
		"Unicode: ASCII | café | e\u0301 | \U0001D11E | 界"
	);
	WriteLine(
		screen,
		4,
		"Arrows move @ | B alert | C cursor | I invalidate"
	);
	WriteLine(
		screen,
		5,
		"Space immediate refresh | Q or Escape exit"
	);
	WriteLine(
		screen,
		6,
		$"Requested cursor visibility: {cursorVisibility}"
	);

	if ( HasMarkerArea( screen ) ) {
		screen.Move(
			markerRow,
			markerColumn
		);
		screen.Write( "@" );
	} else {
		WriteLine(
			screen,
			8,
			"Enlarge the terminal to display the movable marker."
		);
	}

	WriteLine(
		screen,
		screen.Rows - 1,
		$"Status: {status}"
	);
}

static void WriteLine(
	CursesWindow screen,
	int row,
	string text,
	CursesStyle style = default ) {
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

static bool HasMarkerArea( CursesWindow screen ) {
	ArgumentNullException.ThrowIfNull( screen );

	return screen.Rows >= 11
		&& 0 < screen.Columns;
}

static void ClampMarker(
	CursesWindow screen,
	ref int row,
	ref int column ) {
	ArgumentNullException.ThrowIfNull( screen );

	if ( !HasMarkerArea( screen ) ) {
		row = 0;
		column = 0;
		return;
	}

	row = Math.Clamp(
		row,
		9,
		screen.Rows - 2
	);
	column = Math.Clamp(
		column,
		0,
		screen.Columns - 1
	);
}

static CursesCursorVisibility NextCursorVisibility(
	CursesCursorVisibility current ) {
	return current switch {
		CursesCursorVisibility.Hidden => CursesCursorVisibility.Normal,
		CursesCursorVisibility.Normal => CursesCursorVisibility.VeryVisible,
		CursesCursorVisibility.VeryVisible => CursesCursorVisibility.Hidden,
		_ => throw new ArgumentOutOfRangeException(
			nameof( current )
		)
	};
}
