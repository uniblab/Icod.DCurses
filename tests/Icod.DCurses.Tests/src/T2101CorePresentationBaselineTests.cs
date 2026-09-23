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

/// <summary>Freezes the compiled 2.0 identity and production dependency boundary for T2101.</summary>
public sealed class T2101CorePresentationBaselineTests {
	[Fact]
	public void PublishedTwoZeroAssemblyIdentityRemainsFrozen() {
		Assembly assembly = typeof( CursesSession ).Assembly;

		Assert.Equal(
			new Version( 2, 0, 0, 0 ),
			assembly.GetName().Version
		);
		Assert.Equal( 75, assembly.GetExportedTypes().Length );
	}

	[Fact]
	public void ProductionAssemblyReferencesOnlyTerminalAmongIcodAssemblies() {
		string[] references = typeof( CursesSession ).Assembly
			.GetReferencedAssemblies()
			.Select( static reference => reference.Name )
			.Where( static name => name?.StartsWith(
				"Icod.",
				StringComparison.Ordinal
			) is true )
			.Cast<string>()
			.OrderBy( static name => name, StringComparer.Ordinal )
			.ToArray();

		Assert.Equal(
			[ "Icod.Terminal" ],
			references
		);
	}
}
