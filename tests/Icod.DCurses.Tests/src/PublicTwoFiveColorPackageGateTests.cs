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

/// <summary>Guards the durable 2.5 color-surface package and contract documentation.</summary>
public sealed class PublicTwoFiveColorPackageGateTests {
	[Fact]
	public void PublicApiFreezeIsIdenticalToPublishedTwoFour() {
		using JsonDocument previous = ReadFingerprint( "2.4" );
		using JsonDocument current = ReadFingerprint( "2.5" );
		foreach ( string field in new[] { "sha256", "exportedTypeCount", "contractLineCount", "exportedTypes" } ) {
			Assert.Equal(
				previous.RootElement.GetProperty( field ).GetRawText(),
				current.RootElement.GetProperty( field ).GetRawText()
			);
		}
	}

	[Fact]
	public void PackageReadmeExplainsColorAndPointsToRunnableSample() {
		string root = FindRepositoryRoot();
		string readme = File.ReadAllText( Path.Combine( root, "README.md" ) );
		Assert.Contains( "foreground", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "background", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "CursesPanelTransparency.Opaque", readme, StringComparison.Ordinal );
		Assert.Contains( "CursesTextAttributes.Reverse", readme, StringComparison.Ordinal );
		Assert.Contains(
			"dotnet run --project samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj",
			readme,
			StringComparison.Ordinal
		);
		Assert.Contains(
			"https://github.com/uniblab/Icod.DCurses/blob/main/samples/Icod.DCurses.Color.Sample/README.md",
			readme,
			StringComparison.Ordinal
		);
	}

	private static JsonDocument ReadFingerprint( string version ) {
		string path = Path.Combine( FindRepositoryRoot(), "docs", $"Public-API-Fingerprint-{version}.json" );
		return JsonDocument.Parse( File.ReadAllText( path ) );
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? directory = new( AppContext.BaseDirectory );
		while ( directory is not null && !File.Exists( Path.Combine( directory.FullName, "Icod.DCurses.csproj" ) ) ) {
			directory = directory.Parent;
		}
		return directory?.FullName ?? throw new InvalidOperationException( "Repository root not found." );
	}
}
