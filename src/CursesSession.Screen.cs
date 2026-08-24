namespace Icod.DCurses;

using Icod.DCurses.Terminal;

/// <summary>Logical-screen ownership for <see cref="CursesSession"/>.</summary>
public sealed partial class CursesSession {
	private readonly object screenSync = new();
	private CursesScreen? screen;

	/// <summary>
	/// Gets the logical screen sized from the most recently observed terminal dimensions.
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

	/// <summary>
	/// Reobserves live terminal dimensions and synchronizes the logical screen when they changed.
	/// </summary>
	/// <returns>
	/// The controlled dimension result. An unavailable result is returned unchanged; no fallback size is invented.
	/// </returns>
	public TerminalBackendResult<TerminalSize> SynchronizeDimensions() {
		TerminalBackendResult<TerminalSize> dimensions = GetDimensions();
		if ( !dimensions.IsAvailable ) {
			return dimensions;
		}

		TerminalSize size = dimensions.GetRequiredValue();
		if ( ResizeLogicalScreen(
			size.Columns,
			size.Rows
		) ) {
			InvalidatePhysicalScreen();
		}

		return dimensions;
	}

	internal bool ResizeLogicalScreen(
		int columns,
		int rows ) {
		lock ( screenSync ) {
			if ( null == screen ) {
				screen = new CursesScreen(
					columns,
					rows
				);
				return true;
			}

			if ( columns == screen.Columns
				&& rows == screen.Rows ) {
				return false;
			}

			screen.Resize(
				columns,
				rows
			);
			return true;
		}
	}
}
