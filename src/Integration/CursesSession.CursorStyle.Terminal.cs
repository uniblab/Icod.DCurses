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

using Icod.Terminal;

/// <summary>Forwards reversible cursor-style ownership to the canonical Terminal session.</summary>
public sealed partial class CursesSession {
	/// <summary>Acquires one reversible semantic terminal text-cursor style.</summary>
	/// <param name="style">The style to own while the returned lease is active.</param>
	/// <param name="timeout">The baseline-observation timeout for an outermost lease.</param>
	/// <param name="cancellationToken">Cancellation for acquisition only.</param>
	/// <returns>A lease which restores the exact preceding style when released in LIFO order.</returns>
	/// <remarks>
	/// Cursor style and visibility are independent. Acquisition never shows or hides the cursor.
	/// An unsupported or failed baseline observation performs no style mutation and no guessed reset.
	/// </remarks>
	public async ValueTask<CursesCursorStyleLease> AcquireCursorStyleAsync(
		CursesCursorStyle style,
		TimeSpan timeout,
		CancellationToken cancellationToken = default
	) {
		TerminalCursorStyle terminalStyle = ToTerminalCursorStyle( style );
		using SessionWaitCancellationScope wait =
			this.CreateSessionWaitCancellationScope( cancellationToken );
		try {
			using IDisposable activity = await this.AcquireTerminalActivityAsync(
				wait.Token
			).ConfigureAwait( false );
			TerminalCursorStyleLease terminalLease =
				await this.terminalSession.AcquireCursorStyleAsync(
					terminalStyle,
					timeout,
					wait.Token
				).ConfigureAwait( false );
			return new CursesCursorStyleLease( style, terminalLease );
		} catch ( OperationCanceledException ) when (
			this.IsDisposalCancellation( cancellationToken )
		) {
			throw new ObjectDisposedException( nameof( CursesSession ) );
		}
	}

	internal static TerminalCursorStyle ToTerminalCursorStyle( CursesCursorStyle style ) {
		return style switch {
			CursesCursorStyle.BlinkingBlock => TerminalCursorStyle.BlinkingBlock,
			CursesCursorStyle.SteadyBlock => TerminalCursorStyle.SteadyBlock,
			CursesCursorStyle.BlinkingUnderline => TerminalCursorStyle.BlinkingUnderline,
			CursesCursorStyle.SteadyUnderline => TerminalCursorStyle.SteadyUnderline,
			CursesCursorStyle.BlinkingBar => TerminalCursorStyle.BlinkingBar,
			CursesCursorStyle.SteadyBar => TerminalCursorStyle.SteadyBar,
			_ => throw new ArgumentOutOfRangeException(
				nameof( style ),
				style,
				"The curses cursor style is not recognized."
			)
		};
	}
}
