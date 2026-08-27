using System.Globalization;
using System.Text;
using Icod.DCurses;

TimeSpan refreshInterval = TimeSpan.FromMilliseconds( 350 );

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

TopSortKey sortKey = TopSortKey.Cpu;
FocusArea focusArea = FocusArea.Tasks;
StringBuilder promptBuffer = new();

int generation = 0;
int selectedTask = 0;
int taskOffset = 0;
int horizontalOffset = 0;
bool showCommandLine = false;
bool helpVisible = false;
bool promptActive = false;
bool cursorShownForPrompt = false;
bool running = true;
bool dirty = true;
string status = "Ready.";

IReadOnlyList<TopTaskRow> tasks = CreateTasks( generation );

while ( running ) {
	TopLayout layout = default;

	if ( dirty ) {
		if ( helpVisible ) {
			layout = DrawHelp(
				screen,
				status
			);
		} else {
			layout = DrawTop(
				session.Screen,
				tasks,
				sortKey,
				focusArea,
				selectedTask,
				ref taskOffset,
				horizontalOffset,
				showCommandLine,
				generation,
				refreshInterval,
				promptActive,
				promptBuffer.ToString(),
				status
			);
		}

		await session.RefreshAsync();

		if ( promptActive ) {
			int cursorColumn = Math.Clamp(
				layout.PromptColumn,
				0,
				Math.Max(
					0,
					screen.Columns - 1
				)
			);

			_ = await session.SetCursorPositionAsync(
				layout.PromptRow,
				cursorColumn
			);
		}

		dirty = false;
	}

	if ( promptActive != cursorShownForPrompt ) {
		_ = await session.SetCursorVisibilityAsync(
			promptActive
				? CursesCursorVisibility.Normal
				: CursesCursorVisibility.Hidden
		);
		cursorShownForPrompt = promptActive;
	}

	CursesEvent current = await session.ReadEventAsync( refreshInterval );

	switch ( current.Kind ) {
		case CursesEventKind.Timeout:
			if ( !promptActive && !helpVisible ) {
				generation++;
				tasks = CreateTasks( generation );
				status = "Timed refresh.";
				dirty = true;
			}
			break;

		case CursesEventKind.Lifecycle:
			if ( null == current.Lifecycle ) {
				break;
			}

			if ( current.RequiresRepaint ) {
				_ = session.SynchronizeDimensions();
				session.Invalidate();
				status = $"{current.Lifecycle.Kind}: complete relayout/repaint.";
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

			CursesInputEvent input = current.Input;
			if ( CursesInputEventKind.EndOfInput == input.Kind ) {
				running = false;
				break;
			}

			if ( promptActive ) {
				HandlePromptInput(
					input,
					promptBuffer,
					ref promptActive,
					ref refreshInterval,
					ref status
				);
				dirty = true;
				break;
			}

			if ( helpVisible ) {
				if ( CursesKey.Escape == input.Key
					|| IsTextCommand(
						input,
						'h',
						'H',
						'?'
					) ) {
					helpVisible = false;
					status = "Returned from help.";
					dirty = true;
				}
				break;
			}

			int visibleTaskRows = GetVisibleTaskRows( session.Screen.Rows );

			if ( CursesInputEventKind.Key == input.Kind ) {
				if ( CursesKey.Character == input.Key
					&& input.Character.HasValue
					&& 0 != ( input.Modifiers & CursesKeyModifiers.Control )
					&& input.Character.Value.Value is 'l' or 'L' or 12 ) {
					session.Invalidate();
					status = "Ctrl+L invalidated retained physical-screen state.";
					dirty = true;
					break;
				}

				switch ( input.Key ) {
					case CursesKey.Enter:
					case CursesKey.Space:
						generation++;
						tasks = CreateTasks( generation );
						status = "Immediate refresh.";
						dirty = true;
						break;

					case CursesKey.Up:
						selectedTask = Math.Max(
							0,
							selectedTask - 1
						);
						dirty = true;
						break;

					case CursesKey.Down:
						selectedTask = Math.Min(
							tasks.Count - 1,
							selectedTask + 1
						);
						dirty = true;
						break;

					case CursesKey.PageUp:
						selectedTask = Math.Max(
							0,
							selectedTask - visibleTaskRows
						);
						dirty = true;
						break;

					case CursesKey.PageDown:
						selectedTask = Math.Min(
							tasks.Count - 1,
							selectedTask + visibleTaskRows
						);
						dirty = true;
						break;

					case CursesKey.Home:
						selectedTask = 0;
						taskOffset = 0;
						dirty = true;
						break;

					case CursesKey.End:
						selectedTask = tasks.Count - 1;
						dirty = true;
						break;

					case CursesKey.Left:
						horizontalOffset = Math.Max(
							0,
							horizontalOffset - 4
						);
						status = $"Horizontal offset: {horizontalOffset}.";
						dirty = true;
						break;

					case CursesKey.Right:
						horizontalOffset = Math.Min(
							96,
							horizontalOffset + 4
						);
						status = $"Horizontal offset: {horizontalOffset}.";
						dirty = true;
						break;

					case CursesKey.Tab:
						focusArea = MoveFocus(
							focusArea,
							0 != ( input.Modifiers & CursesKeyModifiers.Shift )
								? -1
								: 1
						);
						status = $"Logical focus: {focusArea}.";
						dirty = true;
						break;

					case CursesKey.Escape:
						taskOffset = 0;
						horizontalOffset = 0;
						status = "Escape reset viewport offsets.";
						dirty = true;
						break;
				}
			}

			if ( CursesInputEventKind.Text == input.Kind
				&& input.Character.HasValue ) {
				switch ( input.Character.Value.Value ) {
					case 'q':
					case 'Q':
						running = false;
						break;

					case 'P':
						sortKey = TopSortKey.Cpu;
						status = "Sort: CPU.";
						dirty = true;
						break;

					case 'M':
						sortKey = TopSortKey.Memory;
						status = "Sort: memory.";
						dirty = true;
						break;

					case 'N':
						sortKey = TopSortKey.Pid;
						status = "Sort: PID.";
						dirty = true;
						break;

					case 'T':
						sortKey = TopSortKey.Time;
						status = "Sort: TIME+.";
						dirty = true;
						break;

					case 'c':
					case 'C':
						showCommandLine = !showCommandLine;
						status = showCommandLine
							? "Showing synthetic command lines."
							: "Showing short command names."
						;
						dirty = true;
						break;

					case 'd':
					case 'D':
					case 's':
					case 'S':
						promptBuffer.Clear();
						promptActive = true;
						status = "Edit refresh delay.";
						dirty = true;
						break;

					case 'h':
					case '?':
						helpVisible = true;
						status = "Help view.";
						dirty = true;
						break;

					case 'H':
						status = "Application command 'H' observed.";
						dirty = true;
						break;

					case '=':
						selectedTask = 0;
						taskOffset = 0;
						horizontalOffset = 0;
						status = "Viewport restrictions cleared.";
						dirty = true;
						break;

					case 'i':
					case 'I':
					case 'V':
					case 'E':
					case 'e':
						status =
							$"Application command '{input.Character.Value}' observed.";
						dirty = true;
						break;
				}
			}
			break;
	}
}

return 0;

static TopLayout DrawTop(
	CursesScreen logicalScreen,
	IReadOnlyList<TopTaskRow> sourceTasks,
	TopSortKey sortKey,
	FocusArea focusArea,
	int selectedTask,
	ref int taskOffset,
	int horizontalOffset,
	bool showCommandLine,
	int generation,
	TimeSpan refreshInterval,
	bool promptActive,
	string promptText,
	string status
) {
	ArgumentNullException.ThrowIfNull( logicalScreen );
	ArgumentNullException.ThrowIfNull( sourceTasks );
	ArgumentNullException.ThrowIfNull( promptText );
	ArgumentNullException.ThrowIfNull( status );

	CursesWindow screen = logicalScreen.StandardWindow;
	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	if ( screen.Rows < 10 || screen.Columns < 40 ) {
		WriteLine(
			screen,
			0,
			"Icod.DCurses top-shaped acceptance"
		);
		WriteLine(
			screen,
			2,
			"Enlarge the terminal to at least 40 columns by 10 rows."
		);
		return new TopLayout(
			Math.Max(
				0,
				screen.Rows - 1
			),
			0,
			1
		);
	}

	const int summaryRows = 5;
	const int statusRows = 2;
	int taskRows = screen.Rows - summaryRows - statusRows;

	CursesWindow summaryWindow = logicalScreen.CreateWindow(
		0,
		0,
		summaryRows,
		screen.Columns
	);
	CursesWindow taskWindow = logicalScreen.CreateWindow(
		summaryRows,
		0,
		taskRows,
		screen.Columns
	);
	CursesWindow statusWindow = logicalScreen.CreateWindow(
		screen.Rows - statusRows,
		0,
		statusRows,
		screen.Columns
	);

	CursesWindow headerWindow = taskWindow.CreateSubwindow(
		0,
		0,
		1,
		taskWindow.Columns
	);
	CursesWindow bodyWindow = taskWindow.CreateSubwindow(
		1,
		0,
		taskWindow.Rows - 1,
		taskWindow.Columns
	);

	CursesStyle titleStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Bold
	);
	CursesStyle summaryLabelStyle = new(
		CursesColor.Indexed( 6 ),
		CursesColor.Default,
		CursesTextAttributes.Underline
	);
	CursesStyle headerStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Reverse
	);
	CursesStyle selectedStyle = new(
		CursesColor.Indexed( 7 ),
		CursesColor.Indexed( 4 ),
		CursesTextAttributes.Standout
	);
	CursesStyle hotStyle = new(
		CursesColor.Indexed( 1 ),
		CursesColor.Default,
		CursesTextAttributes.Bold
	);
	CursesStyle statusStyle = new(
		CursesColor.Indexed( 7 ),
		CursesColor.Indexed( 4 )
	);
	CursesStyle promptStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Bold | CursesTextAttributes.Reverse
	);

	if ( FocusArea.Summary == focusArea ) {
		titleStyle = titleStyle.WithAttributes(
			titleStyle.Attributes | CursesTextAttributes.Standout
		);
	}
	if ( FocusArea.Tasks == focusArea ) {
		headerStyle = headerStyle.WithAttributes(
			headerStyle.Attributes | CursesTextAttributes.Standout
		);
	}
	if ( FocusArea.Status == focusArea ) {
		statusStyle = statusStyle.WithAttributes(
			statusStyle.Attributes | CursesTextAttributes.Underline
		);
	}

	summaryWindow.Clear();
	WriteLine(
		summaryWindow,
		0,
		$"Icod.DCurses top-shaped acceptance  gen={generation}",
		titleStyle
	);
	WriteStyledPair(
		summaryWindow,
		1,
		"load average: ",
		$"{0.40 + ( generation % 9 ) * 0.07:F2}, 0.64, 0.81",
		summaryLabelStyle
	);
	WriteStyledPair(
		summaryWindow,
		2,
		"Tasks: ",
		$"{sourceTasks.Count} total, 2 running, {sourceTasks.Count - 2} sleeping",
		summaryLabelStyle
	);
	WriteStyledPair(
		summaryWindow,
		3,
		"%Cpu(s): ",
		$"{21 + generation % 13}.0 us, 4.0 sy, 75.0 id",
		summaryLabelStyle
	);
	WriteStyledPair(
		summaryWindow,
		4,
		"MiB Mem : ",
		"32768 total, 9412 free, 12128 used, 11228 buff/cache",
		summaryLabelStyle
	);

	IReadOnlyList<TopTaskRow> tasks = SortTasks(
		sourceTasks,
		sortKey
	);
	int visibleRows = bodyWindow.Rows;
	selectedTask = Math.Clamp(
		selectedTask,
		0,
		Math.Max(
			0,
			tasks.Count - 1
		)
	);
	taskOffset = EnsureVisible(
		taskOffset,
		selectedTask,
		visibleRows,
		tasks.Count
	);

	headerWindow.Clear();
	WriteLine(
		headerWindow,
		0,
		SliceLine(
			"    PID USER       PR  NI     VIRT      RES S   %CPU  %MEM      TIME+ COMMAND",
			horizontalOffset,
			headerWindow.Columns
		),
		headerStyle
	);

	bodyWindow.Clear();
	for ( int row = 0; row < visibleRows; row++ ) {
		int taskIndex = taskOffset + row;
		if ( taskIndex >= tasks.Count ) {
			break;
		}

		TopTaskRow task = tasks[ taskIndex ];
		string command = showCommandLine
			? task.CommandLine
			: task.Command
		;
		string line = string.Format(
			CultureInfo.InvariantCulture,
			"{0,7} {1,-10} {2,2} {3,3} {4,8} {5,8} {6} {7,6:F1} {8,5:F1} {9,10} {10}",
			task.Pid,
			task.User,
			task.Priority,
			task.Nice,
			task.VirtualKiB,
			task.ResidentKiB,
			task.State,
			task.Cpu,
			task.Memory,
			task.CpuTime,
			command
		);

		CursesStyle rowStyle = 60.0 <= task.Cpu
			? hotStyle
			: CursesStyle.Default
		;
		if ( taskIndex == selectedTask ) {
			rowStyle = new CursesStyle(
				0.0 < task.Cpu
					? rowStyle.Foreground
					: selectedStyle.Foreground,
				selectedStyle.Background,
				selectedStyle.Attributes | rowStyle.Attributes
			);
		}

		WriteLine(
			bodyWindow,
			row,
			SliceLine(
				line,
				horizontalOffset,
				bodyWindow.Columns
			),
			rowStyle
		);
	}

	statusWindow.Clear();
	if ( promptActive ) {
		string prefix = "Change delay from current value: ";
		string promptLine = prefix + promptText;
		WriteLine(
			statusWindow,
			0,
			promptLine,
			promptStyle
		);
		WriteLine(
			statusWindow,
			1,
			"Enter commits | Backspace edits | Escape cancels",
			statusStyle
		);

		return new TopLayout(
			screen.Rows - 2,
			Math.Min(
				screen.Columns - 1,
				prefix.Length + promptText.Length
			),
			visibleRows
		);
	}

	WriteLine(
		statusWindow,
		0,
		$"sort={sortKey} focus={focusArea} row={selectedTask + 1}/{tasks.Count}"
			+ $" x={horizontalOffset} delay={refreshInterval.TotalSeconds:F2}s",
		statusStyle
	);
	WriteLine(
		statusWindow,
		1,
		string.IsNullOrWhiteSpace( status )
			? "P/M/N/T sort | arrows/page/home/end | Tab | d prompt | h help | Q"
			: status,
		statusStyle
	);

	return new TopLayout(
		screen.Rows - 2,
		0,
		visibleRows
	);
}

