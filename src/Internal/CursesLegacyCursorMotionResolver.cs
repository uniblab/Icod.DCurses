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

/// <summary>Retains T2004's legacy line-shift cursor-cost input while refresh bypasses it.</summary>
internal readonly record struct CursesLegacyCursorMotion(
	string Sequence,
	int ByteCount
);

/// <summary>Calculates legacy cursor costs solely for the deferred T2004 line-shift resolver.</summary>
internal sealed class CursesLegacyCursorMotionResolver {
	private readonly TerminalDescription terminal;

	internal CursesLegacyCursorMotionResolver( TerminalDescription terminal ) {
		ArgumentNullException.ThrowIfNull( terminal );
		this.terminal = terminal;
	}

	internal CursesLegacyCursorMotion Resolve(
		int? currentRow,
		int? currentColumn,
		int targetRow,
		int targetColumn
	) {
		CursesLegacyCursorMotion? best = null;
		TryChooseExpanded(
			ref best,
			StringCapability.CursorAddress,
			[ targetRow, targetColumn ]
		);
		if ( 0 == targetRow && 0 == targetColumn ) {
			TryChooseLiteral(
				ref best,
				StringCapability.CursorHome
			);
		}

		CursesLegacyCursorMotion? row = TryExpand(
			StringCapability.RowAddress,
			[ targetRow ]
		);
		CursesLegacyCursorMotion? column = TryExpand(
			StringCapability.ColumnAddress,
			[ targetColumn ]
		);
		if ( row.HasValue && column.HasValue ) {
			ChooseBetter(
				ref best,
				Combine( row.Value, column.Value )
			);
		}

		if ( currentRow.HasValue && currentColumn.HasValue ) {
			int rowDelta = targetRow - currentRow.Value;
			int columnDelta = targetColumn - currentColumn.Value;
			if ( 0 == rowDelta ) {
				if ( 0 == targetColumn ) {
					TryChooseLiteral(
						ref best,
						StringCapability.CarriageReturn
					);
				}
				TryChooseExpanded(
					ref best,
					StringCapability.ColumnAddress,
					[ targetColumn ]
				);
			}
			if ( 0 == columnDelta ) {
				TryChooseExpanded(
					ref best,
					StringCapability.RowAddress,
					[ targetRow ]
				);
			}

			CursesLegacyCursorMotion? vertical = ResolveRelativeAxis(
				rowDelta,
				StringCapability.CursorUp,
				StringCapability.CursorUpOne,
				StringCapability.CursorDown,
				StringCapability.CursorDownOne
			);
			CursesLegacyCursorMotion? horizontal = ResolveRelativeAxis(
				columnDelta,
				StringCapability.CursorLeft,
				StringCapability.CursorLeftOne,
				StringCapability.CursorRight,
				StringCapability.CursorRightOne
			);
			if ( vertical.HasValue && horizontal.HasValue ) {
				ChooseBetter(
					ref best,
					Combine( vertical.Value, horizontal.Value )
				);
			}
		}

		return best
			?? throw new NotSupportedException(
				$"Terminal '{this.terminal.Name}' does not provide a safe cursor-motion capability for the requested position."
			);
	}

	private CursesLegacyCursorMotion? ResolveRelativeAxis(
		int delta,
		StringCapability negativeParameterized,
		StringCapability negativeOne,
		StringCapability positiveParameterized,
		StringCapability positiveOne
	) {
		if ( 0 == delta ) {
			return new CursesLegacyCursorMotion( string.Empty, 0 );
		}

		int count = Math.Abs( delta );
		CursesLegacyCursorMotion? best = TryExpand(
			0 > delta
				? negativeParameterized
				: positiveParameterized,
			[ count ]
		);
		string? literal = this.terminal.GetString(
			0 > delta
				? negativeOne
				: positiveOne
		);
		if ( literal is not null ) {
			StringBuilder repeated = new(
				checked( literal.Length * count )
			);
			for ( int index = 0; index < count; index++ ) {
				repeated.Append( literal );
			}
			ChooseBetter(
				ref best,
				Create( repeated.ToString() )
			);
		}
		return best;
	}

	private void TryChooseExpanded(
		ref CursesLegacyCursorMotion? best,
		StringCapability capability,
		TermInfoParameter[] parameters
	) {
		CursesLegacyCursorMotion? candidate = TryExpand(
			capability,
			parameters
		);
		if ( candidate.HasValue ) {
			ChooseBetter( ref best, candidate.Value );
		}
	}

	private void TryChooseLiteral(
		ref CursesLegacyCursorMotion? best,
		StringCapability capability
	) {
		string? sequence = this.terminal.GetString( capability );
		if ( sequence is not null ) {
			ChooseBetter( ref best, Create( sequence ) );
		}
	}

	private CursesLegacyCursorMotion? TryExpand(
		StringCapability capability,
		TermInfoParameter[] parameters
	) {
		return this.terminal.GetString( capability ) is null
			? null
			: Create(
				this.terminal.Expand(
					capability,
					parameters
				)
			);
	}

	private static CursesLegacyCursorMotion Combine(
		CursesLegacyCursorMotion first,
		CursesLegacyCursorMotion second
	) {
		return new CursesLegacyCursorMotion(
			first.Sequence + second.Sequence,
			checked( first.ByteCount + second.ByteCount )
		);
	}

	private static CursesLegacyCursorMotion Create( string sequence ) {
		return new CursesLegacyCursorMotion(
			sequence,
			GetLegacyTerminalStringByteCount( sequence )
		);
	}

	// Temporary compatibility until Task 4 removes this legacy resolver.
	private static int GetLegacyTerminalStringByteCount( string value ) {
		int byteCount = 0;
		TermInfoOutput.TPuts(
			value,
			1,
			_ => byteCount = checked( byteCount + 1 )
		);
		return byteCount;
	}

	private static void ChooseBetter(
		ref CursesLegacyCursorMotion? current,
		CursesLegacyCursorMotion candidate
	) {
		if ( !current.HasValue
			|| candidate.ByteCount < current.Value.ByteCount ) {
			current = candidate;
		}
	}
}
