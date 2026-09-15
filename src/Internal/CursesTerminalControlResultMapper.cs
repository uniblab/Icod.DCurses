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

namespace Icod.DCurses.Internal;

using Icod.Terminal;

/// <summary>Projects controlled Terminal results while preserving status and diagnostics.</summary>
internal static class CursesTerminalControlResultMapper {
	internal static TerminalControlResult<TResult> Map<TSource, TResult>(
		TerminalControlResult<TSource> source,
		Func<TSource, TResult> projector
	)
		where TSource : class
		where TResult : class {
		ArgumentNullException.ThrowIfNull( source );
		ArgumentNullException.ThrowIfNull( projector );

		return source.Status switch {
			TerminalControlStatus.Available => TerminalControlResult<TResult>.Available(
				projector( source.GetRequiredValue() )
			),
			TerminalControlStatus.Unavailable => TerminalControlResult<TResult>.Unavailable(
				source.Message,
				source.NativeErrorCode
			),
			TerminalControlStatus.Unsupported => TerminalControlResult<TResult>.Unsupported(
				source.Message
			),
			TerminalControlStatus.Failed => TerminalControlResult<TResult>.Failed(
				source.Message,
				source.NativeErrorCode
			),
			_ => throw new ArgumentOutOfRangeException(
				nameof( source ),
				source.Status,
				"The Terminal control status is not recognized by this DCurses release."
			)
		};
	}
}
