/*
	Icod.DCurses.RasterAtlas.Sample
	Original top-down raster-atlas acceptance sample.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
