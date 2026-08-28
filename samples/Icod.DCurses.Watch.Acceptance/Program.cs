using System.Text;
using Icod.DCurses;

TimeSpan refreshInterval = TimeSpan.FromSeconds( 1 );

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

CursesStyle titleStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);

WatchSnapshot[] snapshots = CreateSnapshots();
int snapshotIndex = 0;
WatchSnapshot? previousSnapshot = null;

bool showTitle = true;
bool wrapOutput = false;
bool interpretColors = true;
bool beepOnFailure = true;
bool preservePresentation = false;
bool running = true;
bool dirty = true;

while ( running ) {
	if ( dirty ) {
		DrawWatch(
			screen,
			titleStyle,
			snapshots[ snapshotIndex ],
			previousSnapshot,
			showTitle,
			wrapOutput,
			interpretColors,
			beepOnFailure,
			preservePresentation
		);
		await session.RefreshAsync();
		dirty = false;
	}

	CursesEvent current = await session.ReadEventAsync( refreshInterval );

	switch ( current.Kind ) {
		case CursesEventKind.Timeout:
			if ( !preservePresentation ) {
				WatchSnapshot prior = snapshots[ snapshotIndex ];
				snapshotIndex = ( snapshotIndex + 1 ) % snapshots.Length;
				WatchSnapshot next = snapshots[ snapshotIndex ];

				if ( beepOnFailure
					&& 0 == prior.ExitCode
					&& 0 != next.ExitCode ) {
					await session.AlertAsync();
				}

				previousSnapshot = prior;
				dirty = true;
			}
			break;

		case CursesEventKind.Lifecycle:
			if ( null == current.Lifecycle ) {
				break;
			}

			if ( current.RequiresRepaint ) {
				session.Invalidate();
				dirty = true;
			}

			if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
				or CursesLifecycleEventKind.Termination ) {
				running = false;
			}
			break;

		case CursesEventKind.Input:
			if ( null == current.Input ) {
				break;
			}

			if ( CursesInputEventKind.EndOfInput == current.Input.Kind ) {
				running = false;
				break;
			}

			if ( CursesKey.Space == current.Input.Key ) {
				WatchSnapshot prior = snapshots[ snapshotIndex ];
				snapshotIndex = ( snapshotIndex + 1 ) % snapshots.Length;
				WatchSnapshot next = snapshots[ snapshotIndex ];

				if ( beepOnFailure
					&& 0 == prior.ExitCode
					&& 0 != next.ExitCode ) {
					await session.AlertAsync();
				}

				previousSnapshot = prior;
				dirty = true;
				break;
			}

			if ( CursesInputEventKind.Text != current.Input.Kind
				|| !current.Input.Character.HasValue ) {
				break;
			}

			Rune character = current.Input.Character.Value;
			switch ( character.Value ) {
				case 'q':
				case 'Q':
					running = false;
					break;

				case 't':
				case 'T':
					showTitle = !showTitle;
					dirty = true;
					break;

				case 'w':
				case 'W':
					wrapOutput = !wrapOutput;
					dirty = true;
					break;

				case 'c':
				case 'C':
					interpretColors = !interpretColors;
					dirty = true;
					break;

				case 'b':
				case 'B':
					beepOnFailure = !beepOnFailure;
					dirty = true;
					break;

				case 'p':
				case 'P':
					preservePresentation = !preservePresentation;
					dirty = true;
					break;

				case 'f':
				case 'F': {
					WatchSnapshot prior = snapshots[ snapshotIndex ];
					snapshotIndex = 2;
					WatchSnapshot next = snapshots[ snapshotIndex ];

					if ( beepOnFailure
						&& 0 == prior.ExitCode
						&& 0 != next.ExitCode ) {
						await session.AlertAsync();
					}

					previousSnapshot = prior;
					dirty = true;
					break;
				}
			}
			break;
	}
}

return 0;

static void DrawWatch(
	CursesWindow screen,
	CursesStyle titleStyle,
	WatchSnapshot snapshot,
	WatchSnapshot? previousSnapshot,
	bool showTitle,
	bool wrapOutput,
	bool interpretColors,
	bool beepOnFailure,
	bool preservePresentation
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( snapshot );

	screen.WrapMode = wrapOutput
		? CursesWrapMode.Wrap
		: CursesWrapMode.Clip
	;
	screen.Clear();

	int row = 0;
	if ( showTitle ) {
		WriteLine(
			screen,
			row++,
			"Every 1.0s: synthetic-status",
			titleStyle
		);
		WriteLine(
			screen,
			row++,
			"DCurses T12 watch-shaped acceptance harness"
		);
	}

	int lastContentRow = Math.Max(
		row,
		screen.Rows - 2
	);

	for ( int lineIndex = 0;
		lineIndex < snapshot.Lines.Count && row < lastContentRow;
		lineIndex++, row++ ) {
		bool changed = IsChanged(
			snapshot,
			previousSnapshot,
			lineIndex
		);

		WriteWatchLine(
			screen,
			row,
			snapshot.Lines[ lineIndex ],
			interpretColors,
			changed
		);
	}

	WriteLine(
		screen,
		screen.Rows - 2,
		"Space refresh | T title | W wrap | C colors | B beep | P preserve | F fail | Q exit"
	);
	WriteLine(
		screen,
		screen.Rows - 1,
		$"exit={snapshot.ExitCode}  title={( showTitle ? "on" : "off" )}"
			+ $"  wrap={( wrapOutput ? "wrap" : "clip" )}"
			+ $"  colors={( interpretColors ? "on" : "off" )}"
			+ $"  beep={( beepOnFailure ? "on" : "off" )}"
			+ $"  preserve={( preservePresentation ? "on" : "off" )}"
	);
}

