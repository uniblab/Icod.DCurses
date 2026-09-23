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
using Icod.TermInfo;
using Xunit;

/// <summary>Verifies cost-aware line-shift selection through the retained refresh engine.</summary>
public sealed class CursesLineShiftRefreshTests {
	[Fact]
	public async Task DeleteLinesUsesTerminalPlanAndRetainsExactScreen() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( 8, "A", "B", "C", "D" );
		screen.StandardWindow.Move( 1, 0 );
		await engine.RefreshAsync( screen, 1, 0 );
		output.Clear();

		screen.StandardWindow.DeleteLines();
		await engine.RefreshAsync( screen, 1, 0 );

		Assert.Equal( "D1", output.Text );
		Assert.Equal(
			new[] { "AAAAAAAA", "CCCCCCCC", "DDDDDDDD", "        " },
			ReadRows( screen )
		);
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 1, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task InsertLinesUsesTerminalPlanAndRetainsExactScreen() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( 8, "A", "B", "C", "D" );
		screen.StandardWindow.Move( 1, 0 );
		await engine.RefreshAsync( screen, 1, 0 );
		output.Clear();

		screen.StandardWindow.InsertLines();
		await engine.RefreshAsync( screen, 1, 0 );

		Assert.Equal( "I1", output.Text );
		Assert.Equal(
			new[] { "AAAAAAAA", "        ", "BBBBBBBB", "CCCCCCCC" },
			ReadRows( screen )
		);
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 1, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task InteriorWindowUsesOrderedTemporaryScrollRegion() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateInteriorTerminal(),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( 8, "A", "B", "C", "D", "E" );
		CursesWindow editor = screen.CreateWindow( 1, 0, 2, 8 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		editor.Move( 0, 0 );
		editor.DeleteLines();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( "RCLRC", output.Text );
		Assert.Equal(
			new[] { "AAAAAAAA", "CCCCCCCC", "        ", "DDDDDDDD", "EEEEEEEE" },
			ReadRows( screen )
		);
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task FullScreenDeleteUsesScrollForwardWhenCheaper() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "full-scroll" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ScrollForwardLines, "F%p1%d" )
			.Build();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( terminal, output );
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( 8, "A", "B", "C", "D" );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.StandardWindow.DeleteLines();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( "CF1C", output.Text );
		Assert.Equal(
			new[] { "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "        " },
			ReadRows( screen )
		);
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "line-selection" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.DeleteLines, "D%p1%d" )
			.SetString( StringCapability.InsertLines, "I%p1%d" )
			.Build();
	}

	private static TerminalDescription CreateInteriorTerminal() {
		return new TerminalDescriptionBuilder( "interior-line-selection" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ChangeScrollRegion, "R" )
			.SetString( StringCapability.DeleteLine, "L" )
			.Build();
	}

	private static CursesScreen CreateScreen(
		int columns,
		params string[] rowValues
	) {
		CursesScreen screen = new( columns, rowValues.Length );
		for ( int row = 0; row < rowValues.Length; row++ ) {
			for ( int column = 0; column < columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell(
					rowValues[ row ]
				);
			}
		}
		return screen;
	}

	private static string[] ReadRows( CursesScreen screen ) {
		ArgumentNullException.ThrowIfNull( screen );
		string[] result = new string[ screen.Rows ];
		for ( int row = 0; row < screen.Rows; row++ ) {
			StringBuilder value = new();
			for ( int column = 0; column < screen.Columns; column++ ) {
				CursesCell cell = screen.VirtualScreen[ row, column ];
				value.Append( cell.IsBlank ? " " : cell.Content );
			}
			result[ row ] = value.ToString();
		}
		return result;
	}

	private sealed class RecordingOutput : ILegacyTerminalOutputFixture {
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
