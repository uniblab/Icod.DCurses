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

public static partial class CursesLayout {
	/// <summary>Arranges tracks from the top of the supplied bounds.</summary>
	public static CursesRectangle[] ArrangeRows(
		CursesRectangle bounds,
		ReadOnlySpan<CursesTrack> tracks,
		int gap = 0,
		CursesTrackDistribution distribution = CursesTrackDistribution.Start
	) => ArrangeTracks( bounds, tracks, gap, distribution, rows: true );

	/// <summary>Arranges tracks from the left of the supplied bounds.</summary>
	public static CursesRectangle[] ArrangeColumns(
		CursesRectangle bounds,
		ReadOnlySpan<CursesTrack> tracks,
		int gap = 0,
		CursesTrackDistribution distribution = CursesTrackDistribution.Start
	) => ArrangeTracks( bounds, tracks, gap, distribution, rows: false );

	private static CursesRectangle[] ArrangeTracks(
		CursesRectangle bounds,
		ReadOnlySpan<CursesTrack> tracks,
		int gap,
		CursesTrackDistribution distribution,
		bool rows
	) {
		if ( gap < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( gap ) );
		}
		if ( !Enum.IsDefined( distribution ) ) {
			throw new ArgumentOutOfRangeException( nameof( distribution ) );
		}
		if ( tracks.Length > 4096 ) {
			throw new ArgumentOutOfRangeException( nameof( tracks ) );
		}
		if ( tracks.IsEmpty ) {
			return Array.Empty<CursesRectangle>();
		}

		int extent = rows ? bounds.Rows : bounds.Columns;
		long available = (long)extent - (long)( tracks.Length - 1 ) * gap;
		if ( available < 0 ) {
			throw new ArgumentOutOfRangeException( nameof( gap ) );
		}
		long minimumTotal = 0;
		for ( int index = 0; index < tracks.Length; index++ ) {
			CursesTrack track = tracks[ index ];
			if ( ( track.Kind != CursesTrackKind.Fixed && track.Kind != CursesTrackKind.Weighted )
				|| track.Value < ( track.Kind == CursesTrackKind.Weighted ? 1 : 0 )
				|| track.Minimum < 0
				|| ( track.Maximum.HasValue && track.Maximum.Value < track.Minimum ) ) {
				throw new ArgumentOutOfRangeException( nameof( tracks ) );
			}
			minimumTotal += track.Minimum;
		}
		if ( minimumTotal > available ) {
			throw new ArgumentException( "Track minimums exceed the available extent.", nameof( tracks ) );
		}

		int[] sizes = new int[ tracks.Length ];
		long remaining = available - minimumTotal;
		for ( int index = 0; index < tracks.Length; index++ ) {
			CursesTrack track = tracks[ index ];
			sizes[ index ] = track.Minimum;
			if ( track.Kind == CursesTrackKind.Fixed ) {
				int ideal = Math.Clamp( track.Value, track.Minimum, track.Maximum ?? int.MaxValue );
				int extra = (int)Math.Min( remaining, (long)ideal - track.Minimum );
				sizes[ index ] += extra;
				remaining -= extra;
			}
		}
		DistributeWeighted( tracks, sizes, ref remaining );
		int[] spaces = DistributeSurplus( tracks.Length, remaining, distribution );

		CursesRectangle[] result = new CursesRectangle[ tracks.Length ];
		int cursor = ( rows ? bounds.Row : bounds.Column ) + spaces[ 0 ];
		for ( int index = 0; index < tracks.Length; index++ ) {
			if ( index > 0 ) {
				cursor += gap + spaces[ index ];
			}
			result[ index ] = rows
				? new CursesRectangle( cursor, bounds.Column, sizes[ index ], bounds.Columns )
				: new CursesRectangle( bounds.Row, cursor, bounds.Rows, sizes[ index ] );
			cursor += sizes[ index ];
		}
		return result;
	}

	private static void DistributeWeighted( ReadOnlySpan<CursesTrack> tracks, int[] sizes, ref long remaining ) {
		while ( remaining > 0 ) {
			long totalWeight = 0;
			for ( int index = 0; index < tracks.Length; index++ ) {
				CursesTrack track = tracks[ index ];
				if ( track.Kind == CursesTrackKind.Weighted && sizes[ index ] < ( track.Maximum ?? int.MaxValue ) ) {
					totalWeight += track.Value;
				}
			}
			if ( totalWeight == 0 ) {
				return;
			}

			long awarded = 0;
			bool saturated = false;
			for ( int index = 0; index < tracks.Length; index++ ) {
				CursesTrack track = tracks[ index ];
				int headroom = ( track.Maximum ?? int.MaxValue ) - sizes[ index ];
				if ( track.Kind != CursesTrackKind.Weighted || headroom <= 0 ) {
					continue;
				}
				long quotient = remaining * track.Value / totalWeight;
				int share = (int)Math.Min( quotient, headroom );
				sizes[ index ] += share;
				awarded += share;
				saturated |= share == headroom;
			}
			remaining -= awarded;
			if ( saturated ) {
				continue;
			}
			for ( int index = 0; index < tracks.Length && remaining > 0; index++ ) {
				CursesTrack track = tracks[ index ];
				if ( track.Kind == CursesTrackKind.Weighted && sizes[ index ] < ( track.Maximum ?? int.MaxValue ) ) {
					sizes[ index ]++;
					remaining--;
				}
			}
			return;
		}
	}

	private static int[] DistributeSurplus( int count, long surplus, CursesTrackDistribution distribution ) {
		int[] spaces = new int[ count + 1 ];
		switch ( distribution ) {
			case CursesTrackDistribution.Start:
				spaces[ count ] = (int)surplus;
				break;
			case CursesTrackDistribution.Center:
				spaces[ 0 ] = (int)( surplus / 2 );
				spaces[ count ] = (int)( surplus - spaces[ 0 ] );
				break;
			case CursesTrackDistribution.End:
				spaces[ 0 ] = (int)surplus;
				break;
			case CursesTrackDistribution.SpaceBetween:
				if ( count < 2 ) {
					spaces[ count ] = (int)surplus;
					break;
				}
				for ( int slot = 1; slot < count; slot++ ) {
					spaces[ slot ] = (int)( surplus / ( count - 1 ) + ( slot <= surplus % ( count - 1 ) ? 1 : 0 ) );
				}
				break;
			case CursesTrackDistribution.SpaceAround:
				long halfSpace = surplus / ( 2L * count );
				long leftoverUnits = surplus % ( 2L * count );
				for ( int slot = 0; slot <= count; slot++ ) {
					int units = slot == 0 || slot == count ? 1 : 2;
					spaces[ slot ] = (int)( halfSpace * units + Math.Min( leftoverUnits, units ) );
					leftoverUnits = Math.Max( 0, leftoverUnits - units );
				}
				break;
			case CursesTrackDistribution.SpaceEvenly:
				for ( int slot = 0; slot <= count; slot++ ) {
					spaces[ slot ] = (int)( surplus / ( count + 1 ) + ( slot < surplus % ( count + 1 ) ? 1 : 0 ) );
				}
				break;
		}
		return spaces;
	}
}
