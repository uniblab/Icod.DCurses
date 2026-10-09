/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

namespace Icod.DCurses.RasterAtlas.Sample;

using Icod.DCurses;

/// <summary>Computes and applies one complete sample layout.</summary>
internal static class RasterAtlasSampleLayout {
	internal static bool TryArrange(
		CursesScreen screen,
		CursesWindow standard,
		CursesWindow map,
		CursesWindow status,
		CursesPanel help,
		out int mapRows,
		out int mapColumns
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( standard );
		ArgumentNullException.ThrowIfNull( map );
		ArgumentNullException.ThrowIfNull( status );
		ArgumentNullException.ThrowIfNull( help );

		standard.Clear();
		mapRows = 0;
		mapColumns = 0;
		if ( screen.Rows < 6 || screen.Columns < 20 ) {
			return false;
		}

		mapRows = Math.Min( 30, screen.Rows - 2 );
		mapColumns = Math.Min( 80, screen.Columns );
		map.SetBounds( new CursesRectangle( 0, 0, mapRows, mapColumns ) );
		status.SetBounds( new CursesRectangle( screen.Rows - 2, 0, 2, screen.Columns ) );
		int helpRows = Math.Min( 8, screen.Rows - 2 );
		int helpColumns = Math.Min( 54, screen.Columns - 2 );
		help.SetBounds( new CursesRectangle(
			( screen.Rows - helpRows ) / 2,
			( screen.Columns - helpColumns ) / 2,
			helpRows,
			helpColumns
		) );
		return true;
	}
}
