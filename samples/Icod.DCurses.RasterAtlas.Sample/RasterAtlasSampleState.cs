/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

namespace Icod.DCurses.RasterAtlas.Sample;

using Icod.DCurses;
using Icod.Terminal;

internal enum RasterAtlasTerrain {
	Meadow,
	Forest,
	Water,
	Road
}

internal readonly record struct RasterAtlasMoveResult(
	bool Moved,
	int OldRow,
	int OldColumn,
	int NewRow,
	int NewColumn,
	bool ViewportChanged
);

/// <summary>Application-owned deterministic world and camera.</summary>
internal sealed class RasterAtlasSampleState {
	internal const int WorldRows = 128;
	internal const int WorldColumns = 128;

	internal RasterAtlasSampleState( int viewportRows, int viewportColumns ) {
		ResizeViewport( viewportRows, viewportColumns );
	}

	internal int PlayerRow { get; private set; } = 8;
	internal int PlayerColumn { get; private set; } = 8;
	internal int OriginRow { get; private set; }
	internal int OriginColumn { get; private set; }
	internal int ViewportRows { get; private set; }
	internal int ViewportColumns { get; private set; }

	internal bool ResizeViewport( int rows, int columns ) {
		if ( rows < 1 || rows > 30 ) {
			throw new ArgumentOutOfRangeException( nameof( rows ) );
		}
		if ( columns < 1 || columns > 80 ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		int oldRows = ViewportRows;
		int oldColumns = ViewportColumns;
		int oldOriginRow = OriginRow;
		int oldOriginColumn = OriginColumn;
		ViewportRows = rows;
		ViewportColumns = columns;
		CenterCamera();
		return oldRows != rows
			|| oldColumns != columns
			|| oldOriginRow != OriginRow
			|| oldOriginColumn != OriginColumn;
	}

	internal RasterAtlasMoveResult Move( int rowDelta, int columnDelta ) {
		int oldRow = PlayerRow;
		int oldColumn = PlayerColumn;
		int oldOriginRow = OriginRow;
		int oldOriginColumn = OriginColumn;
		int candidateRow = Math.Clamp( PlayerRow + rowDelta, 0, WorldRows - 1 );
		int candidateColumn = Math.Clamp( PlayerColumn + columnDelta, 0, WorldColumns - 1 );
		if ( !IsPassable( candidateRow, candidateColumn ) ) {
			return new RasterAtlasMoveResult(
				false,
				oldRow,
				oldColumn,
				oldRow,
				oldColumn,
				false
			);
		}
		PlayerRow = candidateRow;
		PlayerColumn = candidateColumn;
		EnsureCameraContainsPlayer();
		return new RasterAtlasMoveResult(
			oldRow != PlayerRow || oldColumn != PlayerColumn,
			oldRow,
			oldColumn,
			PlayerRow,
			PlayerColumn,
			oldOriginRow != OriginRow || oldOriginColumn != OriginColumn
		);
	}

	internal CursesRasterAtlasTileUpdate[] CreateUpdates(
		RasterAtlasMoveResult move,
		CursesRasterAtlasGeometry geometry
	) {
		if ( !move.Moved ) {
			return [];
		}
		if ( move.ViewportChanged ) {
			return CreateFullUpdates( geometry );
		}
		return [
			CreateUpdate( move.OldRow, move.OldColumn, geometry ),
			CreateUpdate( move.NewRow, move.NewColumn, geometry )
		];
	}

	internal CursesRasterAtlasTileUpdate[] CreateFullUpdates(
		CursesRasterAtlasGeometry geometry
	) {
		ValidateGeometry( geometry );
		CursesRasterAtlasTileUpdate[] updates = new CursesRasterAtlasTileUpdate[
			checked( ViewportRows * ViewportColumns )
		];
		int index = 0;
		for ( int row = 0; row < ViewportRows; row++ ) {
			for ( int column = 0; column < ViewportColumns; column++ ) {
				updates[ index++ ] = CreateUpdate(
					OriginRow + row,
					OriginColumn + column,
					geometry
				);
			}
		}
		return updates;
	}

	internal TerminalRasterImage CreateInitialImage(
		CursesRasterAtlasGeometry geometry
	) {
		ValidateGeometry( geometry );
		byte[] pixels = new byte[ checked( geometry.PixelWidth * geometry.PixelHeight * 3 ) ];
		for ( int row = 0; row < ViewportRows; row++ ) {
			for ( int column = 0; column < ViewportColumns; column++ ) {
				GetColor( OriginRow + row, OriginColumn + column, out byte red, out byte green, out byte blue );
				FillTile( pixels, geometry, row, column, red, green, blue );
			}
		}
		return TerminalRasterImage.CreateRgb24(
			geometry.PixelWidth,
			geometry.PixelHeight,
			pixels
		);
	}

	internal CursesCell[] CreateTextFrame() {
		CursesCell[] cells = new CursesCell[ checked( ViewportRows * ViewportColumns ) ];
		for ( int row = 0; row < ViewportRows; row++ ) {
			for ( int column = 0; column < ViewportColumns; column++ ) {
				int worldRow = OriginRow + row;
				int worldColumn = OriginColumn + column;
				string glyph = worldRow == PlayerRow && worldColumn == PlayerColumn
					? "@"
					: TerrainAt( worldRow, worldColumn ) switch {
						RasterAtlasTerrain.Meadow => ".",
						RasterAtlasTerrain.Forest => "♣",
						RasterAtlasTerrain.Water => "~",
						_ => "#"
					};
				cells[ row * ViewportColumns + column ] = new CursesCell( glyph );
			}
		}
		return cells;
	}

	internal static RasterAtlasTerrain TerrainAt( int row, int column ) {
		if ( row < 0 || row >= WorldRows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( column < 0 || column >= WorldColumns ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 == row % 8 || 0 == column % 12 ) {
			return RasterAtlasTerrain.Road;
		}
		int pattern = ( row * 17 + column * 31 + row * column ) % 19;
		return pattern switch {
			0 or 1 => RasterAtlasTerrain.Water,
			2 or 3 or 4 => RasterAtlasTerrain.Forest,
			_ => RasterAtlasTerrain.Meadow
		};
	}

	private CursesRasterAtlasTileUpdate CreateUpdate(
		int worldRow,
		int worldColumn,
		CursesRasterAtlasGeometry geometry
	) {
		int row = worldRow - OriginRow;
		int column = worldColumn - OriginColumn;
		GetColor( worldRow, worldColumn, out byte red, out byte green, out byte blue );
		byte[] pixels = new byte[ checked(
			geometry.TilePixelWidth * geometry.TilePixelHeight * 3
		) ];
		for ( int index = 0; index < pixels.Length; index += 3 ) {
			pixels[ index ] = red;
			pixels[ index + 1 ] = green;
			pixels[ index + 2 ] = blue;
		}
		return new CursesRasterAtlasTileUpdate(
			row,
			column,
			TerminalRasterImage.CreateRgb24(
				geometry.TilePixelWidth,
				geometry.TilePixelHeight,
				pixels
			)
		);
	}

	private void GetColor(
		int row,
		int column,
		out byte red,
		out byte green,
		out byte blue
	) {
		if ( row == PlayerRow && column == PlayerColumn ) {
			( red, green, blue ) = ( 245, 220, 80 );
			return;
		}
		( red, green, blue ) = TerrainAt( row, column ) switch {
			RasterAtlasTerrain.Meadow => ( (byte)46, (byte)125, (byte)50 ),
			RasterAtlasTerrain.Forest => ( (byte)18, (byte)72, (byte)35 ),
			RasterAtlasTerrain.Water => ( (byte)28, (byte)92, (byte)160 ),
			_ => ( (byte)138, (byte)111, (byte)70 )
		};
	}

	private static bool IsPassable( int row, int column ) =>
		TerrainAt( row, column ) is not RasterAtlasTerrain.Water;

	private void CenterCamera() {
		OriginRow = Math.Clamp(
			PlayerRow - ViewportRows / 2,
			0,
			WorldRows - ViewportRows
		);
		OriginColumn = Math.Clamp(
			PlayerColumn - ViewportColumns / 2,
			0,
			WorldColumns - ViewportColumns
		);
	}

	private void EnsureCameraContainsPlayer() {
		if ( PlayerRow < OriginRow ) {
			OriginRow = PlayerRow;
		} else if ( PlayerRow >= OriginRow + ViewportRows ) {
			OriginRow = PlayerRow - ViewportRows + 1;
		}
		if ( PlayerColumn < OriginColumn ) {
			OriginColumn = PlayerColumn;
		} else if ( PlayerColumn >= OriginColumn + ViewportColumns ) {
			OriginColumn = PlayerColumn - ViewportColumns + 1;
		}
	}

	private void ValidateGeometry( CursesRasterAtlasGeometry geometry ) {
		if ( geometry.Rows != ViewportRows || geometry.Columns != ViewportColumns ) {
			throw new ArgumentException(
				"Atlas geometry must match the current sample viewport.",
				nameof( geometry )
			);
		}
	}

	private static void FillTile(
		byte[] pixels,
		CursesRasterAtlasGeometry geometry,
		int tileRow,
		int tileColumn,
		byte red,
		byte green,
		byte blue
	) {
		for ( int pixelRow = 0; pixelRow < geometry.TilePixelHeight; pixelRow++ ) {
			int destination = checked(
				( ( tileRow * geometry.TilePixelHeight + pixelRow ) * geometry.PixelWidth
					+ tileColumn * geometry.TilePixelWidth ) * 3
			);
			for ( int pixelColumn = 0; pixelColumn < geometry.TilePixelWidth; pixelColumn++ ) {
				pixels[ destination++ ] = red;
				pixels[ destination++ ] = green;
				pixels[ destination++ ] = blue;
			}
		}
	}
}
