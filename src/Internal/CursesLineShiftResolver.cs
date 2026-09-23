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

using Icod.Terminal;

/// <summary>Identifies the logical direction of one exact physical line shift.</summary>
internal enum CursesLineShiftKind {
	Insert,
	Delete
}

/// <summary>Identifies the terminal operation selected to realize one exact physical line shift.</summary>
internal enum CursesLineShiftOperation {
	InsertLines,
	DeleteLines,
	ScrollReverse,
	ScrollForward
}

/// <summary>Represents one exact Terminal-planned vertical-region shift.</summary>
internal readonly record struct CursesLineShiftPlan(
	CursesLineShiftKind Kind,
	CursesLineShiftOperation Operation,
	int TopRow,
	int BottomRow,
	int Count,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int CursorAfterRow,
	int CursorAfterColumn
);

/// <summary>
/// Selects exact whole-row insertion, deletion, and scrolling operations when they reproduce the
/// complete desired screen and are strictly cheaper than a conservative direct-rewrite lower bound.
/// </summary>
internal sealed class CursesLineShiftResolver {
	private readonly TerminalScreenPlanner planner;
	private readonly CursesOutputCostModel costModel;
	private readonly CursesPresentationResolver presentationResolver;
	private readonly CursesCursorMotionResolver cursorMotionResolver;

	internal CursesLineShiftResolver(
		TerminalScreenPlanner planner,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( planner );
		ArgumentNullException.ThrowIfNull( costModel );

		this.planner = planner;
		this.costModel = costModel;
		this.presentationResolver = new CursesPresentationResolver( planner );
		this.cursorMotionResolver = new CursesCursorMotionResolver( planner );
	}

	internal CursesLineShiftPlan? Resolve(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		ArgumentNullException.ThrowIfNull( desired );
		ArgumentNullException.ThrowIfNull( physicalScreen );
		if ( desired.Columns != physicalScreen.Columns
			|| desired.Rows != physicalScreen.Rows ) {
			throw new ArgumentException(
				"The logical and physical screen dimensions must match.",
				nameof( physicalScreen )
			);
		}
		if ( 0 > requestedCursorRow || requestedCursorRow >= desired.Rows ) {
			throw new ArgumentOutOfRangeException( nameof( requestedCursorRow ) );
		}
		if ( 0 > requestedCursorColumn || requestedCursorColumn >= desired.Columns ) {
			throw new ArgumentOutOfRangeException( nameof( requestedCursorColumn ) );
		}
		if ( currentCursorRow.HasValue != currentCursorColumn.HasValue ) {
			throw new ArgumentException(
				"Current cursor row and column knowledge must either both be known or both be unknown."
			);
		}

		if ( !TryFindDifferenceRange(
			desired,
			physicalScreen,
			out int firstDifferenceRow,
			out int lastDifferenceRow
		) ) {
			return null;
		}

		int rewriteLowerBound;
		try {
			rewriteLowerBound = GetChangedNonblankByteCount(
				desired,
				physicalScreen,
				firstDifferenceRow,
				lastDifferenceRow
			);
		} catch ( OverflowException ) {
			return null;
		}
		if ( 0 >= rewriteLowerBound ) {
			return null;
		}

		CursesLineShiftPlan? best = null;
		for ( int bottomRow = lastDifferenceRow; bottomRow < desired.Rows; bottomRow++ ) {
			if ( !CursesEditingRegionSafety.IsRetainedStateFree(
				desired,
				physicalScreen,
				firstDifferenceRow,
				bottomRow + 1,
				0,
				desired.Columns
			) ) {
				continue;
			}
			int regionHeight = bottomRow - firstDifferenceRow + 1;
			for ( int count = 1; count <= regionHeight; count++ ) {
				if ( MatchesInsertion(
					desired,
					physicalScreen,
					firstDifferenceRow,
					bottomRow,
					count
				) ) {
					ResolveInsertionCandidates(
						ref best,
						desired,
						firstDifferenceRow,
						bottomRow,
						count,
						rewriteLowerBound,
						currentStyle,
						currentCursorRow,
						currentCursorColumn,
						requestedCursorRow,
						requestedCursorColumn
					);
				}
				if ( MatchesDeletion(
					desired,
					physicalScreen,
					firstDifferenceRow,
					bottomRow,
					count
				) ) {
					ResolveDeletionCandidates(
						ref best,
						desired,
						firstDifferenceRow,
						bottomRow,
						count,
						rewriteLowerBound,
						currentStyle,
						currentCursorRow,
						currentCursorColumn,
						requestedCursorRow,
						requestedCursorColumn
					);
				}
			}
		}
		return best;
	}

