using System.Text;
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

CursesStyle titleStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);

List<string> recentEvents = [];
long eventNumber = 0;

string lastKind = "None";
string lastKey = "-";
string lastCharacter = "-";
string lastModifiers = "-";
string lastFunctionKey = "-";
string lastLifecycle = "-";

DrawInspector(
	screen,
	titleStyle,
	lastKind,
	lastKey,
	lastCharacter,
	lastModifiers,
	lastFunctionKey,
	lastLifecycle,
	recentEvents
);
await session.RefreshAsync();

bool running = true;
while ( running ) {
	CursesEvent current = await session.ReadEventAsync();

	if ( current.RequiresRepaint ) {
		session.Invalidate();
	}

	eventNumber++;

	switch ( current.Kind ) {
		case CursesEventKind.Input:
			if ( null == current.Input ) {
				break;
			}

			CursesInputEvent input = current.Input;
			lastKind = $"Input / {input.Kind}";
			lastKey = input.Key.ToString();
			lastCharacter = input.Character.HasValue
				? FormatRune( input.Character.Value )
				: "-"
			;
			lastModifiers = input.Modifiers.ToString();
			lastFunctionKey = input.FunctionKeyNumber.HasValue
				? $"F{input.FunctionKeyNumber.Value}"
				: "-"
			;
			lastLifecycle = "-";

			AddRecentEvent(
				recentEvents,
				$"{eventNumber:D4}  {DescribeInput( input )}"
			);

			if ( CursesInputEventKind.EndOfInput == input.Kind ) {
				running = false;
				break;
			}

			if ( CursesInputEventKind.Text == input.Kind
				&& input.Character.HasValue
				&& input.Character.Value.Value is 'q' or 'Q' ) {
				running = false;
			}
			break;

		case CursesEventKind.Lifecycle:
			if ( null == current.Lifecycle ) {
				break;
			}

			CursesLifecycleEvent lifecycle = current.Lifecycle;
			lastKind = "Lifecycle";
			lastKey = "-";
			lastCharacter = "-";
			lastModifiers = "-";
			lastFunctionKey = "-";
			lastLifecycle = lifecycle.Kind.ToString();

			AddRecentEvent(
				recentEvents,
				$"{eventNumber:D4}  Lifecycle {lifecycle.Kind}"
			);

			if ( lifecycle.Kind is CursesLifecycleEventKind.Interrupt
				or CursesLifecycleEventKind.Termination ) {
				running = false;
			}
			break;

		case CursesEventKind.Timeout:
			lastKind = "Timeout";
			lastKey = "-";
			lastCharacter = "-";
			lastModifiers = "-";
			lastFunctionKey = "-";
			lastLifecycle = "-";

			AddRecentEvent(
				recentEvents,
				$"{eventNumber:D4}  Timeout"
			);
			break;
	}

	if ( !running ) {
		break;
	}

	DrawInspector(
		screen,
		titleStyle,
		lastKind,
		lastKey,
		lastCharacter,
		lastModifiers,
		lastFunctionKey,
		lastLifecycle,
		recentEvents
	);
	await session.RefreshAsync();
}

return 0;

static void DrawInspector(
	CursesWindow screen,
	CursesStyle titleStyle,
	string lastKind,
	string lastKey,
	string lastCharacter,
	string lastModifiers,
	string lastFunctionKey,
	string lastLifecycle,
	IReadOnlyList<string> recentEvents ) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( lastKind );
	ArgumentNullException.ThrowIfNull( lastKey );
	ArgumentNullException.ThrowIfNull( lastCharacter );
	ArgumentNullException.ThrowIfNull( lastModifiers );
	ArgumentNullException.ThrowIfNull( lastFunctionKey );
	ArgumentNullException.ThrowIfNull( lastLifecycle );
	ArgumentNullException.ThrowIfNull( recentEvents );

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	WriteLine(
		screen,
		0,
		"Icod.DCurses input showcase",
		titleStyle
	);
	WriteLine(
		screen,
		1,
		$"Terminal: {screen.Columns} x {screen.Rows}"
	);
	WriteLine(
		screen,
		3,
		"Try Shift+Tab, Ctrl+R, F7, Shift+F7, Alt+R, and Escape."
	);
	WriteLine(
		screen,
		4,
		"Events are shown exactly as DCurses decoded them. Q exits."
	);
	WriteLine(
		screen,
		6,
		"Last decoded event",
		titleStyle
	);
	WriteLine(
		screen,
		7,
		$"Kind:         {lastKind}"
	);
	WriteLine(
		screen,
		8,
		$"Key:          {lastKey}"
	);
	WriteLine(
		screen,
		9,
		$"Character:    {lastCharacter}"
	);
	WriteLine(
		screen,
		10,
		$"Modifiers:    {lastModifiers}"
	);
	WriteLine(
		screen,
		11,
		$"Function key: {lastFunctionKey}"
	);
	WriteLine(
		screen,
		12,
		$"Lifecycle:    {lastLifecycle}"
	);
	WriteLine(
		screen,
		14,
		"Recent events",
		titleStyle
	);

	int availableRows = Math.Max(
		0,
		screen.Rows - 16
	);
	int count = Math.Min(
		availableRows,
		recentEvents.Count
	);

	for ( int offset = 0; offset < count; offset++ ) {
		int sourceIndex = recentEvents.Count - count + offset;
		WriteLine(
			screen,
			15 + offset,
			recentEvents[ sourceIndex ]
		);
	}
}

static string DescribeInput( CursesInputEvent input ) {
	ArgumentNullException.ThrowIfNull( input );

	if ( CursesInputEventKind.EndOfInput == input.Kind ) {
		return "Input EndOfInput";
	}

	if ( CursesInputEventKind.Text == input.Kind ) {
		return input.Character.HasValue
			? $"Text {FormatRune( input.Character.Value )}"
			: "Text"
		;
	}

	string modifiers = FormatModifiers( input.Modifiers );

	if ( CursesKey.Function == input.Key
		&& input.FunctionKeyNumber.HasValue ) {
		return $"{modifiers}F{input.FunctionKeyNumber.Value}";
	}

	if ( CursesKey.Character == input.Key
		&& input.Character.HasValue ) {
		return $"{modifiers}Character {FormatRune( input.Character.Value )}";
	}

	return $"{modifiers}{input.Key}";
}

static string FormatModifiers( CursesKeyModifiers modifiers ) {
	if ( CursesKeyModifiers.None == modifiers ) {
		return string.Empty;
	}

	return modifiers
		.ToString()
		.Replace(
			", ",
			"+",
			StringComparison.Ordinal
		)
		+ "+";
}

static string FormatRune( Rune rune ) {
	return $"U+{rune.Value:X4} '{rune}'";
}

static void AddRecentEvent(
	List<string> recentEvents,
	string description ) {
	ArgumentNullException.ThrowIfNull( recentEvents );
	ArgumentNullException.ThrowIfNull( description );

	recentEvents.Add( description );

	while ( recentEvents.Count > 32 ) {
		recentEvents.RemoveAt( 0 );
	}
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
