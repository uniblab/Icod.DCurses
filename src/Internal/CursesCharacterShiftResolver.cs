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

/// <summary>Identifies a row-local physical character shift.</summary>
internal enum CursesCharacterShiftKind {
	Insert,
	Delete
}

/// <summary>Represents one exact Terminal-planned row-local character shift.</summary>
internal readonly record struct CursesCharacterShiftPlan(
	CursesCharacterShiftKind Kind,
	int Row,
	int Column,
	int Count,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int CursorAfterRow,
	int CursorAfterColumn
);

/// <summary>
/// Selects exact insert/delete-character operations when they reproduce one desired row and are
/// strictly cheaper than rewriting the changed nonblank payload.
/// </summary>
internal sealed class CursesCharacterShiftResolver {
	private readonly TerminalScreenPlanner planner;
	private readonly CursesPresentationResolver presentationResolver;
	private readonly CursesCursorMotionResolver cursorMotionResolver;
	private readonly CursesOutputCostModel costModel;

	internal CursesCharacterShiftResolver(
		TerminalScreenPlanner planner,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( planner );
		ArgumentNullException.ThrowIfNull( costModel );

		this.planner = planner;
		this.presentationResolver = new CursesPresentationResolver( planner );
		this.cursorMotionResolver = new CursesCursorMotionResolver( planner );
		this.costModel = costModel;
	}

	internal CursesCharacterShiftPlan? Resolve(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn
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
		if ( 0 > row || row >= desired.Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}

		int firstDifference = -1;
		for ( int column = 0; column < desired.Columns; column++ ) {
			if ( !physicalScreen.TryGetCell(
				row,
				column,
				out CursesCell physicalCell
			) ) {
				return null;
			}
			if ( -1 == firstDifference
				&& physicalCell != desired[ row, column ] ) {
				firstDifference = column;
			}
		}
		if ( -1 == firstDifference ) {
			return null;
		}

		if ( !IsSafeShiftRange(
			desired,
			physicalScreen,
			row,
			firstDifference
		) || !CursesEditingRegionSafety.IsRetainedStateFree(
			desired,
			physicalScreen,
			row,
			row + 1,
			firstDifference,
			desired.Columns
		) ) {
			return null;
		}

		int rewriteLowerBound;
		try {
			rewriteLowerBound = GetChangedNonblankByteCount(
				desired,
				physicalScreen,
				row,
				firstDifference
			);
		} catch ( OverflowException ) {
			return null;
		}
		if ( 0 >= rewriteLowerBound ) {
			return null;
		}

		CursesCharacterShiftPlan? best = ResolveInsertion(
			desired,
			physicalScreen,
			row,
			firstDifference,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn
		);
		CursesCharacterShiftPlan? deletion = ResolveDeletion(
			desired,
			physicalScreen,
			row,
			firstDifference,
			rewriteLowerBound,
			currentStyle,
			currentCursorRow,
			currentCursorColumn
		);
		if ( deletion.HasValue
			&& ( !best.HasValue
				|| deletion.Value.Sequence.ByteCount
					< best.Value.Sequence.ByteCount ) ) {
			best = deletion;
		}
		return best;
	}

