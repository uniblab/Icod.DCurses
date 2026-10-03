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

/// <summary>Terminal-backed raster-atlas creation.</summary>
public sealed partial class CursesSession {
	/// <summary>Creates one retained raster atlas from an exactly divisible initial image.</summary>
	public ValueTask<TerminalControlResult<CursesRasterAtlas>> CreateRasterAtlasAsync(
		TerminalRasterImage initialImage,
		int rows,
		int columns,
		CancellationToken cancellationToken = default
	) {
		ArgumentNullException.ThrowIfNull( initialImage );
		CursesRasterAtlasGeometry.ValidateCellAxis( rows, nameof( rows ) );
		CursesRasterAtlasGeometry.ValidateCellAxis( columns, nameof( columns ) );
		if ( 0 != initialImage.Width % columns ) {
			throw new ArgumentException(
				"The initial image width must divide exactly across atlas columns.",
				nameof( initialImage )
			);
		}
		if ( 0 != initialImage.Height % rows ) {
			throw new ArgumentException(
				"The initial image height must divide exactly across atlas rows.",
				nameof( initialImage )
			);
		}
		CursesRasterAtlasGeometry geometry = new(
			rows,
			columns,
			initialImage.Width / columns,
			initialImage.Height / rows
		);
		cancellationToken.ThrowIfCancellationRequested();
		return CreateRasterAtlasCoreAsync( initialImage, geometry, cancellationToken );
	}

