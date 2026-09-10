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

/// <summary>Guards the decision that 1.0 promotes the exact 0.9 public contract.</summary>
public sealed class PublicStableApiBaselineTests {
	[Fact]
	public void OneDotZeroBaselineMatchesFrozenZeroDotNineContract() {
		ApiBaseline zeroDotNine = ReadBaseline(
			"Public-API-Fingerprint-0.9.json"
		);
		ApiBaseline oneDotZero = ReadBaseline(
			"Public-API-Fingerprint-1.0.json"
		);

		Assert.Equal( "0.9.0", zeroDotNine.Release );
		Assert.Equal( "1.0.0", oneDotZero.Release );
		Assert.Equal( zeroDotNine.Sha256, oneDotZero.Sha256 );
		Assert.Equal(
			zeroDotNine.ExportedTypeCount,
			oneDotZero.ExportedTypeCount
		);
		Assert.Equal(
			zeroDotNine.ContractLineCount,
			oneDotZero.ContractLineCount
		);
		Assert.Equal(
			zeroDotNine.ExportedTypes,
			oneDotZero.ExportedTypes
		);
	}

	private static ApiBaseline ReadBaseline( string fileName ) {
		ArgumentException.ThrowIfNullOrEmpty( fileName );
		string path = Path.Combine(
			AppContext.BaseDirectory,
			fileName
		);
		using JsonDocument document = JsonDocument.Parse(
			File.ReadAllText( path )
		);
		JsonElement root = document.RootElement;
		return new ApiBaseline(
			root.GetProperty( "release" ).GetString()!,
			root.GetProperty( "sha256" ).GetString()!,
			root.GetProperty( "exportedTypeCount" ).GetInt32(),
			root.GetProperty( "contractLineCount" ).GetInt32(),
			root.GetProperty( "exportedTypes" )
				.EnumerateArray()
				.Select( static current => current.GetString()! )
				.ToArray()
		);
	}

	private sealed record ApiBaseline(
		string Release,
		string Sha256,
		int ExportedTypeCount,
		int ContractLineCount,
		string[] ExportedTypes
	);
}
