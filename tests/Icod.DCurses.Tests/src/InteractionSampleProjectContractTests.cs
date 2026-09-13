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

public sealed class InteractionSampleProjectContractTests {
	[Fact]
	public void InteractionSampleProjectIsWiredIntoTheSolution() {
		string root = FindRepositoryRoot();
		string projectPath = Path.Combine(
			root,
			"samples",
			"Icod.DCurses.Interaction.Sample",
			"Icod.DCurses.Interaction.Sample.csproj"
		);
		Assert.True( File.Exists( projectPath ) );

		XDocument project = XDocument.Load( projectPath );
		Assert.Equal(
			"net8.0;net9.0;net10.0",
			project.Descendants( "TargetFrameworks" ).Single().Value
		);
		Assert.Equal(
			"Debug;Staging;Release",
			project.Descendants( "Configurations" ).Single().Value
		);
		Assert.Equal(
			"false",
			project.Descendants( "IsPackable" ).Single().Value
		);

		XElement reference = project.Descendants( "ProjectReference" ).Single();
		Assert.Equal(
			@"..\..\Icod.DCurses.csproj",
			reference.Attribute( "Include" )!.Value
		);

		string solution = File.ReadAllText(
			Path.Combine(
				root,
				"Icod.DCurses.sln"
			)
		);
		Assert.Contains(
			@"samples\Icod.DCurses.Interaction.Sample\Icod.DCurses.Interaction.Sample.csproj",
			solution,
			StringComparison.Ordinal
		);
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
