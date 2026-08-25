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
				input.FunctionKeyNumber
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
			_ => throw new ArgumentOutOfRangeException( nameof( key ) )
		};
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
		return converted;
	}
}