static bool IsChanged(
	WatchSnapshot snapshot,
	WatchSnapshot? previousSnapshot,
	int lineIndex
) {
	ArgumentNullException.ThrowIfNull( snapshot );
	if ( previousSnapshot is null ) {
		return false;
	}
	if ( lineIndex >= previousSnapshot.Lines.Count ) {
		return true;
	}

	return !string.Equals(
		GetPlainText( snapshot.Lines[ lineIndex ] ),
		GetPlainText( previousSnapshot.Lines[ lineIndex ] ),
		StringComparison.Ordinal
	);
}

static string GetPlainText(
	WatchLine line
) {
	ArgumentNullException.ThrowIfNull( line );

	StringBuilder builder = new();
	foreach ( WatchSegment segment in line.Segments ) {
		builder.Append( segment.Text );
	}
	return builder.ToString();
}

static void WriteWatchLine(
	CursesWindow screen,
	int row,
	WatchLine line,
	bool interpretColors,
	bool changed
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( line );

	if ( row < 0 || row >= screen.Rows ) {
		return;
	}

	screen.Move(
		row,
		0
	);

	foreach ( WatchSegment segment in line.Segments ) {
		CursesStyle style = interpretColors
			? segment.Style
			: CursesStyle.Default
		;

		if ( changed ) {
			style = style.WithAttributes(
				style.Attributes | CursesTextAttributes.Reverse
			);
		}

		screen.Write(
			segment.Text,
			style
		);
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

static WatchSnapshot[] CreateSnapshots() {
	CursesStyle healthy = new(
		CursesColor.Indexed( 2 ),
		CursesColor.Default
	);
	CursesStyle warning = new(
		CursesColor.Indexed( 3 ),
		CursesColor.Default,
		CursesTextAttributes.Bold
	);
	CursesStyle failure = new(
		CursesColor.Indexed( 1 ),
		CursesColor.Default,
		CursesTextAttributes.Bold
	);

	return [
		new WatchSnapshot(
			0,
			new WatchLine(
				new WatchSegment( "counter: 100", CursesStyle.Default )
			),
			new WatchLine(
				new WatchSegment( "state: ", CursesStyle.Default ),
				new WatchSegment( "OK", healthy )
			),
			new WatchLine(
				new WatchSegment(
					"payload: alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau",
					CursesStyle.Default
				)
			)
		),
		new WatchSnapshot(
			0,
			new WatchLine(
				new WatchSegment( "counter: 101", CursesStyle.Default )
			),
			new WatchLine(
				new WatchSegment( "state: ", CursesStyle.Default ),
				new WatchSegment( "WARN", warning )
			),
			new WatchLine(
				new WatchSegment(
					"payload: alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau",
					CursesStyle.Default
				)
			)
		),
		new WatchSnapshot(
			7,
			new WatchLine(
				new WatchSegment( "counter: 102", CursesStyle.Default )
			),
			new WatchLine(
				new WatchSegment( "state: ", CursesStyle.Default ),
				new WatchSegment( "FAILED", failure )
			),
			new WatchLine(
				new WatchSegment(
					"payload: command returned status 7; output remains visible for inspection",
					CursesStyle.Default
				)
			)
		),
		new WatchSnapshot(
			0,
			new WatchLine(
				new WatchSegment( "counter: 103", CursesStyle.Default )
			),
			new WatchLine(
				new WatchSegment( "state: ", CursesStyle.Default ),
				new WatchSegment( "RECOVERED", healthy )
			),
			new WatchLine(
				new WatchSegment(
					"payload: alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau",
					CursesStyle.Default
				)
			)
		)
	];
}

internal sealed class WatchSnapshot {
	internal WatchSnapshot(
		int exitCode,
		params WatchLine[] lines
	) {
		ArgumentNullException.ThrowIfNull( lines );
		this.ExitCode = exitCode;
		this.Lines = lines;
	}

	internal int ExitCode {
		get;
	}

	internal IReadOnlyList<WatchLine> Lines {
		get;
	}
}

internal sealed class WatchLine {
	internal WatchLine(
		params WatchSegment[] segments
	) {
		ArgumentNullException.ThrowIfNull( segments );
		this.Segments = segments;
	}

	internal IReadOnlyList<WatchSegment> Segments {
		get;
	}
}

internal readonly record struct WatchSegment(
	string Text,
	CursesStyle Style
);
