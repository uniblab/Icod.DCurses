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

public sealed class EditorSampleProjectContractTests {
	[Fact]
	public void EditorSampleBuildsInSolutionUsingOnlyThePublicLibrary() {
		DirectoryInfo? current = new( AppContext.BaseDirectory );
		while ( current is not null && !File.Exists( Path.Combine( current.FullName, "Icod.DCurses.sln" ) ) ) {
			current = current.Parent;
		}
		Assert.NotNull( current );
		string folder = Path.Combine( current.FullName, "samples", "Icod.DCurses.Editor.Sample" );
		string projectPath = Path.Combine( folder, "Icod.DCurses.Editor.Sample.csproj" );
		Assert.True( File.Exists( projectPath ), "The T2110 sample project is missing." );
		XDocument project = XDocument.Load( projectPath );
		Assert.Equal( "Exe", project.Descendants( "OutputType" ).Single().Value );
		Assert.Equal( "net8.0;net9.0;net10.0", project.Descendants( "TargetFrameworks" ).Single().Value );
		Assert.Equal( "Debug;Staging;Release", project.Descendants( "Configurations" ).Single().Value );
		Assert.Equal( "false", project.Descendants( "IsPackable" ).Single().Value );
		Assert.Equal( @"..\..\Icod.DCurses.csproj",
			project.Descendants( "ProjectReference" ).Single().Attribute( "Include" )?.Value );
		Assert.Contains( @"samples\Icod.DCurses.Editor.Sample\Icod.DCurses.Editor.Sample.csproj",
			File.ReadAllText( Path.Combine( current.FullName, "Icod.DCurses.sln" ) ), StringComparison.Ordinal );
		foreach ( string name in new[] {
			"Program.cs",
			"EditorSampleState.cs",
			"EditorSampleInteraction.cs",
			"EditorSampleClipboard.cs"
		} ) {
			string source = File.ReadAllText( Path.Combine( folder, name ) );
			Assert.DoesNotContain( "Icod.DCurses.Internal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.Terminal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.TermInfo", source, StringComparison.Ordinal );
		}

		string program = File.ReadAllText( Path.Combine( folder, "Program.cs" ) );
		Assert.Contains( "AcquireInputProtocolsAsync", program, StringComparison.Ordinal );
		Assert.Contains( "CursesKeyboardReportingMode.EventTypes", program,
			StringComparison.Ordinal );
		Assert.Contains( "BracketedPaste = true", program, StringComparison.Ordinal );
		Assert.Contains( "FocusReporting = true", program, StringComparison.Ordinal );
		Assert.Contains( "CursesMouseTrackingMode.ButtonMotion", program,
			StringComparison.Ordinal );
		Assert.Contains( "await using CursesInputProtocolLease?", program,
			StringComparison.Ordinal );
	}
}
