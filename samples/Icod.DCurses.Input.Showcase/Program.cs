using System.Text;
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

CursesStyle titleStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);

List<CursesInputProtocolLease> protocolLeases = [];
List<string> protocolStatus = [];
List<string> recentEvents = [];

await TryAcquireInputProtocolAsync(
	session,
	"Bracketed paste",
	new CursesInputProtocolOptions {
		BracketedPaste = true
	},
	protocolLeases,
	protocolStatus
);
await TryAcquireInputProtocolAsync(
	session,
	"Focus reporting",
	new CursesInputProtocolOptions {
		FocusReporting = true
	},
	protocolLeases,
	protocolStatus
);
await TryAcquireInputProtocolAsync(
	session,
	"Mouse buttons",
	new CursesInputProtocolOptions {
		MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents
	},
	protocolLeases,
	protocolStatus
);

long eventNumber = 0;
string lastKind = "None";
string lastDetail = "-";
string lastKey = "-";
string lastCharacter = "-";
string lastModifiers = "-";
string lastFunctionKey = "-";
string lastLifecycle = "-";

DrawInspector(
	screen,
	titleStyle,
	protocolStatus,
	lastKind,
	lastDetail,
	lastKey,
	lastCharacter,
	lastModifiers,
	lastFunctionKey,
	lastLifecycle,
	recentEvents
);
await session.RefreshAsync();

try {
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
				lastDetail = DescribeInput( input );
				lastKey = input.Kind is CursesInputEventKind.Text or CursesInputEventKind.Key
					? input.Key.ToString()
					: "-"
				;
				lastCharacter = input.Character.HasValue
					? FormatRune( input.Character.Value )
					: "-"
				;
				lastModifiers = CursesKeyModifiers.None == input.Modifiers
					? "-"
					: input.Modifiers.ToString()
				;
				lastFunctionKey = input.FunctionKeyNumber.HasValue
					? $"F{input.FunctionKeyNumber.Value}"
					: "-"
				;
				lastLifecycle = "-";

				AddRecentEvent(
					recentEvents,
					$"{eventNumber:D4}  {lastDetail}"
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
				lastDetail = $"Lifecycle {lifecycle.Kind}";
				lastKey = "-";
				lastCharacter = "-";
				lastModifiers = "-";
				lastFunctionKey = "-";
				lastLifecycle = lifecycle.Kind.ToString();

				AddRecentEvent(
					recentEvents,
					$"{eventNumber:D4}  {lastDetail}"
				);

				if ( lifecycle.Kind is CursesLifecycleEventKind.Interrupt
					or CursesLifecycleEventKind.Termination ) {
					running = false;
				}
				break;

			case CursesEventKind.Timeout:
				lastKind = "Timeout";
				lastDetail = "Timeout";
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
			protocolStatus,
			lastKind,
			lastDetail,
			lastKey,
			lastCharacter,
			lastModifiers,
			lastFunctionKey,
			lastLifecycle,
			recentEvents
		);
		await session.RefreshAsync();
	}
} finally {
	for ( int index = protocolLeases.Count - 1; 0 <= index; index-- ) {
		await protocolLeases[ index ].DisposeAsync();
	}
}

return 0;

static async ValueTask TryAcquireInputProtocolAsync(
	CursesSession session,
	string name,
	CursesInputProtocolOptions options,
	ICollection<CursesInputProtocolLease> leases,
	ICollection<string> status
) {
	ArgumentNullException.ThrowIfNull( session );
	ArgumentException.ThrowIfNullOrWhiteSpace( name );
	ArgumentNullException.ThrowIfNull( options );
	ArgumentNullException.ThrowIfNull( leases );
	ArgumentNullException.ThrowIfNull( status );

	var result = await session.AcquireInputProtocolsAsync( options );
	if ( result.IsAvailable ) {
		leases.Add( result.GetRequiredValue() );
		status.Add( $"{name}: enabled" );
		return;
	}

	status.Add( $"{name}: {result.Status}" );
}

