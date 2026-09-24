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
		int[] sizes = new int[ tracks.Length ];
		long totalWeight = 0;
		long assigned = 0;
		for ( int index = 0; index < tracks.Length; index++ ) {
			CursesTrack track = tracks[ index ];
			if ( track.Kind == CursesTrackKind.Fixed ) {
				sizes[ index ] = Math.Clamp( track.Value, track.Minimum, track.Maximum ?? int.MaxValue );
			} else if ( track.Kind == CursesTrackKind.Weighted && track.Value > 0 ) {
				sizes[ index ] = track.Minimum;
				totalWeight += track.Value;
			} else {
				throw new ArgumentOutOfRangeException( nameof( tracks ) );
			}
			assigned += sizes[ index ];
		}
		if ( assigned > available ) {
			throw new ArgumentException( "Tracks exceed the available extent.", nameof( tracks ) );
		}

		long remaining = available - assigned;
		if ( totalWeight > 0 ) {
			for ( int index = 0; index < tracks.Length; index++ ) {
				if ( tracks[ index ].Kind != CursesTrackKind.Weighted ) {
					continue;
				}
				int share = (int)( remaining * tracks[ index ].Value / totalWeight );
				sizes[ index ] += share;
				assigned += share;
			}
			for ( int index = 0; index < tracks.Length && assigned < available; index++ ) {
				if ( tracks[ index ].Kind == CursesTrackKind.Weighted ) {
					sizes[ index ]++;
					assigned++;
				}
			}
		}

		CursesRectangle[] result = new CursesRectangle[ tracks.Length ];
		int cursor = rows ? bounds.Row : bounds.Column;
		for ( int index = 0; index < tracks.Length; index++ ) {
			result[ index ] = rows
				? new CursesRectangle( cursor, bounds.Column, sizes[ index ], bounds.Columns )
				: new CursesRectangle( bounds.Row, cursor, bounds.Rows, sizes[ index ] );
			cursor += sizes[ index ];
			if ( index + 1 < tracks.Length ) {
				cursor += gap;
			}
		}
		return result;
	}
}
