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
