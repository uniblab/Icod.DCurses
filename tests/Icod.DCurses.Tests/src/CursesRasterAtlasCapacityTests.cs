/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises raster-atlas geometry and update-list boundaries before output.</summary>
public sealed class CursesRasterAtlasCapacityTests {
	[Theory]
	[InlineData( 0 )]
	[InlineData( 257 )]
	public void GeometryRejectsCellAxesOutsideThePublishedBound( int axis ) {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( axis, 1, 1, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( 1, axis, 1, 1 )
		);
	}

	[Fact]
	public void GeometryAcceptsBothInclusiveCellAxisEdges() {
		Assert.Equal( 1, new CursesRasterAtlasGeometry( 1, 1, 1, 1 ).Rows );
		Assert.Equal( 256, new CursesRasterAtlasGeometry( 256, 256, 1, 1 ).Columns );
	}

	[Fact]
	public void GeometryRejectsPixelProductsOutsideInt32() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( 1, 256, int.MaxValue, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( 256, 1, 1, int.MaxValue )
		);
	}

	[Fact]
	public void UpdateValidationAcceptsTheMaximumUniqueListInRowMajorOrder() {
		TerminalRasterImage pixel = Rgb24();
		CursesRasterAtlasTileUpdate[] updates = Enumerable.Range(
			0,
			CursesRasterAtlas.MaximumUpdatesPerPresentation
		).Reverse().Select(
			index => new CursesRasterAtlasTileUpdate(
				index / 64,
				index % 64,
				pixel
			)
		).ToArray();

		CursesRasterAtlasTileUpdate[] ordered = CursesRasterAtlas.ValidateAndOrderUpdates(
			updates,
			64,
			64,
			1,
			1
		);

		Assert.Equal( CursesRasterAtlas.MaximumUpdatesPerPresentation, ordered.Length );
		for ( int index = 0; index < ordered.Length; index++ ) {
			Assert.Equal( index / 64, ordered[ index ].Row );
			Assert.Equal( index % 64, ordered[ index ].Column );
		}
	}

	[Fact]
	public void UpdateValidationRejectsOverCapacityBeforeReadingElements() {
		ThrowingLargeList updates = new();
		Assert.Throws<ArgumentOutOfRangeException>(
			() => CursesRasterAtlas.ValidateAndOrderUpdates( updates, 256, 256, 1, 1 )
		);
		Assert.Equal( 0, updates.ReadCount );
	}

	[Fact]
	public void UpdateValidationRejectsBoundsDuplicatesAndWrongTileGeometry() {
		TerminalRasterImage pixel = Rgb24();
		Assert.Throws<ArgumentOutOfRangeException>( () => Validate(
			[ new CursesRasterAtlasTileUpdate( -1, 0, pixel ) ]
		) );
		Assert.Throws<ArgumentOutOfRangeException>( () => Validate(
			[ new CursesRasterAtlasTileUpdate( 0, 2, pixel ) ]
		) );
		Assert.Throws<ArgumentException>( () => Validate(
			[
				new CursesRasterAtlasTileUpdate( 0, 0, pixel ),
				new CursesRasterAtlasTileUpdate( 0, 0, pixel )
			]
		) );
		Assert.Throws<ArgumentException>( () => Validate(
			[ new CursesRasterAtlasTileUpdate( 0, 0, TerminalRasterImage.CreateRgb24( 2, 1, new byte[ 6 ] ) ) ]
		) );
	}

	private static CursesRasterAtlasTileUpdate[] Validate(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates
	) => CursesRasterAtlas.ValidateAndOrderUpdates( updates, 2, 2, 1, 1 );

	private static TerminalRasterImage Rgb24() =>
		TerminalRasterImage.CreateRgb24( 1, 1, new byte[ 3 ] );

	private sealed class ThrowingLargeList : IReadOnlyList<CursesRasterAtlasTileUpdate> {
		internal int ReadCount { get; private set; }
		public int Count => CursesRasterAtlas.MaximumUpdatesPerPresentation + 1;
		public CursesRasterAtlasTileUpdate this[ int index ] {
			get {
				ReadCount++;
				throw new InvalidOperationException( "The over-capacity list must not be read." );
			}
		}
		public IEnumerator<CursesRasterAtlasTileUpdate> GetEnumerator() =>
			throw new NotSupportedException();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
			GetEnumerator();
	}
}
