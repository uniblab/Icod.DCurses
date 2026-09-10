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
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies whole-row line and scroll optimization through the retained refresh engine.</summary>
public sealed class CursesLineShiftRefreshTests {
	[Fact]
	public async Task DeleteLinesUsesPhysicalDeleteLinesAndRetainsExactScreen() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateDirectLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D"
		);
		screen.StandardWindow.Move( 1, 0 );
		await engine.RefreshAsync( screen, 1, 0 );
		output.Clear();

		screen.StandardWindow.DeleteLines();
		await engine.RefreshAsync( screen, 1, 0 );

		Assert.Equal( "D1", output.Text );
		Assert.Equal( 1, output.FlushCount );
		Assert.Contains(
			output.TerminalWrites,
			write => "D1" == write.Value
				&& 3 == write.AffectedLines
		);
		Assert.Equal( new string( 'A', 16 ), ReadRow( screen, 0 ) );
		Assert.Equal( new string( 'C', 16 ), ReadRow( screen, 1 ) );
		Assert.Equal( new string( 'D', 16 ), ReadRow( screen, 2 ) );
		Assert.Equal( new string( ' ', 16 ), ReadRow( screen, 3 ) );

		output.Clear();
		await engine.RefreshAsync( screen, 1, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task InsertLinesUsesPhysicalInsertLinesAndRetainsExactScreen() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateDirectLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D"
		);
		screen.StandardWindow.Move( 1, 0 );
		await engine.RefreshAsync( screen, 1, 0 );
		output.Clear();

		screen.StandardWindow.InsertLines();
		await engine.RefreshAsync( screen, 1, 0 );

		Assert.Equal( "I1", output.Text );
		Assert.Equal( 1, output.FlushCount );
		Assert.Contains(
			output.TerminalWrites,
			write => "I1" == write.Value
				&& 3 == write.AffectedLines
		);
		Assert.Equal( new string( 'A', 16 ), ReadRow( screen, 0 ) );
		Assert.Equal( new string( ' ', 16 ), ReadRow( screen, 1 ) );
		Assert.Equal( new string( 'B', 16 ), ReadRow( screen, 2 ) );
		Assert.Equal( new string( 'C', 16 ), ReadRow( screen, 3 ) );
	}

	[Fact]
	public async Task InteriorFullWidthWindowUsesTemporaryScrollRegionAndRestoresIt() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateBoundedLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D",
			"E"
		);
		CursesWindow editor = screen.CreateWindow(
			1,
			0,
			2,
			16
		);
		screen.StandardWindow.Move( 0, 0 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		editor.Move( 0, 0 );
		editor.DeleteLines();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( "R12CDR04C", output.Text );
		Assert.Contains(
			output.TerminalWrites,
			write => "R12" == write.Value
				&& 2 == write.AffectedLines
		);
		Assert.Contains(
			output.TerminalWrites,
			write => "D" == write.Value
				&& 2 == write.AffectedLines
		);
		Assert.Contains(
			output.TerminalWrites,
			write => "R04" == write.Value
				&& 5 == write.AffectedLines
		);
		Assert.Equal( new string( 'A', 16 ), ReadRow( screen, 0 ) );
		Assert.Equal( new string( 'C', 16 ), ReadRow( screen, 1 ) );
		Assert.Equal( new string( ' ', 16 ), ReadRow( screen, 2 ) );
		Assert.Equal( new string( 'D', 16 ), ReadRow( screen, 3 ) );
		Assert.Equal( new string( 'E', 16 ), ReadRow( screen, 4 ) );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task FullScreenDeleteCanPreferScrollForward() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "scroll-forward" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.DeleteLines, "VERYLONG%p1%d" )
			.SetString( StringCapability.ScrollForwardLines, "F%p1%d" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D"
		);
		screen.StandardWindow.Move( 0, 0 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.StandardWindow.DeleteLines();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( "CF1C", output.Text );
		Assert.DoesNotContain( "VERYLONG", output.Text );
		Assert.Contains(
			output.TerminalWrites,
			write => "F1" == write.Value
				&& 4 == write.AffectedLines
		);
	}

	[Fact]
	public async Task PartialWidthWindowDoesNotClaimTerminalScrollRegionOwnership() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateBoundedLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D"
		);
		CursesWindow editor = screen.CreateWindow(
			1,
			4,
			2,
			8
		);
		screen.StandardWindow.Move( 0, 0 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		editor.Move( 0, 0 );
		editor.DeleteLines();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.DoesNotContain(
			output.TerminalWrites,
			write => "D" == write.Value
		);
		Assert.DoesNotContain(
			output.TerminalWrites,
			write => write.Value.StartsWith(
				"R",
				StringComparison.Ordinal
			)
		);
		Assert.Contains( new string( 'C', 8 ), output.Text );
	}

	[Fact]
	public async Task FailedBoundedLineShiftRestoresFullScrollRegionBeforeRethrow() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateBoundedLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D",
			"E"
		);
		CursesWindow editor = screen.CreateWindow(
			1,
			0,
			2,
			16
		);
		screen.StandardWindow.Move( 0, 0 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();
		editor.Move( 0, 0 );
		editor.DeleteLines();
		output.ThrowOnWrites = new HashSet<int> {
			3
		};

		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);

		Assert.Equal( "R12CR04", output.Text );
		Assert.Contains(
			output.TerminalWrites,
			write => "R04" == write.Value
		);

		output.ThrowOnWrites = null;
		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.DoesNotContain( "R12", output.Text );
		Assert.Contains( new string( 'C', 16 ), output.Text );
	}

	[Fact]
	public async Task OperationAndScrollRegionRestorationFailuresAreBothPreserved() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateBoundedLineTerminal(),
			output
		);
		CursesScreen screen = CreateScreen(
			16,
			"A",
			"B",
			"C",
			"D",
			"E"
		);
		CursesWindow editor = screen.CreateWindow(
			1,
			0,
			2,
			16
		);
		screen.StandardWindow.Move( 0, 0 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();
		editor.Move( 0, 0 );
		editor.DeleteLines();
		output.ThrowOnWrites = new HashSet<int> {
			3,
			4
		};

		AggregateException failure = await Assert.ThrowsAsync<AggregateException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);

		Assert.Equal( 2, failure.InnerExceptions.Count );
		Assert.All(
			failure.InnerExceptions,
			inner => Assert.IsType<IOException>( inner )
		);
		Assert.Equal( "R12C", output.Text );
	}

	private static TerminalDescription CreateDirectLineTerminal() {
		return new TerminalDescriptionBuilder( "direct-line" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.DeleteLines, "D%p1%d" )
			.SetString( StringCapability.InsertLines, "I%p1%d" )
			.Build();
	}

	private static TerminalDescription CreateBoundedLineTerminal() {
		return new TerminalDescriptionBuilder( "bounded-line" )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ChangeScrollRegion, "R%p1%d%p2%d" )
			.SetString( StringCapability.DeleteLine, "D" )
			.SetString( StringCapability.InsertLine, "I" )
			.Build();
	}

	private static CursesScreen CreateScreen(
		int columns,
		params string[] rowValues
	) {
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}
		ArgumentNullException.ThrowIfNull( rowValues );
		if ( 0 == rowValues.Length ) {
			throw new ArgumentException(
				"At least one row value is required.",
				nameof( rowValues )
			);
		}

		CursesScreen screen = new( columns, rowValues.Length );
		for ( int row = 0; row < rowValues.Length; row++ ) {
			if ( 1 != rowValues[ row ].Length ) {
				throw new ArgumentException(
					"Each row value must contain exactly one character.",
					nameof( rowValues )
				);
			}
			for ( int column = 0; column < columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell(
					rowValues[ row ]
				);
			}
		}
		return screen;
	}

	private static string ReadRow(
		CursesScreen screen,
		int row
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( 0 > row || row >= screen.Rows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}

		StringBuilder result = new();
		for ( int column = 0; column < screen.Columns; column++ ) {
			CursesCell cell = screen.VirtualScreen[ row, column ];
			if ( cell.IsBlank ) {
				result.Append( ' ' );
			} else {
				result.Append( cell.Content );
			}
		}
		return result.ToString();
	}

	private readonly record struct RecordedTerminalWrite(
		string Value,
		int AffectedLines
	);

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();
		private int writeCount;

		internal HashSet<int>? ThrowOnWrites {
			get;
			set;
		}

		internal int FlushCount {
			get;
			private set;
		}

		internal List<RecordedTerminalWrite> TerminalWrites {
			get;
		} = [];

		internal string Text => text.ToString();

		internal void Clear() {
			text.Clear();
			writeCount = 0;
			FlushCount = 0;
			TerminalWrites.Clear();
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			return WriteCoreAsync(
				value,
				affectedLines: 1,
				terminalString: false,
				cancellationToken
			);
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
			return WriteCoreAsync(
				value,
				affectedLines,
				terminalString: true,
				cancellationToken
			);
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount++;
			return ValueTask.CompletedTask;
		}

		private ValueTask WriteCoreAsync(
			string value,
			int affectedLines,
			bool terminalString,
			CancellationToken cancellationToken
		) {
			cancellationToken.ThrowIfCancellationRequested();
			writeCount++;
			if ( null != ThrowOnWrites
				&& ThrowOnWrites.Contains( writeCount ) ) {
				throw new IOException(
					$"Synthetic T706 output failure at write {writeCount}."
				);
			}

			text.Append( value );
			if ( terminalString ) {
				TerminalWrites.Add(
					new RecordedTerminalWrite(
						value,
						affectedLines
					)
				);
			}
			return ValueTask.CompletedTask;
		}
	}
}
