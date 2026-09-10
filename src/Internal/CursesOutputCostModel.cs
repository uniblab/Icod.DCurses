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

using System.Text;
using Icod.TermInfo;

/// <summary>
/// Computes deterministic encoded-byte costs for candidate refresh output.
/// </summary>
internal sealed class CursesOutputCostModel {
	private readonly Encoding applicationEncoding;

	internal CursesOutputCostModel(
		Encoding applicationEncoding
	) {
		ArgumentNullException.ThrowIfNull( applicationEncoding );
		this.applicationEncoding = applicationEncoding;
	}

	internal int GetApplicationTextByteCount(
		string value
	) {
		ArgumentNullException.ThrowIfNull( value );
		return this.applicationEncoding.GetByteCount( value );
	}

	internal static int GetTerminalStringByteCount(
		string value,
		int affectedLines = 1
	) {
		ArgumentNullException.ThrowIfNull( value );
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		int byteCount = 0;
		TermInfoOutput.TPuts(
			value,
			affectedLines,
			_ => byteCount = checked( byteCount + 1 )
		);
		return byteCount;
	}
}
