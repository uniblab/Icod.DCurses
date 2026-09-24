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
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class PublicTwoTwoDevelopmentIdentityTests {
	[Fact]
	public void DevelopmentIdentityAndNotesDescribeImplementedDiscovery() {
		DirectoryInfo? root = new( AppContext.BaseDirectory );
		while ( root is not null && !File.Exists( Path.Combine( root.FullName, "Icod.DCurses.sln" ) ) ) {
			root = root.Parent;
		}
		Assert.NotNull( root );
		XDocument project = XDocument.Load( Path.Combine( root.FullName, "Icod.DCurses.csproj" ) );
		string GetValue( string name ) => project.Descendants()
			.Single( element => name == element.Name.LocalName ).Value;

		Assert.Equal( "2.2.0-alpha.1", GetValue( "Version" ) );
		Assert.Equal( "2.2.0-alpha.1", GetValue( "PackageVersion" ) );
		Assert.Equal( "2.0.0.0", GetValue( "AssemblyVersion" ) );
		Assert.Contains( "binding discovery", GetValue( "PackageReleaseNotes" ),
			StringComparison.OrdinalIgnoreCase );
		Assert.Equal( "1.18.0", project.Descendants()
			.Single( element => "PackageReference" == element.Name.LocalName )
			.Attribute( "Version" )?.Value );
	}
}
