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

/// <summary>Drives T1606 retained raster-placeholder refresh behavior.</summary>
public sealed class CursesRasterRefreshTests {
	[Fact]
	public async Task FirstRefreshEmitsRasterInsteadOfFallbackTextAndUnchangedRefreshIsSilent() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 3, 1 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.VirtualScreen.SetCell( 0, 1, new CursesCell( "Q" ) );
		screen.VirtualScreen.SetRasterCell( 0, 1, token );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Single( output.RasterWrites );
		Assert.Equal( token, output.RasterWrites[ 0 ] );
		Assert.DoesNotContain( "Q", output.Text );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Empty( output.RasterWrites );
		Assert.DoesNotContain( "Q", output.Text );
	}

	[Fact]
	public async Task DirtyRasterCoordinateReemitsOnlyThatCoordinate() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 4, 1 );
		CursesRasterCell first = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		CursesRasterCell second = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.VirtualScreen.SetRasterCell( 0, 0, first );
		screen.VirtualScreen.SetRasterCell( 0, 3, second );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen.TouchCell( 0, 3 );
		await engine.RefreshAsync( screen, 0, 0 );

		CursesRasterCell actual = Assert.Single( output.RasterWrites );
		Assert.Equal( second, actual );
	}

	[Fact]
	public async Task RemovingRasterRestoresRetainedFallbackText() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 3, 1 );
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.VirtualScreen.SetCell( 0, 1, new CursesCell( "F" ) );
		screen.VirtualScreen.SetRasterCell( 0, 1, token );
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen.SetRasterCell( 0, 1, null );
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Empty( output.RasterWrites );
		Assert.Contains( "F", output.Text );
	}

	[Fact]
	public async Task RasterEmissionForcesRenditionReassertionBeforeFollowingText() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 3, 1 );
		CursesStyle style = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.VirtualScreen.SetCell( 0, 0, new CursesCell( "R", style ) );
		screen.VirtualScreen.SetRasterCell( 0, 0, token );
		screen.VirtualScreen.SetCell( 0, 1, new CursesCell( "X", style ) );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Single( output.RasterWrites );
		Assert.Equal( 2, CountOccurrences( output.Text, "<fg:2>" ) );
		Assert.Equal( 2, CountOccurrences( output.Text, "<bold>" ) );
		Assert.Contains( "X", output.Text );
		Assert.DoesNotContain( "R", output.Text );
	}

	[Fact]
	public async Task RasterPresenceDisablesEraseShortcut() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 6, 1 );
		for ( int column = 0; column < screen.Columns; column++ ) {
			screen.VirtualScreen.SetCell( 0, column, new CursesCell( "x" ) );
		}
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 1; column < screen.Columns; column++ ) {
			screen.VirtualScreen.SetCell( 0, column, CursesCell.Blank() );
		}
		CursesRasterCell token = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell();
		screen.VirtualScreen.SetRasterCell( 0, 2, token );
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Single( output.RasterWrites );
		Assert.DoesNotContain( "<el>", output.Text );
	}

	[Fact]
	public async Task RasterRefreshRequiresTypedRasterOutputBoundary() {
		TextOnlyOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen.SetRasterCell(
			0,
			0,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell()
		);

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "raster-refresh-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}

	private static int CountOccurrences(
		string source,
		string value
	) {
		int count = 0;
		int offset = 0;
		while ( true ) {
			int match = source.IndexOf(
				value,
				offset,
				StringComparison.Ordinal
			);
			if ( 0 > match ) {
				return count;
			}
			count++;
			offset = match + value.Length;
		}
	}

	private sealed class RecordingOutput
		: ITerminalOutput,
		  ITerminalRasterPlaceholderOutput {
		private readonly StringBuilder text = new();
		private readonly List<CursesRasterCell> rasterWrites = [];

		internal string Text => text.ToString();

		internal IReadOnlyList<CursesRasterCell> RasterWrites => rasterWrites;

		internal void Clear() {
			text.Clear();
			rasterWrites.Clear();
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( 0 >= affectedLines ) {
				throw new ArgumentOutOfRangeException( nameof( affectedLines ) );
			}
			text.Append( value );
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteRasterPlaceholderCellAsync(
			CursesRasterCell cell,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			rasterWrites.Add( cell );
			text.Append( "<raster>" );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TextOnlyOutput : ITerminalOutput {
		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}
}
