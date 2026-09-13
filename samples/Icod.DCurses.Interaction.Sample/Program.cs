/*
	Icod.DCurses.Interaction.Sample
	Interactive 1.4 interaction-routing acceptance sample for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text;
using Icod.DCurses;
using Icod.DCurses.Interaction.Sample;

const int MinimumColumns = 64;
const int MinimumRows = 16;
const int PopupColumns = 28;
const int PopupRows = 8;

CursesStyle headingStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold
);
CursesStyle focusedStyle = new(
	CursesColor.Default,
	CursesColor.Default,
	CursesTextAttributes.Bold | CursesTextAttributes.Reverse
);

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow standard = session.StandardScreen;
standard.WrapMode = CursesWrapMode.Clip;

InteractionSampleState state = new();
List<CursesInputProtocolLease> protocolLeases = [];
CursesPointerShapeLease? pointerLease = null;
CursesPointerShape? appliedPointerShape = null;

await TryAcquireInputProtocolAsync(
	session,
	"keyboard event types",
	new CursesInputProtocolOptions {
		KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
	},
	protocolLeases,
	state
);
await TryAcquireInputProtocolAsync(
	session,
	"focus reporting",
	new CursesInputProtocolOptions {
		FocusReporting = true
	},
	protocolLeases,
	state
);
await TryAcquireInputProtocolAsync(
	session,
	"mouse button events",
	new CursesInputProtocolOptions {
		MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents
	},
	protocolLeases,
	state
);

CursesWindow headerWindow = screen.CreateWindow(
	0,
	0,
	1,
	1
);
CursesWindow leftWindow = screen.CreateWindow(
	0,
	0,
	1,
	1
);
CursesWindow rightWindow = screen.CreateWindow(
	0,
	0,
	1,
	1
);
CursesWindow footerWindow = screen.CreateWindow(
	0,
	0,
	1,
	1
);
headerWindow.WrapMode = CursesWrapMode.Clip;
leftWindow.WrapMode = CursesWrapMode.Clip;
rightWindow.WrapMode = CursesWrapMode.Clip;
footerWindow.WrapMode = CursesWrapMode.Clip;

using CursesPanel popup = screen.CreatePanel(
	0,
	0,
	1,
	1
);
popup.ContentWindow.WrapMode = CursesWrapMode.Clip;
popup.Hide();

using CursesInteractionRouter router = new( screen );
CursesRectangle empty = new(
	0,
	0,
	0,
	0
);
using CursesInteractionRegion headerRegion = router.RegisterRegion(
	new CursesInteractionRegionOptions( empty ) {
		PointerShape = CursesPointerShape.Default
	}
);
using CursesInteractionRegion leftRegion = router.RegisterRegion(
	new CursesInteractionRegionOptions( empty ) {
		IsFocusable = true,
		TraversalOrder = 0,
		PointerShape = CursesPointerShape.Text
	}
);
using CursesInteractionRegion rightRegion = router.RegisterRegion(
	new CursesInteractionRegionOptions( empty ) {
		IsFocusable = true,
		TraversalOrder = 1,
		PointerShape = CursesPointerShape.Crosshair
	}
);
using CursesInteractionRegion footerRegion = router.RegisterRegion(
	new CursesInteractionRegionOptions( empty ) {
		PointerShape = CursesPointerShape.Pointer
	}
);
using CursesInteractionRegion popupRegion = router.RegisterRegion(
	new CursesInteractionRegionOptions(
		new CursesRectangle(
			0,
			0,
			PopupRows,
			PopupColumns
		)
	) {
		Panel = popup,
		IsFocusable = true,
		TraversalOrder = 2,
		PointerShape = CursesPointerShape.Move
	}
);

CursesCommand focusNextCommand = new( "focus.next" );
CursesCommand focusPreviousCommand = new( "focus.previous" );
CursesCommand popupToggleCommand = new( "popup.toggle" );
CursesCommand escapeCommand = new( "escape" );
CursesCommand leftActionCommand = new( "left.action" );
CursesCommand rightActionCommand = new( "right.action" );
CursesCommand globalXCommand = new( "global.x" );
CursesCommand quitCommand = new( "quit" );

router.BindGlobalGesture(
	CursesKeyGesture.ForKey( CursesKey.Tab ),
	focusNextCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForKey(
		CursesKey.Tab,
		CursesKeyModifiers.Shift
	),
	focusPreviousCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForFunctionKey( 2 ),
	popupToggleCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForKey( CursesKey.Escape ),
	escapeCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForCharacter( new Rune( 'x' ) ),
	globalXCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForCharacter( new Rune( 'q' ) ),
	quitCommand
);
leftRegion.BindGesture(
	CursesKeyGesture.ForCharacter( new Rune( 'x' ) ),
	leftActionCommand
);
rightRegion.BindGesture(
	CursesKeyGesture.ForCharacter( new Rune( 'r' ) ),
	rightActionCommand
);

bool layoutAvailable = ApplyLayoutPolicy(
	screen,
	headerWindow,
	leftWindow,
	rightWindow,
	footerWindow,
	popup,
	router,
	headerRegion,
	leftRegion,
	rightRegion,
	footerRegion,
	state
);

try {
	while ( state.Running ) {
		Draw(
			screen,
			standard,
			headerWindow,
			leftWindow,
			rightWindow,
			footerWindow,
			popup,
			router,
			leftRegion,
			rightRegion,
			popupRegion,
			state,
			layoutAvailable,
			headingStyle,
			focusedStyle
		);
		await session.RefreshAsync();

		CursesEvent current = await session.ReadEventAsync();
		switch ( current.Kind ) {
			case CursesEventKind.Input:
				if ( current.Input is null ) {
					break;
				}

				CursesInputEvent input = current.Input;
				CursesInteractionResult routed = router.Route( input );

				if ( CursesInputEventKind.EndOfInput == input.Kind ) {
					state.Running = false;
					break;
				}

				if ( CursesInputEventKind.Focus == input.Kind
					&& input.Focus is not null ) {
					state.TerminalFocus = input.Focus.State;
					state.Status = $"Terminal focus: {input.Focus.State}";
				}

				if ( routed.Command is not null ) {
					HandleCommand(
						routed.Command,
						screen,
						popup,
						router,
						leftRegion,
						popupRegion,
						state
					);
				} else if ( CursesInteractionResultKind.Targeted == routed.Kind
					&& routed.Region is not null
					&& CursesInputEventKind.Mouse != input.Kind ) {
					state.Status = $"Targeted {GetRegionName(
						routed.Region,
						headerRegion,
						leftRegion,
						rightRegion,
						footerRegion,
						popupRegion
					)} with {input.Kind}";
				}

				if ( CursesInputEventKind.Mouse == input.Kind
					&& input.Mouse is not null ) {
					CursesPointerShape? desiredShape = routed.Hit?.PointerShape;
					if ( routed.Hit is not null ) {
						state.RoutedTarget = $"{GetRegionName(
							routed.Hit.Region,
							headerRegion,
							leftRegion,
							rightRegion,
							footerRegion,
							popupRegion
						)} screen=({input.Mouse.Row},{input.Mouse.Column}) local=({routed.Hit.LocalRow},{routed.Hit.LocalColumn}) pointer={desiredShape?.ToString() ?? "none"}";
					} else {
						state.RoutedTarget = $"none screen=({input.Mouse.Row},{input.Mouse.Column})";
					}

					if ( desiredShape != appliedPointerShape ) {
						if ( desiredShape.HasValue ) {
							CursesPointerShapeLease? replacement = null;
							try {
								replacement = await session.AcquirePointerShapeAsync(
									desiredShape.Value
								);
							} catch ( Exception exception ) {
								state.Status = $"Pointer shape failed: {exception.Message}";
							}

							if ( replacement is not null ) {
								CursesPointerShapeLease? prior = pointerLease;
								pointerLease = replacement;
								appliedPointerShape = desiredShape;
								state.AppliedPointerShape = desiredShape;
								if ( prior is not null ) {
									await prior.DisposeAsync();
								}
							}
						} else {
							if ( pointerLease is not null ) {
								await pointerLease.DisposeAsync();
								pointerLease = null;
							}
							appliedPointerShape = null;
							state.AppliedPointerShape = null;
						}
					}
				}
				break;

			case CursesEventKind.Lifecycle:
				if ( current.Lifecycle is null ) {
					break;
				}

				if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
					or CursesLifecycleEventKind.Termination ) {
					state.Running = false;
					break;
				}

				if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Resize
					or CursesLifecycleEventKind.Resumed ) {
					_ = session.SynchronizeDimensions();
					layoutAvailable = ApplyLayoutPolicy(
						screen,
						headerWindow,
						leftWindow,
						rightWindow,
						footerWindow,
						popup,
						router,
						headerRegion,
						leftRegion,
						rightRegion,
						footerRegion,
						state
					);
					session.Invalidate();
				}
				break;

			case CursesEventKind.Timeout:
				break;
		}
	}
} finally {
	if ( pointerLease is not null ) {
		await pointerLease.DisposeAsync();
	}
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
	InteractionSampleState state
) {
	ArgumentNullException.ThrowIfNull( session );
	ArgumentException.ThrowIfNullOrWhiteSpace( name );
	ArgumentNullException.ThrowIfNull( options );
	ArgumentNullException.ThrowIfNull( leases );
	ArgumentNullException.ThrowIfNull( state );

	var result = await session.AcquireInputProtocolsAsync( options );
	if ( result.IsAvailable ) {
		leases.Add( result.GetRequiredValue() );
		return;
	}

	state.Status = $"{name}: {result.Status}";
}

static bool ApplyLayoutPolicy(
	CursesScreen screen,
	CursesWindow headerWindow,
	CursesWindow leftWindow,
	CursesWindow rightWindow,
	CursesWindow footerWindow,
	CursesPanel popup,
	CursesInteractionRouter router,
	CursesInteractionRegion headerRegion,
	CursesInteractionRegion leftRegion,
	CursesInteractionRegion rightRegion,
	CursesInteractionRegion footerRegion,
	InteractionSampleState state
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( headerWindow );
	ArgumentNullException.ThrowIfNull( leftWindow );
	ArgumentNullException.ThrowIfNull( rightWindow );
	ArgumentNullException.ThrowIfNull( footerWindow );
	ArgumentNullException.ThrowIfNull( popup );
	ArgumentNullException.ThrowIfNull( router );
	ArgumentNullException.ThrowIfNull( headerRegion );
	ArgumentNullException.ThrowIfNull( leftRegion );
	ArgumentNullException.ThrowIfNull( rightRegion );
	ArgumentNullException.ThrowIfNull( footerRegion );
	ArgumentNullException.ThrowIfNull( state );

	if ( screen.Columns < MinimumColumns
		|| screen.Rows < MinimumRows ) {
		popup.Hide();
		state.PopupVisible = false;
		CursesRectangle empty = new(
			0,
			0,
			0,
			0
		);
		headerRegion.SetBounds( empty );
		leftRegion.SetBounds( empty );
		rightRegion.SetBounds( empty );
		footerRegion.SetBounds( empty );
		_ = router.FocusedRegion;
		state.Status = $"Resize to at least {MinimumColumns}x{MinimumRows}; current {screen.Columns}x{screen.Rows}";
		return false;
	}

	ComputeLayout(
		screen.Bounds,
		out CursesRectangle header,
		out CursesRectangle left,
		out CursesRectangle right,
		out CursesRectangle footer,
		out CursesRectangle popupBounds
	);

	headerWindow.SetBounds( header );
	leftWindow.SetBounds( left );
	rightWindow.SetBounds( right );
	footerWindow.SetBounds( footer );
	popup.SetBounds( popupBounds );
	headerRegion.SetBounds( header );
	leftRegion.SetBounds( left );
	rightRegion.SetBounds( right );
	footerRegion.SetBounds( footer );

	if ( state.PopupVisible ) {
		popup.Show();
	} else {
		popup.Hide();
	}

	if ( router.FocusedRegion is null ) {
		_ = router.Focus( leftRegion );
	}
	return true;
}

static void ComputeLayout(
	CursesRectangle bounds,
	out CursesRectangle header,
	out CursesRectangle left,
	out CursesRectangle right,
	out CursesRectangle footer,
	out CursesRectangle popup
) {
	CursesLayout.Dock(
		bounds,
		CursesDockEdge.Top,
		1,
		out header,
		out CursesRectangle afterHeader
	);
	CursesLayout.Dock(
		afterHeader,
		CursesDockEdge.Bottom,
		2,
		out footer,
		out CursesRectangle body
	);
	CursesLayout.SplitColumnsProportional(
		body,
		1,
		1,
		out left,
		out right
	);

	popup = new CursesRectangle(
		body.Row + ( ( body.Rows - PopupRows ) / 2 ),
		body.Column + ( ( body.Columns - PopupColumns ) / 2 ),
		PopupRows,
		PopupColumns
	);
}

static void HandleCommand(
	CursesCommand command,
	CursesScreen screen,
	CursesPanel popup,
	CursesInteractionRouter router,
	CursesInteractionRegion leftRegion,
	CursesInteractionRegion popupRegion,
	InteractionSampleState state
) {
	ArgumentNullException.ThrowIfNull( command );
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( popup );
	ArgumentNullException.ThrowIfNull( router );
	ArgumentNullException.ThrowIfNull( leftRegion );
	ArgumentNullException.ThrowIfNull( popupRegion );
	ArgumentNullException.ThrowIfNull( state );

	switch ( command.Name ) {
		case "focus.next":
			_ = router.MoveFocus( CursesFocusDirection.Forward );
			state.Status = "Focus moved forward";
			break;

		case "focus.previous":
			_ = router.MoveFocus( CursesFocusDirection.Backward );
			state.Status = "Focus moved backward";
			break;

		case "popup.toggle":
			if ( state.PopupVisible ) {
				popup.Hide();
				state.PopupVisible = false;
				_ = router.FocusedRegion;
				state.Status = "Popup hidden";
			} else if ( screen.Columns >= MinimumColumns
				&& screen.Rows >= MinimumRows ) {
				popup.Show();
				state.PopupVisible = true;
				_ = router.Focus( popupRegion );
				state.Status = "Popup shown and explicitly focused";
			} else {
				state.Status = "Popup unavailable while terminal is below minimum size";
			}
			break;

		case "escape":
			if ( state.PopupVisible ) {
				popup.Hide();
				state.PopupVisible = false;
				_ = router.FocusedRegion;
				state.Status = "Popup closed";
			} else {
				state.Running = false;
			}
			break;

		case "left.action":
			state.Status = "Left local x won over global x";
			break;

		case "right.action":
			state.Status = "Right local r action";
			break;

		case "global.x":
			state.Status = "Global x command";
			break;

		case "quit":
			state.Running = false;
			break;

		default:
			state.Status = $"Unknown command: {command.Name}";
			break;
	}
}

static void Draw(
	CursesScreen screen,
	CursesWindow standard,
	CursesWindow headerWindow,
	CursesWindow leftWindow,
	CursesWindow rightWindow,
	CursesWindow footerWindow,
	CursesPanel popup,
	CursesInteractionRouter router,
	CursesInteractionRegion leftRegion,
	CursesInteractionRegion rightRegion,
	CursesInteractionRegion popupRegion,
	InteractionSampleState state,
	bool layoutAvailable,
	CursesStyle headingStyle,
	CursesStyle focusedStyle
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( standard );
	ArgumentNullException.ThrowIfNull( headerWindow );
	ArgumentNullException.ThrowIfNull( leftWindow );
	ArgumentNullException.ThrowIfNull( rightWindow );
	ArgumentNullException.ThrowIfNull( footerWindow );
	ArgumentNullException.ThrowIfNull( popup );
	ArgumentNullException.ThrowIfNull( router );
	ArgumentNullException.ThrowIfNull( leftRegion );
	ArgumentNullException.ThrowIfNull( rightRegion );
	ArgumentNullException.ThrowIfNull( popupRegion );
	ArgumentNullException.ThrowIfNull( state );

	standard.Clear();
	if ( !layoutAvailable ) {
		WriteLine(
			standard,
			0,
			$"Icod.DCurses 1.4 interaction sample — resize to at least {MinimumColumns}x{MinimumRows}; current {screen.Columns}x{screen.Rows}",
			headingStyle
		);
		WriteLine(
			standard,
			1,
			"Router, regions, and command bindings remain retained while geometry is unavailable."
		);
		return;
	}

	CursesInteractionRegion? focused = router.FocusedRegion;
	headerWindow.Clear();
	leftWindow.Clear();
	rightWindow.Clear();
	footerWindow.Clear();
	popup.ContentWindow.Clear();

	string terminalFocus = state.TerminalFocus?.ToString() ?? "unknown";
	WriteLine(
		headerWindow,
		0,
		$"Icod.DCurses 1.4 interactions | terminal focus={terminalFocus} | {state.Status}",
		headingStyle
	);

	DrawPane(
		leftWindow,
		"LEFT PANE",
		ReferenceEquals(
			focused,
			leftRegion
		),
		"x: local action (shadows global x)",
		"Tab / Shift+Tab: traverse logical focus",
		headingStyle,
		focusedStyle
	);
	DrawPane(
		rightWindow,
		"RIGHT PANE",
		ReferenceEquals(
			focused,
			rightRegion
		),
		"r: right-local action",
		"x: global action while this pane has focus",
		headingStyle,
		focusedStyle
	);

	WriteLine(
		footerWindow,
		0,
		"F2 popup | Esc closes popup then quits | q quit | mouse reports local coordinates"
	);
	WriteLine(
		footerWindow,
		1,
		$"Route: {state.RoutedTarget} | applied pointer={state.AppliedPointerShape?.ToString() ?? "terminal policy"}"
	);

	if ( state.PopupVisible && popup.IsVisible ) {
		WriteLine(
			popup.ContentWindow,
			0,
			ReferenceEquals(
				focused,
				popupRegion
			)
				? "POPUP [LOGICAL FOCUS]"
				: "POPUP",
			ReferenceEquals(
				focused,
				popupRegion
			)
				? focusedStyle
				: headingStyle
		);
		WriteLine(
			popup.ContentWindow,
			2,
			"Retained panel wins mouse routing over body panes."
		);
		WriteLine(
			popup.ContentWindow,
			4,
			"Pointer preference: Move"
		);
		WriteLine(
			popup.ContentWindow,
			6,
			"F2 or Esc closes without a nested event loop."
		);
	}
}

static void DrawPane(
	CursesWindow window,
	string title,
	bool focused,
	string firstLine,
	string secondLine,
	CursesStyle headingStyle,
	CursesStyle focusedStyle
) {
	ArgumentNullException.ThrowIfNull( window );
	ArgumentException.ThrowIfNullOrWhiteSpace( title );
	ArgumentNullException.ThrowIfNull( firstLine );
	ArgumentNullException.ThrowIfNull( secondLine );

	WriteLine(
		window,
		0,
		focused
			? $"{title} [LOGICAL FOCUS]"
			: title,
		focused
			? focusedStyle
			: headingStyle
	);
	WriteLine(
		window,
		2,
		firstLine
	);
	WriteLine(
		window,
		4,
		secondLine
	);
}

static string GetRegionName(
	CursesInteractionRegion region,
	CursesInteractionRegion header,
	CursesInteractionRegion left,
	CursesInteractionRegion right,
	CursesInteractionRegion footer,
	CursesInteractionRegion popup
) {
	ArgumentNullException.ThrowIfNull( region );
	ArgumentNullException.ThrowIfNull( header );
	ArgumentNullException.ThrowIfNull( left );
	ArgumentNullException.ThrowIfNull( right );
	ArgumentNullException.ThrowIfNull( footer );
	ArgumentNullException.ThrowIfNull( popup );

	if ( ReferenceEquals(
		region,
		header
	) ) {
		return "header";
	}
	if ( ReferenceEquals(
		region,
		left
	) ) {
		return "left";
	}
	if ( ReferenceEquals(
		region,
		right
	) ) {
		return "right";
	}
	if ( ReferenceEquals(
		region,
		footer
	) ) {
		return "footer";
	}
	if ( ReferenceEquals(
		region,
		popup
	) ) {
		return "popup";
	}
	return "unknown";
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
