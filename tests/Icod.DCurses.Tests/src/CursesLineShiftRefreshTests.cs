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

/// <summary>Freezes the T2003 ordinary-rewrite fallback for line-shift edits.</summary>
public sealed class CursesLineShiftRefreshTests {
	[Fact]
	public async Task DeleteLinesUsesOrdinaryRewriteUntilT2004() {
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

		Assert.DoesNotContain( "<delete-lines:", output.Text );
		Assert.DoesNotContain( "<scroll-forward:", output.Text );
		Assert.Contains( new string( 'C', 8 ), output.Text );
		Assert.Contains( new string( 'D', 8 ), output.Text );
		Assert.Contains( new string( ' ', 8 ), output.Text );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 1, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task InsertLinesUsesOrdinaryRewriteUntilT2004() {
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

		Assert.DoesNotContain( "<insert-lines:", output.Text );
		Assert.DoesNotContain( "<scroll-reverse:", output.Text );
		Assert.Contains( new string( ' ', 8 ), output.Text );
		Assert.Contains( new string( 'B', 8 ), output.Text );
		Assert.Contains( new string( 'C', 8 ), output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task InteriorWindowDoesNotUseTemporaryScrollRegionUntilT2004() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
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

		Assert.DoesNotContain( "<region:", output.Text );
		Assert.DoesNotContain( "<delete-line>", output.Text );
		Assert.Contains( new string( 'C', 8 ), output.Text );
		Assert.Contains( new string( ' ', 8 ), output.Text );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "line-rewrite" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.DeleteLines, "<delete-lines:%p1%d>" )
			.SetString( StringCapability.InsertLines, "<insert-lines:%p1%d>" )
			.SetString( StringCapability.ScrollForwardLines, "<scroll-forward:%p1%d>" )
			.SetString( StringCapability.ScrollReverseLines, "<scroll-reverse:%p1%d>" )
			.SetString( StringCapability.ChangeScrollRegion, "<region:%p1%d,%p2%d>" )
			.SetString( StringCapability.DeleteLine, "<delete-line>" )
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

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => this.text.ToString();

		internal void Clear() {
			this.text.Clear();
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