static TopLayout DrawHelp(
	CursesWindow screen,
	string status
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( status );

	screen.WrapMode = CursesWrapMode.Clip;
	screen.Clear();

	CursesStyle titleStyle = new(
		CursesColor.Default,
		CursesColor.Default,
		CursesTextAttributes.Bold | CursesTextAttributes.Standout
	);
	CursesStyle sectionStyle = new(
		CursesColor.Indexed( 6 ),
		CursesColor.Default,
		CursesTextAttributes.Underline
	);

	WriteLine(
		screen,
		0,
		"Icod.DCurses top-shaped help view",
		titleStyle
	);
	WriteLine(
		screen,
		2,
		"Navigation",
		sectionStyle
	);
	WriteLine(
		screen,
		3,
		"Up/Down PgUp/PgDn Home/End Left/Right; Tab/Shift+Tab focus"
	);
	WriteLine(
		screen,
		5,
		"Commands",
		sectionStyle
	);
	WriteLine(
		screen,
		6,
		"P CPU | M memory | N PID | T time | c command | d/s delay"
	);
	WriteLine(
		screen,
		7,
		"Ctrl+L repaint | = reset viewport | q quit"
	);
	WriteLine(
		screen,
		9,
		"Press Escape, h, or ? to return.",
		titleStyle
	);
	WriteLine(
		screen,
		Math.Max(
			0,
			screen.Rows - 1
		),
		status
	);

	return new TopLayout(
		Math.Max(
			0,
			screen.Rows - 1
		),
		0,
		GetVisibleTaskRows( screen.Rows )
	);
}

