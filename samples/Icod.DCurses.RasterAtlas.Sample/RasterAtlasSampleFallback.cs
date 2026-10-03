/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

namespace Icod.DCurses.RasterAtlas.Sample;

/// <summary>Classifies terminal setup failures that may safely select text rendering.</summary>
internal static class RasterAtlasSampleFallback {
	internal static bool IsRecoverableSetupException(
		Exception exception
	) {
		ArgumentNullException.ThrowIfNull( exception );
		return exception is TimeoutException
			or InvalidOperationException
			or IOException
			or FormatException;
	}
}
