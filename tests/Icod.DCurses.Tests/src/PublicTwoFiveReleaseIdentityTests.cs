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

using System.Xml.Linq;
using Xunit;

/// <summary>Pins the current 2.5 stable-source identity.</summary>
public sealed class PublicTwoFiveReleaseIdentityTests {
	[Fact]
	public void CandidateIdentityIsExplicitAndSynchronized() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.csproj" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );
		XDocument project = XDocument.Load( Path.Combine( root.FullName, "Icod.DCurses.csproj" ) );
		string[] versions = project.Descendants()
			.Where( element => element.Name.LocalName is "Version" or "PackageVersion" )
			.Select( element => element.Value )
			.ToArray();
		Assert.Equal( [ "2.5.1", "2.5.1" ], versions );
	}

	[Fact]
	public void CandidateHasCuratedReleaseNotesAndPublicationUsesThem() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.csproj" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );

		string notes = File.ReadAllText( Path.Combine(
			root.FullName,
			"docs",
			"releases",
			"2.5.1.md"
		) );
		Assert.Contains( "Ctrl+C", notes, StringComparison.Ordinal );
		Assert.Contains( "Ctrl+Z", notes, StringComparison.Ordinal );
		Assert.Contains( "Alt+F", notes, StringComparison.Ordinal );
		Assert.Contains( "Icod.Terminal 1.29.1", notes, StringComparison.Ordinal );

		string workflow = File.ReadAllText( Path.Combine(
			root.FullName,
			".github",
			"workflows",
			"release.yaml"
		) );
		Assert.Contains( "--notes-file", workflow, StringComparison.Ordinal );
		Assert.Contains( "docs/releases/$version.md", workflow, StringComparison.Ordinal );
	}
}