static void DrawInspector(
	CursesWindow screen,
	CursesStyle titleStyle,
	IReadOnlyList<string> protocolStatus,
	string lastKind,
	string lastDetail,
	string lastKey,
	string lastCharacter,
	string lastModifiers,
	string lastFunctionKey,
	string lastLifecycle,
	IReadOnlyList<string> recentEvents
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( protocolStatus );
	ArgumentNullException.ThrowIfNull( lastKind );
	ArgumentNullException.ThrowIfNull( lastDetail );
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
		"Icod.DCurses rich-input showcase",
		titleStyle
	);
	WriteLine(
		screen,
		1,
		$"Terminal: {screen.Columns} x {screen.Rows}"
	);

	for ( int index = 0; index < protocolStatus.Count; index++ ) {
		WriteLine(
			screen,
			2 + index,
			protocolStatus[ index ]
		);
	}

	WriteLine(
		screen,
		6,
		"Try modified keys, paste, mouse clicks/wheel, focus changes, and resize."
	);
	WriteLine(
		screen,
		7,
		"Events come through CursesSession.ReadEventAsync. Q exits."
	);
	WriteLine(
		screen,
		9,
		"Last decoded event",
		titleStyle
	);
	WriteLine(
		screen,
		10,
		$"Kind:         {lastKind}"
	);
	WriteLine(
		screen,
		11,
		$"Detail:       {lastDetail}"
	);
	WriteLine(
		screen,
		12,
		$"Key:          {lastKey}"
	);
	WriteLine(
		screen,
		13,
		$"Character:    {lastCharacter}"
	);
	WriteLine(
		screen,
		14,
		$"Modifiers:    {lastModifiers}"
	);
	WriteLine(
		screen,
		15,
		$"Function key: {lastFunctionKey}"
	);
	WriteLine(
		screen,
		16,
		$"Lifecycle:    {lastLifecycle}"
	);
	WriteLine(
		screen,
		18,
		"Recent events",
		titleStyle
	);

	int availableRows = Math.Max(
		0,
		screen.Rows - 20
	);
	int count = Math.Min(
		availableRows,
		recentEvents.Count
	);

	for ( int offset = 0; offset < count; offset++ ) {
		int sourceIndex = recentEvents.Count - count + offset;
		WriteLine(
			screen,
			19 + offset,
			recentEvents[ sourceIndex ]
		);
	}
}

static string DescribeInput(
	CursesInputEvent input
) {
	ArgumentNullException.ThrowIfNull( input );

	switch ( input.Kind ) {
		case CursesInputEventKind.Text:
			return input.Character.HasValue
				? $"Text {FormatRune( input.Character.Value )}"
				: "Text"
			;

		case CursesInputEventKind.Key:
			return DescribeKey( input );

		case CursesInputEventKind.Mouse:
			return DescribeMouse(
				input.Mouse
					?? throw new InvalidOperationException(
						"A curses mouse event is missing its payload."
					)
			);

		case CursesInputEventKind.Focus:
			return DescribeFocus(
				input.Focus
					?? throw new InvalidOperationException(
						"A curses focus event is missing its payload."
					)
			);

		case CursesInputEventKind.Paste:
			return DescribePaste(
				input.Paste
					?? throw new InvalidOperationException(
						"A curses paste event is missing its payload."
					)
			);

		case CursesInputEventKind.EndOfInput:
			return "Input EndOfInput";

		default:
			throw new ArgumentOutOfRangeException(
				nameof( input ),
				input.Kind,
				"The curses input-event kind is not recognized."
			);
	}
}

static string DescribeKey(
	CursesInputEvent input
) {
	ArgumentNullException.ThrowIfNull( input );

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

static string DescribeMouse(
	CursesMouseEvent mouse
) {
	ArgumentNullException.ThrowIfNull( mouse );

	string modifiers = FormatModifiers( mouse.Modifiers );
	string button = CursesMouseButton.None == mouse.Button
		? string.Empty
		: $" {mouse.Button}"
	;
	return $"{modifiers}Mouse {mouse.Action}{button} @ ({mouse.Column},{mouse.Row})";
}

static string DescribeFocus(
	CursesFocusEvent focus
) {
	ArgumentNullException.ThrowIfNull( focus );
	return $"Focus {focus.State}";
}

static string DescribePaste(
	CursesPasteEvent paste
) {
	ArgumentNullException.ThrowIfNull( paste );

	return paste.Phase switch {
		CursesPastePhase.Begin => "Paste Begin",
		CursesPastePhase.Data => $"Paste Data ({paste.Text!.Length} UTF-16 code units)",
		CursesPastePhase.End => "Paste End",
		_ => throw new ArgumentOutOfRangeException(
			nameof( paste ),
			paste.Phase,
			"The curses paste phase is not recognized."
		)
	};
}

static string FormatModifiers(
	CursesKeyModifiers modifiers
) {
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

static string FormatRune(
	Rune rune
) {
	return $"U+{rune.Value:X4} '{rune}'";
}

static void AddRecentEvent(
	List<string> recentEvents,
	string description
) {
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