static void HandlePromptInput(
	CursesInputEvent input,
	StringBuilder promptBuffer,
	ref bool promptActive,
	ref TimeSpan refreshInterval,
	ref string status
) {
	ArgumentNullException.ThrowIfNull( input );
	ArgumentNullException.ThrowIfNull( promptBuffer );
	ArgumentNullException.ThrowIfNull( status );

	if ( CursesInputEventKind.Text == input.Kind
		&& input.Character.HasValue ) {
		Rune character = input.Character.Value;
		if ( character.Value is >= '0' and <= '9'
			|| '.' == character.Value ) {
			promptBuffer.Append( character.ToString() );
		}
		return;
	}

	if ( CursesInputEventKind.Key != input.Kind ) {
		return;
	}

	switch ( input.Key ) {
		case CursesKey.Backspace:
			if ( 0 < promptBuffer.Length ) {
				promptBuffer.Length--;
			}
			break;

		case CursesKey.Escape:
			promptActive = false;
			status = "Delay change cancelled.";
			break;

		case CursesKey.Enter:
			if ( double.TryParse(
				promptBuffer.ToString(),
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out double seconds
			)
				&& double.IsFinite( seconds )
				&& 0.05 <= seconds
				&& 60.0 >= seconds ) {
				refreshInterval = TimeSpan.FromSeconds( seconds );
				promptActive = false;
				status = $"Refresh delay changed to {seconds:F2}s.";
			} else {
				status = "Enter a delay from 0.05 through 60 seconds.";
			}
			break;
	}
}

