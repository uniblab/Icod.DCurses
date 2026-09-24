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

using System.Xml.Linq;
using System.Text.Json;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the 2.1 stable-source identity and production boundary.</summary>
public sealed class PublicTwoOneDevelopmentIdentityTests {
	[Fact]
	public void ProjectCarriesTheApprovedTwoOneStableSourceIdentity() {
		XDocument project = LoadProductionProject();

		Assert.Equal(
			"2.1.0",
			GetSingleValue( project, "Version" )
		);
		Assert.Equal(
			"2.1.0",
			GetSingleValue( project, "PackageVersion" )
		);
		Assert.Equal(
			"2.0.0.0",
			GetSingleValue( project, "AssemblyVersion" )
		);
		Assert.Equal(
			"net8.0;net9.0;net10.0",
			GetSingleValue( project, "TargetFrameworks" )
		);
	}

	[Fact]
	public void StableSourceFingerprintAndPackageNotesIdentifyTheSameRelease() {
		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( Path.Combine(
			FindRepositoryRoot(), "docs", "Public-API-Fingerprint-2.1.json" ) ) );
		Assert.Equal( "2.1.0", fingerprint.RootElement.GetProperty( "release" ).GetString() );
		Assert.Equal( "stable-source", fingerprint.RootElement.GetProperty( "status" ).GetString() );
		Assert.Contains( "2.1.0", GetSingleValue( LoadProductionProject(), "PackageReleaseNotes" ),
			StringComparison.Ordinal );
	}

	[Fact]
	public void ProductionProjectReferencesOnlyTerminalOneEighteen() {
		XElement[] references = LoadProductionProject()
			.Descendants()
			.Where( static element => "PackageReference" == element.Name.LocalName )
			.ToArray();

		XElement reference = Assert.Single( references );
		Assert.Equal( "Icod.Terminal", reference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.18.0", reference.Attribute( "Version" )?.Value );
		Assert.DoesNotContain(
			references,
			static current => string.Equals(
				"Icod.TermInfo",
				current.Attribute( "Include" )?.Value,
				StringComparison.Ordinal
			)
		);
	}

	private static XDocument LoadProductionProject() {
		return XDocument.Load(
			Path.Combine(
				FindRepositoryRoot(),
				"Icod.DCurses.csproj"
			)
		);
	}

	private static string GetSingleValue(
		XDocument project,
		string localName
	) {
		return project.Descendants()
			.Single( current => localName == current.Name.LocalName )
			.Value;
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? current = new( AppContext.BaseDirectory );
		while ( current is not null ) {
			if ( File.Exists(
				Path.Combine(
					current.FullName,
					"Icod.DCurses.sln"
				)
			) ) {
				return current.FullName;
			}
			current = current.Parent;
		}

		throw new InvalidOperationException( "Repository root not found." );
	}
}
