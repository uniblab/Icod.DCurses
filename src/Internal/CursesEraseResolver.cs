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
	private readonly int blankByteCount;

	internal CursesEraseResolver(
		TerminalDescription terminal,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( costModel );

		this.terminal = terminal;
		blankByteCount = costModel.GetApplicationTextByteCount( " " );
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
		int rowLiteralCost = EstimateLiteralBlankCost(
			desired,
			physicalScreen,
			row,
			startColumn
		);

		CursesErasePlan? selected = null;
		int selectedTotalCost = rowLiteralCost;
		if ( null != eraseLine ) {
			int eraseLineCost = CursesOutputCostModel.GetTerminalStringByteCount(
				eraseLine
			);
			if ( eraseLineCost < rowLiteralCost ) {
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

		for ( int candidateRow = row + 1; candidateRow < desired.Rows; candidateRow++ ) {
			selectedTotalCost = checked(
				selectedTotalCost
				+ EstimateBestRowCost(
					desired,
					physicalScreen,
					candidateRow,
					0,
					eraseLine
				)
			);
		}

		string? eraseScreen = this.terminal.GetString(
			StringCapability.ClearToEndOfScreen
		);
		if ( null != eraseScreen ) {
			int affectedLines = desired.Rows - row;
			int eraseScreenCost = CursesOutputCostModel.GetTerminalStringByteCount(
				eraseScreen,
				affectedLines
			);
			if ( eraseScreenCost < selectedTotalCost ) {
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
				if ( clearScreenCost < selectedTotalCost ) {
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

	private int EstimateBestRowCost(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		string? eraseLine
	) {
		int literalCost = EstimateLiteralBlankCost(
			desired,
			physicalScreen,
			row,
			startColumn
		);
		if ( 0 == literalCost || null == eraseLine ) {
			return literalCost;
		}

		int eraseLineCost = CursesOutputCostModel.GetTerminalStringByteCount(
			eraseLine
		);
		return Math.Min( literalCost, eraseLineCost );
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
		return checked( changedCellCount * blankByteCount );
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
