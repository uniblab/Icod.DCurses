using Icod.DCurses;
using Icod.Terminal;
using Icod.TermInfo;

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

VerifyApprovedDependencySurface();
VerifyInputSemanticSurface();
VerifyUnicodeWidthSurface();
VerifyColumnTextSurface();

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

static void VerifyApprovedDependencySurface() {
	Type[] approvedDependencyTypes = [
		typeof( TerminalSession ),
		typeof( TerminalEndpoint ),
		typeof( TerminalControlResult<TerminalSize> ),
		typeof( TerminalDescription ),
		typeof( TerminalSize )
	];

	if ( 5 != approvedDependencyTypes.Length ) {
		throw new InvalidOperationException(
			"DCurses package-only dependency-surface smoke validation failed."
		);
	}
}

static void VerifyInputSemanticSurface() {
	if ( 17 != (int)CursesKey.Function
		|| 1 != (int)CursesKeyModifiers.Shift
		|| 2 != (int)CursesKeyModifiers.Control
		|| 4 != (int)CursesKeyModifiers.Alt ) {
		throw new InvalidOperationException(
			"DCurses 0.1 input enum compatibility values changed."
		);
	}

	CursesInputProtocolOptions options = new() {
		KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
	};
	if ( CursesKeyboardReportingMode.EventTypes != options.KeyboardReportingMode ) {
		throw new InvalidOperationException(
			"DCurses package-only keyboard-reporting surface is unavailable."
		);
	}

	string[] requiredInputProperties = [
		nameof( CursesInputEvent.KeyPhase ),
		nameof( CursesInputEvent.ShiftedCharacter ),
		nameof( CursesInputEvent.BaseLayoutCharacter ),
		nameof( CursesInputEvent.AssociatedText )
	];
	foreach ( string propertyName in requiredInputProperties ) {
		if ( null == typeof( CursesInputEvent ).GetProperty( propertyName ) ) {
			throw new InvalidOperationException(
				$"DCurses package-only input surface is missing {propertyName}."
			);
		}
	}

	if ( null == typeof( CursesInputProtocolLease ).GetProperty(
		nameof( CursesInputProtocolLease.KeyboardReportingMode )
	) ) {
		throw new InvalidOperationException(
			"DCurses package-only protocol lease is missing KeyboardReportingMode."
		);
	}

	_ = CursesKey.Unrecognized;
	_ = CursesKey.MediaPlayPause;
	_ = CursesKey.KeypadEnter;
	_ = CursesKeyEventPhase.Release;
	_ = CursesKeyModifiers.Super
		| CursesKeyModifiers.Hyper
		| CursesKeyModifiers.Meta
		| CursesKeyModifiers.CapsLock
		| CursesKeyModifiers.NumLock;
}

static void VerifyUnicodeWidthSurface() {
	if ( "17.0.0" != UnicodeCursesTextWidthProvider.UnicodeDataVersion ) {
		throw new InvalidOperationException(
			"DCurses package-only Unicode width data version is not 17.0.0."
		);
	}

	UnicodeCursesTextWidthProvider narrow = UnicodeCursesTextWidthProvider.Instance;
	UnicodeCursesTextWidthProvider wide =
		UnicodeCursesTextWidthProvider.WideAmbiguousInstance;
	if ( CursesAmbiguousWidthPolicy.Narrow != narrow.AmbiguousWidthPolicy
		|| CursesAmbiguousWidthPolicy.Wide != wide.AmbiguousWidthPolicy
		|| 1 != narrow.GetWidth( "\u03A9" )
		|| 2 != wide.GetWidth( "\u03A9" )
		|| 2 != narrow.GetWidth( "\U00016FF2" ) ) {
		throw new InvalidOperationException(
			"DCurses package-only Unicode width policy surface failed validation."
		);
	}
}

static void VerifyColumnTextSurface() {
	const string text = "A\u754CB";
	if ( 4 != CursesText.MeasureColumns( text )
		|| "A" != CursesText.TruncateToColumns(
			text,
			2
		)
		|| "\u754CB" != CursesText.SliceByColumns(
			text,
			1,
			3
		) ) {
		throw new InvalidOperationException(
			"DCurses package-only column-text surface failed validation."
		);
	}

	if ( 2 != CursesText.MeasureColumns(
		"\u03A9",
		UnicodeCursesTextWidthProvider.WideAmbiguousInstance
	) ) {
		throw new InvalidOperationException(
			"DCurses package-only column helpers did not honor the supplied width provider."
		);
	}
}

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