	internal async ValueTask<CursesRasterAtlasPresentationResult> PresentRasterAtlasAsync(
		CursesRasterAtlas atlas,
		IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
		CancellationToken cancellationToken
	) {
		ArgumentNullException.ThrowIfNull( atlas );
		ArgumentNullException.ThrowIfNull( updates );
		if ( !atlas.BelongsTo( this ) ) {
			throw new InvalidOperationException(
				"The raster atlas belongs to a different CursesSession."
			);
		}

		using IDisposable activity = await AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );
		return await atlas.PresentCoreAsync(
			updates,
			cancellationToken
		).ConfigureAwait( false );
	}

	internal async Task DisposeRasterAtlasAsync(
		CursesRasterAtlas atlas
	) {
		ArgumentNullException.ThrowIfNull( atlas );
		if ( !atlas.BelongsTo( this ) ) {
			throw new InvalidOperationException(
				"The raster atlas belongs to a different CursesSession."
			);
		}

		IDisposable? activity = null;
		try {
			activity = await AcquireTerminalActivityAsync(
				CancellationToken.None
			).ConfigureAwait( false );
		} catch ( ObjectDisposedException ) {
			atlas.AbandonForSessionDisposal();
			return;
		}

		using ( activity ) {
			await atlas.DisposeCoreAsync().ConfigureAwait( false );
		}
	}

	private async ValueTask<TerminalControlResult<CursesRasterAtlas>> CreateRasterAtlasCoreAsync(
		TerminalRasterImage initialImage,
		CursesRasterAtlasGeometry geometry,
		CancellationToken cancellationToken
	) {
		using IDisposable activity = await AcquireTerminalActivityAsync(
			cancellationToken
		).ConfigureAwait( false );

		TerminalRasterPlanningSnapshot plan = terminalSession.GetRasterPlanningSnapshot();
		if ( plan.MaximumResources <= plan.OwnedResourceCount
			|| plan.MaximumPlacements <= plan.OwnedPlacementCount
			|| plan.MaximumAnimationFrames - 2 < plan.AllocatedAnimationFrameCount ) {
			return TerminalControlResult<CursesRasterAtlas>.Unavailable(
				"The advisory local raster snapshot has no room for one atlas resource, placeholder, and two frames."
			);
		}

		TerminalControlResult<TerminalRasterResource> resourceResult =
			await terminalSession.CreateRasterResourceAsync(
				initialImage,
				cancellationToken
			).ConfigureAwait( false );
		if ( !resourceResult.IsAvailable ) {
			return MapControlledFailure<TerminalRasterResource>( resourceResult );
		}

		TerminalRasterResource resource = resourceResult.GetRequiredValue();
		CursesRasterResource resourceFacade = new( this, resource );
		CursesRasterPlaceholder? placeholder = null;
		bool rollbackAttempted = false;
		try {
			TerminalControlResult<CursesRasterPlaceholder> placeholderResult =
				await resourceFacade.CreatePlaceholderAsync(
					geometry.Columns,
					geometry.Rows,
					cancellationToken
				).ConfigureAwait( false );
			if ( !placeholderResult.IsAvailable ) {
				rollbackAttempted = true;
				await DisposeAfterControlledFailureAsync(
					resourceFacade,
					placeholderResult.Message
				).ConfigureAwait( false );
				return MapControlledFailure<CursesRasterPlaceholder>( placeholderResult );
			}
			placeholder = placeholderResult.GetRequiredValue();

			TerminalControlResult<TerminalRasterAnimationFrame> frameResult =
				await resourceFacade.Animation.AddFrameAsync(
					initialImage,
					TimeSpan.FromMilliseconds( 1 ),
					cancellationToken
				).ConfigureAwait( false );
			if ( !frameResult.IsAvailable ) {
				rollbackAttempted = true;
				await DisposeAfterControlledFailureAsync(
					resourceFacade,
					frameResult.Message,
					placeholder
				).ConfigureAwait( false );
				return MapControlledFailure<TerminalRasterAnimationFrame>( frameResult );
			}

			CursesRasterPlaceholder publishedPlaceholder = placeholder;
			placeholder = null;
			return TerminalControlResult<CursesRasterAtlas>.Available(
				new CursesRasterAtlas(
					this,
					geometry,
					resourceFacade,
					publishedPlaceholder,
					resourceFacade.Animation.RootFrame,
					frameResult.GetRequiredValue()
				)
			);
		} catch ( Exception exception ) {
			if ( rollbackAttempted ) {
				throw;
			}
			List<Exception> failures = [ exception ];
			if ( placeholder is not null ) {
				try {
					await placeholder.DisposeAsync().ConfigureAwait( false );
				} catch ( Exception cleanup ) {
					failures.Add( cleanup );
				}
			}
			try {
				await resourceFacade.DisposeAsync().ConfigureAwait( false );
			} catch ( Exception cleanup ) {
				failures.Add( cleanup );
			}
			if ( 1 < failures.Count ) {
				throw new AggregateException(
					"Raster-atlas creation failed and rollback also reported an error.",
					failures
				);
			}
			throw;
		}
	}

	private static TerminalControlResult<CursesRasterAtlas> MapControlledFailure<T>(
		TerminalControlResult<T> result
	) where T : class {
		return result.Status switch {
			TerminalControlStatus.Unavailable => TerminalControlResult<CursesRasterAtlas>.Unavailable(
				result.Message,
				result.NativeErrorCode
			),
			TerminalControlStatus.Unsupported => TerminalControlResult<CursesRasterAtlas>.Unsupported(
				result.Message
			),
			TerminalControlStatus.Failed => TerminalControlResult<CursesRasterAtlas>.Failed(
				result.Message,
				result.NativeErrorCode
			),
			_ => throw new InvalidOperationException( "An available result cannot be mapped as a failure." )
		};
	}

	private static async ValueTask DisposeAfterControlledFailureAsync(
		CursesRasterResource resource,
		string? message,
		CursesRasterPlaceholder? placeholder = null
	) {
		List<Exception>? failures = null;
		if ( placeholder is not null ) {
			try {
				await placeholder.DisposeAsync().ConfigureAwait( false );
			} catch ( Exception exception ) {
				( failures ??= [] ).Add( exception );
			}
		}
		try {
			await resource.DisposeAsync().ConfigureAwait( false );
		} catch ( Exception exception ) {
			( failures ??= [] ).Add( exception );
		}
		if ( failures is not null ) {
			failures.Insert(
				0,
				new InvalidOperationException(
					message ?? "A controlled raster-atlas creation step failed."
				)
			);
			throw new AggregateException(
				"Raster-atlas rollback reported one or more cleanup failures.",
				failures
			);
		}
	}
}
