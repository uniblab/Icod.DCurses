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

public sealed partial class CursesInteractionRouter {
	private CursesInteractionRegion? MoveSpatialFocus(
		CursesFocusDirection direction
	) {
		CursesInteractionRegion? focused = this.focusedRegion;
		if ( focused is null
			|| !this.TryGetEffectiveScreenBounds(
				focused,
				out CursesRectangle focusedBounds
			) ) {
			return null;
		}

		CursesInteractionRegion? selected = null;
		CursesRectangle selectedBounds = default;
		foreach ( CursesInteractionRegion candidate in this.regions ) {
			if ( ReferenceEquals(
				candidate,
				focused
			) || !this.IsRegionEligible( candidate )
				|| !this.TryGetEffectiveScreenBounds(
					candidate,
					out CursesRectangle candidateBounds
				)
				|| !IsSpatialCandidate(
					direction,
					focusedBounds,
					candidateBounds
				) ) {
				continue;
			}

			if ( selected is null
				|| IsPreferredSpatialCandidate(
					direction,
					focusedBounds,
					candidate,
					candidateBounds,
					selected,
					selectedBounds
				) ) {
				selected = candidate;
				selectedBounds = candidateBounds;
			}
		}

		if ( selected is not null ) {
			this.focusedRegion = selected;
		}
		return selected;
	}

	private bool TryGetEffectiveScreenBounds(
		CursesInteractionRegion region,
		out CursesRectangle effectiveBounds
	) {
		ArgumentNullException.ThrowIfNull( region );
		CursesRectangle bounds = region.Bounds;
		if ( bounds.IsEmpty ) {
			effectiveBounds = default;
			return false;
		}

		long top;
		long left;
		long bottom;
		long right;
		CursesPanel? panel = region.Panel;
		if ( panel is null ) {
			top = bounds.Row;
			left = bounds.Column;
			bottom = Math.Min(
				(long)this.Screen.Rows,
				top + bounds.Rows
			);
			right = Math.Min(
				(long)this.Screen.Columns,
				left + bounds.Columns
			);
		} else {
			if ( panel.IsDisposed || !panel.IsVisible ) {
				effectiveBounds = default;
				return false;
			}

			top = (long)panel.Row + bounds.Row;
			left = (long)panel.Column + bounds.Column;
			bottom = Math.Min(
				(long)this.Screen.Rows,
				Math.Min(
					(long)panel.Row + panel.Rows,
					top + bounds.Rows
				)
			);
			right = Math.Min(
				(long)this.Screen.Columns,
				Math.Min(
					(long)panel.Column + panel.Columns,
					left + bounds.Columns
				)
			);
		}

		if ( top >= bottom || left >= right ) {
			effectiveBounds = default;
			return false;
		}

		effectiveBounds = new CursesRectangle(
			checked( (int)top ),
			checked( (int)left ),
			checked( (int)( bottom - top ) ),
			checked( (int)( right - left ) )
		);
		return true;
	}

	private static bool IsSpatialCandidate(
		CursesFocusDirection direction,
		CursesRectangle focusedBounds,
		CursesRectangle candidateBounds
	) {
		return direction switch {
			CursesFocusDirection.Up => candidateBounds.BottomExclusive <= focusedBounds.Row,
			CursesFocusDirection.Down => candidateBounds.Row >= focusedBounds.BottomExclusive,
			CursesFocusDirection.Left => candidateBounds.RightExclusive <= focusedBounds.Column,
			CursesFocusDirection.Right => candidateBounds.Column >= focusedBounds.RightExclusive,
			_ => throw new ArgumentOutOfRangeException( nameof( direction ) )
		};
	}

