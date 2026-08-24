namespace Icod.DCurses;

using Icod.DCurses.Terminal;

/// <summary>Logical-screen ownership for <see cref="CursesSession"/>.</summary>
public sealed partial class CursesSession {
	private readonly object screenSync = new();
	private CursesScreen? screen;

	/// <summary>
	/// Gets the lazily created logical screen sized from the current terminal dimensions.
	/// </summary>
	public CursesScreen Screen {
		get {
			lock ( screenSync ) {
				if ( null == screen ) {
					TerminalBackendResult<TerminalSize> dimensions = GetDimensions();
					if ( !dimensions.IsAvailable ) {
						throw new InvalidOperationException(
							dimensions.Message
								?? "The terminal dimensions are unavailable for logical-screen creation."
						);
					}

					TerminalSize size = dimensions.GetRequiredValue();
					screen = new CursesScreen(
						size.Columns,
						size.Rows
					);
				}

				return screen;
			}
		}
	}

	/// <summary>Gets the standard window covering <see cref="Screen"/>.</summary>
	public CursesWindow StandardScreen => Screen.StandardWindow;

	internal void ResizeLogicalScreen(
		int columns,
		int rows ) {
		lock ( screenSync ) {
			screen?.Resize(
				columns,
				rows
			);
		}
	}
}
