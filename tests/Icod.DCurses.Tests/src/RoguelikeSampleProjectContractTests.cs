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

public sealed class RoguelikeSampleProjectContractTests {
	[Fact]
	public void RoguelikeSampleBuildsInTheSolutionWithOnlyThePublicLibrary() {
		string root = FindRepositoryRoot();
		string folder = Path.Combine( root, "samples", "Icod.DCurses.Roguelike.Sample" );
		string projectPath = Path.Combine( folder, "Icod.DCurses.Roguelike.Sample.csproj" );
		Assert.True( File.Exists( projectPath ), "The T2109 sample project is missing." );
		XDocument project = XDocument.Load( projectPath );
		Assert.Equal( "net8.0;net9.0;net10.0",
			project.Descendants( "TargetFrameworks" ).Single().Value );
		Assert.Equal( "Debug;Staging;Release",
			project.Descendants( "Configurations" ).Single().Value );
		Assert.Equal( "false", project.Descendants( "IsPackable" ).Single().Value );
		Assert.Equal( @"..\..\Icod.DCurses.csproj",
			project.Descendants( "ProjectReference" ).Single().Attribute( "Include" )?.Value );
		string solution = File.ReadAllText( Path.Combine( root, "Icod.DCurses.sln" ) );
		Assert.Contains( @"samples\Icod.DCurses.Roguelike.Sample\Icod.DCurses.Roguelike.Sample.csproj",
			solution, StringComparison.Ordinal );
		foreach ( string name in new[] {
			"Program.cs",
			"RoguelikeSampleState.cs",
			"RoguelikeSampleInteraction.cs"
		} ) {
			string source = File.ReadAllText( Path.Combine( folder, name ) );
			Assert.DoesNotContain( "Icod.DCurses.Internal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.Terminal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.TermInfo", source, StringComparison.Ordinal );
		}
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
