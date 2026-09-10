/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

using System.Runtime.ExceptionServices;
using Icod.DCurses.Internal;
using Icod.Terminal;

/// <summary>Physical-screen synchronization over Terminal-backed output.</summary>
public sealed partial class CursesSession {
	private readonly object refreshSync = new();
	private CursesRefreshEngine? refreshEngine;
	private CursesScreen? panelRefreshProjection;
	private TerminalSynchronizedOutputLease? pendingSynchronizedOutputCleanup;

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

		if ( !this.Options.UseSynchronizedOutput ) {
			await this.RefreshCoreAsync(
				cancellationToken
			).ConfigureAwait( false );
			return;
		}

		await this.RetryPendingSynchronizedOutputCleanupAsync().ConfigureAwait( false );
		TerminalSynchronizedOutputLease synchronizedOutput =
			await this.HostSession.AcquireSynchronizedOutputAsync(
				cancellationToken
			).ConfigureAwait( false );
		Exception? refreshFailure = null;
		try {
			await this.RefreshCoreAsync(
				cancellationToken
			).ConfigureAwait( false );
		} catch ( Exception exception ) {
			refreshFailure = exception;
		}

		try {
			await synchronizedOutput.DisposeAsync().ConfigureAwait( false );
		} catch ( Exception synchronizationFailure ) {
			this.pendingSynchronizedOutputCleanup = synchronizedOutput;
			this.InvalidatePhysicalScreen();
			if ( refreshFailure is not null ) {
				throw new AggregateException(
					"Curses refresh failed and synchronized-output restoration also reported an error.",
					refreshFailure,
					synchronizationFailure
				);
			}
			throw;
		}

		if ( refreshFailure is not null ) {
			ExceptionDispatchInfo.Capture( refreshFailure ).Throw();
		}
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

	private async ValueTask RefreshCoreAsync(
		CancellationToken cancellationToken
	) {
		cancellationToken.ThrowIfCancellationRequested();
		_ = this.SynchronizeDimensions();
		CursesScreen currentScreen = this.Screen;
		if ( !currentScreen.HasPanels ) {
			await this.GetRefreshEngine().RefreshAsync(
				currentScreen,
				currentScreen.StandardWindow.CursorRow,
				currentScreen.StandardWindow.CursorColumn,
				cancellationToken
			).ConfigureAwait( false );
			return;
		}

		CursesVirtualScreen composed = currentScreen.ComposePanels();
		CursesScreen projection = this.SynchronizePanelRefreshProjection(
			currentScreen,
			composed
		);
		await this.GetRefreshEngine().RefreshAsync(
			projection,
			currentScreen.StandardWindow.CursorRow,
			currentScreen.StandardWindow.CursorColumn,
			cancellationToken
		).ConfigureAwait( false );
		composed.MarkClean();
	}

	private CursesScreen SynchronizePanelRefreshProjection(
		CursesScreen sourceScreen,
		CursesVirtualScreen composed
	) {
		ArgumentNullException.ThrowIfNull( sourceScreen );
		ArgumentNullException.ThrowIfNull( composed );

		bool replaceProjection = this.panelRefreshProjection is null
			|| this.panelRefreshProjection.Columns != composed.Columns
			|| this.panelRefreshProjection.Rows != composed.Rows
			|| !ReferenceEquals(
				this.panelRefreshProjection.TextWidthProvider,
				sourceScreen.TextWidthProvider
			);
		if ( replaceProjection ) {
			this.panelRefreshProjection = new CursesScreen(
				composed.Columns,
				composed.Rows,
				sourceScreen.TextWidthProvider
			);
		}

		CursesScreen projection = this.panelRefreshProjection
			?? throw new InvalidOperationException(
				"The panel refresh projection was not initialized."
			);
		CursesVirtualScreen destination = projection.VirtualScreen;
		bool repairOnReplacement = destination.RepairWideFootprintsOnReplacement;
		destination.RepairWideFootprintsOnReplacement = false;
		try {
			for ( int row = 0; row < composed.Rows; row++ ) {
				for ( int column = 0; column < composed.Columns; column++ ) {
					if ( !replaceProjection
						&& !composed.IsDirty(
							row,
							column
						) ) {
						continue;
					}

					CursesCell desiredCell = composed.GetCell(
						row,
						column
					);
					CursesCellMetadata? desiredMetadata = composed.GetMetadata(
						row,
						column
					);
					CursesCell currentCell = destination.GetCell(
						row,
						column
					);
					CursesCellMetadata? currentMetadata = destination.GetMetadata(
						row,
						column
					);
					if ( currentCell == desiredCell
						&& Equals(
							currentMetadata,
							desiredMetadata
						) ) {
						if ( composed.IsDirty(
							row,
							column
						) ) {
							destination.TouchCell(
								row,
								column
							);
						}
						continue;
					}

					if ( currentCell != desiredCell ) {
						destination.SetCell(
							row,
							column,
							desiredCell
						);
					}
					if ( !Equals(
						currentMetadata,
						desiredMetadata
					) ) {
						destination.SetMetadata(
							row,
							column,
							desiredMetadata
						);
					}
				}
			}

			CursesCellFootprint.Repair( destination );
		} finally {
			destination.RepairWideFootprintsOnReplacement = repairOnReplacement;
		}
		return projection;
	}

	private async ValueTask ResetRefreshRenditionAsync() {
		await this.RetryPendingSynchronizedOutputCleanupAsync().ConfigureAwait( false );

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

	private async ValueTask RetryPendingSynchronizedOutputCleanupAsync() {
		TerminalSynchronizedOutputLease? pending = this.pendingSynchronizedOutputCleanup;
		if ( pending is null ) {
			return;
		}

		try {
			await pending.DisposeAsync().ConfigureAwait( false );
			this.pendingSynchronizedOutputCleanup = null;
		} catch {
			this.InvalidatePhysicalScreen();
			throw;
		}
	}

	private CursesRefreshEngine GetRefreshEngine() {
		lock ( this.refreshSync ) {
			this.refreshEngine ??= new CursesRefreshEngine(
				this.Terminal,
				this.refreshOutput,
				this.HostSession.ApplicationEncoding
			);
			return this.refreshEngine;
		}
	}
}
