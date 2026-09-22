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
		const string payload = "A界🙂";

		Assert.Equal(
			Encoding.UTF8.GetByteCount( payload ),
			utf8.GetApplicationTextByteCount( payload )
		);
		Assert.Equal(
			Encoding.Unicode.GetByteCount( payload ),
			utf16.GetApplicationTextByteCount( payload )
		);
	}

	[Fact]
	public void NullApplicationTextIsRejected() {
		CursesOutputCostModel model = new( Encoding.UTF8 );

		Assert.Throws<ArgumentNullException>(
			() => model.GetApplicationTextByteCount( null! )
		);
	}

	[Fact]
	public void NullApplicationEncodingIsRejected() {
		Assert.Throws<ArgumentNullException>(
			() => new CursesOutputCostModel( null! )
		);
	}
}
