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

using System.Text.Json;
using Xunit;

/// <summary>Guards the explicit DCurses 2.0 development fingerprint artifact.</summary>
public sealed class PublicTwoZeroApiBaselineTests {
	[Fact]
	public void TwoZeroFingerprintContainsOnlyTheApprovedMemberDelta() {
		using JsonDocument twoZero = ReadBaseline(
			"Public-API-Fingerprint-2.0.json"
		);
		using JsonDocument oneSix = ReadBaseline(
			"Public-API-Fingerprint-1.6.json"
		);
		JsonElement root = twoZero.RootElement;

		Assert.Equal( 1, root.GetProperty( "schema" ).GetInt32() );
		Assert.Equal(
			"2.0.0-alpha.1",
			root.GetProperty( "release" ).GetString()
		);
		Assert.Equal(
			"development",
			root.GetProperty( "status" ).GetString()
		);
		Assert.Equal(
			"1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65",
			root.GetProperty( "sha256" ).GetString()
		);
		Assert.Equal( 75, root.GetProperty( "exportedTypeCount" ).GetInt32() );
		Assert.Equal( 559, root.GetProperty( "contractLineCount" ).GetInt32() );
		Assert.Equal(
			oneSix.RootElement.GetProperty( "exportedTypes" ).GetRawText(),
			root.GetProperty( "exportedTypes" ).GetRawText()
		);
	}

	private static JsonDocument ReadBaseline(
		string fileName
	) {
		ArgumentException.ThrowIfNullOrWhiteSpace( fileName );
		return JsonDocument.Parse(
			File.ReadAllText(
				Path.Combine(
					AppContext.BaseDirectory,
					fileName
				)
			)
		);
	}
}
