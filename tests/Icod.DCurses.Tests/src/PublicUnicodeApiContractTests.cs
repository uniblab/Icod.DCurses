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
using Icod.DCurses;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Freezes the intentionally small public Unicode/column surface introduced by the 0.3 release line.
/// </summary>
public sealed class PublicUnicodeApiContractTests {
	[Fact]
	public void AmbiguousWidthPolicyValuesAreFrozen() {
		Assert.Equal( 0, (int)CursesAmbiguousWidthPolicy.Narrow );
		Assert.Equal( 1, (int)CursesAmbiguousWidthPolicy.Wide );
	}

	[Fact]
	public void CursesTextDeclaresOnlyApprovedColumnHelpers() {
		MethodInfo[] methods = typeof( CursesText )
			.GetMethods(
				BindingFlags.Public
					| BindingFlags.Static
					| BindingFlags.DeclaredOnly
			)
			.OrderBy( static method => method.Name, StringComparer.Ordinal )
			.ToArray();

		Assert.Equal( 3, methods.Length );
		AssertMethod(
			methods[ 0 ],
			nameof( CursesText.MeasureColumns ),
			typeof( int ),
			[ typeof( string ), typeof( ICursesTextWidthProvider ) ]
		);
		AssertMethod(
			methods[ 1 ],
			nameof( CursesText.SliceByColumns ),
			typeof( string ),
			[
				typeof( string ),
				typeof( int ),
				typeof( int ),
				typeof( ICursesTextWidthProvider )
			]
		);
		AssertMethod(
			methods[ 2 ],
			nameof( CursesText.TruncateToColumns ),
			typeof( string ),
			[ typeof( string ), typeof( int ), typeof( ICursesTextWidthProvider ) ]
		);
	}

	[Fact]
	public void BuiltInProviderExposesOnlyApprovedNewPolicyMembers() {
		Assert.Equal(
			CursesAmbiguousWidthPolicy.Narrow,
			UnicodeCursesTextWidthProvider.Instance.AmbiguousWidthPolicy
		);
		Assert.Equal(
			CursesAmbiguousWidthPolicy.Wide,
			UnicodeCursesTextWidthProvider.WideAmbiguousInstance.AmbiguousWidthPolicy
		);
		Assert.Equal(
			"17.0.0",
			UnicodeCursesTextWidthProvider.UnicodeDataVersion
		);
	}

	private static void AssertMethod(
		MethodInfo method,
		string expectedName,
		Type expectedReturnType,
		IReadOnlyList<Type> expectedParameterTypes ) {
		ArgumentNullException.ThrowIfNull( method );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedName );
		ArgumentNullException.ThrowIfNull( expectedReturnType );
		ArgumentNullException.ThrowIfNull( expectedParameterTypes );

		Assert.Equal( expectedName, method.Name );
		Assert.Equal( expectedReturnType, method.ReturnType );
		ParameterInfo[] parameters = method.GetParameters();
		Assert.Equal( expectedParameterTypes.Count, parameters.Length );
		for ( int index = 0; index < parameters.Length; index++ ) {
			Assert.Equal(
				expectedParameterTypes[ index ],
				parameters[ index ].ParameterType
			);
		}

		ParameterInfo widthProvider = parameters[ ^1 ];
		Assert.Equal( typeof( ICursesTextWidthProvider ), widthProvider.ParameterType );
		Assert.True( widthProvider.HasDefaultValue );
		Assert.Null( widthProvider.DefaultValue );
	}
}
