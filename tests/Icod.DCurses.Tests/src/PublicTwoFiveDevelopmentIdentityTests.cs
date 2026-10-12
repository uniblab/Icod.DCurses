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

using System.Reflection;
using System.Xml.Linq;
using Xunit;

/// <summary>Guards the 2.5 preview identity and frozen color foundation.</summary>
public sealed class PublicTwoFiveDevelopmentIdentityTests {
	[Fact]
	public void PreviewIdentityPreservesColorFoundationAndTerminalBoundary() {
		XDocument production = XDocument.Load(
			Path.Combine( FindRepositoryRoot(), "Icod.DCurses.csproj" )
		);

		Assert.Equal( "2.0.0.0", GetSingleValue( production, "AssemblyVersion" ) );

		XElement reference = Assert.Single(
			production.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
		);
		Assert.Equal( "Icod.Terminal", reference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.29.0", reference.Attribute( "Version" )?.Value );
		Assert.DoesNotContain(
			production.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
				&& "Icod.TermInfo" == current.Attribute( "Include" )?.Value
		);

		Assert.Equal(
			[ "Default", "Indexed", "Rgb" ],
			Enum.GetNames<CursesColorKind>()
		);
		Assert.Equal(
			[ "Opaque", "BlankCellsTransparent" ],
			Enum.GetNames<CursesPanelTransparency>()
		);
		AssertPublicStaticMethod(
			typeof( CursesColor ),
			nameof( CursesColor.Indexed ),
			typeof( CursesColor ),
			[ typeof( int ) ]
		);
		AssertPublicStaticMethod(
			typeof( CursesColor ),
			nameof( CursesColor.Rgb ),
			typeof( CursesColor ),
			[ typeof( byte ), typeof( byte ), typeof( byte ) ]
		);
		Assert.Equal( typeof( CursesColor ), typeof( CursesStyle ).GetProperty(
			nameof( CursesStyle.Foreground )
		)?.PropertyType );
		Assert.Equal( typeof( CursesColor ), typeof( CursesStyle ).GetProperty(
			nameof( CursesStyle.Background )
		)?.PropertyType );
		Assert.Equal( typeof( CursesStyle ), typeof( CursesCell ).GetProperty(
			nameof( CursesCell.Style )
		)?.PropertyType );
		Assert.Equal( typeof( bool ), typeof( CursesPresentationCapabilities ).GetProperty(
			nameof( CursesPresentationCapabilities.SupportsForegroundColor )
		)?.PropertyType );
		Assert.Equal( typeof( bool ), typeof( CursesPresentationCapabilities ).GetProperty(
			nameof( CursesPresentationCapabilities.SupportsBackgroundColor )
		)?.PropertyType );

		Assert.Equal(
			[ "2.5.0-alpha.1", "2.5.0-alpha.1" ],
			[
				GetSingleValue( production, "Version" ),
				GetSingleValue( production, "PackageVersion" )
			]
		);
	}

	private static void AssertPublicStaticMethod(
		Type declaringType,
		string name,
		Type returnType,
		Type[] parameterTypes
	) {
		MethodInfo? method = declaringType.GetMethod(
			name,
			BindingFlags.Public | BindingFlags.Static,
			binder: null,
			types: parameterTypes,
			modifiers: null
		);
		Assert.NotNull( method );
		Assert.Equal( returnType, method.ReturnType );
	}

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
