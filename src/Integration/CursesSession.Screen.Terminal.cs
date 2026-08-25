namespace Icod.DCurses;

using Icod.Terminal;
using Icod.TermInfo;

/// <summary>Logical-screen ownership backed by canonical Terminal live dimensions.</summary>
public sealed partial class CursesSession {
	private readonly object screenSync = new();
	private CursesScreen? screen;

	/// <summary>Gets the logical screen sized from the most recently observed terminal dimensions.</summary>
	public CursesScreen Screen {
		get {
			lock ( this.screenSync ) {
				if ( this.screen is null ) {
					TerminalControlResult<TerminalSize> dimensions = this.GetDimensions();
					if ( !dimensions.IsAvailable ) {
						throw new InvalidOperationException(
							dimensions.Message
								?? "The terminal dimensions are unavailable for logical-screen creation."
						);
					}

					TerminalSize size = dimensions.GetRequiredValue();
					this.screen = new CursesScreen(
						size.Columns,
						size.Rows
					);
				}

				return this.screen;
			}
		}
	}

	/// <summary>Gets the standard window covering <see cref="Screen"/>.</summary>
	public CursesWindow StandardScreen => this.Screen.StandardWindow;

	/// <summary>
	/// Reobserves live Terminal dimensions and synchronizes the logical screen when changed.
	/// </summary>
	/// <returns>The controlled Terminal live-size result.</returns>
	public TerminalControlResult<TerminalSize> SynchronizeDimensions() {
		TerminalControlResult<TerminalSize> dimensions = this.GetDimensions();
		if ( !dimensions.IsAvailable ) {
			return dimensions;
		}

		TerminalSize size = dimensions.GetRequiredValue();
		if ( this.ResizeLogicalScreen( size.Columns, size.Rows ) ) {
			this.InvalidatePhysicalScreen();
		}

		return dimensions;
	}

	/// <summary>Resizes the materialized logical screen when its dimensions changed.</summary>
	internal bool ResizeLogicalScreen(
		int columns,
		int rows
	) {
		lock ( this.screenSync ) {
			if ( this.screen is null ) {
				this.screen = new CursesScreen( columns, rows );
				return true;
			}
			if (
				columns == this.screen.Columns
				&& rows == this.screen.Rows
			) {
				return false;
			}

			this.screen.Resize( columns, rows );
			return true;
		}
	}
}
