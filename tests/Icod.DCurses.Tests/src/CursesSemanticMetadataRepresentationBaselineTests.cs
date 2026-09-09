using System.Runtime.CompilerServices;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Establishes the test-side representation baseline used by the 1.1 semantic-metadata memory gate.
/// </summary>
public sealed class CursesSemanticMetadataRepresentationBaselineTests {
	[Fact]
	public void CandidateCellRepresentationsExposePermanentPerCellCost() {
		int baselineSize = Unsafe.SizeOf<CursesCell>();
		int referenceCandidateSize = Unsafe.SizeOf<CellWithMetadataReference>();
		int tokenCandidateSize = Unsafe.SizeOf<CellWithMetadataToken>();

		Assert.True( 0 < baselineSize );
		Assert.True( baselineSize < referenceCandidateSize );
		Assert.True( baselineSize <= tokenCandidateSize );

		long largePadCellCount = 2_048L * 256L;
		long referenceCandidateOverhead =
			( referenceCandidateSize - baselineSize ) * largePadCellCount;

		Assert.True( 0 < referenceCandidateOverhead );
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
