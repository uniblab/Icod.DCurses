namespace Icod.DCurses;

using Icod.DCurses.Internal;

/// <summary>Physical-screen synchronization over Terminal-backed output.</summary>
public sealed partial class CursesSession {
	private readonly object refreshSync = new();
	private CursesRefreshEngine? refreshEngine;

	/// <summary>
	/// Synchronizes the desired logical screen with the terminal and leaves the physical cursor
	/// at the current <see cref="StandardScreen"/> cursor position.
	/// </summary>
	public async ValueTask RefreshAsync(
		CancellationToken cancellationToken = default
	) {
		cancellationToken.ThrowIfCancellationRequested();
		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );

		_ = this.SynchronizeDimensions();
		CursesScreen currentScreen = this.Screen;
		await this.GetRefreshEngine().RefreshAsync(
			currentScreen,
			currentScreen.StandardWindow.CursorRow,
			currentScreen.StandardWindow.CursorColumn,
			cancellationToken
		).ConfigureAwait( false );
	}

	/// <summary>Invalidates all physical-screen knowledge for the next refresh.</summary>
	public void Invalidate() {
		this.InvalidatePhysicalScreen();
	}

	/// <summary>Invalidates retained physical-screen knowledge for the session refresh engine.</summary>
	internal void InvalidatePhysicalScreen() {
		lock ( this.refreshSync ) {
			this.refreshEngine?.Invalidate();
		}
	}

	private async ValueTask ResetRefreshRenditionAsync() {
		CursesRefreshEngine? engine;
		lock ( this.refreshSync ) {
			engine = this.refreshEngine;
		}

		if ( engine is null ) {
			return;
		}

		await engine.ResetRenditionAsync(
			CancellationToken.None
		).ConfigureAwait( false );
	}

	private CursesRefreshEngine GetRefreshEngine() {
		lock ( this.refreshSync ) {
			this.refreshEngine ??= new CursesRefreshEngine(
				this.Terminal,
				this.refreshOutput
			);
			return this.refreshEngine;
		}
	}
}
