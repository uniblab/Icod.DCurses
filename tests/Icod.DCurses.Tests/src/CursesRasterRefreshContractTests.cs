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
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes raster refresh on the Terminal-owned transactional boundary.</summary>
public sealed class CursesRasterRefreshContractTests {
	[Fact]
	public void RasterPlaceholderOutputUsesPreparedTerminalTransaction() {
		Assembly assembly = typeof( CursesRefreshEngine ).Assembly;
		MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(
			typeof( CursesPreparedRefresh ).GetMethod(
				"WriteRasterPlaceholderCell",
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				[ typeof( CursesRasterCell ) ],
				modifiers: null
			)
		);
		Assert.Equal( typeof( void ), method.ReturnType );
		Assert.Null(
			assembly.GetType(
				"Icod.DCurses.Terminal.ITerminalRasterPlaceholderOutput",
				throwOnError: false
			)
		);
	}

	[Fact]
	public void LegacyTerminalOutputAdapterRemainsDeleted() {
		Assembly assembly = typeof( CursesRefreshEngine ).Assembly;
		Assert.Null(
			assembly.GetType(
				"Icod.DCurses.Terminal.TerminalSessionCursesOutput",
				throwOnError: false
			)
		);
	}

	[Fact]
	public void PhysicalScreenStateRetainsRasterAsThirdAxis() {
		Type type = typeof( CursesPhysicalScreenState );
		PropertyInfo count = Assert.IsAssignableFrom<PropertyInfo>(
			type.GetProperty(
				"RasterCellCount",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		Assert.Equal( typeof( int ), count.PropertyType );

		MethodInfo get = Assert.IsAssignableFrom<MethodInfo>(
			type.GetMethod(
				"GetRasterCell",
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				[ typeof( int ), typeof( int ) ],
				modifiers: null
			)
		);
		Assert.Equal( typeof( CursesRasterCell? ), get.ReturnType );

		MethodInfo set = Assert.IsAssignableFrom<MethodInfo>(
			type.GetMethod(
				"SetCell",
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				[
					typeof( int ),
					typeof( int ),
					typeof( CursesCell ),
					typeof( CursesCellMetadata ),
					typeof( CursesRasterCell? )
				],
				modifiers: null
			)
		);
		Assert.Equal( typeof( void ), set.ReturnType );
	}
}
