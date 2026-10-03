/*
	Icod.DCurses
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.DCurses;

using Icod.DCurses.Internal;
using Icod.Terminal;

public sealed partial class CursesSession {
	/// <summary>Refreshes text and presents one complete cell-aligned raster image in the same output transaction.</summary>
	/// <param name="image">The complete application-owned image; pixels must exactly match <paramref name="geometry"/>.</param>
	/// <param name="row">The non-negative destination row.</param>
	/// <param name="column">The non-negative destination column.</param>
	/// <param name="geometry">Fresh exact cell-pixel geometry, observed again after terminal or font resize.</param>
	/// <param name="cancellationToken">Cancellation before output begins.</param>
	/// <remarks>
	/// Verify Terminal's ordinary RasterGraphics capability before use. Terminal chooses verified Kitty or Sixel.
	/// Each call clears and repaints the screen, omitting text covered by the image. The final
	/// screen row must remain outside the image. Visible panels may not overlap the image, and retained
	/// raster cells may not coexist with it. The image must not split a wide text footprint at either edge.
	/// No source pixels or image identity are retained or replayed.
	/// The next ordinary refresh clears the image and repaints text. Physical erasure and placement depend
	/// on the terminal's graphics behavior. Output failure invalidates physical knowledge; no retry occurs.
	/// </remarks>
	/// <exception cref="ArgumentException">Image dimensions do not match a non-default geometry.</exception>
	/// <exception cref="ArgumentOutOfRangeException">A destination coordinate is negative or overflows.</exception>
	/// <exception cref="InvalidOperationException">The current screen cannot contain the frame, a panel overlaps, retained raster cells exist, or an image edge splits wide text.</exception>
	/// <exception cref="NotSupportedException">Verified raster output or a screen-clear operation is unavailable.</exception>
	public ValueTask RefreshRasterAsync(
		TerminalRasterImage image,
		int row,
		int column,
		CursesRasterAtlasGeometry geometry,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( image );
		if ( geometry.Rows < 1 || geometry.Columns < 1
			|| image.Width != geometry.PixelWidth || image.Height != geometry.PixelHeight ) {
			throw new ArgumentException( "The complete image must match non-default exact cell geometry.", nameof( geometry ) );
		}
		CursesRectangle bounds = new( row, column, geometry.Rows, geometry.Columns );
		return this.RefreshWithRasterAsync( new CursesRasterRefreshFrame( image, bounds ), cancellationToken );
	}

	private static void ValidateRasterRefresh( CursesScreen screen, CursesRasterRefreshFrame frame ) {
		if ( frame.Bounds.BottomExclusive >= screen.Rows || frame.Bounds.RightExclusive > screen.Columns ) {
			throw new InvalidOperationException( "The raster frame must fit the current screen and leave its final row unused. Requery geometry after resize." );
		}
		foreach ( CursesPanel panel in screen.SnapshotPanelsBottomToTop() ) {
			if ( panel.IsVisible && !panel.Bounds.Intersect( frame.Bounds ).IsEmpty ) {
				throw new InvalidOperationException( "Visible panels cannot overlap an immediate raster frame. Present a text view while the panel is open." );
			}
		}
		for ( int row = 0; row < screen.Rows; row++ ) {
			if ( row >= frame.Bounds.Row && row < frame.Bounds.BottomExclusive
				&& ( screen.VirtualScreen.GetCell( row, frame.Bounds.Column ).IsContinuation
					|| ( frame.Bounds.RightExclusive < screen.Columns
						&& screen.VirtualScreen.GetCell( row, frame.Bounds.RightExclusive ).IsContinuation ) ) ) {
				throw new InvalidOperationException( "A raster frame edge cannot split a wide text footprint." );
			}
			for ( int column = 0; column < screen.Columns; column++ ) {
				if ( screen.VirtualScreen.GetRasterCell( row, column ).HasValue ) {
					throw new InvalidOperationException( "Immediate frames cannot coexist with retained raster cells." );
				}
			}
		}
	}
}