static IReadOnlyList<TopTaskRow> SortTasks(
	IReadOnlyList<TopTaskRow> tasks,
	TopSortKey sortKey
) {
	ArgumentNullException.ThrowIfNull( tasks );

	IEnumerable<TopTaskRow> ordered = sortKey switch {
		TopSortKey.Cpu => tasks.OrderByDescending( task => task.Cpu ),
		TopSortKey.Memory => tasks.OrderByDescending( task => task.Memory ),
		TopSortKey.Pid => tasks.OrderBy( task => task.Pid ),
		TopSortKey.Time => tasks.OrderByDescending( task => task.CpuTimeTicks ),
		_ => throw new ArgumentOutOfRangeException( nameof( sortKey ) )
	};

	return ordered.ToArray();
}

static int EnsureVisible(
	int currentOffset,
	int selectedIndex,
	int visibleRows,
	int taskCount
) {
	int maximumOffset = Math.Max(
		0,
		taskCount - visibleRows
	);
	int offset = Math.Clamp(
		currentOffset,
		0,
		maximumOffset
	);

	if ( selectedIndex < offset ) {
		offset = selectedIndex;
	} else if ( selectedIndex >= offset + visibleRows ) {
		offset = selectedIndex - visibleRows + 1;
	}

	return Math.Clamp(
		offset,
		0,
		maximumOffset
	);
}

