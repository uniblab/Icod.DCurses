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

/// <summary>Identifies a row-local physical character shift.</summary>
internal enum CursesCharacterShiftKind {
	Insert,
	Delete
}

/// <summary>Represents one exact row-local character shift selected by deterministic byte cost.</summary>
internal readonly record struct CursesCharacterShiftPlan(
	CursesCharacterShiftKind Kind,
	int Row,
	int Column,
	int Count,
	string Sequence,
	int ByteCount
);

/// <summary>
/// Selects exact insert/delete-character operations when they reproduce one desired row and are
/// strictly cheaper than rewriting the changed nonblank payload.
/// </summary>
internal sealed class CursesCharacterShiftResolver {
	private readonly TerminalDescription terminal;
	private readonly CursesOutputCostModel costModel;

	internal CursesCharacterShiftResolver(
		TerminalDescription terminal,
		CursesOutputCostModel costModel
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( costModel );

		this.terminal = terminal;
		this.costModel = costModel;
	}

	internal CursesCharacterShiftPlan? Resolve(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		CursesStyle? currentStyle
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

		if ( !currentStyle.HasValue
			|| CursesStyle.Default != currentStyle.Value ) {
			return null;
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
		) ) {
			return null;
		}

		int rewriteLowerBound = GetChangedNonblankByteCount(
			desired,
			physicalScreen,
			row,
			firstDifference
		);
		if ( 0 >= rewriteLowerBound ) {
			return null;
		}

		CursesCharacterShiftPlan? best = ResolveInsertion(
			desired,
			physicalScreen,
			row,
			firstDifference,
			rewriteLowerBound
		);
		CursesCharacterShiftPlan? deletion = ResolveDeletion(
			desired,
			physicalScreen,
			row,
			firstDifference,
			rewriteLowerBound
		);
		if ( deletion.HasValue
			&& ( !best.HasValue
				|| deletion.Value.ByteCount < best.Value.ByteCount ) ) {
			best = deletion;
		}
		return best;
	}

	private CursesCharacterShiftPlan? ResolveInsertion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int rewriteLowerBound
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
			if ( !TryResolveOperation(
				StringCapability.InsertCharacters,
				StringCapability.InsertCharacter,
				count,
				out string sequence,
				out int byteCount
			) || byteCount >= rewriteLowerBound ) {
				continue;
			}

			CursesCharacterShiftPlan candidate = new(
				CursesCharacterShiftKind.Insert,
				row,
				startColumn,
				count,
				sequence,
				byteCount
			);
			ChooseBetter( ref best, candidate );
		}
		return best;
	}

	private CursesCharacterShiftPlan? ResolveDeletion(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physicalScreen,
		int row,
		int startColumn,
		int rewriteLowerBound
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
			if ( !TryResolveOperation(
				StringCapability.DeleteCharacters,
				StringCapability.DeleteCharacter,
				count,
				out string sequence,
				out int byteCount
			) || byteCount >= rewriteLowerBound ) {
				continue;
			}

			CursesCharacterShiftPlan candidate = new(
				CursesCharacterShiftKind.Delete,
				row,
				startColumn,
				count,
				sequence,
				byteCount
			);
			ChooseBetter( ref best, candidate );
		}
		return best;
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

	private static bool IsSimpleCell(
		CursesCell cell
	) {
		return !cell.IsContinuation
			&& 1 == cell.DisplayWidth
			&& !cell.IsLineGlyph;
	}

	private static bool IsDefaultBlank(
		CursesCell cell
	) {
		return cell.IsBlank
			&& cell.Style.IsDefault;
	}

	private bool TryResolveOperation(
		StringCapability parameterizedCapability,
		StringCapability oneCapability,
		int count,
		out string sequence,
		out int byteCount
	) {
		if ( 0 >= count ) {
			throw new ArgumentOutOfRangeException( nameof( count ) );
		}

		string? parameterized = this.terminal.GetString( parameterizedCapability );
		string? bestSequence = null;
		int bestByteCount = int.MaxValue;
		if ( !string.IsNullOrEmpty( parameterized ) ) {
			string expanded = this.terminal.Expand(
				parameterizedCapability,
				count
			);
			if ( 0 < expanded.Length ) {
				bestSequence = expanded;
				bestByteCount = CursesOutputCostModel.GetTerminalStringByteCount(
					expanded
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
				repeatedSequence
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

	private static void ChooseBetter(
		ref CursesCharacterShiftPlan? current,
		CursesCharacterShiftPlan candidate
	) {
		if ( !current.HasValue
			|| candidate.ByteCount < current.Value.ByteCount ) {
			current = candidate;
		}
	}
}
