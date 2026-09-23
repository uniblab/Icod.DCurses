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

/// <summary>Identifies the physical erase operation selected for one blank refresh region.</summary>
internal enum CursesEraseKind {
	ClearToEndOfLine,
	ClearToEndOfScreen,
	ClearScreen
}

/// <summary>Represents one Terminal-planned erase and its resulting retained observations.</summary>
internal readonly record struct CursesErasePlan(
	CursesEraseKind Kind,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int? CursorAfterRow,
	int? CursorAfterColumn
);

/// <summary>Selects the cheapest safe Terminal-planned erase for default-styled blank cells.</summary>
internal sealed class CursesEraseResolver {
	private readonly TerminalScreenPlanner planner;
	private readonly CursesPresentationResolver presentationResolver;
	private readonly CursesCursorMotionResolver cursorMotionResolver;
	private readonly int blankByteCount;

	internal CursesEraseResolver(
		TerminalScreenPlanner planner,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( planner );
		ArgumentNullException.ThrowIfNull( costModel );

		this.planner = planner;
		this.presentationResolver = new CursesPresentationResolver( planner );
		this.cursorMotionResolver = new CursesCursorMotionResolver( planner );
		this.blankByteCount = costModel.GetApplicationTextByteCount( " " );
	}

	internal CursesErasePlan? Resolve(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
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
		if ( 0 > startColumn || startColumn >= desired.Columns ) {
			throw new ArgumentOutOfRangeException( nameof( startColumn ) );
		}

		if ( !IsDefaultBlankRange(
			desired,
			row,
			startColumn,
			desired.Columns
		) || !CursesEditingRegionSafety.IsRetainedStateFree(
			desired,
			physicalScreen,
			row,
			row + 1,
			startColumn,
			desired.Columns
		) ) {
			return null;
		}

		int rowLiteralCost = EstimateLiteralBlankCost(
			desired,
			physicalScreen,
			row,
			startColumn
		);
		CursesErasePlan? selected = null;
		int selectedTotalCost = rowLiteralCost;
		CursesTerminalPlanSequence? eraseLine = TryCreateSequence(
			TerminalScreenEraseKind.ToEndOfLine,
			affectedLines: 1,
			currentStyle,
			currentCursorRow,
			currentCursorColumn,
			row,
			startColumn,
			moveCursor: true
		);
		if ( eraseLine is not null
			&& eraseLine.ByteCount < selectedTotalCost ) {
			selected = new CursesErasePlan(
				CursesEraseKind.ClearToEndOfLine,
				eraseLine,
				CursesStyle.Default,
				row,
				startColumn
			);
			selectedTotalCost = eraseLine.ByteCount;
		}

		if ( !IsDefaultBlankTail( desired, row, startColumn ) ) {
			return selected;
		}

		try {
			for ( int candidateRow = row + 1; candidateRow < desired.Rows; candidateRow++ ) {
				selectedTotalCost = checked(
					selectedTotalCost
						+ EstimateLiteralBlankCost(
							desired,
							physicalScreen,
							candidateRow,
							0
						)
				);
			}
		} catch ( OverflowException ) {
			return selected;
		}

		if ( IsTailRetainedStateFree(
			desired,
			physicalScreen,
			row,
			startColumn
		) ) {
			CursesTerminalPlanSequence? eraseScreen = TryCreateSequence(
				TerminalScreenEraseKind.ToEndOfScreen,
				desired.Rows - row,
				currentStyle,
				currentCursorRow,
				currentCursorColumn,
				row,
				startColumn,
				moveCursor: true
			);
			if ( eraseScreen is not null
				&& eraseScreen.ByteCount < selectedTotalCost ) {
				selected = new CursesErasePlan(
					CursesEraseKind.ClearToEndOfScreen,
					eraseScreen,
					CursesStyle.Default,
					row,
					startColumn
				);
				selectedTotalCost = eraseScreen.ByteCount;
			}
		}

		if ( IsWholeScreenDefaultBlank( desired )
			&& CursesEditingRegionSafety.IsRetainedStateFree(
				desired,
				physicalScreen,
				0,
				desired.Rows,
				0,
				desired.Columns
			) ) {
			CursesTerminalPlanSequence? clearScreen = TryCreateSequence(
				TerminalScreenEraseKind.Screen,
				desired.Rows,
				currentStyle,
				currentCursorRow,
				currentCursorColumn,
				row,
				startColumn,
				moveCursor: false
			);
			if ( clearScreen is not null
				&& clearScreen.ByteCount < selectedTotalCost ) {
				selected = new CursesErasePlan(
					CursesEraseKind.ClearScreen,
					clearScreen,
					CursesStyle.Default,
					CursorAfterRow: null,
					CursorAfterColumn: null
				);
			}
		}

		return selected;
	}

