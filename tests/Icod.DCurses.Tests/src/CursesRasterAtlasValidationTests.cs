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
using System.Runtime.CompilerServices;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Freezes validation that must complete before atlas terminal I/O.</summary>
public sealed class CursesRasterAtlasValidationTests {
	[Theory]
	[InlineData( 0, 1, 1, 1 )]
	[InlineData( -1, 1, 1, 1 )]
	[InlineData( 257, 1, 1, 1 )]
	[InlineData( 1, 0, 1, 1 )]
	[InlineData( 1, -1, 1, 1 )]
	[InlineData( 1, 257, 1, 1 )]
	[InlineData( 1, 1, 0, 1 )]
	[InlineData( 1, 1, 1, 0 )]
	public void GeometryRejectsInvalidAxes(
		int rows,
		int columns,
		int tilePixelWidth,
		int tilePixelHeight
	) {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry(
				rows,
				columns,
				tilePixelWidth,
				tilePixelHeight
			)
		);
	}

	[Fact]
	public void GeometryRejectsPixelMultiplicationOverflow() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( 1, 256, int.MaxValue, 1 )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CursesRasterAtlasGeometry( 256, 1, 1, int.MaxValue )
		);
	}

	[Fact]
	public void TileUpdateRejectsNullImage() {
		Assert.Throws<ArgumentNullException>(
			() => new CursesRasterAtlasTileUpdate( 0, 0, null! )
		);
	}

	[Theory]
	[InlineData( 0 )]
	[InlineData( -1 )]
	public void GeometryQueryRejectsNonPositiveTimeoutBeforeSessionAccess(
		int milliseconds
	) {
		CursesSession session = Uninitialized<CursesSession>();
		Assert.Throws<ArgumentOutOfRangeException>(
			() => session.QueryRasterAtlasGeometryAsync(
				1,
				1,
				TimeSpan.FromMilliseconds( milliseconds )
			)
		);
	}

	[Theory]
	[InlineData( 0, 1 )]
	[InlineData( -1, 1 )]
	[InlineData( 257, 1 )]
	[InlineData( 1, 0 )]
	[InlineData( 1, -1 )]
	[InlineData( 1, 257 )]
	public void CreationRejectsInvalidAxesBeforeSessionAccess(
		int rows,
		int columns
	) {
		CursesSession session = Uninitialized<CursesSession>();
		TerminalRasterImage image = Rgb24( 1, 1 );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => session.CreateRasterAtlasAsync( image, rows, columns )
		);
	}

	[Fact]
	public void CreationRejectsNullAndNonDivisibleInitialImagesBeforeSessionAccess() {
		CursesSession session = Uninitialized<CursesSession>();
		Assert.Throws<ArgumentNullException>(
			() => session.CreateRasterAtlasAsync( null!, 1, 1 )
		);
		Assert.Throws<ArgumentException>(
			() => session.CreateRasterAtlasAsync( Rgb24( 2, 2 ), 1, 3 )
		);
		Assert.Throws<ArgumentException>(
			() => session.CreateRasterAtlasAsync( Rgb24( 2, 2 ), 3, 1 )
		);
	}

	[Fact]
	public void PresentRejectsNullOversizedCoordinatesDuplicatesFormatsAndSizesBeforeIo() {
		CursesRasterAtlas atlas = UninitializedAtlas( 2, 2, 1, 1 );
		TerminalRasterImage pixel = Rgb24( 1, 1 );

		Assert.Throws<ArgumentNullException>(
			() => atlas.PresentAsync( null! )
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => atlas.PresentAsync(
				Enumerable.Repeat(
					new CursesRasterAtlasTileUpdate( 0, 0, pixel ),
					CursesRasterAtlas.MaximumUpdatesPerPresentation + 1
				).ToArray()
			)
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => atlas.PresentAsync(
				[ new CursesRasterAtlasTileUpdate( 2, 0, pixel ) ]
			)
		);
		Assert.Throws<ArgumentException>(
			() => atlas.PresentAsync(
				[
					new CursesRasterAtlasTileUpdate( 0, 0, pixel ),
					new CursesRasterAtlasTileUpdate( 0, 0, pixel )
				]
			)
		);
		Assert.Throws<NotSupportedException>(
			() => atlas.PresentAsync(
				[ new CursesRasterAtlasTileUpdate( 0, 0, Indexed8( 1, 1 ) ) ]
			)
		);
		Assert.Throws<ArgumentException>(
			() => atlas.PresentAsync(
				[ new CursesRasterAtlasTileUpdate( 0, 0, Rgb24( 2, 1 ) ) ]
			)
		);
	}

	private static CursesRasterAtlas UninitializedAtlas(
		int rows,
		int columns,
		int tilePixelWidth,
		int tilePixelHeight
	) {
		CursesRasterAtlas atlas = Uninitialized<CursesRasterAtlas>();
		SetField( atlas, "rows", rows );
		SetField( atlas, "columns", columns );
		SetField( atlas, "tilePixelWidth", tilePixelWidth );
		SetField( atlas, "tilePixelHeight", tilePixelHeight );
		return atlas;
	}

	private static T Uninitialized<T>() where T : class {
		return (T)RuntimeHelpers.GetUninitializedObject( typeof( T ) );
	}

	private static void SetField(
		object target,
		string name,
		object value
	) {
		FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(
			target.GetType().GetField(
				name,
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		field.SetValue( target, value );
	}

	private static TerminalRasterImage Rgb24(
		int width,
		int height
	) {
		return TerminalRasterImage.CreateRgb24(
			width,
			height,
			new byte[ checked( width * height * 3 ) ]
		);
	}

	private static TerminalRasterImage Indexed8(
		int width,
		int height
	) {
		return TerminalRasterImage.CreateIndexed8(
			width,
			height,
			new byte[ checked( width * height ) ],
			[ new TerminalRasterColor( 0, 0, 0 ) ]
		);
	}
}
