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

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the production dependency and compiled metadata boundary.</summary>
public sealed class T2007DependencyBoundaryTests {
	private const string TerminalPackage = "Icod.Terminal";
	private const string TerminalVersion = "1.18.0";
	private const string TermInfoAssembly = "Icod.TermInfo";

	[Fact]
	public void ProductionProjectDeclaresOnlyTerminalAtQualifiedMinimum() {
		XDocument project = XDocument.Load(
			Path.Combine( FindRepositoryRoot(), "Icod.DCurses.csproj" )
		);
		AssertProductionProjectDependencies( project );
	}

	[Fact]
	public void DirectTermInfoReferenceFailsTheProjectGuard() {
		XDocument project = XDocument.Parse(
			"<Project><ItemGroup>"
				+ "<PackageReference Include='Icod.Terminal' Version='1.18.0' />"
				+ "<PackageReference Include='Icod.TermInfo' Version='1.15.0' />"
				+ "</ItemGroup></Project>"
		);
		Assert.Throws<InvalidOperationException>(
			() => AssertProductionProjectDependencies( project )
		);
	}

	[Theory]
	[InlineData( "using Icod.TermInfo;" )]
	[InlineData( "Icod.TermInfo.TerminalDescription description;" )]
	[InlineData( "session.WriteTerminalStringAsync( value );" )]
	public void ForbiddenProductionTokensFailTheActualSourceGuard(
		string source
	) {
		Assert.True(
			T2005TransactionalBoundaryContractTests.ContainsForbiddenProductionToken( source )
		);
	}

	[Fact]
	public void CompiledProductionAssemblyHasNoTermInfoAssemblyOrTypeReferences() {
		Assembly assembly = typeof( CursesSession ).Assembly;
		foreach ( AssemblyName reference in assembly.GetReferencedAssemblies() ) {
			Assert.False(
				IsForbiddenAssemblyName( reference.Name ),
				$"Production assembly references {reference.Name}."
			);
		}

		using FileStream stream = File.OpenRead( assembly.Location );
		using PEReader pe = new( stream );
		MetadataReader metadata = pe.GetMetadataReader();
		foreach ( AssemblyReferenceHandle handle in metadata.AssemblyReferences ) {
			string name = metadata.GetString( metadata.GetAssemblyReference( handle ).Name );
			Assert.False( IsForbiddenAssemblyName( name ), $"AssemblyRef: {name}" );
		}
		foreach ( TypeReferenceHandle handle in metadata.TypeReferences ) {
			TypeReference type = metadata.GetTypeReference( handle );
			string namespaceName = metadata.GetString( type.Namespace );
			Assert.False(
				IsForbiddenTypeNamespace( namespaceName ),
				$"TypeRef: {namespaceName}.{metadata.GetString( type.Name )}"
			);
		}
	}

	[Fact]
	public void MetadataGuardsRejectInjectedTermInfoIdentities() {
		Assert.True( IsForbiddenAssemblyName( "Icod.TermInfo" ) );
		Assert.True( IsForbiddenTypeNamespace( "Icod.TermInfo" ) );
		Assert.True( IsForbiddenTypeNamespace( "Icod.TermInfo.Internal" ) );
		Assert.False( IsForbiddenAssemblyName( "Icod.Terminal" ) );
		Assert.False( IsForbiddenTypeNamespace( "Icod.Terminal" ) );
	}

	private static void AssertProductionProjectDependencies( XDocument project ) {
		ArgumentNullException.ThrowIfNull( project );
		XElement[] references = project.Descendants()
			.Where( element => "PackageReference" == element.Name.LocalName )
			.ToArray();
		if ( 1 != references.Length
			|| TerminalPackage != references[ 0 ].Attribute( "Include" )?.Value
			|| TerminalVersion != references[ 0 ].Attribute( "Version" )?.Value ) {
			throw new InvalidOperationException(
				"Production must depend directly only on Icod.Terminal 1.18.0."
			);
		}
	}

	private static bool IsForbiddenAssemblyName( string? value ) {
		return string.Equals( value, TermInfoAssembly, StringComparison.Ordinal );
	}

	private static bool IsForbiddenTypeNamespace( string? value ) {
		return string.Equals( value, TermInfoAssembly, StringComparison.Ordinal )
			|| ( value?.StartsWith( TermInfoAssembly + ".", StringComparison.Ordinal ) ?? false );
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
