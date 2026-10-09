using System.Reflection;
using System.Runtime.CompilerServices;
using Icod.DCurses;
using Icod.Terminal;

internal static class RasterSmoke {
	internal static void Verify() {
		TerminalRasterImage image = TerminalRasterImage.CreateRgb24(
			1,
			1,
			[ 0x20, 0x40, 0x80 ]
		);
		if ( 1 != image.Width
			|| 1 != image.Height
			|| 1 != image.PixelCount ) {
			throw new InvalidOperationException(
				"Package-only TerminalRasterImage construction changed."
			);
		}

		CursesRasterOwnershipState ownership = new(
			CursesRasterOwnershipStatus.Current,
			CursesRasterOwnershipLossReason.None
		);
		if ( CursesRasterOwnershipStatus.Current != ownership.Status
			|| CursesRasterOwnershipLossReason.None != ownership.LossReason ) {
			throw new InvalidOperationException(
				"Package-only raster ownership state changed."
			);
		}

		CursesRasterAtlasGeometry geometry = new( 2, 3, 4, 5 );
		CursesRasterAtlasTileUpdate update = new( 1, 2, image );
		if ( 12 != geometry.PixelWidth
			|| 10 != geometry.PixelHeight
			|| 1 != update.Row
			|| 2 != update.Column
			|| !ReferenceEquals( image, update.Image )
			|| 4096 != CursesRasterAtlas.MaximumUpdatesPerPresentation ) {
			throw new InvalidOperationException(
				"Package-only raster-atlas value surface changed."
			);
		}

		Type resource = typeof( CursesRasterResource );
		Type placeholder = typeof( CursesRasterPlaceholder );
		Type cell = typeof( CursesRasterCell );
		if ( null == typeof( CursesSession ).GetMethod(
			nameof( CursesSession.CreateRasterResourceAsync ),
			[ typeof( TerminalRasterImage ), typeof( CancellationToken ) ]
		) || null == resource.GetMethod(
			nameof( CursesRasterResource.CreatePlaceholderAsync ),
			[ typeof( int ), typeof( int ), typeof( CancellationToken ) ]
		) || null == placeholder.GetMethod(
			nameof( CursesRasterPlaceholder.GetCell ),
			[ typeof( int ), typeof( int ) ]
		) || null == typeof( CursesWindow ).GetMethod(
			nameof( CursesWindow.WriteRasterCell ),
			[ cell ]
		) || null == typeof( CursesVirtualScreen ).GetMethod(
			nameof( CursesVirtualScreen.SetRasterCell ),
			[ typeof( int ), typeof( int ), typeof( CursesRasterCell? ) ]
		) || null == typeof( CursesSession ).GetMethod(
			nameof( CursesSession.QueryRasterAtlasGeometryAsync ),
			[ typeof( int ), typeof( int ), typeof( TimeSpan ), typeof( CancellationToken ) ]
		) || null == typeof( CursesSession ).GetMethod(
			nameof( CursesSession.CreateRasterAtlasAsync ),
			[ typeof( TerminalRasterImage ), typeof( int ), typeof( int ), typeof( CancellationToken ) ]
		) || null == typeof( CursesWindow ).GetMethod(
			nameof( CursesWindow.WriteRasterAtlas ),
			[ typeof( int ), typeof( int ), typeof( CursesRasterAtlas ), typeof( CursesRectangle ) ]
		) || null == typeof( CursesSession ).GetMethod(
			nameof( CursesSession.RefreshRasterAsync ),
			[ typeof( TerminalRasterImage ), typeof( int ), typeof( int ), typeof( CursesRasterAtlasGeometry ), typeof( CancellationToken ) ]
		) ) {
			throw new InvalidOperationException(
				"Package-only retained-raster public surface changed."
			);
		}
	}
}

internal static class RasterSmokeModuleInitializer {
	[ModuleInitializer]
	internal static void Initialize() {
		RasterSmoke.Verify();
	}
}
