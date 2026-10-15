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
using System.Xml.Linq;
using Xunit;

/// <summary>Guards the published 2.4 fingerprint and current dependency boundary.</summary>
public sealed class PublicTwoFourDevelopmentIdentityTests {
	[Fact]
	public void PublishedFingerprintAndCurrentSourcePreserveTerminalBoundary() {
		XDocument production = XDocument.Load(
			Path.Combine( FindRepositoryRoot(), "Icod.DCurses.csproj" )
		);
		XDocument tests = XDocument.Load(
			Path.Combine(
				FindRepositoryRoot(),
				"tests",
				"Icod.DCurses.Tests",
				"Icod.DCurses.Tests.csproj"
			)
		);

		Assert.Equal( "2.0.0.0", GetSingleValue( production, "AssemblyVersion" ) );
		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( Path.Combine(
			FindRepositoryRoot(),
			"docs",
			"Public-API-Fingerprint-2.4.json"
		) ) );
		Assert.Equal(
			"2.4.0",
			fingerprint.RootElement.GetProperty( "release" ).GetString()
		);
		Assert.Equal(
			"stable-source",
			fingerprint.RootElement.GetProperty( "status" ).GetString()
		);

		XElement reference = Assert.Single(
			production.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
		);
		Assert.Equal( "Icod.Terminal", reference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.29.1", reference.Attribute( "Version" )?.Value );
		Assert.DoesNotContain(
			production.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
				&& "Icod.TermInfo" == current.Attribute( "Include" )?.Value
		);

		XElement testTerminal = Assert.Single(
			tests.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
				&& "Icod.Terminal" == current.Attribute( "Include" )?.Value
		);
		Assert.Equal( "1.29.1", testTerminal.Attribute( "Version" )?.Value );
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
			if ( File.Exists( Path.Combine( current.FullName, "Icod.DCurses.sln" ) ) ) {
				return current.FullName;
			}
			current = current.Parent;
		}
		throw new InvalidOperationException( "Repository root not found." );
	}
}
