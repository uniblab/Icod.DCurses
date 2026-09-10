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

using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes the deliberate public API delta for the 0.7 refresh-optimization release.</summary>
public sealed class PublicRefreshOptimizationApiContractTests {
	[Fact]
	public void SynchronizedOutputOptionIsThePublicRefreshOptimizationDelta() {
		System.Reflection.PropertyInfo? property = typeof( CursesSessionOptions )
			.GetProperty( nameof( CursesSessionOptions.UseSynchronizedOutput ) );

		Assert.NotNull( property );
		Assert.Equal( typeof( bool ), property.PropertyType );
		Assert.True( property.CanRead );
		Assert.True( property.CanWrite );
		Assert.False( new CursesSessionOptions().UseSynchronizedOutput );
	}

	[Fact]
	public void OptimizationImplementationTypesRemainInternal() {
		Type[] implementationTypes = [
			typeof( CursesOutputCostModel ),
			typeof( CursesCursorMotionResolver ),
			typeof( CursesEraseResolver ),
			typeof( CursesCharacterShiftResolver ),
			typeof( CursesLineShiftResolver ),
			typeof( CursesRefreshEngine )
		];

		Assert.All(
			implementationTypes,
			type => {
				Assert.False( type.IsPublic );
				Assert.False( type.IsNestedPublic );
			}
		);
	}
}