	private CursesCharacterShiftPlan? ResolveInsertion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int rewriteLowerBound,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn
	) {
		CursesCharacterShiftPlan? best = null;
		for ( int count = 1; startColumn + count <= desired.Columns; count++ ) {
			CursesCell inserted = desired[ row, startColumn + count - 1 ];
			if ( !IsDefaultBlank( inserted ) ) {
				break;
			}
			if ( !MatchesInsertion(
				desired,
				physicalScreen,
				row,
				startColumn,
				count
			) ) {
				continue;
			}
			CursesTerminalPlanSequence? sequence = TryCreateSequence(
				CursesCharacterShiftKind.Insert,
				count,
				currentStyle,
				currentCursorRow,
				currentCursorColumn,
				row,
				startColumn
			);
			if ( sequence is null
				|| sequence.ByteCount >= rewriteLowerBound ) {
				continue;
			}

			ChooseBetter(
				ref best,
				new CursesCharacterShiftPlan(
					CursesCharacterShiftKind.Insert,
					row,
					startColumn,
					count,
					sequence,
					CursesStyle.Default,
					row,
					startColumn
				)
			);
		}
		return best;
	}

	private CursesCharacterShiftPlan? ResolveDeletion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int rewriteLowerBound,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn
	) {
		CursesCharacterShiftPlan? best = null;
		for ( int count = 1; startColumn + count <= desired.Columns; count++ ) {
			CursesCell trailing = desired[ row, desired.Columns - count ];
			if ( !IsDefaultBlank( trailing ) ) {
				break;
			}
			if ( !MatchesDeletion(
				desired,
				physicalScreen,
				row,
				startColumn,
				count
			) ) {
				continue;
			}
			CursesTerminalPlanSequence? sequence = TryCreateSequence(
				CursesCharacterShiftKind.Delete,
				count,
				currentStyle,
				currentCursorRow,
				currentCursorColumn,
				row,
				startColumn
			);
			if ( sequence is null
				|| sequence.ByteCount >= rewriteLowerBound ) {
				continue;
			}

			ChooseBetter(
				ref best,
				new CursesCharacterShiftPlan(
					CursesCharacterShiftKind.Delete,
					row,
					startColumn,
					count,
					sequence,
					CursesStyle.Default,
					row,
					startColumn
				)
			);
		}
		return best;
	}

	private CursesTerminalPlanSequence? TryCreateSequence(
		CursesCharacterShiftKind kind,
		int count,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int targetRow,
		int targetColumn
	) {
		TerminalScreenOperationPlan? operation = this.planner.PlanCharacterShift(
			CursesCharacterShiftKind.Insert == kind
				? TerminalScreenCharacterShiftKind.Insert
				: TerminalScreenCharacterShiftKind.Delete,
			count
		);
		if ( !operation.HasValue || 0 == operation.Value.ByteCount ) {
			return null;
		}

		List<TerminalScreenOperationPlan> plans = [];
		if ( !TryPrepareDefaultRendition( plans, currentStyle ) ) {
			return null;
		}
		if ( currentCursorRow != targetRow
			|| currentCursorColumn != targetColumn ) {
			try {
				plans.Add(
					this.cursorMotionResolver.Resolve(
						currentCursorRow,
						currentCursorColumn,
						targetRow,
						targetColumn
					)
				);
			} catch ( NotSupportedException ) {
				return null;
			}
		}
		plans.Add( operation.Value );

		try {
			return new CursesTerminalPlanSequence( plans );
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

	private static bool MatchesInsertion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int count
	) {
		for ( int column = startColumn + count; column < desired.Columns; column++ ) {
			if ( !physicalScreen.TryGetCell(
				row,
				column - count,
				out CursesCell source
			) || desired[ row, column ] != source ) {
				return false;
			}
		}
		return true;
	}

	private static bool MatchesDeletion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int count
	) {
		int shiftedEnd = desired.Columns - count;
		for ( int column = startColumn; column < shiftedEnd; column++ ) {
			if ( !physicalScreen.TryGetCell(
				row,
				column + count,
				out CursesCell source
			) || desired[ row, column ] != source ) {
				return false;
			}
		}
		return true;
	}

	private int GetChangedNonblankByteCount(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn
	) {
		int byteCount = 0;
		for ( int column = startColumn; column < desired.Columns; column++ ) {
			CursesCell desiredCell = desired[ row, column ];
			if ( desiredCell.IsBlank ) {
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
		return byteCount;
	}

	private static bool IsSafeShiftRange(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn
	) {
		for ( int column = startColumn; column < desired.Columns; column++ ) {
			CursesCell desiredCell = desired[ row, column ];
			if ( !IsSimpleCell( desiredCell ) ) {
				return false;
			}
			if ( !physicalScreen.TryGetCell(
				row,
				column,
				out CursesCell physicalCell
			) || !IsSimpleCell( physicalCell ) ) {
				return false;
			}
		}
		return true;
	}

	private static bool IsSimpleCell( CursesCell cell ) {
		return !cell.IsContinuation
			&& 1 == cell.DisplayWidth
			&& !cell.IsLineGlyph;
	}

	private static bool IsDefaultBlank( CursesCell cell ) {
		return cell.IsBlank
			&& cell.Style.IsDefault;
	}

	private static void ChooseBetter(
		ref CursesCharacterShiftPlan? current,
		CursesCharacterShiftPlan candidate
	) {
		if ( !current.HasValue
			|| candidate.Sequence.ByteCount
				< current.Value.Sequence.ByteCount ) {
			current = candidate;
		}
	}
}
