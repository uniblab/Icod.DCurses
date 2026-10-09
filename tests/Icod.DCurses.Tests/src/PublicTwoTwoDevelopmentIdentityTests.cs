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
using System.Xml.Linq;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class PublicTwoThreeDevelopmentIdentityTests {
	[Fact]
	public void ProjectCarriesTheApprovedStableTwoThreePatchIdentity() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.sln" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );
		XDocument project = XDocument.Load( Path.Combine( root.FullName, "Icod.DCurses.csproj" ) );
		string GetValue( string name ) => project.Descendants()
			.Single( element => name == element.Name.LocalName ).Value;

		Assert.Equal( "2.3.1", GetValue( "Version" ) );
		Assert.Equal( "2.3.1", GetValue( "PackageVersion" ) );
		Assert.Equal( "2.0.0.0", GetValue( "AssemblyVersion" ) );
		Assert.Contains( "CursesRasterAtlas", GetValue( "PackageReleaseNotes" ),
			StringComparison.Ordinal );
		Assert.Contains( "documentation", GetValue( "PackageReleaseNotes" ),
			StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "no runtime or public API changes", GetValue( "PackageReleaseNotes" ),
			StringComparison.OrdinalIgnoreCase );
	}

	[Fact]
	public void CurrentReleaseDocumentationCarriesDurableTwoThreePatchState() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.sln" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );

		string readme = File.ReadAllText( Path.Combine( root.FullName, "README.md" ) );
		Assert.Contains( "Icod.DCurses 2.3.1", readme, StringComparison.Ordinal );
		Assert.Contains( "--version 2.3.1", readme, StringComparison.Ordinal );
		Assert.Contains(
			"For the previous stable release:\n\n```text\ndotnet add package Icod.DCurses --version 2.3.0",
			readme,
			StringComparison.Ordinal
		);
		Assert.DoesNotContain( "release candidate", readme, StringComparison.OrdinalIgnoreCase );

		string roadmap = File.ReadAllText( Path.Combine(
			root.FullName,
			"Icod.DCurses-Development-Roadmap.md"
		) );
		Assert.Contains( "Current stable source/package identity:** `2.3.1`", roadmap,
			StringComparison.Ordinal );
		Assert.Contains( "Active feature track:** maintenance", roadmap,
			StringComparison.Ordinal );
		Assert.DoesNotContain( "Latest tagged stable release:** `2.2.0`", roadmap,
			StringComparison.Ordinal );
		Assert.DoesNotContain( "merge/main/tag pending", roadmap,
			StringComparison.OrdinalIgnoreCase );

		string changelog = File.ReadAllText( Path.Combine( root.FullName, "CHANGELOG.md" ) );
		Assert.Contains( "## 2.3.1 — Documentation and release-state correction", changelog,
			StringComparison.Ordinal );

		string readiness = File.ReadAllText( Path.Combine(
			root.FullName,
			"docs",
			"2.3-Release-Readiness.md"
		) );
		Assert.Contains( "Published 2.3.0 closure", readiness, StringComparison.Ordinal );

		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( Path.Combine(
			root.FullName,
			"docs",
			"Public-API-Fingerprint-2.3.json"
		) ) );
		Assert.Equal( "2.3.1",
			fingerprint.RootElement.GetProperty( "release" ).GetString() );
		Assert.Equal( "stable-source",
			fingerprint.RootElement.GetProperty( "status" ).GetString() );
	}

	[Fact]
	public void PublishedTwoTwoFingerprintRemainsFrozen() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.sln" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );
		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( Path.Combine(
			root.FullName,
			"docs",
			"Public-API-Fingerprint-2.2.json"
		) ) );

		Assert.Equal( "2.2.0",
			fingerprint.RootElement.GetProperty( "release" ).GetString() );
		Assert.Equal( "stable-source",
			fingerprint.RootElement.GetProperty( "status" ).GetString() );
	}
}
