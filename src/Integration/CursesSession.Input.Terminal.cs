namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>Maps canonical Terminal input/lifecycle events into the curses-shaped event facade.</summary>
public sealed partial class CursesSession {
	/// <summary>Gets the Terminal Escape ambiguity window used by this session.</summary>
	public static TimeSpan DefaultEscapeSequenceTimeout =>
		TerminalSession.DefaultEscapeSequenceTimeout;

	/// <summary>Waits indefinitely for decoded input or lifecycle activity.</summary>
	public ValueTask<CursesEvent> ReadEventAsync(
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		return this.ConvertTerminalEventAsync(
			this.terminalSession.ReadEventAsync( cancellationToken ),
			cancellationToken
		);
	}

	/// <summary>Waits for decoded input or lifecycle activity for at most the supplied interval.</summary>
	public ValueTask<CursesEvent> ReadEventAsync(
		TimeSpan timeout,
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		return this.ConvertTerminalEventAsync(
			this.terminalSession.ReadEventAsync( timeout, cancellationToken ),
			cancellationToken
		);
	}

	/// <summary>Waits for decoded input or lifecycle activity until the supplied deadline.</summary>
	public ValueTask<CursesEvent> ReadEventAsync(
		DateTimeOffset deadline,
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		return this.ConvertTerminalEventAsync(
			this.terminalSession.ReadEventAsync( deadline, cancellationToken ),
			cancellationToken
		);
	}

	private async ValueTask<CursesEvent> ConvertTerminalEventAsync(
		ValueTask<TerminalEvent> pending,
		CancellationToken cancellationToken
	) {
		TerminalEvent terminalEvent = await pending.ConfigureAwait( false );
		return terminalEvent.Kind switch {
			TerminalEventKind.Input => CursesEvent.FromInput(
				ConvertInputEvent(
					terminalEvent.Input
						?? throw new InvalidOperationException(
							"Terminal input event payload is missing."
						)
				)
			),
			TerminalEventKind.Lifecycle => CursesEvent.FromLifecycle(
				this.ConvertLifecycleEvent(
					terminalEvent.Lifecycle
						?? throw new InvalidOperationException(
							"Terminal lifecycle event payload is missing."
						)
				)
			),
			TerminalEventKind.Timeout => CursesEvent.TimedOut(),
			TerminalEventKind.Cancelled => throw new OperationCanceledException( cancellationToken ),
			_ => throw new ArgumentOutOfRangeException(
				nameof( terminalEvent ),
				terminalEvent.Kind,
				"The Terminal event kind is not recognized."
			)
		};
	}