static int GetVisibleTaskRows(
	int screenRows
) {
	return Math.Max(
		1,
		screenRows - 8
	);
}

static FocusArea MoveFocus(
	FocusArea current,
	int delta
) {
	const int count = 3;
	int value = ( (int)current + delta ) % count;
	if ( 0 > value ) {
		value += count;
	}
	return (FocusArea)value;
}

static bool IsTextCommand(
	CursesInputEvent input,
	params int[] accepted
) {
	ArgumentNullException.ThrowIfNull( input );
	ArgumentNullException.ThrowIfNull( accepted );

	if ( CursesInputEventKind.Text != input.Kind
		|| !input.Character.HasValue ) {
		return false;
	}

	return accepted.Contains( input.Character.Value.Value );
}

static string SliceLine(
	string text,
	int horizontalOffset,
	int columns
) {
	ArgumentNullException.ThrowIfNull( text );

	if ( 0 >= columns
		|| horizontalOffset >= text.Length ) {
		return string.Empty;
	}

	int length = Math.Min(
		columns,
		text.Length - horizontalOffset
	);
	return text.Substring(
		horizontalOffset,
		length
	);
}

static void WriteStyledPair(
	CursesWindow window,
	int row,
	string label,
	string value,
	CursesStyle labelStyle
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( label );
	ArgumentNullException.ThrowIfNull( value );

	if ( row < 0 || row >= window.Rows ) {
		return;
	}

	window.Move(
		row,
		0
	);
	window.Write(
		label,
		labelStyle
	);
	window.Write( value );
}

