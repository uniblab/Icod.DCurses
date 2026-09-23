/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses.Tests;

using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Verifies cost-aware erase selection through the retained refresh engine.</summary>
public sealed class CursesEraseIntegrationTests {
	[Fact]
	public async Task BlankTailUsesTerminalEraseToEndOfLineWhenCheaper() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateFilledScreen( 40, 1 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 3; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<el>", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task BlankScreenTailUsesTerminalEraseToEndOfScreenWhenCheaper() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateFilledScreen( 40, 3 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 2; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 1, column ] = CursesCell.Blank();
		}
		for ( int column = 0; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 2, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<ed>", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task WholeBlankScreenUsesTerminalClearWhenCheaper() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateFilledScreen( 40, 3 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<clear>", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "erase-selection" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.ClearToEndOfScreen, "<ed>........" )
			.SetString( StringCapability.ClearScreen, "<clear>" )
			.Build();
	}

	private static CursesScreen CreateFilledScreen(
		int columns,
		int rows
	) {
		CursesScreen screen = new( columns, rows );
		for ( int row = 0; row < rows; row++ ) {
			for ( int column = 0; column < columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell( "x" );
			}
		}
		return screen;
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();

		internal int WriteCount {
			get;
			private set;
		}

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => this.text.ToString();

		internal void Clear() {
			this.text.Clear();
			this.WriteCount = 0;
			this.FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			return this.WriteTerminalStringAsync(
				value,
				cancellationToken: cancellationToken
			);
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			this.WriteCount++;
			this.text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.FlushCount++;
			return ValueTask.CompletedTask;
		}
	}
}
