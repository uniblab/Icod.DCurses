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

/// <summary>Represents one safely expanded physical cursor-motion sequence and its byte cost.</summary>
internal readonly record struct CursesCursorMotion(
	string Sequence,
	int ByteCount
);

/// <summary>Selects the shortest safe advertised cursor-motion sequence.</summary>
internal sealed class CursesCursorMotionResolver {
	private readonly TerminalDescription terminal;

	internal CursesCursorMotionResolver(
		TerminalDescription terminal
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		this.terminal = terminal;
	}

	internal CursesCursorMotion Resolve(
		int? currentRow,
		int? currentColumn,
		int targetRow,
		int targetColumn
	) {
		if ( 0 > targetRow ) {
			throw new ArgumentOutOfRangeException( nameof( targetRow ) );
		}
		if ( 0 > targetColumn ) {
			throw new ArgumentOutOfRangeException( nameof( targetColumn ) );
		}

		CursesCursorMotion? best = null;
		if ( TryExpand(
			StringCapability.CursorAddress,
			[ targetRow, targetColumn ],
			out CursesCursorMotion absolute
		) ) {
			best = absolute;
		}

		if ( 0 == targetRow
			&& 0 == targetColumn
			&& TryLiteral(
				StringCapability.CursorHome,
				out CursesCursorMotion home
			) ) {
			ChooseBetter( ref best, home );
		}

		if ( TryExpand(
			StringCapability.RowAddress,
			[ targetRow ],
			out CursesCursorMotion rowAddress
		) && TryExpand(
			StringCapability.ColumnAddress,
			[ targetColumn ],
			out CursesCursorMotion columnAddress
		) ) {
			ChooseBetter(
				ref best,
				Combine( rowAddress, columnAddress )
			);
		}

		if ( currentRow.HasValue && currentColumn.HasValue ) {
			int rowDelta = targetRow - currentRow.Value;
			int columnDelta = targetColumn - currentColumn.Value;

			if ( 0 == rowDelta ) {
				if ( 0 == targetColumn
					&& TryLiteral(
						StringCapability.CarriageReturn,
						out CursesCursorMotion carriageReturn
					) ) {
					ChooseBetter( ref best, carriageReturn );
				}

				if ( TryExpand(
					StringCapability.ColumnAddress,
					[ targetColumn ],
					out CursesCursorMotion sameRowColumnAddress
				) ) {
					ChooseBetter( ref best, sameRowColumnAddress );
				}
			}

			if ( 0 == columnDelta
				&& TryExpand(
					StringCapability.RowAddress,
					[ targetRow ],
					out CursesCursorMotion sameColumnRowAddress
				) ) {
				ChooseBetter( ref best, sameColumnRowAddress );
			}

			CursesCursorMotion? vertical = ResolveRelativeAxis(
				rowDelta,
				StringCapability.CursorUp,
				StringCapability.CursorUpOne,
				StringCapability.CursorDown,
				StringCapability.CursorDownOne
			);
			CursesCursorMotion? horizontal = ResolveRelativeAxis(
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

	private CursesCursorMotion? ResolveRelativeAxis(
		int delta,
		StringCapability negativeParameterized,
		StringCapability negativeOne,
		StringCapability positiveParameterized,
		StringCapability positiveOne
	) {
		if ( 0 == delta ) {
			return new CursesCursorMotion(
				string.Empty,
				0
			);
		}

		int count = Math.Abs( delta );
		StringCapability parameterized = 0 > delta
			? negativeParameterized
			: positiveParameterized;
		StringCapability one = 0 > delta
			? negativeOne
			: positiveOne;

		CursesCursorMotion? best = null;
		if ( TryExpand(
			parameterized,
			[ count ],
			out CursesCursorMotion parameterizedMotion
		) ) {
			best = parameterizedMotion;
		}
		if ( TryRepeatedLiteral(
			one,
			count,
			out CursesCursorMotion repeatedMotion
		) ) {
			ChooseBetter( ref best, repeatedMotion );
		}
		return best;
	}

	private bool TryExpand(
		StringCapability capability,
		TermInfoParameter[] parameters,
		out CursesCursorMotion motion
	) {
		ArgumentNullException.ThrowIfNull( parameters );
		if ( null == this.terminal.GetString( capability ) ) {
			motion = default;
			return false;
		}

		string sequence = this.terminal.Expand(
			capability,
			parameters
		);
		motion = CreateMotion( sequence );
		return true;
	}

	private bool TryLiteral(
		StringCapability capability,
		out CursesCursorMotion motion
	) {
		string? sequence = this.terminal.GetString( capability );
		if ( null == sequence ) {
			motion = default;
			return false;
		}

		motion = CreateMotion( sequence );
		return true;
	}

	private bool TryRepeatedLiteral(
		StringCapability capability,
		int count,
		out CursesCursorMotion motion
	) {
		if ( 0 >= count ) {
			throw new ArgumentOutOfRangeException( nameof( count ) );
		}

		string? sequence = this.terminal.GetString( capability );
		if ( null == sequence ) {
			motion = default;
			return false;
		}

		StringBuilder repeated = new(
			checked( sequence.Length * count )
		);
		for ( int index = 0; index < count; index++ ) {
			repeated.Append( sequence );
		}
		motion = CreateMotion( repeated.ToString() );
		return true;
	}

	private static CursesCursorMotion Combine(
		CursesCursorMotion first,
		CursesCursorMotion second
	) {
		return new CursesCursorMotion(
			first.Sequence + second.Sequence,
			checked( first.ByteCount + second.ByteCount )
		);
	}

	private static CursesCursorMotion CreateMotion(
		string sequence
	) {
		ArgumentNullException.ThrowIfNull( sequence );
		return new CursesCursorMotion(
			sequence,
			CursesOutputCostModel.GetTerminalStringByteCount( sequence )
		);
	}

	private static void ChooseBetter(
		ref CursesCursorMotion? current,
		CursesCursorMotion candidate
	) {
		if ( !current.HasValue
			|| candidate.ByteCount < current.Value.ByteCount ) {
			current = candidate;
		}
	}
}
