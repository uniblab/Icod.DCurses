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

using System.Text;
using Icod.TermInfo;

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

/// <summary>Represents one exact vertical region shift selected by deterministic byte cost.</summary>
internal readonly record struct CursesLineShiftPlan(
	CursesLineShiftKind Kind,
	CursesLineShiftOperation Operation,
	int TopRow,
	int BottomRow,
	int Count,
	string OperationSequence,
	int OperationAffectedLines,
	bool UsesTemporaryScrollRegion,
	string? SetRegionSequence,
	int SetRegionAffectedLines,
	string? RestoreRegionSequence,
	int RestoreRegionAffectedLines,
	int OperationRow,
	int OperationColumn,
	int? CursorAfterRow,
	int? CursorAfterColumn,
	int ByteCount
);

/// <summary>
/// Selects exact whole-row insertion, deletion, and scrolling operations when they reproduce the
/// complete desired screen and are strictly cheaper than a conservative direct-rewrite lower bound.
/// </summary>
internal sealed class CursesLineShiftResolver {
	private readonly TerminalDescription terminal;
	private readonly CursesOutputCostModel costModel;
	private readonly CursesCursorMotionResolver cursorMotionResolver;

	internal CursesLineShiftResolver(
		TerminalDescription terminal,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( costModel );

		this.terminal = terminal;
		this.costModel = costModel;
		cursorMotionResolver = new CursesCursorMotionResolver( terminal );
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
		if ( !currentStyle.HasValue
			|| CursesStyle.Default != currentStyle.Value ) {
			return null;
		}

		if ( !TryFindDifferenceRange(
			desired,
			physicalScreen,
			out int firstDifferenceRow,
			out int lastDifferenceRow
		) ) {
			return null;
		}

		int rewriteLowerBound = GetChangedNonblankByteCount(
			desired,
			physicalScreen,
			firstDifferenceRow,
			lastDifferenceRow
		);
		if ( 0 >= rewriteLowerBound ) {
			return null;
		}

		CursesLineShiftPlan? best = null;
		for ( int bottomRow = lastDifferenceRow; bottomRow < desired.Rows; bottomRow++ ) {
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
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		bool reachesScreenBottom = bottomRow + 1 == desired.Rows;
		if ( reachesScreenBottom ) {
			TryAddLineOperationCandidate(
				ref best,
				CursesLineShiftKind.Insert,
				CursesLineShiftOperation.InsertLines,
				StringCapability.InsertLines,
				StringCapability.InsertLine,
				desired,
				topRow,
				bottomRow,
				count,
				temporaryScrollRegion: false,
				operationRow: topRow,
				rewriteLowerBound,
				currentCursorRow,
				currentCursorColumn,
				requestedCursorRow,
				requestedCursorColumn
			);
		} else {
			TryAddLineOperationCandidate(
				ref best,
				CursesLineShiftKind.Insert,
				CursesLineShiftOperation.InsertLines,
				StringCapability.InsertLines,
				StringCapability.InsertLine,
				desired,
				topRow,
				bottomRow,
				count,
				temporaryScrollRegion: true,
				operationRow: topRow,
				rewriteLowerBound,
				currentCursorRow,
				currentCursorColumn,
				requestedCursorRow,
				requestedCursorColumn
			);
		}

		bool fullScreenRegion = 0 == topRow
			&& bottomRow + 1 == desired.Rows;
		TryAddScrollCandidate(
			ref best,
			CursesLineShiftKind.Insert,
			CursesLineShiftOperation.ScrollReverse,
			StringCapability.ScrollReverseLines,
			StringCapability.ScrollReverse,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !fullScreenRegion,
			operationRow: topRow,
			rewriteLowerBound,
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
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		bool reachesScreenBottom = bottomRow + 1 == desired.Rows;
		if ( reachesScreenBottom ) {
			TryAddLineOperationCandidate(
				ref best,
				CursesLineShiftKind.Delete,
				CursesLineShiftOperation.DeleteLines,
				StringCapability.DeleteLines,
				StringCapability.DeleteLine,
				desired,
				topRow,
				bottomRow,
				count,
				temporaryScrollRegion: false,
				operationRow: topRow,
				rewriteLowerBound,
				currentCursorRow,
				currentCursorColumn,
				requestedCursorRow,
				requestedCursorColumn
			);
		} else {
			TryAddLineOperationCandidate(
				ref best,
				CursesLineShiftKind.Delete,
				CursesLineShiftOperation.DeleteLines,
				StringCapability.DeleteLines,
				StringCapability.DeleteLine,
				desired,
				topRow,
				bottomRow,
				count,
				temporaryScrollRegion: true,
				operationRow: topRow,
				rewriteLowerBound,
				currentCursorRow,
				currentCursorColumn,
				requestedCursorRow,
				requestedCursorColumn
			);
		}

		bool fullScreenRegion = 0 == topRow
			&& bottomRow + 1 == desired.Rows;
		TryAddScrollCandidate(
			ref best,
			CursesLineShiftKind.Delete,
			CursesLineShiftOperation.ScrollForward,
			StringCapability.ScrollForwardLines,
			StringCapability.ScrollForward,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion: !fullScreenRegion,
			operationRow: bottomRow,
			rewriteLowerBound,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
	}

	private void TryAddLineOperationCandidate(
		ref CursesLineShiftPlan? best,
		CursesLineShiftKind kind,
		CursesLineShiftOperation operation,
		StringCapability parameterizedCapability,
		StringCapability oneCapability,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		bool temporaryScrollRegion,
		int operationRow,
		int rewriteLowerBound,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		TryAddCandidate(
			ref best,
			kind,
			operation,
			parameterizedCapability,
			oneCapability,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion,
			operationRow,
			rewriteLowerBound,
			currentCursorRow,
			currentCursorColumn,
			requestedCursorRow,
			requestedCursorColumn
		);
	}

	private void TryAddScrollCandidate(
		ref CursesLineShiftPlan? best,
		CursesLineShiftKind kind,
		CursesLineShiftOperation operation,
		StringCapability parameterizedCapability,
		StringCapability oneCapability,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		bool temporaryScrollRegion,
		int operationRow,
		int rewriteLowerBound,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		TryAddCandidate(
			ref best,
			kind,
			operation,
			parameterizedCapability,
			oneCapability,
			desired,
			topRow,
			bottomRow,
			count,
			temporaryScrollRegion,
			operationRow,
			rewriteLowerBound,
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
		StringCapability parameterizedCapability,
		StringCapability oneCapability,
		CursesVirtualScreen desired,
		int topRow,
		int bottomRow,
		int count,
		bool temporaryScrollRegion,
		int operationRow,
		int rewriteLowerBound,
		int? currentCursorRow,
		int? currentCursorColumn,
		int requestedCursorRow,
		int requestedCursorColumn
	) {
		int regionHeight = bottomRow - topRow + 1;
		if ( !TryResolveOperation(
			parameterizedCapability,
			oneCapability,
			count,
			regionHeight,
			out string operationSequence,
			out int operationByteCount
		) ) {
			return;
		}

		string? setRegionSequence = null;
		string? restoreRegionSequence = null;
		int setRegionAffectedLines = 0;
		int restoreRegionAffectedLines = 0;
		int setRegionByteCount = 0;
		int restoreRegionByteCount = 0;
		int? motionStartRow = currentCursorRow;
		int? motionStartColumn = currentCursorColumn;
		if ( temporaryScrollRegion ) {
			if ( !TryExpandScrollRegion(
				topRow,
				bottomRow,
				regionHeight,
				out setRegionSequence,
				out setRegionByteCount
			) || !TryExpandScrollRegion(
				0,
				desired.Rows - 1,
				desired.Rows,
				out restoreRegionSequence,
				out restoreRegionByteCount
			) ) {
				return;
			}
			setRegionAffectedLines = regionHeight;
			restoreRegionAffectedLines = desired.Rows;
			motionStartRow = null;
			motionStartColumn = null;
		}

		CursesCursorMotion operationMotion;
		try {
			operationMotion = cursorMotionResolver.Resolve(
				motionStartRow,
				motionStartColumn,
				operationRow,
				0
			);
		} catch ( NotSupportedException ) {
			return;
		}

		int? cursorAfterRow = operationRow;
		int? cursorAfterColumn = 0;
		if ( temporaryScrollRegion ) {
			cursorAfterRow = null;
			cursorAfterColumn = null;
		}

		CursesCursorMotion finalMotion;
		try {
			finalMotion = cursorMotionResolver.Resolve(
				cursorAfterRow,
				cursorAfterColumn,
				requestedCursorRow,
				requestedCursorColumn
			);
		} catch ( NotSupportedException ) {
			return;
		}

		int totalByteCount = checked(
			setRegionByteCount
			+ operationMotion.ByteCount
			+ operationByteCount
			+ restoreRegionByteCount
			+ finalMotion.ByteCount
		);
		if ( totalByteCount >= rewriteLowerBound ) {
			return;
		}

		CursesLineShiftPlan candidate = new(
			kind,
			operation,
			topRow,
			bottomRow,
			count,
			operationSequence,
			regionHeight,
			temporaryScrollRegion,
			setRegionSequence,
			setRegionAffectedLines,
			restoreRegionSequence,
			restoreRegionAffectedLines,
			operationRow,
			0,
			cursorAfterRow,
			cursorAfterColumn,
			totalByteCount
		);
		ChooseBetter( ref best, candidate );
	}

	private bool TryExpandScrollRegion(
		int topRow,
		int bottomRow,
		int affectedLines,
		out string? sequence,
		out int byteCount
	) {
		string? capability = this.terminal.GetString(
			StringCapability.ChangeScrollRegion
		);
		if ( string.IsNullOrEmpty( capability ) ) {
			sequence = null;
			byteCount = 0;
			return false;
		}

		string expanded = this.terminal.Expand(
			StringCapability.ChangeScrollRegion,
			topRow,
			bottomRow
		);
		if ( 0 == expanded.Length ) {
			sequence = null;
			byteCount = 0;
			return false;
		}

		sequence = expanded;
		byteCount = CursesOutputCostModel.GetTerminalStringByteCount(
			expanded,
			affectedLines
		);
		return true;
	}

	private bool TryResolveOperation(
		StringCapability parameterizedCapability,
		StringCapability oneCapability,
		int count,
		int affectedLines,
		out string sequence,
		out int byteCount
	) {
		if ( 0 >= count ) {
			throw new ArgumentOutOfRangeException( nameof( count ) );
		}
		if ( 0 >= affectedLines ) {
			throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
		}

		string? bestSequence = null;
		int bestByteCount = int.MaxValue;
		string? parameterized = this.terminal.GetString( parameterizedCapability );
		if ( !string.IsNullOrEmpty( parameterized ) ) {
			string expanded = this.terminal.Expand(
				parameterizedCapability,
				count
			);
			if ( 0 < expanded.Length ) {
				bestSequence = expanded;
				bestByteCount = CursesOutputCostModel.GetTerminalStringByteCount(
					expanded,
					affectedLines
				);
			}
		}

		string? one = this.terminal.GetString( oneCapability );
		if ( !string.IsNullOrEmpty( one ) ) {
			StringBuilder repeated = new(
				checked( one.Length * count )
			);
			for ( int index = 0; index < count; index++ ) {
				repeated.Append( one );
			}
			string repeatedSequence = repeated.ToString();
			int repeatedByteCount = CursesOutputCostModel.GetTerminalStringByteCount(
				repeatedSequence,
				affectedLines
			);
			if ( repeatedByteCount < bestByteCount ) {
				bestSequence = repeatedSequence;
				bestByteCount = repeatedByteCount;
			}
		}

		if ( null == bestSequence ) {
			sequence = string.Empty;
			byteCount = 0;
			return false;
		}

		sequence = bestSequence;
		byteCount = bestByteCount;
		return true;
	}

	private bool TryFindDifferenceRange(
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
			|| candidate.ByteCount < current.Value.ByteCount ) {
			current = candidate;
		}
	}
}
