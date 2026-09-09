using System.Runtime.CompilerServices;
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

		Assert.Equal(
			72,
			baselineSize
		);
		Assert.Equal(
			80,
			referenceCandidateSize
		);
		Assert.Equal(
			80,
			tokenCandidateSize
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
