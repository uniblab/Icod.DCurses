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
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the T1602 public retained-raster ownership facade candidate.</summary>
public sealed class CursesRasterPublicApiCandidateTests {
	[Fact]
	public void RasterFacadeMatchesFrozenT1602Candidate() {
		Assembly assembly = typeof( CursesSession ).Assembly;
		Type resourceType = RequireType( assembly, "Icod.DCurses.CursesRasterResource" );
		Type placeholderType = RequireType( assembly, "Icod.DCurses.CursesRasterPlaceholder" );
		Type cellType = RequireType( assembly, "Icod.DCurses.CursesRasterCell" );
		Type statusType = RequireType( assembly, "Icod.DCurses.CursesRasterOwnershipStatus" );
		Type reasonType = RequireType( assembly, "Icod.DCurses.CursesRasterOwnershipLossReason" );
		Type stateType = RequireType( assembly, "Icod.DCurses.CursesRasterOwnershipState" );

		Assert.True( resourceType.IsSealed );
		Assert.True( placeholderType.IsSealed );
		Assert.True( cellType.IsValueType );
		Assert.Empty( resourceType.GetConstructors() );
		Assert.Empty( placeholderType.GetConstructors() );
		Assert.Empty( cellType.GetConstructors() );
		Assert.Contains( typeof( IAsyncDisposable ), resourceType.GetInterfaces() );
		Assert.Contains( typeof( IAsyncDisposable ), placeholderType.GetInterfaces() );

		AssertEnumValue( statusType, "Current", 0 );
		AssertEnumValue( statusType, "Stale", 1 );
		AssertEnumValue( statusType, "Released", 2 );
		AssertEnumValue( statusType, "Disposed", 3 );
		AssertEnumValue( reasonType, "None", 0 );
		AssertEnumValue( reasonType, "SessionStateLost", 1 );
		AssertEnumValue( reasonType, "ResourceMissing", 2 );
		AssertEnumValue( reasonType, "ParentPlacementLost", 3 );
		AssertEnumValue( reasonType, "AncestorReleased", 4 );
		AssertEnumValue( reasonType, "ResourceReleased", 5 );
		AssertEnumValue( reasonType, "ExplicitDisposal", 6 );

		ConstructorInfo? stateConstructor = stateType.GetConstructor( [ statusType, reasonType ] );
		Assert.NotNull( stateConstructor );
		Assert.Equal( statusType, stateType.GetProperty( "Status" )?.PropertyType );
		Assert.Equal( reasonType, stateType.GetProperty( "LossReason" )?.PropertyType );

		Assert.Equal( typeof( int ), cellType.GetProperty( "Row" )?.PropertyType );
		Assert.Equal( typeof( int ), cellType.GetProperty( "Column" )?.PropertyType );

		MethodInfo createResource = RequireMethod(
			typeof( CursesSession ),
			"CreateRasterResourceAsync",
			[ typeof( TerminalRasterImage ), typeof( CancellationToken ) ]
		);
		Assert.Equal(
			MakeControlValueTask( resourceType ),
			createResource.ReturnType
		);

		Assert.Equal( stateType, resourceType.GetProperty( "OwnershipState" )?.PropertyType );
		MethodInfo createPlaceholder = RequireMethod(
			resourceType,
			"CreatePlaceholderAsync",
			[ typeof( int ), typeof( int ), typeof( CancellationToken ) ]
		);
		Assert.Equal(
			MakeControlValueTask( placeholderType ),
			createPlaceholder.ReturnType
		);
		Assert.Equal( typeof( ValueTask ), RequireMethod( resourceType, "DisposeAsync", [] ).ReturnType );

		Assert.Equal( typeof( int ), placeholderType.GetProperty( "Columns" )?.PropertyType );
		Assert.Equal( typeof( int ), placeholderType.GetProperty( "Rows" )?.PropertyType );
		Assert.Equal( stateType, placeholderType.GetProperty( "OwnershipState" )?.PropertyType );
		Assert.Equal(
			cellType,
			RequireMethod(
				placeholderType,
				"GetCell",
				[ typeof( int ), typeof( int ) ]
			).ReturnType
		);
		Assert.Equal( typeof( ValueTask ), RequireMethod( placeholderType, "DisposeAsync", [] ).ReturnType );
	}

	private static Type RequireType(
		Assembly assembly,
		string fullName
	) {
		ArgumentNullException.ThrowIfNull( assembly );
		ArgumentException.ThrowIfNullOrWhiteSpace( fullName );
		Type? type = assembly.GetType(
			fullName,
			throwOnError: false,
			ignoreCase: false
		);
		Assert.True(
			type is not null,
			$"Required T1602 public type {fullName} is missing."
		);
		return type!;
	}

	private static MethodInfo RequireMethod(
		Type type,
		string name,
		Type[] parameters
	) {
		ArgumentNullException.ThrowIfNull( type );
		ArgumentException.ThrowIfNullOrWhiteSpace( name );
		ArgumentNullException.ThrowIfNull( parameters );
		MethodInfo? method = type.GetMethod(
			name,
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
			binder: null,
			types: parameters,
			modifiers: null
		);
		Assert.True(
			method is not null,
			$"Required T1602 member {type.FullName}.{name} is missing."
		);
		return method!;
	}

	private static Type MakeControlValueTask( Type valueType ) {
		ArgumentNullException.ThrowIfNull( valueType );
		Type controlResult = typeof( TerminalControlResult<> ).MakeGenericType( valueType );
		return typeof( ValueTask<> ).MakeGenericType( controlResult );
	}

	private static void AssertEnumValue(
		Type enumType,
		string name,
		int expected
	) {
		ArgumentNullException.ThrowIfNull( enumType );
		Assert.True( enumType.IsEnum );
		object value = Enum.Parse(
			enumType,
			name,
			ignoreCase: false
		);
		Assert.Equal(
			expected,
			Convert.ToInt32( value, System.Globalization.CultureInfo.InvariantCulture )
		);
	}
}
