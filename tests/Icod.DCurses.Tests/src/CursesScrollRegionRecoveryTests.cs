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

/// <summary>Verifies fail-closed recovery after uncertain temporary scroll-region output.</summary>
public sealed class CursesScrollRegionRecoveryTests {
	[Fact]
	public async Task FailureAfterRegionSetupPrependsFullRegionResetToRetry() {
		FailingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal( "region-recovery", "R" ),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( "A", "B", "C", "D", "E" );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();
		ApplyInteriorDelete( screen );
		output.ThrowAfterAcceptingWrite = 1;

		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);

		Assert.StartsWith( "R", output.Text, StringComparison.Ordinal );
		Assert.Equal( DamagedRows, ReadRows( screen ) );

		output.ThrowAfterAcceptingWrite = null;
		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.StartsWith( "R", output.Text, StringComparison.Ordinal );
		Assert.Contains( "AAAAAAAA", output.Text, StringComparison.Ordinal );
		Assert.Contains( "CCCCCCCC", output.Text, StringComparison.Ordinal );
		Assert.DoesNotContain( "L", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task FailureBeforeAnyBytesStillRequiresConservativeRegionReset() {
		FailingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal( "conservative-recovery", "R" ),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen screen = CreateScreen( "A", "B", "C", "D", "E" );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();
		ApplyInteriorDelete( screen );
		output.ThrowBeforeAcceptingWrite = 1;

		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( DamagedRows, ReadRows( screen ) );

		output.ThrowBeforeAcceptingWrite = null;
		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.StartsWith( "R", output.Text, StringComparison.Ordinal );
		Assert.Contains( "AAAAAAAA", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task MissingFullRegionRecoveryPlanFailsBeforeOutputAndRetainsRequirement() {
		FailingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(
					"missing-six-row-recovery",
					"%?%p2%{5}%=%t%eR%;"
				),
				output
			);
		CursesRefreshEngine engine = context.Engine;
		CursesScreen damaged = CreateScreen( "A", "B", "C", "D", "E" );
		await engine.RefreshAsync( damaged, 0, 0 );
		output.Clear();
		ApplyInteriorDelete( damaged );
		output.ThrowAfterAcceptingWrite = 1;
		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( damaged, 0, 0 )
		);

		output.ThrowAfterAcceptingWrite = null;
		output.Clear();
		CursesScreen unsupportedSize = CreateScreen(
			"A", "B", "C", "D", "E", "F"
		);
		await Assert.ThrowsAsync<NotSupportedException>(
			async () => await engine.RefreshAsync( unsupportedSize, 0, 0 )
		);

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
		Assert.Equal( DamagedRows, ReadRows( damaged ) );

		output.Clear();
		await engine.RefreshAsync( damaged, 0, 0 );
		Assert.StartsWith( "R", output.Text, StringComparison.Ordinal );
		Assert.Contains( "AAAAAAAA", output.Text, StringComparison.Ordinal );
	}

	private static readonly string[] DamagedRows = [
		"AAAAAAAA",
		"CCCCCCCC",
		"        ",
		"DDDDDDDD",
		"EEEEEEEE"
	];

	private static TerminalDescription CreateTerminal(
		string name,
		string scrollRegion
	) {
		return new TerminalDescriptionBuilder( name )
			.SetString( StringCapability.CursorAddress, "C" )
			.SetString( StringCapability.ChangeScrollRegion, scrollRegion )
			.SetString( StringCapability.DeleteLine, "L" )
			.Build();
	}

	private static CursesScreen CreateScreen( params string[] rows ) {
		CursesScreen screen = new( 8, rows.Length );
		for ( int row = 0; row < rows.Length; row++ ) {
			for ( int column = 0; column < screen.Columns; column++ ) {
				screen.VirtualScreen[ row, column ] = new CursesCell( rows[ row ] );
			}
		}
		return screen;
	}

	private static void ApplyInteriorDelete( CursesScreen screen ) {
		CursesWindow editor = screen.CreateWindow( 1, 0, 2, screen.Columns );
		editor.DeleteLines();
	}

	private static string[] ReadRows( CursesScreen screen ) {
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

	private sealed class FailingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();
		private int writeCount;

		internal int? ThrowBeforeAcceptingWrite {
			get;
			set;
		}

		internal int? ThrowAfterAcceptingWrite {
			get;
			set;
		}

		internal int WriteCount => this.writeCount;

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => this.text.ToString();

		internal void Clear() {
			this.text.Clear();
			this.writeCount = 0;
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
			if ( 0 >= affectedLines ) {
				throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
			}
			cancellationToken.ThrowIfCancellationRequested();
			this.writeCount++;
			if ( this.ThrowBeforeAcceptingWrite == this.writeCount ) {
				throw new IOException( "Synthetic failure before accepting output." );
			}
			this.text.Append( value );
			if ( this.ThrowAfterAcceptingWrite == this.writeCount ) {
				throw new IOException( "Synthetic failure after accepting output." );
			}
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
