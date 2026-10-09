/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

namespace Icod.DCurses.RasterAtlas.Sample;

/// <summary>Classifies terminal setup failures that may safely select text rendering.</summary>
internal static class RasterAtlasSampleFallback {
	internal static void WriteSetupFailure(
		TextWriter writer,
		string stage,
		Exception exception
	) {
		ArgumentNullException.ThrowIfNull( writer );
		ArgumentException.ThrowIfNullOrWhiteSpace( stage );
		ArgumentNullException.ThrowIfNull( exception );
		writer.WriteLine( $"Raster atlas setup failed while {stage}:" );
		writer.WriteLine( exception );
	}

	internal static bool IsRecoverablePresentationException(
		Exception exception
	) {
		ArgumentNullException.ThrowIfNull( exception );
		return exception is TimeoutException
			or InvalidOperationException
			or IOException
			or OperationCanceledException;
	}

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
