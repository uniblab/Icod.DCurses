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
using Icod.Terminal;
using Xunit;

/// <summary>Guards the exact approved public break set for DCurses 2.0.</summary>
public sealed class PublicTwoZeroApiContractTests {
	[Fact]
	public void PublicSurfaceMatchesTheApprovedTwoZeroBreakManifest() {
		Type session = typeof( CursesSession );
		PropertyInfo? profile = session.GetProperty(
			"Profile",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
		);
		PropertyInfo? terminal = session.GetProperty(
			"Terminal",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
		);
		MethodInfo getDimensions = Assert.IsAssignableFrom<MethodInfo>(
			session.GetMethod( "GetDimensions", Type.EmptyTypes )
		);
		MethodInfo synchronizeDimensions = Assert.IsAssignableFrom<MethodInfo>(
			session.GetMethod( "SynchronizeDimensions", Type.EmptyTypes )
		);
		PropertyInfo lifecycleDimensions = Assert.IsAssignableFrom<PropertyInfo>(
			typeof( CursesLifecycleEvent ).GetProperty( "Dimensions" )
		);
		Type dimensionsResult = typeof( TerminalControlResult<TerminalDimensions> );

		Assert.NotNull( profile );
		Assert.Equal( typeof( TerminalProfile ), profile.PropertyType );
		Assert.Null( terminal );
		Assert.Equal( dimensionsResult, getDimensions.ReturnType );
		Assert.Equal( dimensionsResult, synchronizeDimensions.ReturnType );
		Assert.Equal( typeof( TerminalDimensions? ), lifecycleDimensions.PropertyType );
		Assert.Equal(
			new Version( 2, 0, 0, 0 ),
			typeof( CursesSession ).Assembly.GetName().Version
		);
	}
}
