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

/// <summary>Guards the zero-public-delta decision for the 0.8 hardening release.</summary>
public sealed class PublicHardeningApiContractTests {
	[Fact]
	public void SessionLifetimeHardeningTypesRemainNonpublic() {
		Type sessionType = typeof( CursesSession );
		Type? waitScope = sessionType.GetNestedType(
			"SessionWaitCancellationScope",
			BindingFlags.NonPublic
		);

		Assert.NotNull( waitScope );
		Assert.False( waitScope!.IsPublic );
		Assert.False( waitScope.IsNestedPublic );
	}

	[Fact]
	public void NoHardeningOrConcurrencyTypeIsExported() {
		string[] forbiddenFragments = [
			"Hardening",
			"Concurrency",
			"CancellationScope",
			"SessionLifetime"
		];
		Type[] exported = typeof( CursesSession ).Assembly.GetExportedTypes();

		foreach ( Type type in exported ) {
			Assert.DoesNotContain(
				forbiddenFragments,
				fragment => type.Name.Contains(
					fragment,
					StringComparison.Ordinal
				)
			);
		}
	}
}