	private static bool IsPreferredSpatialCandidate(
		CursesFocusDirection direction,
		CursesRectangle focusedBounds,
		CursesInteractionRegion candidate,
		CursesRectangle candidateBounds,
		CursesInteractionRegion selected,
		CursesRectangle selectedBounds
	) {
		bool candidateOverlaps = HasPerpendicularOverlap(
			direction,
			focusedBounds,
			candidateBounds
		);
		bool selectedOverlaps = HasPerpendicularOverlap(
			direction,
			focusedBounds,
			selectedBounds
		);
		if ( candidateOverlaps != selectedOverlaps ) {
			return candidateOverlaps;
		}

		long candidatePrimaryDistance = GetPrimaryEdgeDistance(
			direction,
			focusedBounds,
			candidateBounds
		);
		long selectedPrimaryDistance = GetPrimaryEdgeDistance(
			direction,
			focusedBounds,
			selectedBounds
		);
		if ( candidatePrimaryDistance != selectedPrimaryDistance ) {
			return candidatePrimaryDistance < selectedPrimaryDistance;
		}

		long candidateCenterDistance = GetPerpendicularCenterDistance(
			direction,
			focusedBounds,
			candidateBounds
		);
		long selectedCenterDistance = GetPerpendicularCenterDistance(
			direction,
			focusedBounds,
			selectedBounds
		);
		if ( candidateCenterDistance != selectedCenterDistance ) {
			return candidateCenterDistance < selectedCenterDistance;
		}

		if ( candidate.TraversalOrder != selected.TraversalOrder ) {
			return candidate.TraversalOrder < selected.TraversalOrder;
		}
		return candidate.RegistrationOrdinal < selected.RegistrationOrdinal;
	}

	private static bool HasPerpendicularOverlap(
		CursesFocusDirection direction,
		CursesRectangle focusedBounds,
		CursesRectangle candidateBounds
	) {
		return direction switch {
			CursesFocusDirection.Up or CursesFocusDirection.Down
				=> IntervalsOverlap(
					focusedBounds.Column,
					focusedBounds.RightExclusive,
					candidateBounds.Column,
					candidateBounds.RightExclusive
				),
			CursesFocusDirection.Left or CursesFocusDirection.Right
				=> IntervalsOverlap(
					focusedBounds.Row,
					focusedBounds.BottomExclusive,
					candidateBounds.Row,
					candidateBounds.BottomExclusive
				),
			_ => throw new ArgumentOutOfRangeException( nameof( direction ) )
		};
	}

	private static long GetPrimaryEdgeDistance(
		CursesFocusDirection direction,
		CursesRectangle focusedBounds,
		CursesRectangle candidateBounds
	) {
		return direction switch {
			CursesFocusDirection.Up
				=> (long)focusedBounds.Row - candidateBounds.BottomExclusive,
			CursesFocusDirection.Down
				=> (long)candidateBounds.Row - focusedBounds.BottomExclusive,
			CursesFocusDirection.Left
				=> (long)focusedBounds.Column - candidateBounds.RightExclusive,
			CursesFocusDirection.Right
				=> (long)candidateBounds.Column - focusedBounds.RightExclusive,
			_ => throw new ArgumentOutOfRangeException( nameof( direction ) )
		};
	}

	private static long GetPerpendicularCenterDistance(
		CursesFocusDirection direction,
		CursesRectangle focusedBounds,
		CursesRectangle candidateBounds
	) {
		long focusedCenter;
		long candidateCenter;
		if ( CursesFocusDirection.Up == direction
			|| CursesFocusDirection.Down == direction ) {
			focusedCenter = ( (long)focusedBounds.Column * 2 ) + focusedBounds.Columns;
			candidateCenter = ( (long)candidateBounds.Column * 2 ) + candidateBounds.Columns;
		} else if ( CursesFocusDirection.Left == direction
			|| CursesFocusDirection.Right == direction ) {
			focusedCenter = ( (long)focusedBounds.Row * 2 ) + focusedBounds.Rows;
			candidateCenter = ( (long)candidateBounds.Row * 2 ) + candidateBounds.Rows;
		} else {
			throw new ArgumentOutOfRangeException( nameof( direction ) );
		}

		return Math.Abs( candidateCenter - focusedCenter );
	}

	private static bool IntervalsOverlap(
		int firstStart,
		int firstEnd,
		int secondStart,
		int secondEnd
	) {
		return firstStart < secondEnd && secondStart < firstEnd;
	}
}
