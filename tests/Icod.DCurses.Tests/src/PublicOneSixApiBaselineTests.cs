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

using System.Text.Json;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the explicit T1610 1.6 fingerprint artifact and its development-time alias.</summary>
public sealed class PublicOneSixApiBaselineTests {
	[Fact]
	public void ExplicitOneSixFingerprintMatchesDevelopmentAlias() {
		using JsonDocument explicitBaseline = ReadBaseline( "Public-API-Fingerprint-1.6.json" );
		using JsonDocument developmentAlias = ReadBaseline( "Public-API-Fingerprint-1.2.json" );

		JsonElement explicitRoot = explicitBaseline.RootElement;
		JsonElement aliasRoot = developmentAlias.RootElement;

		Assert.Equal( 1, explicitRoot.GetProperty( "schema" ).GetInt32() );
		Assert.Equal( "1.6.0-alpha.2", explicitRoot.GetProperty( "release" ).GetString() );
		Assert.Equal( "development", explicitRoot.GetProperty( "status" ).GetString() );
		Assert.Equal(
			"266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458",
			explicitRoot.GetProperty( "sha256" ).GetString()
		);
		Assert.Equal( 75, explicitRoot.GetProperty( "exportedTypeCount" ).GetInt32() );
		Assert.Equal( 559, explicitRoot.GetProperty( "contractLineCount" ).GetInt32() );

		Assert.Equal(
			explicitRoot.GetProperty( "sha256" ).GetString(),
			aliasRoot.GetProperty( "sha256" ).GetString()
		);
		Assert.Equal(
			explicitRoot.GetProperty( "exportedTypeCount" ).GetInt32(),
			aliasRoot.GetProperty( "exportedTypeCount" ).GetInt32()
		);
		Assert.Equal(
			explicitRoot.GetProperty( "contractLineCount" ).GetInt32(),
			aliasRoot.GetProperty( "contractLineCount" ).GetInt32()
		);
		Assert.Equal(
			explicitRoot.GetProperty( "exportedTypes" ).GetRawText(),
			aliasRoot.GetProperty( "exportedTypes" ).GetRawText()
		);
	}

	private static JsonDocument ReadBaseline( string fileName ) {
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