static void WriteLine(
	CursesWindow window,
	int row,
	string text,
	CursesStyle style = default
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentNullException.ThrowIfNull( text );

	if ( row < 0 || row >= window.Rows ) {
		return;
	}

	window.Move(
		row,
		0
	);
	window.Write(
		text,
		style
	);
}

static IReadOnlyList<TopTaskRow> CreateTasks(
	int generation
) {
	int pulse = generation % 17;
	return [
		new TopTaskRow( 101, "root", 20, 0, 121600, 18320, 'S', 2.1 + pulse, 0.7, 4201, "systemd", "/sbin/init --system --deserialize 31" ),
		new TopTaskRow( 245, "root", 20, 0, 88240, 11840, 'S', 0.8, 0.4, 2190, "journald", "/usr/lib/systemd/systemd-journald" ),
		new TopTaskRow( 511, "messagebus", 20, 0, 15440, 7280, 'S', 0.2, 0.2, 744, "dbus-daemon", "/usr/bin/dbus-daemon --system --address=systemd:" ),
		new TopTaskRow( 804, "root", 20, 0, 392000, 42400, 'S', 4.0 + pulse * 0.4, 1.8, 9701, "NetworkManager", "/usr/sbin/NetworkManager --no-daemon" ),
		new TopTaskRow( 1208, "demo", 20, 0, 892000, 126000, 'S', 16.0 + pulse * 0.8, 4.2, 18812, "terminal", "/usr/bin/terminal-emulator --profile development --working-directory /tmp" ),
		new TopTaskRow( 1512, "demo", 20, 0, 1482000, 216000, 'S', 65.0 + pulse * 0.9, 7.1, 24009, "dotnet", "dotnet run --project samples/Icod.DCurses.Top.Acceptance/Icod.DCurses.Top.Acceptance.csproj" ),
		new TopTaskRow( 1721, "demo", 20, 0, 742000, 98000, 'R', 39.0 + pulse * 0.5, 3.3, 16332, "browser", "/usr/bin/browser --type=renderer --enable-features=LongSyntheticCommandLine" ),
		new TopTaskRow( 1802, "demo", 20, 0, 128000, 18300, 'S', 3.5, 0.6, 1802, "ssh", "ssh -o ServerAliveInterval=30 build-host.example.test" ),
		new TopTaskRow( 2014, "demo", 20, 0, 632000, 70200, 'S', 6.9 + pulse * 0.3, 2.3, 7233, "editor", "/usr/bin/editor --reuse-window /home/demo/project/very/long/path/source.cs" ),
		new TopTaskRow( 2231, "demo", 20, 0, 116000, 12200, 'S', 1.4, 0.4, 1420, "bash", "/usr/bin/bash -l" ),
		new TopTaskRow( 2310, "demo", 20, 0, 101000, 9100, 'S', 0.5, 0.3, 802, "git", "git status --short --branch" ),
		new TopTaskRow( 2501, "demo", 20, 0, 97000, 8200, 'S', 0.3, 0.2, 441, "watch-helper", "/usr/bin/helper --emit synthetic monitoring data" ),
		new TopTaskRow( 2722, "demo", 20, 0, 125000, 14300, 'R', 8.7 + pulse * 0.2, 0.5, 2601, "worker-a", "/opt/demo/worker --queue alpha --threads 4" ),
		new TopTaskRow( 2723, "demo", 20, 0, 125000, 14200, 'S', 7.2 + pulse * 0.2, 0.5, 2410, "worker-b", "/opt/demo/worker --queue beta --threads 4" ),
		new TopTaskRow( 2900, "demo", 25, 5, 84000, 6600, 'S', 0.1, 0.2, 331, "sleep", "sleep 3600" ),
		new TopTaskRow( 3011, "root", 20, 0, 204000, 18400, 'S', 1.9, 0.6, 1198, "cron", "/usr/sbin/cron -f" ),
		new TopTaskRow( 3202, "demo", 20, 0, 518000, 48200, 'S', 5.1 + pulse * 0.1, 1.6, 5504, "testhost", "dotnet test Icod.DCurses.sln --no-restore" ),
		new TopTaskRow( 3401, "demo", 20, 0, 111000, 10300, 'S', 0.7, 0.3, 620, "tail", "tail -f acceptance.log" )
	];
}

