/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

using Icod.Terminal;

/// <summary>Owns one retained, terminal-cell-aligned raster tile atlas.</summary>
public sealed class CursesRasterAtlas : IAsyncDisposable {
	/// <summary>Gets the maximum number of changed tiles accepted by one presentation.</summary>
	public const int MaximumUpdatesPerPresentation = 4096;

	private readonly int rows;
	private readonly int columns;
	private readonly int tilePixelWidth;
	private readonly int tilePixelHeight;

	private CursesRasterPlaceholder? placeholder;
	private bool requiresRecreation;

	internal CursesRasterAtlas(
		CursesRasterAtlasGeometry geometry,
		CursesRasterPlaceholder placeholder
	) {
		ArgumentNullException.ThrowIfNull( placeholder );
		rows = geometry.Rows;
		columns = geometry.Columns;
		tilePixelWidth = geometry.TilePixelWidth;
		tilePixelHeight = geometry.TilePixelHeight;
		this.placeholder = placeholder;
	}

	/// <summary>Gets the atlas height in terminal cells.</summary>
	public int Rows => rows;

	/// <summary>Gets the atlas width in terminal cells.</summary>
	public int Columns => columns;

	/// <summary>Gets one tile's width in pixels.</summary>
	public int TilePixelWidth => tilePixelWidth;

	/// <summary>Gets one tile's height in pixels.</summary>
	public int TilePixelHeight => tilePixelHeight;

	/// <summary>Gets the complete atlas width in pixels.</summary>
	public int PixelWidth => checked( columns * tilePixelWidth );

	/// <summary>Gets the complete atlas height in pixels.</summary>
	public int PixelHeight => checked( rows * tilePixelHeight );

	/// <summary>Gets the most conservative current ownership certainty.</summary>
	public CursesRasterOwnershipState OwnershipState => Volatile.Read( ref placeholder )?.OwnershipState
		?? new CursesRasterOwnershipState(
			CursesRasterOwnershipStatus.Disposed,
			CursesRasterOwnershipLossReason.ExplicitDisposal
		);

	/// <summary>Gets whether ambiguous output or lifecycle loss requires caller-driven recreation.</summary>
	public bool RequiresRecreation => Volatile.Read( ref requiresRecreation );

	/// <summary>Gets one opaque retained placeholder cell.</summary>
	public CursesRasterCell GetCell(
		int row,
		int column
	) {
		return Volatile.Read( ref placeholder )?.GetCell( row, column )
			?? throw new ObjectDisposedException( nameof( CursesRasterAtlas ) );
	}

	/// <summary>Presents a bounded set of replacement tiles.</summary>
	public ValueTask<CursesRasterAtlasPresentationResult> PresentAsync(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
		CancellationToken cancellationToken = default
	) {
		CursesRasterAtlasTileUpdate[] validated = ValidateAndOrderUpdates( updates );
		cancellationToken.ThrowIfCancellationRequested();
		if ( 0 == validated.Length ) {
			return ValueTask.FromResult(
				new CursesRasterAtlasPresentationResult(
					CursesRasterAtlasPresentationStatus.NoChanges,
					0,
					0,
					frameSelected: false,
					message: null
				)
			);
		}

		return ValueTask.FromResult(
			new CursesRasterAtlasPresentationResult(
				CursesRasterAtlasPresentationStatus.Failed,
				validated.Length,
				0,
				frameSelected: false,
				"Raster-atlas presentation has not been initialized."
			)
		);
	}

	/// <summary>Releases the atlas placeholder.</summary>
	public ValueTask DisposeAsync() {
		CursesRasterPlaceholder? current = Interlocked.Exchange( ref placeholder, null );
		return current?.DisposeAsync() ?? ValueTask.CompletedTask;
	}

	internal bool BelongsTo( CursesSession session ) {
		return Volatile.Read( ref placeholder )?.BelongsTo( session ) ?? false;
	}

	private CursesRasterAtlasTileUpdate[] ValidateAndOrderUpdates(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates
	) {
		ArgumentNullException.ThrowIfNull( updates );
		if ( MaximumUpdatesPerPresentation < updates.Count ) {
			throw new ArgumentOutOfRangeException( nameof( updates ) );
		}

		CursesRasterAtlasTileUpdate[] copy = new CursesRasterAtlasTileUpdate[ updates.Count ];
		HashSet<long> coordinates = new( updates.Count );
		for ( int index = 0; index < updates.Count; index++ ) {
			CursesRasterAtlasTileUpdate update = updates[ index ];
			if ( update.Row < 0 || rows <= update.Row ) {
				throw new ArgumentOutOfRangeException( nameof( updates ) );
			}
			if ( update.Column < 0 || columns <= update.Column ) {
				throw new ArgumentOutOfRangeException( nameof( updates ) );
			}
			TerminalRasterImage image = update.Image
				?? throw new ArgumentException( "An atlas update image cannot be null.", nameof( updates ) );
			if ( image.PixelFormat is not TerminalRasterPixelFormat.Rgb24
				and not TerminalRasterPixelFormat.Rgba32 ) {
				throw new NotSupportedException(
					"Raster-atlas tile updates require RGB24 or RGBA32 pixels."
				);
			}
			if ( image.Width != tilePixelWidth || image.Height != tilePixelHeight ) {
				throw new ArgumentException(
					"Every raster-atlas update must exactly match one tile.",
					nameof( updates )
				);
			}

			long coordinate = ( (long)update.Row << 32 ) | (uint)update.Column;
			if ( !coordinates.Add( coordinate ) ) {
				throw new ArgumentException(
					"A presentation cannot update the same atlas coordinate more than once.",
					nameof( updates )
				);
			}
			copy[ index ] = update;
		}

		Array.Sort(
			copy,
			static ( left, right ) => {
				int row = left.Row.CompareTo( right.Row );
				return 0 != row ? row : left.Column.CompareTo( right.Column );
			}
		);
		return copy;
	}
}
