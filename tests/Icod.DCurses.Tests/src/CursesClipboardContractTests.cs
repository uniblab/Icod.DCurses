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

namespace Icod.DCurses.Tests;

using Icod.Terminal;
using Xunit;

/// <summary>Verifies the DCurses-owned clipboard option contract.</summary>
public sealed class CursesClipboardContractTests {
	[Fact]
	public void ClipboardSelectionHasStableSemanticValues() {
		Assert.Equal(
			[ "Clipboard", "Primary", "Secondary", "Select" ],
			Enum.GetNames<CursesClipboardSelection>()
		);
	}

	[Fact]
	public void ClipboardOptionsDefaultAndBoundaries() {
		CursesClipboardOptions defaults = new();
		Assert.Equal( 65_536, defaults.MaximumReadBytes );
		Assert.Equal( 65_536, defaults.MaximumWriteBytes );

		new CursesSessionOptions {
			ClipboardOptions = new() {
				MaximumReadBytes = 1,
				MaximumWriteBytes = 16_777_216
			}
		}.Validate();

		Assert.Throws<ArgumentOutOfRangeException>( () => new CursesSessionOptions {
			ClipboardOptions = new() { MaximumReadBytes = 0 }
		}.Validate() );
		Assert.Throws<ArgumentOutOfRangeException>( () => new CursesSessionOptions {
			ClipboardOptions = new() { MaximumWriteBytes = 16_777_217 }
		}.Validate() );
	}

	[Fact]
	public void OpeningOwnedSessionPassesIndependentBudgets() {
		CursesSessionOptions options = new() {
			ClipboardOptions = new() {
				MaximumReadBytes = 131_072,
				MaximumWriteBytes = 262_144
			}
		};

		TerminalSessionOptions terminalOptions = CursesSession.CreateOwnedTerminalOptions(
			options
		);

		Assert.Equal( 131_072, terminalOptions.ClipboardOptions.MaximumReadBytes );
		Assert.Equal( 262_144, terminalOptions.ClipboardOptions.MaximumWriteBytes );
		Assert.NotSame( options.ClipboardOptions, terminalOptions.ClipboardOptions );
	}
}