	private CursesTerminalPlanSequence? TryCreateSequence(
		TerminalScreenEraseKind kind,
		int affectedLines,
		CursesStyle? currentStyle,
		int? currentCursorRow,
		int? currentCursorColumn,
		int targetRow,
		int targetColumn,
		bool moveCursor
	) {
		TerminalScreenOperationPlan? operation = this.planner.PlanErase(
			kind,
			affectedLines
		);
		if ( !operation.HasValue || 0 == operation.Value.ByteCount ) {
			return null;
		}

		List<TerminalScreenOperationPlan> plans = [];
		if ( !TryPrepareDefaultRendition( plans, currentStyle ) ) {
			return null;
		}
		if ( moveCursor
			&& ( currentCursorRow != targetRow
				|| currentCursorColumn != targetColumn ) ) {
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

	private static bool IsTailRetainedStateFree(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn
	) {
		if ( !CursesEditingRegionSafety.IsRetainedStateFree(
			desired,
			physicalScreen,
			row,
			row + 1,
			startColumn,
			desired.Columns
		) ) {
			return false;
		}
		return row + 1 >= desired.Rows
			|| CursesEditingRegionSafety.IsRetainedStateFree(
				desired,
				physicalScreen,
				row + 1,
				desired.Rows,
				0,
				desired.Columns
			);
	}

	private int EstimateLiteralBlankCost(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn
	) {
		int changedCellCount = 0;
		for ( int column = startColumn; column < desired.Columns; column++ ) {
			if ( NeedsUpdate(
				desired,
				physicalScreen,
				row,
				column
			) ) {
				changedCellCount++;
			}
		}
		return checked( changedCellCount * this.blankByteCount );
	}

	private static bool IsDefaultBlankTail(
		CursesVirtualScreen desired,
		int row,
		int startColumn
	) {
		if ( !IsDefaultBlankRange(
			desired,
			row,
			startColumn,
			desired.Columns
		) ) {
			return false;
		}

		for ( int candidateRow = row + 1; candidateRow < desired.Rows; candidateRow++ ) {
			if ( !IsDefaultBlankRange(
				desired,
				candidateRow,
				0,
				desired.Columns
			) ) {
				return false;
			}
		}
		return true;
	}

	private static bool IsWholeScreenDefaultBlank(
		CursesVirtualScreen desired
	) {
		for ( int row = 0; row < desired.Rows; row++ ) {
			if ( !IsDefaultBlankRange(
				desired,
				row,
				0,
				desired.Columns
			) ) {
				return false;
			}
		}
		return true;
	}

	private static bool IsDefaultBlankRange(
		CursesVirtualScreen desired,
		int row,
		int startColumn,
		int endColumnExclusive
	) {
		for ( int column = startColumn; column < endColumnExclusive; column++ ) {
			CursesCell cell = desired[ row, column ];
			if ( !cell.IsBlank || !cell.Style.IsDefault ) {
				return false;
			}
		}
		return true;
	}

	private static bool NeedsUpdate(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int column
	) {
		if ( desired.IsDirty( row, column ) ) {
			return true;
		}
		if ( !physicalScreen.TryGetCell(
			row,
			column,
			out CursesCell physicalCell
		) ) {
			return true;
		}
		return physicalCell != desired[ row, column ];
	}
}
