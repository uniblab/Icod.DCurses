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
	private readonly CursesSession owner;
	private readonly TerminalRasterAnimation animation;
	private readonly object disposeSync = new();

	private CursesRasterResource? resource;
	private CursesRasterPlaceholder? placeholder;
	private TerminalRasterAnimationFrame frontFrame;
	private TerminalRasterAnimationFrame backFrame;
	private Task? disposeTask;
	private bool requiresRecreation;

	internal CursesRasterAtlas(
		CursesSession owner,
		CursesRasterAtlasGeometry geometry,
		CursesRasterResource resource,
		CursesRasterPlaceholder placeholder,
		TerminalRasterAnimationFrame frontFrame,
		TerminalRasterAnimationFrame backFrame
	) {
		ArgumentNullException.ThrowIfNull( owner );
		ArgumentNullException.ThrowIfNull( resource );
		ArgumentNullException.ThrowIfNull( placeholder );
		ArgumentNullException.ThrowIfNull( frontFrame );
		ArgumentNullException.ThrowIfNull( backFrame );
		this.owner = owner;
		rows = geometry.Rows;
		columns = geometry.Columns;
		tilePixelWidth = geometry.TilePixelWidth;
		tilePixelHeight = geometry.TilePixelHeight;
		this.resource = resource;
		this.placeholder = placeholder;
		animation = resource.Animation;
		this.frontFrame = frontFrame;
		this.backFrame = backFrame;
		requiresRecreation = false;
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
	public CursesRasterOwnershipState OwnershipState {
		get {
			CursesRasterResource? currentResource = Volatile.Read( ref resource );
			CursesRasterPlaceholder? currentPlaceholder = Volatile.Read( ref placeholder );
			if ( currentResource is null || currentPlaceholder is null ) {
				return DisposedState;
			}

			CursesRasterOwnershipState resourceState = currentResource.OwnershipState;
			CursesRasterOwnershipState placeholderState = currentPlaceholder.OwnershipState;
			return (int)resourceState.Status >= (int)placeholderState.Status
				? resourceState
				: placeholderState;
		}
	}

	/// <summary>Gets whether ambiguous output or lifecycle loss requires caller-driven recreation.</summary>
	public bool RequiresRecreation {
		get {
			if ( Volatile.Read( ref requiresRecreation ) ) {
				return true;
			}

			CursesRasterOwnershipStatus status = OwnershipState.Status;
			return status is CursesRasterOwnershipStatus.Stale
				or CursesRasterOwnershipStatus.Released;
		}
	}

	/// <summary>Gets one opaque retained placeholder cell.</summary>
	/// <param name="row">Zero-based atlas row.</param>
	/// <param name="column">Zero-based atlas column.</param>
	/// <returns>A session-bound cell suitable for retained projection.</returns>
	/// <exception cref="ArgumentOutOfRangeException">A coordinate is outside the atlas.</exception>
	/// <exception cref="InvalidOperationException">The atlas requires explicit recreation.</exception>
	/// <exception cref="ObjectDisposedException">The atlas has been disposed.</exception>
	public CursesRasterCell GetCell(
		int row,
		int column
	) {
		if ( RequiresRecreation ) {
			throw new InvalidOperationException(
				"The raster atlas must be recreated before its retained cells can be used."
			);
		}
		return Volatile.Read( ref placeholder )?.GetCell( row, column )
			?? throw new ObjectDisposedException( nameof( CursesRasterAtlas ) );
	}

	/// <summary>Presents a bounded set of replacement tiles.</summary>
	/// <remarks>
	/// Validation and deterministic row-major ordering complete before output. A controlled
	/// failure preserves the known front frame and is returned in the result. An exception
	/// after possible output marks this atlas as requiring explicit recreation.
	/// </remarks>
	/// <param name="updates">Up to 4,096 unique exact-sized RGB24 or RGBA32 tiles.</param>
	/// <param name="cancellationToken">Cancellation for validation, activity acquisition and output.</param>
	/// <returns>The acknowledged presentation extent.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="updates"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The list is too large or a coordinate is outside the atlas.</exception>
	/// <exception cref="ArgumentException">A coordinate is duplicated or a tile has the wrong dimensions.</exception>
	/// <exception cref="NotSupportedException">A tile is not RGB24 or RGBA32.</exception>
	/// <exception cref="InvalidOperationException">The atlas is stale or requires recreation.</exception>
	/// <exception cref="OperationCanceledException">Cancellation is requested.</exception>
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
		if ( RequiresRecreation ) {
			throw new InvalidOperationException(
				"The raster atlas must be recreated before another presentation."
			);
		}
		if ( CursesRasterOwnershipStatus.Current != OwnershipState.Status ) {
			throw new InvalidOperationException(
				"The raster atlas is not current and cannot be presented."
			);
		}

		return owner.PresentRasterAtlasAsync( this, validated, cancellationToken );
	}

	/// <summary>Releases the atlas placeholder before its raster resource.</summary>
	/// <remarks>Repeated and concurrent calls observe the same disposal task.</remarks>
	/// <returns>A task representing serialized Terminal cleanup.</returns>
	public ValueTask DisposeAsync() {
		lock ( disposeSync ) {
			disposeTask ??= owner.DisposeRasterAtlasAsync( this );
			return new ValueTask( disposeTask );
		}
	}

	internal async Task DisposeCoreAsync() {
		CursesRasterPlaceholder? current = Interlocked.Exchange( ref placeholder, null );
		CursesRasterResource? currentResource = Interlocked.Exchange( ref resource, null );
		List<Exception>? failures = null;
		if ( current is not null ) {
			try {
				await current.DisposeAsync().ConfigureAwait( false );
			} catch ( Exception exception ) {
				( failures ??= [] ).Add( exception );
			}
		}
		if ( currentResource is not null ) {
			try {
				await currentResource.DisposeAsync().ConfigureAwait( false );
			} catch ( Exception exception ) {
				( failures ??= [] ).Add( exception );
			}
		}
		owner.InvalidatePhysicalScreen();
		if ( failures is not null ) {
			throw new AggregateException( "Raster-atlas disposal reported one or more failures.", failures );
		}
	}

	internal void AbandonForSessionDisposal() {
		_ = Interlocked.Exchange( ref placeholder, null );
		_ = Interlocked.Exchange( ref resource, null );
		owner.InvalidatePhysicalScreen();
	}

	internal bool BelongsTo( CursesSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		return ReferenceEquals( owner, session );
	}

	internal TerminalRasterAnimation Animation => animation;

	internal TerminalRasterAnimationFrame FrontFrame => frontFrame;

	internal TerminalRasterAnimationFrame BackFrame => backFrame;

	internal void SwapFrames() {
		( frontFrame, backFrame ) = ( backFrame, frontFrame );
	}

	internal async ValueTask<CursesRasterAtlasPresentationResult> PresentCoreAsync(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
		CancellationToken cancellationToken
	) {
		if ( RequiresRecreation
			|| CursesRasterOwnershipStatus.Current != OwnershipState.Status ) {
			throw new InvalidOperationException(
				"The raster atlas lost current ownership before presentation began."
			);
		}

		TerminalRasterImage? fullCoverage = TryCreateFullCoverageImage( updates );
		int completed = 0;
		try {
			TerminalControlMutationResult composition = await animation.ComposeFrameAsync(
				frontFrame,
				backFrame,
				new TerminalRasterSourceRectangle( 0, 0, PixelWidth, PixelHeight ),
				0,
				0,
				TerminalRasterFrameCompositionMode.Replace,
				cancellationToken
			).ConfigureAwait( false );
			if ( !composition.Succeeded ) {
				return FromControlledFailure( composition, updates.Count, completed );
			}

			if ( fullCoverage is not null ) {
				TerminalControlMutationResult replacement =
					await animation.UpdateFrameRegionAsync(
						backFrame,
						fullCoverage,
						0,
						0,
						cancellationToken
					).ConfigureAwait( false );
				if ( !replacement.Succeeded ) {
					return FromControlledFailure( replacement, updates.Count, completed );
				}
				completed = updates.Count;
			} else {
				foreach ( CursesRasterAtlasTileUpdate update in updates ) {
					TerminalControlMutationResult replacement =
						await animation.UpdateFrameRegionAsync(
							backFrame,
							update.Image,
							checked( update.Column * tilePixelWidth ),
							checked( update.Row * tilePixelHeight ),
							cancellationToken
						).ConfigureAwait( false );
					if ( !replacement.Succeeded ) {
						return FromControlledFailure( replacement, updates.Count, completed );
					}
					completed++;
				}
			}

			TerminalControlMutationResult selection = await animation.SelectFrameAsync(
				backFrame,
				cancellationToken
			).ConfigureAwait( false );
			if ( !selection.Succeeded ) {
				return FromControlledFailure( selection, updates.Count, completed );
			}

			SwapFrames();
			return new CursesRasterAtlasPresentationResult(
				CursesRasterAtlasPresentationStatus.Presented,
				updates.Count,
				completed,
				frameSelected: true,
				message: null
			);
		} catch {
			Volatile.Write( ref requiresRecreation, true );
			owner.InvalidatePhysicalScreen();
			throw;
		}
	}

	private TerminalRasterImage? TryCreateFullCoverageImage(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates
	) {
		if ( updates.Count != checked( rows * columns ) ) {
			return null;
		}

		TerminalRasterPixelFormat format = updates[ 0 ].Image.PixelFormat;
		if ( updates.Any( update => update.Image.PixelFormat != format ) ) {
			return null;
		}

		int bytesPerPixel = TerminalRasterPixelFormat.Rgb24 == format ? 3 : 4;
		byte[] pixels = new byte[ checked( PixelWidth * PixelHeight * bytesPerPixel ) ];
		foreach ( CursesRasterAtlasTileUpdate update in updates ) {
			for ( int pixelRow = 0; pixelRow < tilePixelHeight; pixelRow++ ) {
				int destination = checked(
					( ( update.Row * tilePixelHeight + pixelRow ) * PixelWidth
						+ update.Column * tilePixelWidth ) * bytesPerPixel
				);
				for ( int pixelColumn = 0; pixelColumn < tilePixelWidth; pixelColumn++ ) {
					TerminalRasterColor color = update.Image.GetPixelColor(
						pixelColumn,
						pixelRow
					);
					pixels[ destination++ ] = color.Red;
					pixels[ destination++ ] = color.Green;
					pixels[ destination++ ] = color.Blue;
					if ( TerminalRasterPixelFormat.Rgba32 == format ) {
						pixels[ destination++ ] = color.Alpha;
					}
				}
			}
		}

		return TerminalRasterPixelFormat.Rgb24 == format
			? TerminalRasterImage.CreateRgb24( PixelWidth, PixelHeight, pixels )
			: TerminalRasterImage.CreateRgba32( PixelWidth, PixelHeight, pixels );
	}

	private CursesRasterAtlasTileUpdate[] ValidateAndOrderUpdates(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates
	) {
		return ValidateAndOrderUpdates(
			updates,
			rows,
			columns,
			tilePixelWidth,
			tilePixelHeight
		);
	}

	internal static CursesRasterAtlasTileUpdate[] ValidateAndOrderUpdates(
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
		int rows,
		int columns,
		int tilePixelWidth,
		int tilePixelHeight
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

	private static CursesRasterAtlasPresentationResult FromControlledFailure(
		TerminalControlMutationResult result,
		int requested,
		int completed
	) {
		CursesRasterAtlasPresentationStatus status = result.Status switch {
			TerminalControlStatus.Unsupported => CursesRasterAtlasPresentationStatus.Unsupported,
			TerminalControlStatus.Unavailable => CursesRasterAtlasPresentationStatus.Unavailable,
			TerminalControlStatus.Failed => CursesRasterAtlasPresentationStatus.Failed,
			_ => throw new InvalidOperationException(
				"A successful Terminal mutation cannot be mapped as an atlas failure."
			)
		};
		return new CursesRasterAtlasPresentationResult(
			status,
			requested,
			completed,
			frameSelected: false,
			result.Message
		);
	}

	private static CursesRasterOwnershipState DisposedState => new(
		CursesRasterOwnershipStatus.Disposed,
		CursesRasterOwnershipLossReason.ExplicitDisposal
	);
}