	private void ResolveInsertionCandidates(
		ref CursesLineShiftPlan? best,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		int rewriteLowerBound,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		bool reachesScreenBottom = bottomRow + 1 == desired.Rows;
		TryAddCandidate(
			ref best,
			CursesLineShiftKind.Insert,
			CursesLineShiftOperation.InsertLines,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !reachesScreenBottom,
			operationRow: topRow,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);

		bool fullScreenRegion = 0 == topRow && reachesScreenBottom;
		TryAddCandidate(
			ref best,
			CursesLineShiftKind.Insert,
			CursesLineShiftOperation.ScrollReverse,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !fullScreenRegion,
			operationRow: topRow,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
	}

	private void ResolveDeletionCandidates(
		ref CursesLineShiftPlan? best,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		int rewriteLowerBound,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		bool reachesScreenBottom = bottomRow + 1 == desired.Rows;
		TryAddCandidate(
			ref best,
			CursesLineShiftKind.Delete,
			CursesLineShiftOperation.DeleteLines,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !reachesScreenBottom,
			operationRow: topRow,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);

		bool fullScreenRegion = 0 == topRow && reachesScreenBottom;
		TryAddCandidate(
			ref best,
			CursesLineShiftKind.Delete,
			CursesLineShiftOperation.ScrollForward,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !fullScreenRegion,
			operationRow: bottomRow,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
	}

	private void TryAddCandidate(
		ref CursesLineShiftPlan? best,
		CursesLineShiftKind kind,
		CursesLineShiftOperation operation,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		bool temporaryScrollRegion,
		int operationRow,
		int rewriteLowerBound,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		CursesTerminalPlanSequence? sequence = TryCreateSequence(
			operation,
			desired.Rows,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion,
			operationRow,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
		if ( sequence is null || sequence.ByteCount >= rewriteLowerBound ) {
			return;
		}

		CursesLineShiftPlan candidate = new(
			kind,
			operation,
			topRow,
			bottomRow,
			count,
			sequence,
			CursesStyle.Default,
			requestedCursorRow,
			requestedCursorColumn
		);
		ChooseBetter( ref best, candidate );
	}

	private CursesTerminalPlanSequence? TryCreateSequence(
		CursesLineShiftOperation operation,
		int screenRows,
		int topRow,
		int bottomRow,
		int count,
		bool temporaryScrollRegion,
		int operationRow,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		int regionHeight = bottomRow - topRow + 1;
		TerminalScreenOperationPlan? lineShift = this.planner.PlanLineShift(
			MapOperation( operation ),
			count,
			regionHeight
		);
		if ( !lineShift.HasValue || 0 == lineShift.Value.ByteCount ) {
			return null;
		}

		List<TerminalScreenOperationPlan> plans = [];
		if ( !TryPrepareDefaultRendition( plans, currentStyle ) ) {
			return null;
		}

		if ( temporaryScrollRegion ) {
			TerminalScreenOperationPlan? setRegion = this.planner.PlanScrollRegion(
				topRow,
				bottomRow,
				regionHeight
			);
			TerminalScreenOperationPlan? restoreRegion = this.planner.PlanScrollRegion(
				0,
				screenRows - 1,
				screenRows
			);
			if ( !setRegion.HasValue
				|| 0 == setRegion.Value.ByteCount
				|| !restoreRegion.HasValue
				|| 0 == restoreRegion.Value.ByteCount ) {
				return null;
			}

			plans.Add( setRegion.Value );
			if ( !TryAddCursorMotion( plans, null, null, operationRow, 0 ) ) {
				return null;
			}
			plans.Add( lineShift.Value );
			plans.Add( restoreRegion.Value );
			if ( !TryAddCursorMotion(
				plans,
				null,
				null,
				requestedCursorRow,
				requestedCursorColumn
			) ) {
				return null;
			}
		} else {
			if ( !TryAddCursorMotion(
				plans,
				currentCursorRow,
				currentCursorColumn,
				operationRow,
				0
			) ) {
				return null;
			}
			plans.Add( lineShift.Value );
			if ( !TryAddCursorMotion(
				plans,
				operationRow,
				0,
				requestedCursorRow,
				requestedCursorColumn
			) ) {
				return null;
			}
		}

		try {
			return new CursesTerminalPlanSequence(
				plans,
				temporaryScrollRegion
			);
		} catch ( OverflowException ) {
			return null;
		}
	}

	private bool TryPrepareDefaultRendition(
		List<TerminalScreenOperationPlan> plans,
		CursesStyle? currentStyle
	) {
		TerminalScreenOperationPlan? setup = null;
		if ( !currentStyle.HasValue ) {
			setup = this.presentationResolver.PlanBaseline();
		} else if ( !currentStyle.Value.IsDefault ) {
			setup = this.presentationResolver.PlanReset( currentStyle.Value );
		}

		if ( !currentStyle.HasValue || !currentStyle.Value.IsDefault ) {
			if ( !setup.HasValue ) {
				return false;
			}
			plans.Add( setup.Value );
		}
		return true;
	}

	private bool TryAddCursorMotion(
		List<TerminalScreenOperationPlan> plans,
		int? currentRow,
		int? currentColumn,
		int targetRow,
		int targetColumn
	) {
		try {
			plans.Add(
				this.cursorMotionResolver.Resolve(
					currentRow,
					currentColumn,
					targetRow,
					targetColumn
				)
			);
			return true;
		} catch ( NotSupportedException ) {
			return false;
		}
	}

	private static TerminalScreenLineShiftKind MapOperation(
		CursesLineShiftOperation operation
	) {
		return operation switch {
			CursesLineShiftOperation.InsertLines => TerminalScreenLineShiftKind.Insert,
			CursesLineShiftOperation.DeleteLines => TerminalScreenLineShiftKind.Delete,
			CursesLineShiftOperation.ScrollForward => TerminalScreenLineShiftKind.ScrollForward,
			CursesLineShiftOperation.ScrollReverse => TerminalScreenLineShiftKind.ScrollReverse,
			_ => throw new ArgumentOutOfRangeException( nameof( operation ) )
		};
	}

	private static bool TryFindDifferenceRange(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		out int firstDifferenceRow,
		out int lastDifferenceRow
	) {
		firstDifferenceRow = -1;
		lastDifferenceRow = -1;
		for ( int row = 0; row < desired.Rows; row++ ) {
			bool rowDiffers = false;
			for ( int column = 0; column < desired.Columns; column++ ) {
				if ( !physicalScreen.TryGetCell(
					row,
					column,
					out CursesCell physicalCell
				) ) {
					return false;
				}
				if ( physicalCell != desired[ row, column ] ) {
					rowDiffers = true;
				}
			}
			if ( rowDiffers ) {
				if ( -1 == firstDifferenceRow ) {
					firstDifferenceRow = row;
				}
				lastDifferenceRow = row;
			}
		}
		return -1 != firstDifferenceRow;
	}

	private int GetChangedNonblankByteCount(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int firstRow,
		int lastRow
	) {
		int byteCount = 0;
		for ( int row = firstRow; row <= lastRow; row++ ) {
			for ( int column = 0; column < desired.Columns; column++ ) {
				CursesCell desiredCell = desired[ row, column ];
				if ( desiredCell.IsBlank
					|| desiredCell.IsContinuation
					|| desiredCell.IsLineGlyph ) {
					continue;
				}
				if ( !physicalScreen.TryGetCell(
					row,
					column,
					out CursesCell physicalCell
				) || desiredCell == physicalCell ) {
					continue;
				}
				byteCount = checked(
					byteCount
						+ this.costModel.GetApplicationTextByteCount(
							desiredCell.Content
						)
				);
			}
		}
		return byteCount;
	}

	private static bool MatchesInsertion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int topRow,
		int bottomRow,
		int count
	) {
		if ( topRow + count - 1 > bottomRow ) {
			return false;
		}
		for ( int row = topRow; row < topRow + count; row++ ) {
			if ( !IsDefaultBlankRow( desired, row ) ) {
				return false;
			}
		}
		for ( int row = topRow + count; row <= bottomRow; row++ ) {
			if ( !RowsEqual(
				desired,
				physicalScreen,
				row,
				row - count
			) ) {
				return false;
			}
		}
		return true;
	}

	private static bool MatchesDeletion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int topRow,
		int bottomRow,
		int count
	) {
		if ( bottomRow - count + 1 < topRow ) {
			return false;
		}
		for ( int row = bottomRow - count + 1; row <= bottomRow; row++ ) {
			if ( !IsDefaultBlankRow( desired, row ) ) {
				return false;
			}
		}
		for ( int row = topRow; row <= bottomRow - count; row++ ) {
			if ( !RowsEqual(
				desired,
				physicalScreen,
				row,
				row + count
			) ) {
				return false;
			}
		}
		return true;
	}

	private static bool RowsEqual(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int desiredRow,
		int physicalRow
	) {
		for ( int column = 0; column < desired.Columns; column++ ) {
			if ( !physicalScreen.TryGetCell(
				physicalRow,
				column,
				out CursesCell physicalCell
			) || desired[ desiredRow, column ] != physicalCell ) {
				return false;
			}
		}
		return true;
	}

	private static bool IsDefaultBlankRow(
		CursesVirtualScreen desired,
		int row
	) {
		for ( int column = 0; column < desired.Columns; column++ ) {
			CursesCell cell = desired[ row, column ];
			if ( !cell.IsBlank || !cell.Style.IsDefault ) {
				return false;
			}
		}
		return true;
	}

	private static void ChooseBetter(
		ref CursesLineShiftPlan? current,
		CursesLineShiftPlan candidate
	) {
		if ( !current.HasValue
			|| candidate.Sequence.ByteCount
				< current.Value.Sequence.ByteCount ) {
			current = candidate;
		}
	}
}