internal enum TopSortKey {
	Cpu,
	Memory,
	Pid,
	Time
}

internal enum FocusArea {
	Summary,
	Tasks,
	Status
}

internal readonly record struct TopLayout(
	int PromptRow,
	int PromptColumn,
	int VisibleTaskRows
);

internal sealed class TopTaskRow {
	internal TopTaskRow(
		int pid,
		string user,
		int priority,
		int nice,
		long virtualKiB,
		long residentKiB,
		char state,
		double cpu,
		double memory,
		long cpuTimeTicks,
		string command,
		string commandLine
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( user );
		ArgumentException.ThrowIfNullOrWhiteSpace( command );
		ArgumentException.ThrowIfNullOrWhiteSpace( commandLine );

		this.Pid = pid;
		this.User = user;
		this.Priority = priority;
		this.Nice = nice;
		this.VirtualKiB = virtualKiB;
		this.ResidentKiB = residentKiB;
		this.State = state;
		this.Cpu = cpu;
		this.Memory = memory;
		this.CpuTimeTicks = cpuTimeTicks;
		this.Command = command;
		this.CommandLine = commandLine;
	}

	internal int Pid {
		get;
	}

	internal string User {
		get;
	}

	internal int Priority {
		get;
	}

	internal int Nice {
		get;
	}

	internal long VirtualKiB {
		get;
	}

	internal long ResidentKiB {
		get;
	}

	internal char State {
		get;
	}

	internal double Cpu {
		get;
	}

	internal double Memory {
		get;
	}

	internal long CpuTimeTicks {
		get;
	}

	internal string CpuTime {
		get {
			TimeSpan elapsed = TimeSpan.FromMilliseconds( this.CpuTimeTicks );
			return $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 10:D2}";
		}
	}

	internal string Command {
		get;
	}

	internal string CommandLine {
		get;
	}
}
