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
using System.Text.Json;
using Xunit;

/// <summary>Reviews the intentional additive public delta over stable 2.3.1.</summary>
public sealed class PublicTwoFourApiBaselineTests {
	private static readonly string[] ExpectedAddedTypes = [
		"Icod.DCurses.CursesClipboardOptions",
		"Icod.DCurses.CursesClipboardSelection",
		"Icod.DCurses.CursesCursorStyle",
		"Icod.DCurses.CursesCursorStyleLease"
	];

	[Fact]
	public void TwoFourAddsOnlyTheReviewedTypesOverTwoThree() {
		using JsonDocument prior = ReadFingerprint( "Public-API-Fingerprint-2.3.json" );
		using JsonDocument current = ReadFingerprint( "Public-API-Fingerprint-2.4.json" );
		string[] priorTypes = ReadTypes( prior );
		string[] currentTypes = ReadTypes( current );

		Assert.Empty( priorTypes.Except( currentTypes, StringComparer.Ordinal ) );
		Assert.Equal(
			ExpectedAddedTypes,
			currentTypes.Except( priorTypes, StringComparer.Ordinal )
				.OrderBy( static value => value, StringComparer.Ordinal )
		);
		Assert.Equal( "2.4.0", current.RootElement.GetProperty( "release" ).GetString() );
		Assert.Equal( "stable-source", current.RootElement.GetProperty( "status" ).GetString() );
	}

	[Fact]
	public void TwoFourSessionMembersUseOnlyDcursesAndFrameworkTypes() {
		PropertyInfo clipboardOptions = Assert.Single(
			typeof( CursesSessionOptions ).GetProperties(),
			static property => property.Name == nameof( CursesSessionOptions.ClipboardOptions )
		);
		Assert.Equal( typeof( CursesClipboardOptions ), clipboardOptions.PropertyType );

		MethodInfo[] clipboardMethods = typeof( CursesSession ).GetMethods()
			.Where( static method => method.Name is nameof( CursesSession.WriteClipboardAsync )
				or nameof( CursesSession.ReadClipboardAsync ) )
			.ToArray();
		Assert.Equal( 3, clipboardMethods.Length );
		MethodInfo cursorMethod = Assert.Single(
			typeof( CursesSession ).GetMethods(),
			static method => method.Name == nameof( CursesSession.AcquireCursorStyleAsync )
		);
		Assert.Equal( typeof( CursesCursorStyle ), cursorMethod.GetParameters()[ 0 ].ParameterType );
		Assert.DoesNotContain(
			clipboardMethods.Append( cursorMethod )
				.SelectMany( static method => method.GetParameters() )
				.Select( static parameter => parameter.ParameterType.Namespace ),
			static namespaceName => namespaceName is not null
				&& namespaceName.StartsWith( "Icod.Terminal", StringComparison.Ordinal )
		);
	}

	private static JsonDocument ReadFingerprint( string name ) {
		return JsonDocument.Parse( File.ReadAllText( Path.Combine( AppContext.BaseDirectory, name ) ) );
	}

	private static string[] ReadTypes( JsonDocument fingerprint ) {
		return fingerprint.RootElement.GetProperty( "exportedTypes" )
			.EnumerateArray()
			.Select( static element => element.GetString()! )
			.ToArray();
	}
}
