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

/// <summary>Builds one ordered Terminal-owned semantic screen-output transaction.</summary>
internal sealed class CursesPreparedRefresh {
	private readonly TerminalScreenOutputTransaction transaction;
	private readonly CursesRefreshDiagnosticsAccumulator? diagnostics;

	internal CursesPreparedRefresh(
		TerminalSession session,
		bool useSynchronizedOutput,
		CursesRefreshDiagnosticsAccumulator? diagnostics = null
	) {
		ArgumentNullException.ThrowIfNull( session );
		this.transaction = session.CreateScreenOutputTransaction(
			new TerminalScreenOutputTransactionOptions {
				UseSynchronizedOutput = useSynchronizedOutput
			}
		);
		this.diagnostics = diagnostics;
	}

	internal void AddPlan( TerminalScreenOperationPlan plan,
		CursesRefreshOperationKinds kind = CursesRefreshOperationKinds.None ) {
		this.transaction.Add( plan );
		this.diagnostics?.RecordItem( kind );
	}

	internal void WriteText( string value ) {
		this.transaction.WriteText( value );
		this.diagnostics?.RecordPayload( CursesRefreshOperationKinds.Text );
	}

	internal void WriteHyperlink(
		string value,
		CursesHyperlink hyperlink
	) {
		ArgumentNullException.ThrowIfNull( hyperlink );
		this.transaction.WriteHyperlink(
			value,
			hyperlink.Uri,
			hyperlink.Identifier
		);
		this.diagnostics?.RecordPayload( CursesRefreshOperationKinds.Text | CursesRefreshOperationKinds.Hyperlink );
	}

	internal void WriteRasterPlaceholderCell(
		CursesRasterCell cell
	) {
		this.transaction.WriteRasterPlaceholderCell( cell.TerminalCell );
		this.diagnostics?.RecordRasterCells( 1 );
	}

	internal void WriteRasterPlaceholderCells(
		ReadOnlyMemory<CursesRasterCell> cells
	) {
		TerminalRasterPlaceholderCell[] terminalCells =
			new TerminalRasterPlaceholderCell[ cells.Length ];
		ReadOnlySpan<CursesRasterCell> source = cells.Span;
		for ( int index = 0; index < source.Length; index++ ) {
			terminalCells[ index ] = source[ index ].TerminalCell;
		}
		this.transaction.WriteRasterPlaceholderCells( terminalCells );
		this.diagnostics?.RecordRasterCells( cells.Length );
	}

	internal ValueTask CommitAsync(
		CancellationToken cancellationToken = default
	) {
		return this.transaction.CommitAsync( cancellationToken );
	}
}
