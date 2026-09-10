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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Guards the intentionally exposed Icod.Terminal and Icod.TermInfo types in the
/// public DCurses contract.
/// </summary>
public sealed class PublicDependencyBoundaryTests {
	private static readonly HashSet<string> ExpectedDependencyTypes = new(
		StringComparer.Ordinal
	) {
		"Icod.Terminal.TerminalControlResult`1",
		"Icod.Terminal.TerminalEndpoint",
		"Icod.Terminal.TerminalSession",
		"Icod.TermInfo.TerminalDescription",
		"Icod.TermInfo.TerminalSize"
	};

	[Fact]
	public void PublicApiExposesOnlyApprovedTerminalAndTermInfoTypes() {
		HashSet<string> actual = [];
		Assembly assembly = typeof( CursesSession ).Assembly;

		foreach ( Type type in assembly.GetExportedTypes() ) {
			CollectType( type.BaseType, actual );
			foreach ( Type interfaceType in type.GetInterfaces() ) {
				CollectType( interfaceType, actual );
			}

			foreach ( ConstructorInfo constructor in type.GetConstructors() ) {
				foreach ( ParameterInfo parameter in constructor.GetParameters() ) {
					CollectType( parameter.ParameterType, actual );
				}
			}

			foreach ( PropertyInfo property in type.GetProperties() ) {
				CollectType( property.PropertyType, actual );
				foreach ( ParameterInfo parameter in property.GetIndexParameters() ) {
					CollectType( parameter.ParameterType, actual );
				}
			}

			foreach ( EventInfo eventInfo in type.GetEvents() ) {
				CollectType( eventInfo.EventHandlerType, actual );
			}

			foreach ( MethodInfo method in type.GetMethods() ) {
				CollectType( method.ReturnType, actual );
				foreach ( ParameterInfo parameter in method.GetParameters() ) {
					CollectType( parameter.ParameterType, actual );
				}
			}
		}

		Assert.Equal(
			ExpectedDependencyTypes.OrderBy( static value => value, StringComparer.Ordinal ),
			actual.OrderBy( static value => value, StringComparer.Ordinal )
		);
	}

	private static void CollectType(
		Type? type,
		HashSet<string> result
	) {
		ArgumentNullException.ThrowIfNull( result );
		if ( type is null ) {
			return;
		}
		if ( type.IsByRef || type.IsPointer || type.IsArray ) {
			CollectType( type.GetElementType(), result );
			return;
		}
		if ( type.IsGenericParameter ) {
			return;
		}

		Type definition = type.IsGenericType
			? type.GetGenericTypeDefinition()
			: type
		;
		string? namespaceName = definition.Namespace;
		if ( string.Equals(
			namespaceName,
			"Icod.Terminal",
			StringComparison.Ordinal
		) || string.Equals(
			namespaceName,
			"Icod.TermInfo",
			StringComparison.Ordinal
		) ) {
			result.Add( definition.FullName! );
		}

		if ( type.IsGenericType ) {
			foreach ( Type argument in type.GetGenericArguments() ) {
				CollectType( argument, result );
			}
		}
	}
}
