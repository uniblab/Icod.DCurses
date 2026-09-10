/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies deterministic byte-cost calculations used by refresh optimization.</summary>
public sealed class CursesOutputCostModelTests {
	[Fact]
	public void ApplicationTextUsesConfiguredEncoding() {
		CursesOutputCostModel utf8 = new( Encoding.UTF8 );
		CursesOutputCostModel utf16 = new( Encoding.Unicode );

		Assert.Equal(
			4,
			utf8.GetApplicationTextByteCount( "A界" )
		);
		Assert.Equal(
			4,
			utf16.GetApplicationTextByteCount( "A界" )
		);
	}

	[Fact]
	public void TerminalStringIgnoresPaddingDirectiveSourceBytes() {
		int byteCount = CursesOutputCostModel.GetTerminalStringByteCount(
			"\u001b[H$<25*>X",
			affectedLines: 8
		);

		Assert.Equal( 4, byteCount );
	}

	[Fact]
	public void TerminalStringCountsLatin1ProtocolCharactersAsOneByte() {
		int byteCount = CursesOutputCostModel.GetTerminalStringByteCount(
			"\u001b[38;5;255m"
		);

		Assert.Equal( 11, byteCount );
	}

	[Fact]
	public void TerminalStringRejectsNonpositiveAffectedLineCount() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesOutputCostModel.GetTerminalStringByteCount(
				"x",
				affectedLines: 0
			)
		);
	}
}
