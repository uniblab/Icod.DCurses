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

using System.Text;
using Icod.DCurses;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies erase selection at the retained-screen refresh boundary.</summary>
public sealed class CursesEraseIntegrationTests {
	[Fact]
	public async Task ShortBlankTailUsesLiteralFallbackWhenEraseLineCostsMore() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(
				eraseLine: "<el>",
				eraseScreen: null,
				clearScreen: null
			),
			output
		);
		CursesScreen screen = CreateFilledScreen( 6, 1 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 3; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.DoesNotContain( "<el>", output.Text );
		Assert.Contains( "   ", output.Text );
	}

	[Fact]
	public async Task DefaultBlankScreenTailUsesEraseToEndOfScreenAndRetainsKnowledge() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(
				eraseLine: "LLLL",
				eraseScreen: "D",
				clearScreen: "CCCCCCCC"
			),
			output
		);
		CursesScreen screen = CreateFilledScreen( 6, 3 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 2; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 1, column ] = CursesCell.Blank();
		}
		for ( int column = 0; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 2, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "D", output.Text );
		Assert.DoesNotContain( "LLLL", output.Text );
		Assert.Contains( 2, output.AffectedLines );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task WholeDefaultBlankScreenUsesClearAndRetainsKnowledge() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(
				eraseLine: "LLLL",
				eraseScreen: "DD",
				clearScreen: "C"
			),
			output
		);
		CursesScreen screen = CreateFilledScreen( 8, 3 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "C", output.Text );
		Assert.Contains( 3, output.AffectedLines );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 1, output.FlushCount );
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

	private static TerminalDescription CreateTerminal(
		string eraseLine,
		string? eraseScreen,
		string? clearScreen
	) {
		ArgumentNullException.ThrowIfNull( eraseLine );
		TerminalDescriptionBuilder builder = new TerminalDescriptionBuilder( "erase-integration" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, eraseLine );
		if ( null != eraseScreen ) {
			builder.SetString(
				StringCapability.ClearToEndOfScreen,
				eraseScreen
			);
		}
		if ( null != clearScreen ) {
			builder.SetString(
				StringCapability.ClearScreen,
				clearScreen
			);
		}
		return builder.Build();
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();

		internal string Text => text.ToString();

		internal List<int> AffectedLines {
			get;
		} = [];

		internal int FlushCount {
			get;
			private set;
		}

		internal void Clear() {
			text.Clear();
			AffectedLines.Clear();
			FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			if ( 0 >= affectedLines ) {
				throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
			}
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			AffectedLines.Add( affectedLines );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount++;
			return ValueTask.CompletedTask;
		}
	}
}
