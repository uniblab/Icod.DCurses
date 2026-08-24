namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>Physical-screen synchronization for <see cref="CursesSession"/>.</summary>
public sealed partial class CursesSession {
	private readonly object refreshSync = new();
	private CursesRefreshEngine? refreshEngine;

	/// <summary>
	/// Synchronizes the desired logical screen with the terminal and leaves the physical cursor at
	/// the current <see cref="StandardScreen"/> cursor position.
	/// </summary>
	/// <param name="cancellationToken">Cancellation for this refresh operation.</param>
	/// <returns>A value task representing the refresh boundary.</returns>
	public ValueTask RefreshAsync(
		CancellationToken cancellationToken = default ) {
		cancellationToken.ThrowIfCancellationRequested();

		_ = SynchronizeDimensions();
		CursesScreen currentScreen = Screen;
		CursesRefreshEngine engine = GetRefreshEngine();
		return engine.RefreshAsync(
			currentScreen,
			currentScreen.StandardWindow.CursorRow,
			currentScreen.StandardWindow.CursorColumn,
			cancellationToken
		);
	}

	/// <summary>
	/// Invalidates all physical-screen knowledge so the next refresh performs a complete repaint.
	/// </summary>
	public void Invalidate() {
		lock ( refreshSync ) {
			refreshEngine?.Invalidate();
		}
	}

	internal void InvalidatePhysicalScreen() {
		lock ( refreshSync ) {
			refreshEngine?.Invalidate();
		}
	}

	private async ValueTask ResetRefreshRenditionAsync() {
		CursesRefreshEngine? engine;
		lock ( refreshSync ) {
			engine = refreshEngine;
		}

		if ( null == engine ) {
			return;
		}

		await engine.ResetRenditionAsync(
			CancellationToken.None
		).ConfigureAwait( false );
	}

	private CursesRefreshEngine GetRefreshEngine() {
		lock ( refreshSync ) {
			refreshEngine ??= new CursesRefreshEngine(
				Terminal,
				Backend.Output
			);
			return refreshEngine;
		}
	}
}
