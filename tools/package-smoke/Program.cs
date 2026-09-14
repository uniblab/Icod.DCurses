using System.Reflection;
using System.Text;
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
VerifySemanticMetadataSurface();
VerifyInputSemanticSurface();
VerifyUnicodeWidthSurface();
VerifyColumnTextSurface();
VerifyWindowEditingSurface();
VerifyPadSurface();
VerifyInteractionSurface();

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

static void VerifySemanticMetadataSurface() {
	CursesHyperlink hyperlink = new(
		"https://example.test/package-smoke%2fdocs",
		"package-smoke"
	);
	if ( "https://example.test/package-smoke%2Fdocs" != hyperlink.Uri
		|| "package-smoke" != hyperlink.Identifier ) {
		throw new InvalidOperationException(
			"DCurses package-only hyperlink value surface failed validation."
		);
	}

	CursesCellMetadata metadata = new( hyperlink );
	if ( hyperlink != metadata.Hyperlink ) {
		throw new InvalidOperationException(
			"DCurses package-only semantic metadata surface failed validation."
		);
	}

	CursesScreen logical = new(
		20,
		2
	);
	CursesWindow window = logical.StandardWindow;
	window.Move(
		0,
		1
	);
	window.WriteWithMetadata(
		"docs",
		metadata
	);
	if ( metadata != window.GetMetadata( 0, 1 )
		|| metadata != logical.VirtualScreen.GetMetadata( 0, 4 ) ) {
		throw new InvalidOperationException(
			"DCurses package-only metadata-aware write surface failed validation."
		);
	}

	window.SetMetadata(
		0,
		2,
		null
	);
	if ( null != window.GetMetadata( 0, 2 ) ) {
		throw new InvalidOperationException(
			"DCurses package-only semantic metadata removal surface failed validation."
		);
	}

	window.SetMetadata(
		0,
		2,
		metadata
	);
	if ( metadata != window.GetMetadata( 0, 2 ) ) {
		throw new InvalidOperationException(
			"DCurses package-only semantic metadata mutation surface failed validation."
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
		|| 2 != narrow.GetWidth( "\U00016FF2" )
		|| 2 != narrow.GetWidth( "\u00A9\uFE0F" )
		|| 1 != narrow.GetWidth( "\u2605\uFE0F" ) ) {
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

static void VerifyWindowEditingSurface() {
	CursesScreen logical = new(
		16,
		8
	);
	CursesWindow source = logical.CreateWindow(
		1,
		1,
		4,
		8
	);
	source.Reposition(
		2,
		2
	);
	source.FillRectangle(
		0,
		0,
		4,
		8,
		CursesCell.Blank()
	);
	source.Move(
		0,
		0
	);
	source.Write( "A\u754CBC" );
	if ( "A" != source.GetCell( 0, 0 ).Content ) {
		throw new InvalidOperationException(
			"DCurses package-only window inspection surface failed validation."
		);
	}

	source.Move(
		0,
		1
	);
	source.InsertCells();
	source.DeleteCells();
	source.Move(
		1,
		0
	);
	source.InsertLines();
	source.DeleteLines();
	source.ClearToBeginningOfLine();

	CursesWindow destination = logical.CreateWindow(
		1,
		1,
		4,
		8
	);
	source.CopyRectangleTo(
		destination,
		0,
		0,
		2,
		4,
		0,
		0
	);
	source.OverlayRectangleTo(
		destination,
		0,
		0,
		2,
		4,
		1,
		2
	);
	destination.DrawHorizontalLine(
		2,
		1,
		5,
		new CursesCell( "-" )
	);
	destination.DrawVerticalLine(
		0,
		7,
		4,
		new CursesCell( "|" )
	);
	destination.DrawBorder(
		new CursesCell( "-" ),
		new CursesCell( "|" ),
		new CursesCell( "+" ),
		new CursesCell( "+" ),
		new CursesCell( "+" ),
		new CursesCell( "+" )
	);
	destination.TouchRegion(
		1,
		1,
		2,
		3
	);
	if ( !destination.IsRegionTouched( 1, 1, 2, 3 ) ) {
		throw new InvalidOperationException(
			"DCurses package-only damage-range surface failed validation."
		);
	}
}

static void VerifyPadSurface() {
	CursesPad pad = new(
		40,
		20
	);
	CursesWindow derived = pad.ContentWindow.CreateSubwindow(
		2,
		3,
		6,
		12
	);
	derived.Move(
		1,
		1
	);
	derived.Write( "A\u754CB" );

	CursesScreen destination = new(
		8,
		3
	);
	pad.PresentTo(
		destination.StandardWindow,
		3,
		4,
		1,
		4,
		0,
		0
	);
	if ( "A" != destination.StandardWindow.GetCell( 0, 0 ).Content
		|| "\u754C" != destination.StandardWindow.GetCell( 0, 1 ).Content
		|| !destination.StandardWindow.GetCell( 0, 2 ).IsContinuation
		|| "B" != destination.StandardWindow.GetCell( 0, 3 ).Content ) {
		throw new InvalidOperationException(
			"DCurses package-only pad presentation surface failed validation."
		);
	}

	CursesPadViewport viewport = pad.CreateViewport(
		destination.StandardWindow,
		2,
		3,
		3,
		8,
		0,
		0
	);
	if ( !viewport.HasVisiblePadChanges ) {
		throw new InvalidOperationException(
			"A fresh package-only pad viewport must require presentation."
		);
	}
	viewport.Present();
	if ( viewport.HasVisiblePadChanges ) {
		throw new InvalidOperationException(
			"A presented package-only pad viewport remained changed."
		);
	}

	pad.ContentWindow.TouchRegion(
		2,
		3,
		1,
		1
	);
	if ( !viewport.HasVisiblePadChanges ) {
		throw new InvalidOperationException(
			"Package-only pad damage was not observed by the viewport."
		);
	}
	viewport.Present();
	viewport.PanBy(
		1,
		1
	);
	if ( !viewport.HasVisiblePadChanges ) {
		throw new InvalidOperationException(
			"Package-only pad panning did not invalidate the viewport."
		);
	}
	viewport.Present();
}

static void VerifyInteractionSurface() {
	CursesScreen logical = new(
		20,
		8
	);
	using CursesInteractionRouter router = new( logical );
	using CursesInteractionRegion first = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle(
				1,
				2,
				3,
				5
			)
		) {
			IsFocusable = true,
			TraversalOrder = 10,
			HitTestPriority = 2,
			PointerShape = CursesPointerShape.Pointer
		}
	);
	using CursesInteractionRegion second = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle(
				1,
				8,
				3,
				5
			)
		) {
			IsFocusable = true,
			TraversalOrder = 20
		}
	);

	if ( !ReferenceEquals(
		logical,
		router.Screen
	) || !router.Focus( first )
		|| !ReferenceEquals(
			first,
			router.FocusedRegion
		) ) {
		throw new InvalidOperationException(
			"DCurses package-only interaction focus surface failed validation."
		);
	}

	if ( !ReferenceEquals(
		second,
		router.MoveFocus( CursesFocusDirection.Forward )
	) || !ReferenceEquals(
		first,
		router.MoveFocus( CursesFocusDirection.Forward )
	) ) {
		throw new InvalidOperationException(
			"DCurses package-only interaction traversal surface failed validation."
		);
	}

	CursesInteractionHit hit = router.HitTest(
		2,
		3
	) ?? throw new InvalidOperationException(
		"DCurses package-only interaction hit-test surface returned no hit."
	);
	if ( !ReferenceEquals(
		first,
		hit.Region
	) || 1 != hit.LocalRow
		|| 1 != hit.LocalColumn
		|| CursesPointerShape.Pointer != hit.PointerShape ) {
		throw new InvalidOperationException(
			"DCurses package-only interaction hit-test surface failed validation."
		);
	}

	CursesKeyGesture localGesture = CursesKeyGesture.ForCharacter(
		new Rune( 'x' ),
		CursesKeyModifiers.Control
	);
	CursesCommand localCommand = new( "package-smoke.local" );
	first.BindGesture(
		localGesture,
		localCommand
	);
	if ( !first.UnbindGesture( localGesture ) ) {
		throw new InvalidOperationException(
			"DCurses package-only local gesture-binding surface failed validation."
		);
	}

	CursesKeyGesture globalGesture = CursesKeyGesture.ForFunctionKey( 2 );
	CursesCommand globalCommand = new( "package-smoke.global" );
	router.BindGlobalGesture(
		globalGesture,
		globalCommand
	);
	if ( !router.UnbindGlobalGesture( globalGesture ) ) {
		throw new InvalidOperationException(
			"DCurses package-only global gesture-binding surface failed validation."
		);
	}

	if ( 4096 != CursesInteractionRouter.MaximumRegions
		|| 256 != CursesInteractionRouter.MaximumRegionGestureBindings
		|| 1024 != CursesInteractionRouter.MaximumGlobalGestureBindings
		|| 16384 != CursesInteractionRouter.MaximumGestureBindings
		|| 30 != Enum.GetValues<CursesPointerShape>().Length ) {
		throw new InvalidOperationException(
			"DCurses package-only interaction bounds or pointer vocabulary changed."
		);
	}

	MethodInfo? routeMethod = typeof( CursesInteractionRouter ).GetMethod(
		nameof( CursesInteractionRouter.Route ),
		[
			typeof( CursesInputEvent )
		]
	);
	if ( routeMethod is null
		|| typeof( CursesInteractionResult ) != routeMethod.ReturnType ) {
		throw new InvalidOperationException(
			"DCurses package-only routing surface is unavailable."
		);
	}

	MethodInfo? pointerMethod = typeof( CursesSession ).GetMethod(
		nameof( CursesSession.AcquirePointerShapeAsync ),
		[
			typeof( CursesPointerShape ),
			typeof( CancellationToken )
		]
	);
	if ( pointerMethod is null
		|| typeof( ValueTask<CursesPointerShapeLease> ) != pointerMethod.ReturnType
		|| null == typeof( CursesPointerShapeLease ).GetProperty(
			nameof( CursesPointerShapeLease.Shape )
		) ) {
		throw new InvalidOperationException(
			"DCurses package-only pointer-shape lease surface is unavailable."
		);
	}

	Func<CursesInteractionRouter, CursesInputEvent, CursesInteractionResult> routeCompiler =
		CompileRoutingSurface;
	Func<CursesSession, CancellationToken, ValueTask<CursesPointerShapeLease>> pointerCompiler =
		CompilePointerShapeSurfaceAsync;
	_ = routeCompiler;
	_ = pointerCompiler;
}

static CursesInteractionResult CompileRoutingSurface(
	CursesInteractionRouter router,
	CursesInputEvent input
) {
	ArgumentNullException.ThrowIfNull( router );
	ArgumentNullException.ThrowIfNull( input );
	return router.Route( input );
}

static ValueTask<CursesPointerShapeLease> CompilePointerShapeSurfaceAsync(
	CursesSession session,
	CancellationToken cancellationToken = default
) {
	ArgumentNullException.ThrowIfNull( session );
	return session.AcquirePointerShapeAsync(
		CursesPointerShape.Pointer,
		cancellationToken
	);
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
