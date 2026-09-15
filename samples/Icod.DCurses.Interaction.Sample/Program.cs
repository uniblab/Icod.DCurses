/*
	Icod.DCurses.Interaction.Sample
	Interactive 1.5 advanced-interaction acceptance sample for Icod.DCurses.
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

List<CursesInputProtocolLease> protocolLeases = [];
CursesPointerShapeLease? pointerShapeLease = null;
CursesPointerShape? appliedPointerShape = null;
CursesPointerCaptureLease? pointerCaptureLease = null;
CursesInteractionScopeLease? popupScopeLease = null;
CursesInteractionScopeLease? nestedPopupScopeLease = null;
CursesFocusState? terminalFocus = null;
string statusText = "Ready";
string routedTargetText = "-";
bool running = true;
bool popupVisible = false;
int dragOffsetRow = 0;
int dragOffsetColumn = 0;

await TryAcquireInputProtocolAsync(
	session,
	new CursesInputProtocolOptions {
		KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
	},
	protocolLeases
);
await TryAcquireInputProtocolAsync(
	session,
	new CursesInputProtocolOptions {
		FocusReporting = true
	},
	protocolLeases
);
await TryAcquireInputProtocolAsync(
	session,
	new CursesInputProtocolOptions {
		MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents
	},
	protocolLeases
);

CursesWindow headerWindow = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow leftWindow = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow rightWindow = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow footerWindow = screen.CreateWindow( 0, 0, 1, 1 );
headerWindow.WrapMode = CursesWrapMode.Clip;
leftWindow.WrapMode = CursesWrapMode.Clip;
rightWindow.WrapMode = CursesWrapMode.Clip;
footerWindow.WrapMode = CursesWrapMode.Clip;

using CursesPanel popup = screen.CreatePanel( 0, 0, 1, 1 );
popup.ContentWindow.WrapMode = CursesWrapMode.Clip;
popup.Hide();

using CursesInteractionRouter router = new( screen );
using CursesInteractionScope popupScope = router.RegisterScope();
using CursesInteractionScope nestedPopupScope = router.RegisterScope(
	new CursesInteractionScopeOptions {
		Parent = popupScope
	}
);

CursesRectangle empty = new( 0, 0, 0, 0 );
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
		Scope = nestedPopupScope,
		IsFocusable = true,
		TraversalOrder = 2,
		PointerShape = CursesPointerShape.Move
	}
);

CursesCommand focusNextCommand = new( "focus.next" );
CursesCommand focusPreviousCommand = new( "focus.previous" );
CursesCommand focusUpCommand = new( "focus.up" );
CursesCommand focusDownCommand = new( "focus.down" );
CursesCommand focusLeftCommand = new( "focus.left" );
CursesCommand focusRightCommand = new( "focus.right" );
CursesCommand popupToggleCommand = new( "popup.toggle" );
CursesCommand popupScopeCommand = new( "popup.scope" );
CursesCommand popupNestedCommand = new( "popup.nested" );
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
	CursesKeyGesture.ForKey( CursesKey.Up ),
	focusUpCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForKey( CursesKey.Down ),
	focusDownCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForKey( CursesKey.Left ),
	focusLeftCommand
);
router.BindGlobalGesture(
	CursesKeyGesture.ForKey( CursesKey.Right ),
	focusRightCommand
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
popupScope.BindGesture(
	CursesKeyGesture.ForCharacter( new Rune( 's' ) ),
	popupScopeCommand
);
nestedPopupScope.BindGesture(
	CursesKeyGesture.ForCharacter( new Rune( 'n' ) ),
	popupNestedCommand
);

bool layoutAvailable = ApplyLayoutPolicy(
	screen,
	headerWindow,
	leftWindow,
	rightWindow,
	footerWindow,
	popup,
	headerRegion,
	leftRegion,
	rightRegion,
	footerRegion,
	popupVisible
);
if ( layoutAvailable ) {
	_ = router.Focus( leftRegion );
}

try {
	while ( running ) {
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
			terminalFocus,
			statusText,
			routedTargetText,
			appliedPointerShape,
			popupVisible,
			nestedPopupScopeLease is not null,
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
					running = false;
					break;
				}

				if ( CursesInputEventKind.Focus == input.Kind
					&& input.Focus is not null ) {
					terminalFocus = input.Focus.State;
					statusText = $"Terminal focus: {input.Focus.State}";
				}

				if ( routed.Command is not null ) {
					switch ( routed.Command.Name ) {
						case "focus.next":
							_ = router.MoveFocus( CursesFocusDirection.Forward );
							statusText = "Focus moved forward";
							break;

						case "focus.previous":
							_ = router.MoveFocus( CursesFocusDirection.Backward );
							statusText = "Focus moved backward";
							break;

						case "focus.up":
							_ = router.MoveFocus( CursesFocusDirection.Up );
							statusText = "Spatial focus: up";
							break;

						case "focus.down":
							_ = router.MoveFocus( CursesFocusDirection.Down );
							statusText = "Spatial focus: down";
							break;

						case "focus.left":
							_ = router.MoveFocus( CursesFocusDirection.Left );
							statusText = "Spatial focus: left";
							break;

						case "focus.right":
							_ = router.MoveFocus( CursesFocusDirection.Right );
							statusText = "Spatial focus: right";
							break;

						case "popup.toggle":
							if ( popupVisible ) {
								ClosePopup(
									popup,
									ref pointerCaptureLease,
									ref nestedPopupScopeLease,
									ref popupScopeLease
								);
								popupVisible = false;
								statusText = "Popup scope closed; prior root focus restored";
							} else if ( layoutAvailable ) {
								popup.Show();
								popupScopeLease = router.ActivateScope( popupScope );
								popupVisible = true;
								_ = router.Focus( popupRegion );
								statusText = "Popup modal scope activated";
							} else {
								statusText = "Popup unavailable below minimum terminal size";
							}
							break;

						case "popup.scope":
							statusText = "Outer popup-scope command resolved";
							break;

						case "popup.nested":
							if ( nestedPopupScopeLease is null ) {
								nestedPopupScopeLease = router.ActivateScope( nestedPopupScope );
								statusText = "Nested scope activated; outer scope commands are now outside the boundary";
							} else {
								nestedPopupScopeLease.Dispose();
								nestedPopupScopeLease = null;
								statusText = "Nested scope closed; saved popup focus restored";
							}
							break;

						case "escape":
							if ( popupVisible ) {
								ClosePopup(
									popup,
									ref pointerCaptureLease,
									ref nestedPopupScopeLease,
									ref popupScopeLease
								);
								popupVisible = false;
								statusText = "Popup closed";
							} else {
								running = false;
							}
							break;

						case "left.action":
							statusText = "Left local x won over global x";
							break;

						case "right.action":
							statusText = "Right local r action";
							break;

						case "global.x":
							statusText = "Global x command";
							break;

						case "quit":
							running = false;
							break;
					}
				}

				if ( CursesInputEventKind.Mouse == input.Kind
					&& input.Mouse is not null ) {
					CursesMouseEvent mouse = input.Mouse;
					if ( CursesMouseAction.Press == mouse.Action
						&& CursesMouseButton.Primary == mouse.Button
						&& ReferenceEquals(
							routed.Region,
							popupRegion
						) ) {
						dragOffsetRow = routed.Hit?.LocalRow ?? 0;
						dragOffsetColumn = routed.Hit?.LocalColumn ?? 0;
						pointerCaptureLease?.Dispose();
						pointerCaptureLease = router.CapturePointer(
							popupRegion,
							CursesMouseButton.Primary
						);
						statusText = "Popup acquired explicit primary-button pointer capture";
					}

					CursesPointerTarget? pointerTarget = routed.PointerTarget;
					CursesPointerGestureKind? gestureKind = routed.PointerGesture?.Kind;
					if ( pointerTarget is not null
						&& ReferenceEquals(
							pointerTarget.Region,
							popupRegion
						)
						&& gestureKind is CursesPointerGestureKind.DragStart
							or CursesPointerGestureKind.DragMove ) {
						int maximumRow = Math.Max(
							0,
							screen.Rows - popup.Rows
						);
						int maximumColumn = Math.Max(
							0,
							screen.Columns - popup.Columns
						);
						popup.MoveTo(
							Math.Clamp(
								mouse.Row - dragOffsetRow,
								0,
								maximumRow
							),
							Math.Clamp(
								mouse.Column - dragOffsetColumn,
								0,
								maximumColumn
							)
						);
						statusText = $"{gestureKind}: application policy moved the retained popup";
					}

					if ( gestureKind is CursesPointerGestureKind.DragEnd
						or CursesPointerGestureKind.Release ) {
						pointerCaptureLease?.Dispose();
						pointerCaptureLease = null;
					}

					if ( pointerTarget is not null ) {
						routedTargetText = $"captured {GetRegionName(
							pointerTarget.Region,
							headerRegion,
							leftRegion,
							rightRegion,
							footerRegion,
							popupRegion
						)} local=({pointerTarget.LocalRow},{pointerTarget.LocalColumn}) inside={pointerTarget.IsInside} gesture={gestureKind}";
					} else if ( routed.Hit is not null ) {
						routedTargetText = $"{GetRegionName(
							routed.Hit.Region,
							headerRegion,
							leftRegion,
							rightRegion,
							footerRegion,
							popupRegion
						)} local=({routed.Hit.LocalRow},{routed.Hit.LocalColumn}) gesture={gestureKind}";
					} else {
						routedTargetText = $"none screen=({mouse.Row},{mouse.Column}) gesture={gestureKind}";
					}

					CursesPointerShape? desiredShape = routed.Hit?.PointerShape
						?? routed.PointerTarget?.Region.PointerShape;
					if ( desiredShape != appliedPointerShape ) {
						CursesPointerShapeLease? replacement = null;
						if ( desiredShape.HasValue ) {
							try {
								replacement = await session.AcquirePointerShapeAsync(
									desiredShape.Value
								);
							} catch ( Exception exception ) {
								statusText = $"Pointer shape failed: {exception.Message}";
							}
						}

						CursesPointerShapeLease? prior = pointerShapeLease;
						pointerShapeLease = replacement;
						appliedPointerShape = replacement?.Shape;
						if ( prior is not null ) {
							await prior.DisposeAsync();
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
					running = false;
					break;
				}
				if ( current.Lifecycle.Kind is CursesLifecycleEventKind.Resize
					or CursesLifecycleEventKind.Resumed ) {
					_ = session.SynchronizeDimensions();
					if ( screen.Columns < MinimumColumns
						|| screen.Rows < MinimumRows ) {
						if ( popupVisible ) {
							ClosePopup(
								popup,
								ref pointerCaptureLease,
								ref nestedPopupScopeLease,
								ref popupScopeLease
							);
							popupVisible = false;
						}
					}
					layoutAvailable = ApplyLayoutPolicy(
						screen,
						headerWindow,
						leftWindow,
						rightWindow,
						footerWindow,
						popup,
						headerRegion,
						leftRegion,
						rightRegion,
						footerRegion,
						popupVisible
					);
					if ( layoutAvailable && router.FocusedRegion is null ) {
						_ = router.Focus( leftRegion );
					}
					session.Invalidate();
				}
				break;

			case CursesEventKind.Timeout:
				break;
		}
	}
} finally {
	ClosePopup(
		popup,
		ref pointerCaptureLease,
		ref nestedPopupScopeLease,
		ref popupScopeLease
	);
	if ( pointerShapeLease is not null ) {
		await pointerShapeLease.DisposeAsync();
	}
	for ( int index = protocolLeases.Count - 1; 0 <= index; index-- ) {
		await protocolLeases[ index ].DisposeAsync();
	}
}

return 0;

static async ValueTask TryAcquireInputProtocolAsync(
	CursesSession session,
	CursesInputProtocolOptions options,
	ICollection<CursesInputProtocolLease> leases
) {
	ArgumentNullException.ThrowIfNull( session );
	ArgumentNullException.ThrowIfNull( options );
	ArgumentNullException.ThrowIfNull( leases );

	var result = await session.AcquireInputProtocolsAsync( options );
	if ( result.IsAvailable ) {
		leases.Add( result.GetRequiredValue() );
	}
}

static bool ApplyLayoutPolicy(
	CursesScreen screen,
	CursesWindow headerWindow,
	CursesWindow leftWindow,
	CursesWindow rightWindow,
	CursesWindow footerWindow,
	CursesPanel popup,
	CursesInteractionRegion headerRegion,
	CursesInteractionRegion leftRegion,
	CursesInteractionRegion rightRegion,
	CursesInteractionRegion footerRegion,
	bool popupVisible
) {
	ArgumentNullException.ThrowIfNull( screen );
	ArgumentNullException.ThrowIfNull( headerWindow );
	ArgumentNullException.ThrowIfNull( leftWindow );
	ArgumentNullException.ThrowIfNull( rightWindow );
	ArgumentNullException.ThrowIfNull( footerWindow );
	ArgumentNullException.ThrowIfNull( popup );

	if ( screen.Columns < MinimumColumns
		|| screen.Rows < MinimumRows ) {
		popup.Hide();
		CursesRectangle unavailable = new( 0, 0, 0, 0 );
		headerRegion.SetBounds( unavailable );
		leftRegion.SetBounds( unavailable );
		rightRegion.SetBounds( unavailable );
		footerRegion.SetBounds( unavailable );
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

	if ( popupVisible ) {
		popup.Show();
	} else {
		popup.Hide();
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

static void ClosePopup(
	CursesPanel popup,
	ref CursesPointerCaptureLease? captureLease,
	ref CursesInteractionScopeLease? nestedLease,
	ref CursesInteractionScopeLease? outerLease
) {
	ArgumentNullException.ThrowIfNull( popup );
	captureLease?.Dispose();
	captureLease = null;
	if ( nestedLease is not null ) {
		nestedLease.Dispose();
		nestedLease = null;
	}
	if ( outerLease is not null ) {
		outerLease.Dispose();
		outerLease = null;
	}
	popup.Hide();
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
	CursesFocusState? terminalFocus,
	string statusText,
	string routedTargetText,
	CursesPointerShape? appliedPointerShape,
	bool popupVisible,
	bool nestedScopeActive,
	bool layoutAvailable,
	CursesStyle headingStyle,
	CursesStyle focusedStyle
) {
	standard.Clear();
	if ( !layoutAvailable ) {
		WriteLine(
			standard,
			0,
			$"Icod.DCurses 1.5 interaction sample — resize to at least {MinimumColumns}x{MinimumRows}; current {screen.Columns}x{screen.Rows}",
			headingStyle
		);
		WriteLine(
			standard,
			1,
			"Router, scopes, bindings, and region identity remain retained while geometry is unavailable."
		);
		return;
	}

	CursesInteractionRegion? focused = router.FocusedRegion;
	headerWindow.Clear();
	leftWindow.Clear();
	rightWindow.Clear();
	footerWindow.Clear();
	popup.ContentWindow.Clear();

	WriteLine(
		headerWindow,
		0,
		$"Icod.DCurses 1.5 advanced interactions | terminal focus={terminalFocus?.ToString() ?? "unknown"} | {statusText}",
		headingStyle
	);
	DrawPane(
		leftWindow,
		"LEFT PANE",
		ReferenceEquals( focused, leftRegion ),
		"x: local action (shadows global x)",
		"Tab/Shift+Tab or arrows: logical focus",
		headingStyle,
		focusedStyle
	);
	DrawPane(
		rightWindow,
		"RIGHT PANE",
		ReferenceEquals( focused, rightRegion ),
		"r: right-local action",
		"x: global action while this pane has focus",
		headingStyle,
		focusedStyle
	);
	WriteLine(
		footerWindow,
		0,
		"F2 modal popup | arrows spatial focus | S outer scope | N nested scope | q quit"
	);
	WriteLine(
		footerWindow,
		1,
		$"Route: {routedTargetText} | pointer={appliedPointerShape?.ToString() ?? "terminal policy"}"
	);

	if ( popupVisible && popup.IsVisible ) {
		WriteLine(
			popup.ContentWindow,
			0,
			ReferenceEquals( focused, popupRegion )
				? "POPUP [LOGICAL FOCUS]"
				: "POPUP",
			ReferenceEquals( focused, popupRegion )
				? focusedStyle
				: headingStyle
		);
		WriteLine(
			popup.ContentWindow,
			2,
			"Primary drag: explicit capture + DragStart/DragMove/DragEnd"
		);
		WriteLine(
			popup.ContentWindow,
			4,
			nestedScopeActive
				? "N: leave nested scope | outer S is outside boundary"
				: "S: outer scoped command | N: enter nested scope"
		);
		WriteLine(
			popup.ContentWindow,
			6,
			"Router supplies mechanism; this sample owns move/command policy."
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
	WriteLine( window, 2, firstLine );
	WriteLine( window, 4, secondLine );
}

static string GetRegionName(
	CursesInteractionRegion region,
	CursesInteractionRegion header,
	CursesInteractionRegion left,
	CursesInteractionRegion right,
	CursesInteractionRegion footer,
	CursesInteractionRegion popup
) {
	if ( ReferenceEquals( region, header ) ) {
		return "header";
	}
	if ( ReferenceEquals( region, left ) ) {
		return "left";
	}
	if ( ReferenceEquals( region, right ) ) {
		return "right";
	}
	if ( ReferenceEquals( region, footer ) ) {
		return "footer";
	}
	if ( ReferenceEquals( region, popup ) ) {
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
	window.Move( row, 0 );
	window.Write( text, style );
}
