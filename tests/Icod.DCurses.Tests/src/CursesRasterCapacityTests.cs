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

using System.Runtime.CompilerServices;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises DCurses-owned raster bounds without duplicating Terminal live registries.</summary>
public sealed class CursesRasterCapacityTests {
	[Theory]
	[InlineData( 0, 1 )]
	[InlineData( -1, 1 )]
	[InlineData( 257, 1 )]
	[InlineData( 1, 0 )]
	[InlineData( 1, -1 )]
	[InlineData( 1, 257 )]
	public void OutOfRangePlaceholderDimensionsFailBeforeDisposedStateIsObserved(
		int columns,
		int rows
	) {
		CursesRasterResource resource =
			(CursesRasterResource)RuntimeHelpers.GetUninitializedObject(
				typeof( CursesRasterResource )
			);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => resource.CreatePlaceholderAsync(
				columns,
				rows
			)
		);
	}

	[Theory]
	[InlineData( 1, 1 )]
	[InlineData( 256, 1 )]
	[InlineData( 1, 256 )]
	[InlineData( 256, 256 )]
	public void BoundaryPlaceholderDimensionsPassLocalValidationBeforeDisposedStateIsObserved(
		int columns,
		int rows
	) {
		CursesRasterResource resource =
			(CursesRasterResource)RuntimeHelpers.GetUninitializedObject(
				typeof( CursesRasterResource )
			);

		Assert.Throws<ObjectDisposedException>(
			() => resource.CreatePlaceholderAsync(
				columns,
				rows
			)
		);
	}

	[Fact]
	public void DefaultRasterCellRejectionIsMutationAtomic() {
		CursesVirtualScreen screen = new(
			4,
			2
		);
		screen.MarkClean();

		Assert.Throws<ArgumentException>(
			() => screen.SetRasterCell(
				1,
				2,
				default( CursesRasterCell )
			)
		);

		Assert.Equal( 0, screen.RasterCellCount );
		Assert.Equal( 0, screen.RasterAllocatedRowCount );
		Assert.False( screen.RasterStorageAllocated );
		Assert.False( screen.IsDirty( 1, 2 ) );
	}

	[Fact]
	public void DefaultWindowRasterWriteDoesNotAdvanceCursorOrAllocateStorage() {
		CursesScreen screen = new(
			4,
			2
		);
		CursesWindow window = screen.StandardWindow;
		window.Move(
			1,
			2
		);
		screen.VirtualScreen.MarkClean();

		Assert.Throws<ArgumentException>(
			() => window.WriteRasterCell( default )
		);

		Assert.Equal( 1, window.CursorRow );
		Assert.Equal( 2, window.CursorColumn );
		Assert.Equal( 0, screen.VirtualScreen.RasterCellCount );
		Assert.False( screen.VirtualScreen.RasterStorageAllocated );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}
}
