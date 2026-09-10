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

/// <summary>
/// Delegates reversible rich-input protocol ownership to the canonical Terminal session.
/// </summary>
public sealed partial class CursesSession {
	/// <summary>
	/// Acquires one reversible set of rich-input protocol reporting requirements.
	/// </summary>
	public async ValueTask<TerminalControlResult<CursesInputProtocolLease>> AcquireInputProtocolsAsync(
		CursesInputProtocolOptions options,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( options );
		options.Validate();
		cancellationToken.ThrowIfCancellationRequested();

		using IDisposable activity = await this.AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );

		TerminalControlResult<TerminalInputProtocolLease> result =
			await this.terminalSession.AcquireInputProtocolsAsync(
				options.ToTerminalOptions(),
				cancellationToken
			).ConfigureAwait( false );

		return result.Status switch {
			TerminalControlStatus.Available =>
				TerminalControlResult<CursesInputProtocolLease>.Available(
					new CursesInputProtocolLease(
						result.GetRequiredValue(),
						options
					)
				),
			TerminalControlStatus.Unavailable =>
				TerminalControlResult<CursesInputProtocolLease>.Unavailable(
					result.Message,
					result.NativeErrorCode
				),
			TerminalControlStatus.Unsupported =>
				TerminalControlResult<CursesInputProtocolLease>.Unsupported(
					result.Message
				),
			TerminalControlStatus.Failed =>
				TerminalControlResult<CursesInputProtocolLease>.Failed(
					result.Message,
					result.NativeErrorCode
				),
			_ => throw new ArgumentOutOfRangeException(
				nameof( result ),
				result.Status,
				"The Terminal input-protocol result status is not recognized."
			)
		};
	}
}
