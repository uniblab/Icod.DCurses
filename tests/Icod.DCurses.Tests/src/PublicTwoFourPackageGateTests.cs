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

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

/// <summary>Guards durable package documentation and consumer witnesses.</summary>
public sealed partial class PublicTwoFourPackageGateTests {
	[Fact]
	public void PackageReadmeIsDurableAndUsesResolvableLinks() {
		string root = FindRepositoryRoot();
		string readme = File.ReadAllText( Path.Combine( root, "README.md" ) );

		Assert.Contains( "dotnet add package Icod.DCurses", readme, StringComparison.Ordinal );
		Assert.Contains( "CursesClipboardOptions", readme, StringComparison.Ordinal );
		Assert.Contains( "AcquireCursorStyleAsync", readme, StringComparison.Ordinal );
		Assert.DoesNotContain( "This source tree defines", readme, StringComparison.Ordinal );
		Assert.DoesNotContain( "After publication", readme, StringComparison.OrdinalIgnoreCase );

		foreach ( Match match in MarkdownLink().Matches( readme ) ) {
			string destination = match.Groups[ "destination" ].Value;
			Assert.True(
				destination.StartsWith( "https://", StringComparison.Ordinal )
					|| destination.StartsWith( "#", StringComparison.Ordinal ),
				$"Packaged README contains a non-resolvable relative link: {destination}"
			);
		}
	}

	[Fact]
	public void SampleGuidesDescribeTheCurrentReleaseAndEditorFoundation() {
		string root = FindRepositoryRoot();
		string readme = File.ReadAllText(
			Path.Combine( root, "samples", "README.md" )
		);
		string rasterReadme = File.ReadAllText(
			Path.Combine(
				root,
				"samples",
				"Icod.DCurses.RasterAtlas.Sample",
				"README.md"
			)
		);

		Assert.Contains( "Icod.Terminal 1.29.2", readme, StringComparison.Ordinal );
		Assert.Contains( "clipboard", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "cursor style", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "pointer capture", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "bracketed paste", readme, StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "stable 2.3.1 maintenance source", readme,
			StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "Icod.Terminal 1.28.0", readme,
			StringComparison.Ordinal );
		Assert.Contains( "current 2.5.2 source consumes stable `Icod.Terminal 1.29.2`",
			rasterReadme, StringComparison.Ordinal );
		Assert.DoesNotContain( "stable 2.3.1 maintenance source", rasterReadme,
			StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "sample consumes stable Terminal 1.28.0", rasterReadme,
			StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain(
			"resolved direct package is `Icod.Terminal 1.28.0`",
			rasterReadme,
			StringComparison.OrdinalIgnoreCase
		);
	}

	[Fact]
	public void PackageConsumerAndVerifierPreserveTheCurrentBoundary() {
		string root = FindRepositoryRoot();
		string smoke = File.ReadAllText(
			Path.Combine( root, "tools", "package-smoke", "Program.cs" )
		);
		string verifier = File.ReadAllText(
			Path.Combine( root, "tools", "package-verifier", "Program.cs" )
		);
		XDocument project = XDocument.Load( Path.Combine( root, "Icod.DCurses.csproj" ) );

		Assert.Contains( "CursesClipboardOptions", smoke, StringComparison.Ordinal );
		Assert.Contains( "WriteClipboardAsync", smoke, StringComparison.Ordinal );
		Assert.Contains( "ReadClipboardAsync", smoke, StringComparison.Ordinal );
		Assert.Contains( "AcquireCursorStyleAsync", smoke, StringComparison.Ordinal );
		Assert.Contains( "TerminalPackageVersion = \"1.29.2\"", verifier,
			StringComparison.Ordinal );
		Assert.Contains( "UTF-8", GetSingleValue( project, "PackageReleaseNotes" ),
			StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "mouse", GetSingleValue( project, "PackageReleaseNotes" ),
			StringComparison.OrdinalIgnoreCase );
	}

	[GeneratedRegex( @"\[[^\]]+\]\((?<destination>[^)]+)\)", RegexOptions.CultureInvariant )]
	private static partial Regex MarkdownLink();

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
