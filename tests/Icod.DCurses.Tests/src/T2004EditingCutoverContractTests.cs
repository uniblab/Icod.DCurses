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

/// <summary>Freezes the T2004 editing cutover source boundary.</summary>
public sealed class T2004EditingCutoverContractTests {
	private static readonly string[] MigratedProductionPaths = [
		"src/Internal/CursesEraseResolver.cs",
		"src/Internal/CursesCharacterShiftResolver.cs",
		"src/Internal/CursesLineShiftResolver.cs",
		"src/Internal/CursesOutputCostModel.cs",
		"src/Internal/CursesTerminalPlanSequence.cs",
		"src/Internal/CursesEditingRegionSafety.cs",
		"src/Internal/CursesRefreshEngine.cs"
	];

	private static readonly string[] ForbiddenTokens = [
		"Icod.TermInfo",
		"TerminalDescription",
		"StringCapability",
		"TermInfoParameter",
		"TermInfoOutput",
		"TerminalCapabilityWriter",
		"WriteTerminalStringAsync"
	];

	[Fact]
	public void EditingCutoverPathsContainNoTermInfoOrRawTerminalTokens() {
		string root = FindRepositoryRoot();
		Assert.False(
			File.Exists(
				Path.Combine(
					root,
					"src",
					"Internal",
					"CursesLegacyCursorMotionResolver.cs"
				)
			),
			"The legacy TermInfo cursor resolver must remain deleted."
		);

		foreach ( string path in MigratedProductionPaths ) {
			string source = File.ReadAllText(
				Path.Combine(
					root,
					path.Replace(
						'/',
						Path.DirectorySeparatorChar
					)
				)
			);

			foreach ( string token in ForbiddenTokens ) {
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
