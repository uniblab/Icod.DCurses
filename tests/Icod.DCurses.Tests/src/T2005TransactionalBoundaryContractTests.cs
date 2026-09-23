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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the Terminal-owned transactional output boundary for T2005.</summary>
public sealed class T2005TransactionalBoundaryContractTests {
	private static readonly string[] ForbiddenProductionTokens = [
		"Icod.TermInfo",
		"TerminalDescription",
		"StringCapability",
		"TermInfoParameter",
		"TermInfoOutput",
		"TerminalCapabilityWriter",
		"TerminalSessionCursesOutput",
		"WriteTerminalStringAsync",
		"ITerminalOutput",
		"ITerminalHyperlinkOutput",
		".Output.FlushAsync"
	];

	[Fact]
	public void ProductionSourceContainsNoRawOutputOrTermInfoSeam() {
		string root = FindRepositoryRoot();
		string sourceRoot = Path.Combine( root, "src" );

		Assert.False(
			File.Exists(
				Path.Combine(
					sourceRoot,
					"Integration",
					"TerminalOutputShim.cs"
				)
			),
			"The obsolete per-write Terminal output shim must remain deleted."
		);
		Assert.False(
			File.Exists(
				Path.Combine(
					sourceRoot,
					"Internal",
					"TerminalCapabilityWriter.cs"
				)
			),
			"The obsolete raw capability writer must remain deleted."
		);

		foreach ( string path in Directory.EnumerateFiles(
			sourceRoot,
			"*.cs",
			SearchOption.AllDirectories
		) ) {
			string source = File.ReadAllText( path );
			foreach ( string token in ForbiddenProductionTokens ) {
				Assert.DoesNotContain(
					token,
					source,
					StringComparison.Ordinal
				);
			}
		}
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
