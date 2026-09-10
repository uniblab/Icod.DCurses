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

/// <summary>Runs deterministic release-gate workloads for the accepted 0.7 optimization set.</summary>
public sealed class CursesOptimizationRegretTests {
	[Fact]
	public async Task LargeFullRepaintHasDeterministicOutputCost() {
		const int columns = 160;
		const int rows = 60;
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateCursorOnlyTerminal(),
			output
		);
		CursesScreen screen = new( columns, rows );
		FillScreen( screen, "X" );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 9661, output.ByteCount );
		Assert.Equal( 121, output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task ThousandSmallUpdatesRemainDeterministicAndBounded() {
		const int iterations = 1000;
		MeasuringOutput output = new();
		CursesRefreshEngine engine = new(
			CreateCursorOnlyTerminal(),
			output
		);
		CursesScreen screen = new( 8, 1 );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Reset();

		for ( int iteration = 0; iteration < iterations; iteration++ ) {
			screen.VirtualScreen[ 0, 0 ] = new CursesCell(
				0 == ( iteration & 1 )
					? "X"
					: "Y"
			);
			await engine.RefreshAsync( screen, 0, 0 );
		}

		Assert.Equal( 2000, output.ByteCount );
		Assert.Equal( 2000, output.WriteCount );
		Assert.Equal( 1000, output.FlushCount );
	}

	[Fact]
	public async Task EditorCharacterShiftBeatsOrdinaryRewrite() {
		const string initial = "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEF";
		MeasuringOutput optimizedOutput = new();
		MeasuringOutput fallbackOutput = new();
		CursesRefreshEngine optimizedEngine = new(
			new TerminalDescriptionBuilder( "editor-optimized" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.InsertCharacters, "I%p1%d" )
				.Build(),
			optimizedOutput
		);
		CursesRefreshEngine fallbackEngine = new(
			CreateCursorOnlyTerminal(),
			fallbackOutput
		);
		CursesScreen optimizedScreen = CreateSingleRowScreen( initial );
		CursesScreen fallbackScreen = CreateSingleRowScreen( initial );
		optimizedScreen.StandardWindow.Move( 0, 1 );
		fallbackScreen.StandardWindow.Move( 0, 1 );
		await optimizedEngine.RefreshAsync( optimizedScreen, 0, 1 );
		await fallbackEngine.RefreshAsync( fallbackScreen, 0, 1 );
		optimizedOutput.Reset();
		fallbackOutput.Reset();

		optimizedScreen.StandardWindow.InsertCells( 2 );
		fallbackScreen.StandardWindow.InsertCells( 2 );
		await optimizedEngine.RefreshAsync( optimizedScreen, 0, 1 );
		await fallbackEngine.RefreshAsync( fallbackScreen, 0, 1 );

		Assert.Equal( 2, optimizedOutput.ByteCount );
		Assert.Equal( 34, fallbackOutput.ByteCount );
		Assert.Equal( 1, optimizedOutput.WriteCount );
		Assert.Equal( 3, fallbackOutput.WriteCount );
		AssertRowsEqual( optimizedScreen, fallbackScreen );
	}

	[Fact]
	public async Task PagerLineShiftBeatsOrdinaryRewrite() {
		const int columns = 32;
		MeasuringOutput optimizedOutput = new();
		MeasuringOutput fallbackOutput = new();
		CursesRefreshEngine optimizedEngine = new(
			new TerminalDescriptionBuilder( "pager-optimized" )
				.SetString( StringCapability.CursorAddress, "C" )
				.SetString( StringCapability.DeleteLines, "D%p1%d" )
				.Build(),
			optimizedOutput
		);
		CursesRefreshEngine fallbackEngine = new(
			CreateCursorOnlyTerminal(),
			fallbackOutput
		);
		CursesScreen optimizedScreen = CreateRowPatternScreen( columns );
		CursesScreen fallbackScreen = CreateRowPatternScreen( columns );
		await optimizedEngine.RefreshAsync( optimizedScreen, 0, 0 );
		await fallbackEngine.RefreshAsync( fallbackScreen, 0, 0 );
		optimizedOutput.Reset();
		fallbackOutput.Reset();
		optimizedScreen.StandardWindow.Move( 1, 0 );
		fallbackScreen.StandardWindow.Move( 1, 0 );

		optimizedScreen.StandardWindow.DeleteLines();
		fallbackScreen.StandardWindow.DeleteLines();
		await optimizedEngine.RefreshAsync( optimizedScreen, 0, 0 );
		await fallbackEngine.RefreshAsync( fallbackScreen, 0, 0 );

		Assert.Equal( 4, optimizedOutput.ByteCount );
		Assert.Equal( 166, fallbackOutput.ByteCount );
		Assert.Equal( 3, optimizedOutput.WriteCount );
		Assert.Equal( 11, fallbackOutput.WriteCount );
		AssertRowsEqual( optimizedScreen, fallbackScreen );
	}

	private static TerminalDescription CreateCursorOnlyTerminal() {
		return new TerminalDescriptionBuilder( "regret-fallback" )
			.SetString( StringCapability.CursorAddress, "C" )
			.Build();
	}

	private static CursesScreen CreateSingleRowScreen(
		string content
	) {
		ArgumentNullException.ThrowIfNull( content );
		CursesScreen screen = new( content.Length, 1 );
		for ( int column = 0; column < content.Length; column++ ) {
			screen.VirtualScreen[ 0, column ] = new CursesCell(
				content[ column ].ToString()
			);
		}
		return screen;
	}

	private static CursesScreen CreateRowPatternScreen(
		int columns
	) {
		if ( 0 >= columns ) {
			throw new ArgumentOutOfRangeException( nameof( columns ) );
		}

		CursesScreen screen = new( columns, 6 );
		for ( int row = 0; row < screen.Rows; row++ ) {
			string content = ((char)( 'A' + row )).ToString();
			for ( int column = 0; column < columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell( content );
			}
		}
		return screen;
	}

	private static void FillScreen(
		CursesScreen screen,
		string content
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( content );
		for ( int row = 0; row < screen.Rows; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell( content );
			}
		}
	}

	private static void AssertRowsEqual(
		CursesScreen expected,
		CursesScreen actual
	) {
		ArgumentNullException.ThrowIfNull( expected );
		ArgumentNullException.ThrowIfNull( actual );
		Assert.Equal( expected.Columns, actual.Columns );
		Assert.Equal( expected.Rows, actual.Rows );
		for ( int row = 0; row < expected.Rows; row++ ) {
			for ( int column = 0; column < expected.Columns; column++ ) {
				Assert.Equal(
					expected.VirtualScreen[ row, column ],
					actual.VirtualScreen[ row, column ]
				);
			}
		}
	}

	private sealed class MeasuringOutput : ITerminalOutput {
		private readonly CursesOutputCostModel costModel = new( Encoding.UTF8 );

		internal int ByteCount {
			get;
			private set;
		}

		internal int WriteCount {
			get;
			private set;
		}

		internal int FlushCount {
			get;
			private set;
		}

		internal void Reset() {
			ByteCount = 0;
			WriteCount = 0;
			FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			ByteCount = checked(
				ByteCount + costModel.GetApplicationTextByteCount( value )
			);
			WriteCount = checked( WriteCount + 1 );
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
			ByteCount = checked(
				ByteCount
					+ CursesOutputCostModel.GetTerminalStringByteCount(
						value,
						affectedLines
					)
			);
			WriteCount = checked( WriteCount + 1 );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount = checked( FlushCount + 1 );
			return ValueTask.CompletedTask;
		}
	}
}
