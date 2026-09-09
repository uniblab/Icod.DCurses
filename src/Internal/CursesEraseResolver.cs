namespace Icod.DCurses.Internal;

using Icod.TermInfo;

/// <summary>Identifies the physical erase operation selected for one blank refresh region.</summary>
internal enum CursesEraseKind {
	ClearToEndOfLine,
	ClearToEndOfScreen,
	ClearScreen
}

/// <summary>Represents one advertised erase sequence and its deterministic terminal-byte cost.</summary>
internal readonly record struct CursesErasePlan(
	CursesEraseKind Kind,
	string Sequence,
	int ByteCount,
	int AffectedLines
);

/// <summary>Selects the cheapest safe advertised erase operation for default-styled blank cells.</summary>
internal sealed class CursesEraseResolver {
	private readonly TerminalDescription terminal;
	private readonly CursesOutputCostModel costModel;
	private readonly int blankByteCount;

	internal CursesEraseResolver(
		TerminalDescription terminal,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( costModel );

		this.terminal = terminal;
		this.costModel = costModel;
		blankByteCount = this.costModel.GetApplicationTextByteCount( " " );
	}

	internal CursesErasePlan? Resolve(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn
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
		) ) {
			return null;
		}

		string? eraseLine = this.terminal.GetString(
			StringCapability.ClearToEndOfLine
		);
		int rowFallbackCost = EstimateRowFallbackCost(
			desired,
			physicalScreen,
			row,
			startColumn,
			eraseLine
		);

		CursesErasePlan? selected = null;
		int selectedTotalCost = rowFallbackCost;
		if ( null != eraseLine ) {
			int eraseLineCost = CursesOutputCostModel.GetTerminalStringByteCount(
				eraseLine
			);
			if ( eraseLineCost < rowFallbackCost ) {
				selected = new CursesErasePlan(
					CursesEraseKind.ClearToEndOfLine,
					eraseLine,
					eraseLineCost,
					1
				);
				selectedTotalCost = eraseLineCost;
			}
		}

		if ( !IsDefaultBlankTail(
			desired,
			row,
			startColumn
		) ) {
			return selected;
		}

		int remainingFallbackCost = rowFallbackCost;
		int remainingAfterCurrentRow = 0;
		for ( int candidateRow = row + 1; candidateRow < desired.Rows; candidateRow++ ) {
			remainingAfterCurrentRow = checked(
				remainingAfterCurrentRow
				+ EstimateRowFallbackCost(
					desired,
					physicalScreen,
					candidateRow,
					0,
					eraseLine
				)
			);
		}
		remainingFallbackCost = checked(
			remainingFallbackCost + remainingAfterCurrentRow
		);
		selectedTotalCost = checked(
			selectedTotalCost + remainingAfterCurrentRow
		);

		string? eraseScreen = this.terminal.GetString(
			StringCapability.ClearToEndOfScreen
		);
		if ( null != eraseScreen ) {
			int affectedLines = desired.Rows - row;
			int eraseScreenCost = CursesOutputCostModel.GetTerminalStringByteCount(
				eraseScreen,
				affectedLines
			);
			if ( eraseScreenCost < selectedTotalCost
				&& eraseScreenCost < remainingFallbackCost ) {
				selected = new CursesErasePlan(
					CursesEraseKind.ClearToEndOfScreen,
					eraseScreen,
					eraseScreenCost,
					affectedLines
				);
				selectedTotalCost = eraseScreenCost;
			}
		}

		if ( IsWholeScreenDefaultBlank( desired ) ) {
			string? clearScreen = this.terminal.GetString(
				StringCapability.ClearScreen
			);
			if ( null != clearScreen ) {
				int clearScreenCost = CursesOutputCostModel.GetTerminalStringByteCount(
					clearScreen,
					desired.Rows
				);
				if ( clearScreenCost < selectedTotalCost
					&& clearScreenCost < remainingFallbackCost ) {
					selected = new CursesErasePlan(
						CursesEraseKind.ClearScreen,
						clearScreen,
						clearScreenCost,
						desired.Rows
					);
				}
			}
		}

		return selected;
	}

	private int EstimateRowFallbackCost(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		string? eraseLine
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

		int literalCost = checked( changedCellCount * blankByteCount );
		if ( 0 == literalCost || null == eraseLine ) {
			return literalCost;
		}

		int eraseLineCost = CursesOutputCostModel.GetTerminalStringByteCount(
			eraseLine
		);
		return Math.Min( literalCost, eraseLineCost );
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
