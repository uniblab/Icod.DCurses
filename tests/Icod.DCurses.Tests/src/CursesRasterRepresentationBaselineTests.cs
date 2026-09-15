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

/// <summary>Freezes the T1603 sparse retained-raster representation cost and allocation shape.</summary>
public sealed class CursesRasterRepresentationBaselineTests {
	[Fact]
	public void UnconditionalRasterReferenceWouldRepeatTheRejectedFourMiBLargePadTax() {
		Assert.Equal( 8, IntPtr.Size );
		const long Rows = 2_048;
		const long Columns = 256;
		long cellCount = Rows * Columns;
		long permanentReferencePayload = cellCount * IntPtr.Size;

		Assert.Equal( 524_288L, cellCount );
		Assert.Equal( 4L * 1024L * 1024L, permanentReferencePayload );
	}

	[Fact]
	public void TenSparseRasterRowsRetainTheEstablishedThirtySixKiBReferenceShape() {
		Assert.Equal( 8, IntPtr.Size );
		const int Rows = 2_048;
		const int Columns = 256;
		const int RasterRows = 10;
		CursesSparseCellPlane<object> plane = new( Columns, Rows );
		object marker = new();

		for ( int index = 0; index < RasterRows; index++ ) {
			plane.Set(
				index * 197,
				index,
				marker
			);
		}

		Assert.Equal( RasterRows, plane.AllocatedRowCount );
		long materializedSlots = Rows + ( (long)RasterRows * Columns );
		Assert.Equal( 4_608L, materializedSlots );
		Assert.Equal( 36L * 1024L, materializedSlots * IntPtr.Size );
	}

	[Fact]
	public void VirtualScreenRasterStorageIsLazyAndReleasedWhenEmpty() {
		CursesVirtualScreen screen = new( 256, 2_048 );
		PropertyInfo storageAllocated = RequireInternalProperty(
			typeof( CursesVirtualScreen ),
			"RasterStorageAllocated"
		);
		PropertyInfo allocatedRows = RequireInternalProperty(
			typeof( CursesVirtualScreen ),
			"RasterAllocatedRowCount"
		);
		MethodInfo setRasterCell = RequirePublicMethod(
			typeof( CursesVirtualScreen ),
			"SetRasterCell"
		);
		CursesRasterCell token = CreateLogicalRasterCell();

		Assert.False( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 0, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );

		_ = setRasterCell.Invoke(
			screen,
			[ 197, 7, token ]
		);

		Assert.True( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 1, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );

		_ = setRasterCell.Invoke(
			screen,
			[ 197, 7, null ]
		);

		Assert.False( Assert.IsType<bool>( storageAllocated.GetValue( screen ) ) );
		Assert.Equal( 0, Assert.IsType<int>( allocatedRows.GetValue( screen ) ) );
	}

	internal static CursesRasterCell CreateLogicalRasterCell() {
		CursesRasterPlaceholder placeholder =
			(CursesRasterPlaceholder)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
				typeof( CursesRasterPlaceholder )
			);
		return new CursesRasterCell(
			placeholder,
			default
		);
	}

	private static PropertyInfo RequireInternalProperty(
		Type type,
		string name
	) {
		PropertyInfo? property = type.GetProperty(
			name,
			BindingFlags.Instance | BindingFlags.NonPublic
		);
		Assert.True(
			property is not null,
			$"Required T1603 internal property {type.FullName}.{name} is missing."
		);
		return property!;
	}

	private static MethodInfo RequirePublicMethod(
		Type type,
		string name
	) {
		MethodInfo? method = type.GetMethods( BindingFlags.Instance | BindingFlags.Public )
			.SingleOrDefault( current => string.Equals( current.Name, name, StringComparison.Ordinal ) );
		Assert.True(
			method is not null,
			$"Required T1603 public method {type.FullName}.{name} is missing."
		);
		return method!;
	}
}
