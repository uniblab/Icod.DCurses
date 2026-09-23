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

/// <summary>Freezes the T1608 mixed-media sample and package-consumer acceptance boundary.</summary>
public sealed class CursesMixedMediaApplicationAcceptanceTests {
	[Fact]
	public void RepositoryContainsMixedMediaSampleAndPackageOnlyRasterWitnesses() {
		string root = FindRepositoryRoot();
		string sampleProject = Path.Combine(
			root,
			"samples",
			"Icod.DCurses.MixedMedia.Sample",
			"Icod.DCurses.MixedMedia.Sample.csproj"
		);
		string sampleProgram = Path.Combine(
			root,
			"samples",
			"Icod.DCurses.MixedMedia.Sample",
			"Program.cs"
		);
		string packageSmoke = Path.Combine(
			root,
			"tools",
			"package-smoke",
			"Program.cs"
		);
		string rasterSmoke = Path.Combine(
			root,
			"tools",
			"package-smoke",
			"RasterSmoke.cs"
		);
		string unixPackageValidator = Path.Combine(
			root,
			".github",
			"scripts",
			"verify-release-package.sh"
		);
		string windowsPackageValidator = Path.Combine(
			root,
			".github",
			"scripts",
			"verify-release-package.cmd"
		);
		string solution = Path.Combine(
			root,
			"Icod.DCurses.sln"
		);

		Assert.True(
			File.Exists( sampleProject ),
			"T1608 requires the mixed-media sample project."
		);
		Assert.True(
			File.Exists( sampleProgram ),
			"T1608 requires the mixed-media sample program."
		);
		Assert.True(
			File.Exists( rasterSmoke ),
			"T1608 requires a package-only raster witness."
		);

		string solutionText = File.ReadAllText( solution );
		Assert.Contains(
			"Icod.DCurses.MixedMedia.Sample",
			solutionText,
			StringComparison.Ordinal
		);

		string sampleText = File.ReadAllText( sampleProgram );
		Assert.Contains( "CreateRasterResourceAsync", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CreatePlaceholderAsync", sampleText, StringComparison.Ordinal );
		Assert.Contains( "WriteRasterCell", sampleText, StringComparison.Ordinal );
		Assert.Contains( "WriteWithMetadata", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesHyperlink", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesPanel", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesPad", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesInteractionRouter", sampleText, StringComparison.Ordinal );
		Assert.Contains( "WaitForInputAsync", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesInputEventKind.EndOfInput", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesLifecycleEventKind.Interrupt", sampleText, StringComparison.Ordinal );
		Assert.Contains( "CursesLifecycleEventKind.Termination", sampleText, StringComparison.Ordinal );
		Assert.DoesNotContain( "KittyGraphics", sampleText, StringComparison.Ordinal );
		Assert.DoesNotContain( "Sixel", sampleText, StringComparison.Ordinal );

		int firstPrompt = sampleText.IndexOf(
			"Press any key to pan",
			StringComparison.Ordinal
		);
		Assert.True( 0 <= firstPrompt, "The first-frame input prompt is required." );
		int firstRefresh = sampleText.IndexOf(
			"await session.RefreshAsync();",
			firstPrompt,
			StringComparison.Ordinal
		);
		Assert.True( firstPrompt < firstRefresh, "The first frame must be refreshed." );
		int firstWait = sampleText.IndexOf(
			"if ( !await WaitForInputAsync( session ) )",
			firstRefresh,
			StringComparison.Ordinal
		);
		Assert.True( firstRefresh < firstWait, "The first frame must wait for input." );
		int pan = sampleText.IndexOf(
			"viewport.PanBy(",
			firstWait,
			StringComparison.Ordinal
		);
		Assert.True( firstWait < pan, "Panning must follow the first input wait." );
		int secondRefresh = sampleText.IndexOf(
			"await session.RefreshAsync();",
			pan,
			StringComparison.Ordinal
		);
		Assert.True( pan < secondRefresh, "The panned frame must be refreshed." );
		int secondWait = sampleText.IndexOf(
			"await WaitForInputAsync( session );",
			secondRefresh,
			StringComparison.Ordinal
		);
		Assert.True(
			secondRefresh < secondWait,
			"The panned frame must wait for input before exit."
		);

		string packageSmokeText = string.Concat(
			File.ReadAllText( packageSmoke ),
			Environment.NewLine,
			File.ReadAllText( rasterSmoke )
		);
		string[] requiredPackageWitnesses = [
			"TerminalRasterImage",
			"CursesRasterResource",
			"CursesRasterPlaceholder",
			"CursesRasterCell",
			"CursesRasterOwnershipState",
			"CursesRasterOwnershipStatus",
			"CursesRasterOwnershipLossReason"
		];
		foreach ( string witness in requiredPackageWitnesses ) {
			Assert.Contains(
				witness,
				packageSmokeText,
				StringComparison.Ordinal
			);
		}

		Assert.Contains(
			"RasterSmoke.cs",
			File.ReadAllText( unixPackageValidator ),
			StringComparison.Ordinal
		);
		Assert.Contains(
			"RasterSmoke.cs",
			File.ReadAllText( windowsPackageValidator ),
			StringComparison.Ordinal
		);
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? directory = new( AppContext.BaseDirectory );
		while ( directory is not null ) {
			if ( File.Exists(
				Path.Combine(
					directory.FullName,
					"Icod.DCurses.sln"
				)
			) ) {
				return directory.FullName;
			}
			directory = directory.Parent;
		}

		throw new InvalidOperationException(
			"Unable to locate the Icod.DCurses repository root for T1608 acceptance."
		);
	}
}
