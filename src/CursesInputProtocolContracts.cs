namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>
/// Identifies the requested intensity of terminal mouse tracking.
/// </summary>
public enum CursesMouseTrackingMode {
	/// <summary>Report mouse button press, release, and wheel activity.</summary>
	ButtonEvents,

	/// <summary>Report button activity plus motion while a button is held.</summary>
	ButtonMotion,

	/// <summary>Report button activity plus all mouse motion.</summary>
	AnyMotion
}

/// <summary>
/// Describes reversible rich-input protocol reporting requested by a curses consumer.
/// </summary>
public sealed class CursesInputProtocolOptions {
	/// <summary>Gets or initializes whether bracketed-paste reporting is required.</summary>
	public bool BracketedPaste {
		get;
		init;
	}

	/// <summary>Gets or initializes whether terminal focus reporting is required.</summary>
	public bool FocusReporting {
		get;
		init;
	}

	/// <summary>Gets or initializes the requested mouse tracking intensity, when any.</summary>
	public CursesMouseTrackingMode? MouseTrackingMode {
		get;
		init;
	}

	internal void Validate() {
		if ( this.MouseTrackingMode.HasValue
			&& !Enum.IsDefined( this.MouseTrackingMode.Value ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( this.MouseTrackingMode ),
				this.MouseTrackingMode.Value,
				"The curses mouse tracking mode is not recognized."
			);
		}
		if ( !this.BracketedPaste
			&& !this.FocusReporting
			&& !this.MouseTrackingMode.HasValue ) {
			throw new ArgumentException(
				"At least one curses input protocol must be requested."
			);
		}
	}

	internal TerminalInputProtocolOptions ToTerminalOptions() {
		this.Validate();
		return new TerminalInputProtocolOptions {
			BracketedPaste = this.BracketedPaste,
			FocusReporting = this.FocusReporting,
			MouseTrackingMode = this.MouseTrackingMode switch {
				CursesMouseTrackingMode.ButtonEvents => TerminalMouseTrackingMode.ButtonEvents,
				CursesMouseTrackingMode.ButtonMotion => TerminalMouseTrackingMode.ButtonMotion,
				CursesMouseTrackingMode.AnyMotion => TerminalMouseTrackingMode.AnyMotion,
				null => null,
				_ => throw new ArgumentOutOfRangeException( nameof( this.MouseTrackingMode ) )
			}
		};
	}
}

/// <summary>
/// Owns one reversible rich-input protocol request made through a curses session.
/// </summary>
public sealed class CursesInputProtocolLease : IAsyncDisposable {
	private readonly TerminalInputProtocolLease terminalLease;

	internal CursesInputProtocolLease(
		TerminalInputProtocolLease terminalLease,
		CursesInputProtocolOptions options
	) {
		ArgumentNullException.ThrowIfNull( terminalLease );
		ArgumentNullException.ThrowIfNull( options );

		this.terminalLease = terminalLease;
		this.BracketedPaste = options.BracketedPaste;
		this.FocusReporting = options.FocusReporting;
		this.MouseTrackingMode = options.MouseTrackingMode;
	}

	/// <summary>Gets whether this lease requests bracketed-paste reporting.</summary>
	public bool BracketedPaste {
		get;
	}

	/// <summary>Gets whether this lease requests focus reporting.</summary>
	public bool FocusReporting {
		get;
	}

	/// <summary>Gets the mouse tracking request owned by this lease, when any.</summary>
	public CursesMouseTrackingMode? MouseTrackingMode {
		get;
	}

	/// <summary>Releases this input-protocol request.</summary>
	public ValueTask DisposeAsync() {
		return this.terminalLease.DisposeAsync();
	}
}
