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

/// <summary>Pins the current 2.5 release-candidate source identity.</summary>
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
		Assert.Equal( [ "2.5.0-rc.1", "2.5.0-rc.1" ], versions );
	}
}
