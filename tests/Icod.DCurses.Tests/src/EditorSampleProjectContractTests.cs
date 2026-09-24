using System.Xml.Linq;
using Xunit;

namespace Icod.DCurses.Tests;

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
		foreach ( string name in new[] { "Program.cs", "EditorSampleState.cs" } ) {
			string source = File.ReadAllText( Path.Combine( folder, name ) );
			Assert.DoesNotContain( "Icod.DCurses.Internal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.Terminal", source, StringComparison.Ordinal );
			Assert.DoesNotContain( "Icod.TermInfo", source, StringComparison.Ordinal );
		}
	}
}
