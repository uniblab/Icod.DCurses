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

namespace Icod.DCurses.Internal;

/// <summary>Determines whether a rectangular editing footprint contains only known plain cells.</summary>
internal static class CursesEditingRegionSafety {
	internal static bool IsRetainedStateFree(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int topRow,
		int bottomRowExclusive,
		int startColumn,
		int endColumnExclusive
	) {
		ArgumentNullException.ThrowIfNull( desired );
		ArgumentNullException.ThrowIfNull( physical );
		if ( physical.Columns != desired.Columns
			|| physical.Rows != desired.Rows ) {
			throw new ArgumentException(
				"Physical and desired screen dimensions must match.",
				nameof( physical )
			);
		}
		if ( 0 > topRow || desired.Rows <= topRow ) {
			throw new ArgumentOutOfRangeException( nameof( topRow ) );
		}
		if ( topRow >= bottomRowExclusive
			|| desired.Rows < bottomRowExclusive ) {
			throw new ArgumentOutOfRangeException( nameof( bottomRowExclusive ) );
		}
		if ( 0 > startColumn || desired.Columns <= startColumn ) {
			throw new ArgumentOutOfRangeException( nameof( startColumn ) );
		}
		if ( startColumn >= endColumnExclusive
			|| desired.Columns < endColumnExclusive ) {
			throw new ArgumentOutOfRangeException( nameof( endColumnExclusive ) );
		}

		for ( int row = topRow; row < bottomRowExclusive; row++ ) {
			for ( int column = startColumn; column < endColumnExclusive; column++ ) {
				if ( !physical.TryGetCell( row, column, out _ )
					|| desired.GetMetadata( row, column ) is not null
					|| physical.GetMetadata( row, column ) is not null
					|| desired.GetRasterCell( row, column ).HasValue
					|| physical.GetRasterCell( row, column ).HasValue ) {
					return false;
				}
			}
		}
		return true;
	}
}
