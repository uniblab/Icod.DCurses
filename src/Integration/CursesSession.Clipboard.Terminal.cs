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

/// <summary>Forwards explicit bounded clipboard operations to the canonical Terminal session.</summary>
public sealed partial class CursesSession {
	/// <summary>Writes strict UTF-8 text to one terminal-managed clipboard or selection.</summary>
	/// <remarks>
	/// Successful completion means the complete OSC 52 frame was emitted. It does not prove that
	/// the terminal accepted, retained, or exposed the content.
	/// </remarks>
	public async ValueTask WriteClipboardAsync(
		CursesClipboardSelection selection,
		string value,
		CancellationToken cancellationToken = default
	) {
		TerminalClipboardSelection terminalSelection = ToTerminalClipboardSelection( selection );
		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.terminalSession.WriteClipboardAsync(
			terminalSelection,
			value,
			cancellationToken
		).ConfigureAwait( false );
	}

	/// <summary>Writes exact bounded bytes to one terminal-managed clipboard or selection.</summary>
	/// <remarks>
	/// Successful completion means the complete OSC 52 frame was emitted. It does not prove that
	/// the terminal accepted, retained, or exposed the content.
	/// </remarks>
	public async ValueTask WriteClipboardAsync(
		CursesClipboardSelection selection,
		ReadOnlyMemory<byte> payload,
		CancellationToken cancellationToken = default
	) {
		TerminalClipboardSelection terminalSelection = ToTerminalClipboardSelection( selection );
		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		await this.terminalSession.WriteClipboardAsync(
			terminalSelection,
			payload,
			cancellationToken
		).ConfigureAwait( false );
	}

	/// <summary>Explicitly requests bounded bytes from one terminal-managed clipboard or selection.</summary>
	/// <remarks>
	/// A timeout is not evidence that clipboard reads are unsupported. DCurses never performs this
	/// privacy-sensitive query during session open, refresh, lifecycle handling, or disposal.
	/// </remarks>
	public async ValueTask<byte[]> ReadClipboardAsync(
		CursesClipboardSelection selection,
		TimeSpan timeout,
		CancellationToken cancellationToken = default
	) {
		TerminalClipboardSelection terminalSelection = ToTerminalClipboardSelection( selection );
		using SessionWaitCancellationScope wait =
			this.CreateSessionWaitCancellationScope( cancellationToken );
		try {
			using IDisposable activity = await this.AcquireTerminalActivityAsync(
				wait.Token
			).ConfigureAwait( false );
			return await this.terminalSession.ReadClipboardAsync(
				terminalSelection,
				timeout,
				wait.Token
			).ConfigureAwait( false );
		} catch ( OperationCanceledException ) when (
			this.IsDisposalCancellation( cancellationToken )
		) {
			throw new ObjectDisposedException( nameof( CursesSession ) );
		}
	}

	private static TerminalClipboardSelection ToTerminalClipboardSelection(
		CursesClipboardSelection selection
	) {
		return selection switch {
			CursesClipboardSelection.Clipboard => TerminalClipboardSelection.Clipboard,
			CursesClipboardSelection.Primary => TerminalClipboardSelection.Primary,
			CursesClipboardSelection.Secondary => TerminalClipboardSelection.Secondary,
			CursesClipboardSelection.Select => TerminalClipboardSelection.Select,
			_ => throw new ArgumentOutOfRangeException(
				nameof( selection ),
				selection,
				"The curses clipboard selection is not recognized."
			)
		};
	}
}