	private static CursesInputEvent ConvertInputEvent(
		TerminalInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );
		return input.Kind switch {
			TerminalInputEventKind.Text => CursesInputEvent.FromText(
				input.Character
					?? throw new InvalidOperationException(
						"Terminal text event has no character."
					)
			),
			TerminalInputEventKind.Key => CursesInputEvent.FromKey(
				ConvertKey( input.Key ),
				ConvertModifiers( input.Modifiers ),
				input.Character,
				input.FunctionKeyNumber,
				ConvertKeyPhase(
					input.KeyPhase
						?? throw new InvalidOperationException(
							"Terminal key event has no key phase."
						)
				),
				input.ShiftedCharacter,
				input.BaseLayoutCharacter,
				input.AssociatedText
			),
			TerminalInputEventKind.Mouse => CursesInputEvent.FromMouse(
				ConvertMouseEvent(
					input.Mouse
						?? throw new InvalidOperationException(
							"Terminal mouse event payload is missing."
						)
				)
			),
			TerminalInputEventKind.Focus => CursesInputEvent.FromFocus(
				ConvertFocusEvent(
					input.Focus
						?? throw new InvalidOperationException(
							"Terminal focus event payload is missing."
						)
				)
			),
			TerminalInputEventKind.Paste => CursesInputEvent.FromPaste(
				ConvertPasteEvent(
					input.Paste
						?? throw new InvalidOperationException(
							"Terminal paste event payload is missing."
						)
				)
			),
			TerminalInputEventKind.EndOfInput => CursesInputEvent.EndOfInput(),
			_ => throw new ArgumentOutOfRangeException(
				nameof( input ),
				input.Kind,
				"The Terminal input-event kind is not recognized."
			)
		};
	}

	private static CursesKey ConvertKey(
		TerminalKey key
	) {
		return key switch {
			TerminalKey.None => CursesKey.None,
			TerminalKey.Character => CursesKey.Character,
			TerminalKey.Enter => CursesKey.Enter,
			TerminalKey.Space => CursesKey.Space,
			TerminalKey.Escape => CursesKey.Escape,
			TerminalKey.Backspace => CursesKey.Backspace,
			TerminalKey.Tab => CursesKey.Tab,
			TerminalKey.Up => CursesKey.Up,
			TerminalKey.Down => CursesKey.Down,
			TerminalKey.Left => CursesKey.Left,
			TerminalKey.Right => CursesKey.Right,
			TerminalKey.Home => CursesKey.Home,
			TerminalKey.End => CursesKey.End,
			TerminalKey.PageUp => CursesKey.PageUp,
			TerminalKey.PageDown => CursesKey.PageDown,
			TerminalKey.Insert => CursesKey.Insert,
			TerminalKey.Delete => CursesKey.Delete,
			TerminalKey.Function => CursesKey.Function,
			TerminalKey.CapsLock => CursesKey.CapsLock,
			TerminalKey.ScrollLock => CursesKey.ScrollLock,
			TerminalKey.NumLock => CursesKey.NumLock,
			TerminalKey.PrintScreen => CursesKey.PrintScreen,
			TerminalKey.Pause => CursesKey.Pause,
			TerminalKey.Menu => CursesKey.Menu,
			TerminalKey.Keypad0 => CursesKey.Keypad0,
			TerminalKey.Keypad1 => CursesKey.Keypad1,
			TerminalKey.Keypad2 => CursesKey.Keypad2,
			TerminalKey.Keypad3 => CursesKey.Keypad3,
			TerminalKey.Keypad4 => CursesKey.Keypad4,
			TerminalKey.Keypad5 => CursesKey.Keypad5,
			TerminalKey.Keypad6 => CursesKey.Keypad6,
			TerminalKey.Keypad7 => CursesKey.Keypad7,
			TerminalKey.Keypad8 => CursesKey.Keypad8,
			TerminalKey.Keypad9 => CursesKey.Keypad9,
			TerminalKey.KeypadDecimal => CursesKey.KeypadDecimal,
			TerminalKey.KeypadDivide => CursesKey.KeypadDivide,
			TerminalKey.KeypadMultiply => CursesKey.KeypadMultiply,
			TerminalKey.KeypadSubtract => CursesKey.KeypadSubtract,
			TerminalKey.KeypadAdd => CursesKey.KeypadAdd,
			TerminalKey.KeypadEnter => CursesKey.KeypadEnter,
			TerminalKey.KeypadEqual => CursesKey.KeypadEqual,
			TerminalKey.KeypadSeparator => CursesKey.KeypadSeparator,
			TerminalKey.KeypadLeft => CursesKey.KeypadLeft,
			TerminalKey.KeypadRight => CursesKey.KeypadRight,
			TerminalKey.KeypadUp => CursesKey.KeypadUp,
			TerminalKey.KeypadDown => CursesKey.KeypadDown,
			TerminalKey.KeypadPageUp => CursesKey.KeypadPageUp,
			TerminalKey.KeypadPageDown => CursesKey.KeypadPageDown,
			TerminalKey.KeypadHome => CursesKey.KeypadHome,
			TerminalKey.KeypadEnd => CursesKey.KeypadEnd,
			TerminalKey.KeypadInsert => CursesKey.KeypadInsert,
			TerminalKey.KeypadDelete => CursesKey.KeypadDelete,
			TerminalKey.KeypadBegin => CursesKey.KeypadBegin,
			TerminalKey.MediaPlay => CursesKey.MediaPlay,
			TerminalKey.MediaPause => CursesKey.MediaPause,
			TerminalKey.MediaPlayPause => CursesKey.MediaPlayPause,
			TerminalKey.MediaReverse => CursesKey.MediaReverse,
			TerminalKey.MediaStop => CursesKey.MediaStop,
			TerminalKey.MediaFastForward => CursesKey.MediaFastForward,
			TerminalKey.MediaRewind => CursesKey.MediaRewind,
			TerminalKey.MediaTrackNext => CursesKey.MediaTrackNext,
			TerminalKey.MediaTrackPrevious => CursesKey.MediaTrackPrevious,
			TerminalKey.MediaRecord => CursesKey.MediaRecord,
			TerminalKey.VolumeDown => CursesKey.VolumeDown,
			TerminalKey.VolumeUp => CursesKey.VolumeUp,
			TerminalKey.VolumeMute => CursesKey.VolumeMute,
			TerminalKey.LeftShift => CursesKey.LeftShift,
			TerminalKey.LeftControl => CursesKey.LeftControl,
			TerminalKey.LeftAlt => CursesKey.LeftAlt,
			TerminalKey.LeftSuper => CursesKey.LeftSuper,
			TerminalKey.LeftHyper => CursesKey.LeftHyper,
			TerminalKey.LeftMeta => CursesKey.LeftMeta,
			TerminalKey.RightShift => CursesKey.RightShift,
			TerminalKey.RightControl => CursesKey.RightControl,
			TerminalKey.RightAlt => CursesKey.RightAlt,
			TerminalKey.RightSuper => CursesKey.RightSuper,
			TerminalKey.RightHyper => CursesKey.RightHyper,
			TerminalKey.RightMeta => CursesKey.RightMeta,
			TerminalKey.IsoLevel3Shift => CursesKey.IsoLevel3Shift,
			TerminalKey.IsoLevel5Shift => CursesKey.IsoLevel5Shift,
			TerminalKey.Unrecognized => CursesKey.Unrecognized,
			_ => throw new ArgumentOutOfRangeException( nameof( key ) )
		};
	}

	private static CursesKeyEventPhase ConvertKeyPhase(
		TerminalKeyEventPhase phase
	) {
		return phase switch {
			TerminalKeyEventPhase.Press => CursesKeyEventPhase.Press,
			TerminalKeyEventPhase.Repeat => CursesKeyEventPhase.Repeat,
			TerminalKeyEventPhase.Release => CursesKeyEventPhase.Release,
			_ => throw new ArgumentOutOfRangeException( nameof( phase ) )
		};
	}

	private static CursesMouseEvent ConvertMouseEvent(
		TerminalMouseEvent mouse
	) {
		ArgumentNullException.ThrowIfNull( mouse );
		return new CursesMouseEvent(
			mouse.Action switch {
				TerminalMouseAction.Press => CursesMouseAction.Press,
				TerminalMouseAction.Release => CursesMouseAction.Release,
				TerminalMouseAction.Move => CursesMouseAction.Move,
				TerminalMouseAction.WheelUp => CursesMouseAction.WheelUp,
				TerminalMouseAction.WheelDown => CursesMouseAction.WheelDown,
				TerminalMouseAction.WheelLeft => CursesMouseAction.WheelLeft,
				TerminalMouseAction.WheelRight => CursesMouseAction.WheelRight,
				_ => throw new ArgumentOutOfRangeException( nameof( mouse ) )
			},
			mouse.Button switch {
				TerminalMouseButton.None => CursesMouseButton.None,
				TerminalMouseButton.Primary => CursesMouseButton.Primary,
				TerminalMouseButton.Middle => CursesMouseButton.Middle,
				TerminalMouseButton.Secondary => CursesMouseButton.Secondary,
				TerminalMouseButton.Button4 => CursesMouseButton.Button4,
				TerminalMouseButton.Button5 => CursesMouseButton.Button5,
				TerminalMouseButton.Button6 => CursesMouseButton.Button6,
				TerminalMouseButton.Button7 => CursesMouseButton.Button7,
				_ => throw new ArgumentOutOfRangeException( nameof( mouse ) )
			},
			mouse.Column,
			mouse.Row,
			ConvertModifiers( mouse.Modifiers )
		);
	}

	private static CursesFocusEvent ConvertFocusEvent(
		TerminalFocusEvent focus
	) {
		ArgumentNullException.ThrowIfNull( focus );
		return new CursesFocusEvent(
			focus.State switch {
				TerminalFocusState.Focused => CursesFocusState.Focused,
				TerminalFocusState.Unfocused => CursesFocusState.Unfocused,
				_ => throw new ArgumentOutOfRangeException( nameof( focus ) )
			}
		);
	}

	private static CursesPasteEvent ConvertPasteEvent(
		TerminalPasteEvent paste
	) {
		ArgumentNullException.ThrowIfNull( paste );
		return new CursesPasteEvent(
			paste.Phase switch {
				TerminalPastePhase.Begin => CursesPastePhase.Begin,
				TerminalPastePhase.Data => CursesPastePhase.Data,
				TerminalPastePhase.End => CursesPastePhase.End,
				_ => throw new ArgumentOutOfRangeException( nameof( paste ) )
			},
			paste.Text
		);
	}

	private static CursesKeyModifiers ConvertModifiers(
		TerminalKeyModifiers modifiers
	) {
		CursesKeyModifiers converted = CursesKeyModifiers.None;
		if ( 0 != ( modifiers & TerminalKeyModifiers.Shift ) ) {
			converted |= CursesKeyModifiers.Shift;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.Control ) ) {
			converted |= CursesKeyModifiers.Control;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.Alt ) ) {
			converted |= CursesKeyModifiers.Alt;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.Super ) ) {
			converted |= CursesKeyModifiers.Super;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.Hyper ) ) {
			converted |= CursesKeyModifiers.Hyper;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.Meta ) ) {
			converted |= CursesKeyModifiers.Meta;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.CapsLock ) ) {
			converted |= CursesKeyModifiers.CapsLock;
		}
		if ( 0 != ( modifiers & TerminalKeyModifiers.NumLock ) ) {
			converted |= CursesKeyModifiers.NumLock;
		}
		return converted;
	}
}
