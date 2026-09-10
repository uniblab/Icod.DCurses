using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises the row-sparse storage selected for semantic cell metadata.</summary>
public sealed class CursesSparseCellPlaneTests {
	[Fact]
	public void EmptyPlaneAllocatesNoSemanticRows() {
		CursesSparseCellPlane<string> plane = new(
			256,
			2_048
		);

		Assert.True( plane.IsEmpty );
		Assert.Equal( 0, plane.Count );
		Assert.Equal( 0, plane.AllocatedRowCount );
		Assert.Null( plane.Get( 100, 200 ) );
	}

	[Fact]
	public void ValuesAllocateOnlyRowsWhichContainSemantics() {
		CursesSparseCellPlane<string> plane = new(
			256,
			2_048
		);

		plane.Set( 10, 20, "first" );
		plane.Set( 10, 21, "second" );
		plane.Set( 1_000, 42, "third" );

		Assert.Equal( 3, plane.Count );
		Assert.Equal( 2, plane.AllocatedRowCount );
		Assert.Equal( "first", plane.Get( 10, 20 ) );
		Assert.Equal( "second", plane.Get( 10, 21 ) );
		Assert.Equal( "third", plane.Get( 1_000, 42 ) );
		Assert.Null( plane.Get( 11, 20 ) );
	}

	[Fact]
	public void RemovingLastValueReleasesItsRowStorage() {
		CursesSparseCellPlane<string> plane = new(
			8,
			4
		);
		plane.Set( 1, 2, "first" );
		plane.Set( 1, 3, "second" );
		plane.Set( 2, 4, "third" );

		plane.Set( 1, 2, null );
		Assert.Equal( 2, plane.Count );
		Assert.Equal( 2, plane.AllocatedRowCount );

		plane.Set( 1, 3, null );
		Assert.Equal( 1, plane.Count );
		Assert.Equal( 1, plane.AllocatedRowCount );

		plane.Set( 2, 4, null );
		Assert.True( plane.IsEmpty );
		Assert.Equal( 0, plane.Count );
		Assert.Equal( 0, plane.AllocatedRowCount );
	}

	[Fact]
	public void SnapshotAndReplaceRowAreDetachedAndDeterministic() {
		CursesSparseCellPlane<string> plane = new(
			5,
			3
		);
		plane.Set( 1, 1, "one" );
		plane.Set( 1, 3, "three" );

		string?[] snapshot = plane.SnapshotRow( 1 );
		snapshot[ 1 ] = null;
		snapshot[ 2 ] = "two";

		Assert.Equal( "one", plane.Get( 1, 1 ) );
		Assert.Null( plane.Get( 1, 2 ) );

		plane.ReplaceRow(
			1,
			snapshot
		);

		Assert.Null( plane.Get( 1, 1 ) );
		Assert.Equal( "two", plane.Get( 1, 2 ) );
		Assert.Equal( "three", plane.Get( 1, 3 ) );
		Assert.Equal( 2, plane.Count );
		Assert.Equal( 1, plane.AllocatedRowCount );
	}

	[Fact]
	public void ClearReleasesAllSemanticRows() {
		CursesSparseCellPlane<string> plane = new(
			10,
			10
		);
		plane.Set( 2, 3, "a" );
		plane.Set( 7, 8, "b" );

		plane.Clear();

		Assert.True( plane.IsEmpty );
		Assert.Equal( 0, plane.Count );
		Assert.Equal( 0, plane.AllocatedRowCount );
		Assert.Null( plane.Get( 2, 3 ) );
		Assert.Null( plane.Get( 7, 8 ) );
	}

	[Theory]
	[InlineData( 0, 1 )]
	[InlineData( 1, 0 )]
	[InlineData( -1, 1 )]
	[InlineData( 1, -1 )]
	public void ConstructorRejectsInvalidDimensions(
		int columns,
		int rows
	) {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesSparseCellPlane<string>(
				columns,
				rows
			)
		);
	}

	[Theory]
	[InlineData( -1, 0 )]
	[InlineData( 2, 0 )]
	[InlineData( 0, -1 )]
	[InlineData( 0, 3 )]
	public void CoordinateOperationsRejectOutOfRangeCoordinates(
		int row,
		int column
	) {
		CursesSparseCellPlane<string> plane = new(
			3,
			2
		);

		Assert.Throws<ArgumentOutOfRangeException>(
			() => plane.Get(
				row,
				column
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => plane.Set(
				row,
				column,
				"value"
			)
		);
	}
}
