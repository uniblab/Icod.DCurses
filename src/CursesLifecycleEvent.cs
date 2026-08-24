namespace Icod.DCurses;

using Icod.DCurses.Terminal;

/// <summary>
/// Identifies a managed terminal or process lifecycle event observed by a curses session.
/// </summary>
public enum CursesLifecycleEventKind {
	/// <summary>The terminal dimensions may have changed.</summary>
	Resize,

	/// <summary>An interactive interrupt request, such as Ctrl+C or SIGINT, was observed.</summary>
	Interrupt,

	/// <summary>A termination request, such as SIGTERM, SIGHUP, SIGQUIT, or Ctrl+Break, was observed.</summary>
	Termination,

	/// <summary>The session restored terminal state immediately before a supported POSIX suspension.</summary>
	Suspending,

	/// <summary>The process resumed and the curses presentation was re-entered.</summary>
	Resumed
}

/// <summary>
/// Represents one managed lifecycle notification delivered by a <see cref="CursesSession"/>.
/// </summary>
public sealed class CursesLifecycleEvent {
	internal CursesLifecycleEvent(
		CursesLifecycleEventKind kind,
		TerminalSize? dimensions = null ) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		Kind = kind;
		Dimensions = dimensions;
	}

	/// <summary>Gets the lifecycle event kind.</summary>
	public CursesLifecycleEventKind Kind {
		get;
	}

	/// <summary>
	/// Gets freshly observed terminal dimensions for resize and resume events when available.
	/// </summary>
	public TerminalSize? Dimensions {
		get;
	}

	/// <summary>
	/// Gets whether a renderer should treat its physical-screen image as invalid and repaint.
	/// </summary>
	public bool RequiresRepaint =>
		Kind is CursesLifecycleEventKind.Resize
			or CursesLifecycleEventKind.Resumed;
}
