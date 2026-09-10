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
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Establishes the test-side representation baseline used by the 1.1 semantic-metadata memory gate.
/// </summary>
public sealed class CursesSemanticMetadataRepresentationBaselineTests {
	[Fact]
	public void CandidateCellRepresentationsExposePermanentPerCellCost() {
		Assert.Equal(
			8,
			IntPtr.Size
		);

		int baselineSize = Unsafe.SizeOf<CursesCell>();
		int referenceCandidateSize = Unsafe.SizeOf<CellWithMetadataReference>();
		int tokenCandidateSize = Unsafe.SizeOf<CellWithMetadataToken>();

		Assert.True( 0 < baselineSize );
		Assert.Equal(
			IntPtr.Size,
			referenceCandidateSize - baselineSize
		);
		Assert.Equal(
			IntPtr.Size,
			tokenCandidateSize - baselineSize
		);

		const long LargePadCellCount = 2_048L * 256L;
		long referenceCandidateOverhead =
			( referenceCandidateSize - baselineSize ) * LargePadCellCount;

		Assert.Equal(
			4L * 1024L * 1024L,
			referenceCandidateOverhead
		);
	}

	[Fact]
	public void RowSparseReferencePlaneScalesWithSemanticRows() {
		const int Rows = 2_048;
		const int Columns = 256;
		const int LinkedRows = 10;

		long topLevelRowReferences = (long)Rows * IntPtr.Size;
		long linkedRowReferences = (long)LinkedRows * Columns * IntPtr.Size;
		long sparseReferencePayload = topLevelRowReferences + linkedRowReferences;
		long denseReferencePayload = (long)Rows * Columns * IntPtr.Size;

		Assert.Equal(
			16L * 1024L,
			topLevelRowReferences
		);
		Assert.Equal(
			20L * 1024L,
			linkedRowReferences
		);
		Assert.Equal(
			36L * 1024L,
			sparseReferencePayload
		);
		Assert.Equal(
			4L * 1024L * 1024L,
			denseReferencePayload
		);
	}

	[Fact]
	public void ReferencePadMaterializesOnlySemanticRowsAtAcceptedScale() {
		const int Rows = 2_048;
		const int Columns = 256;
		const int LinkedRows = 10;
		CursesSparseCellPlane<object> plane = new(
			Columns,
			Rows
		);
		object marker = new();

		Assert.True( plane.IsEmpty );
		Assert.Equal( 0, plane.AllocatedRowCount );

		for ( int index = 0; index < LinkedRows; index++ ) {
			plane.Set(
				index * 197,
				index,
				marker
			);
		}

		Assert.Equal( LinkedRows, plane.Count );
		Assert.Equal( LinkedRows, plane.AllocatedRowCount );
		long materializedReferenceSlots = Rows
			+ ( (long)plane.AllocatedRowCount * Columns );
		Assert.Equal( 4_608L, materializedReferenceSlots );
		Assert.Equal(
			36L * 1024L,
			materializedReferenceSlots * IntPtr.Size
		);

		plane.Clear();

		Assert.True( plane.IsEmpty );
		Assert.Equal( 0, plane.AllocatedRowCount );
	}

	[Fact]
	public void LargePadReferenceScaleRemainsExplicit() {
		const long Rows = 2_048;
		const long Columns = 256;

		Assert.Equal(
			524_288L,
			Rows * Columns
		);
	}

	private readonly record struct CellWithMetadataReference(
		CursesCell Cell,
		object? Metadata
	);

	private readonly record struct CellWithMetadataToken(
		CursesCell Cell,
		int MetadataToken
	);
}
