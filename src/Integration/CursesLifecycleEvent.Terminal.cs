namespace Icod.DCurses;

using Icod.TermInfo;

/// <summary>
/// Identifies a managed terminal or process lifecycle event observed by a curses session.
/// </summary>
public enum CursesLifecycleEventKind {
	/// <summary>The terminal dimensions may have changed.</summary>
	Resize,

	/// <summary>An interactive interrupt request was observed.</summary>
	Interrupt,

	/// <summary>A process termination request was observed.</summary>
	Termination,

	/// <summary>The session prepared higher-layer state immediately before POSIX suspension.</summary>
	Suspending,

	/// <summary>The process resumed and the curses presentation was re-entered.</summary>
	Resumed
}

/// <summary>Represents one lifecycle notification delivered by a <see cref="CursesSession"/>.</summary>
public sealed class CursesLifecycleEvent {
	internal CursesLifecycleEvent(
		CursesLifecycleEventKind kind,
		TerminalSize? dimensions = null
	) {
		if ( !Enum.IsDefined( kind ) ) {
			throw new ArgumentOutOfRangeException( nameof( kind ) );
		}

		this.Kind = kind;
		this.Dimensions = dimensions;
	}

	/// <summary>Gets the lifecycle event kind.</summary>
	public CursesLifecycleEventKind Kind {
		get;
	}

	/// <summary>Gets fresh dimensions for resize/resume events when available.</summary>
	public TerminalSize? Dimensions {
		get;
	}

	/// <summary>Gets whether the physical-screen image should be treated as invalid.</summary>
	public bool RequiresRepaint =>
		this.Kind is CursesLifecycleEventKind.Resize
			or CursesLifecycleEventKind.Resumed;
}
