using Icod.DCurses;

CursesScreen logicalScreen = new(
	80,
	24
);
CursesWindow screen = logicalScreen.StandardWindow;
screen.WrapMode = CursesWrapMode.Clip;
screen.Clear();
screen.Move(
	0,
	0
);
screen.Write(
	"package-only DCurses consumer",
	new CursesStyle(
		CursesColor.Indexed( 2 ),
		CursesColor.Default,
		CursesTextAttributes.Bold
	)
);

CursesWindow status = logicalScreen.CreateWindow(
	23,
	0,
	1,
	80
);
status.Write(
	"ready",
	new CursesStyle(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Reverse
	)
);

logicalScreen.Resize(
	100,
	30
);

if ( 100 != logicalScreen.Columns
	|| 30 != logicalScreen.Rows
	|| 100 != logicalScreen.StandardWindow.Columns
	|| 30 != logicalScreen.StandardWindow.Rows ) {
	Console.Error.WriteLine(
		"DCurses package-only virtual-screen smoke validation failed."
	);
	return 1;
}

if ( string.Equals(
	Environment.GetEnvironmentVariable( "ICOD_DCURSES_SMOKE_INTERACTIVE" ),
	"1",
	StringComparison.Ordinal
) ) {
	return await RunInteractiveAsync();
}

Console.WriteLine(
	"DCurses package-only consumer compiled and executed successfully."
);
return 0;

static async Task<int> RunInteractiveAsync() {
	await using CursesSession session = await CursesSession.OpenAsync(
		new CursesSessionOptions {
			InputMode = CursesInputMode.CBreak,
			EchoInput = false,
			UseAlternateScreen = true,
			EnableKeypad = true,
			HideCursor = true
		}
	);

	CursesWindow screen = session.StandardScreen;
	screen.Clear();
	screen.Move(
		0,
		0
	);
	screen.Write(
		"Icod.DCurses package-only interactive smoke. Press any key to exit.",
		new CursesStyle(
			CursesColor.Default,
			CursesColor.Default,
			CursesTextAttributes.Bold
		)
	);
	await session.RefreshAsync();
	_ = await session.ReadEventAsync();
	return 0;
}
