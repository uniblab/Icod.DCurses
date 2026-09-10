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

/// <summary>Preserves refresh/damage coverage after the Terminal T10 output cutover.</summary>
public sealed class CursesRefreshEngineTerminalTests {
	[Fact]
	public async Task SmallChangeDoesNotRepaintUnchangedScreen() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 5, 2 );

		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen[ 0, 2 ] = new CursesCell( "X" );
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<cup:0,2>", output.Text );
		Assert.Contains( "X", output.Text );
		Assert.DoesNotContain( "<cup:1,0>", output.Text );
	}

	[Fact]
	public async Task ForcedInvalidationProducesCompleteRepaint() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 4, 2 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A" );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "B" );
		screen.VirtualScreen[ 1, 0 ] = new CursesCell( "C" );
		screen.VirtualScreen[ 1, 1 ] = new CursesCell( "D" );

		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		engine.Invalidate();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<cup:0,0>", output.Text );
		Assert.Contains( "<cup:1,0>", output.Text );
		Assert.Contains( "AB", output.Text );
		Assert.Contains( "CD", output.Text );
	}

	[Fact]
	public async Task ContinuationCellsReserveColumnsWithoutWritingBlankBytes() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 4, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "界" );
		screen.VirtualScreen[ 0, 1 ] = CursesCell.Continuation();
		screen.VirtualScreen[ 0, 2 ] = new CursesCell( "X" );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "界X", output.Text );
		Assert.DoesNotContain( "界 X", output.Text );
	}

	[Fact]
	public async Task SameStyleRunDoesNotRepeatRenditionChanges() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 4, 1 );
		CursesStyle style = new(
			CursesColor.Indexed( 2 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", style );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "B", style );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, CountOccurrences( output.Text, "<bold>" ) );
		Assert.Equal( 1, CountOccurrences( output.Text, "<fg:2>" ) );
		Assert.Contains( "AB", output.Text );
	}

	[Fact]
	public async Task TrailingDefaultBlanksUseEraseToEndOfLineWhenCheaper() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 6, 1 );
		for ( int column = 0; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = new CursesCell( "x" );
		}
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 1; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<cup:0,1>", output.Text );
		Assert.Contains( "<el>", output.Text );
	}

	[Fact]
	public async Task RefreshLeavesCursorAtRequestedPositionAndFlushesOnce() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 5, 2 );

		await engine.RefreshAsync( screen, 1, 3 );

		Assert.EndsWith( "<cup:1,3>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task FailedOutputInvalidatesPhysicalKnowledgeForNextRefresh() {
		RecordingOutput output = new() {
			ThrowOnWrite = 2
		};
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 4, 2 );

		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( screen, 0, 0 )
		);

		output.ThrowOnWrite = null;
		output.Clear();
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<cup:0,0>", output.Text );
		Assert.Contains( "<cup:1,0>", output.Text );
	}

	[Fact]
	public async Task ResetRenditionRestoresTerminalDefaults() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"B",
			new CursesStyle(
				CursesColor.Indexed( 1 ),
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		await engine.ResetRenditionAsync();

		Assert.Contains( "<sgr0>", output.Text );
		Assert.Contains( "<op>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task RgbColorUsesTermInfoSemanticDirectColorExpansion() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "rgb-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 1 << 24 )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.SetExtendedBoolean( "RGB" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"R",
			new CursesStyle(
				CursesColor.Rgb( 12, 34, 56 ),
				CursesColor.Default
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		string expected = TerminalColors.ExpandForeground(
			terminal,
			new TerminalRgbColor( 12, 34, 56 )
		);
		Assert.Contains( expected, output.Text );
	}

	[Fact]
	public async Task UnsupportedRgbAndOutOfRangeIndexDegradeWithoutThrowing() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"R",
			new CursesStyle(
				CursesColor.Rgb( 12, 34, 56 ),
				CursesColor.Default
			)
		);
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"I",
			new CursesStyle(
				CursesColor.Indexed( 8 ),
				CursesColor.Default
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "RI", output.Text );
		Assert.DoesNotContain( "<fg:", output.Text );
	}

	[Fact]
	public async Task ModernOptionalAttributesUseAdvertisedSemanticCapabilities() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Italic
					| CursesTextAttributes.Blink
					| CursesTextAttributes.Conceal
					| CursesTextAttributes.Strikeout
			)
		);

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<italic>", output.Text );
		Assert.Contains( "<blink>", output.Text );
		Assert.Contains( "<conceal>", output.Text );
		Assert.Contains( "<strike>", output.Text );
	}

	[Fact]
	public async Task NoColorVideoRestrictionRemovesOnlyPhysicalAttribute() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ncv-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetNumber( NumericCapability.NoColorVideo, 32 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = new( 1, 1 );
		CursesStyle requested = new(
			CursesColor.Indexed( 1 ),
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "B", requested );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<fg:1>", output.Text );
		Assert.DoesNotContain( "<bold>", output.Text );
		Assert.Equal( requested, screen.VirtualScreen[ 0, 0 ].Style );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "refresh-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterDimMode, "<dim>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.SetString( StringCapability.EnterStandoutMode, "<standout>" )
			.SetString( StringCapability.EnterItalicMode, "<italic>" )
			.SetString( StringCapability.ExitItalicMode, "</italic>" )
			.SetString( StringCapability.EnterBlinkMode, "<blink>" )
			.SetString( StringCapability.EnterInvisibleMode, "<conceal>" )
			.SetExtendedString( "smxx", "<strike>" )
			.SetExtendedString( "rmxx", "</strike>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}

	private static int CountOccurrences(
		string source,
		string value
	) {
		ArgumentNullException.ThrowIfNull( source );
		ArgumentNullException.ThrowIfNull( value );

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

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly StringBuilder text = new();
		private int writeCount;

		internal int? ThrowOnWrite {
			get;
			set;
		}

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => text.ToString();

		internal void Clear() {
			text.Clear();
			writeCount = 0;
			FlushCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			return WriteCoreAsync( value, cancellationToken );
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
			return WriteCoreAsync( value, cancellationToken );
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
			CancellationToken cancellationToken
		) {
			cancellationToken.ThrowIfCancellationRequested();
			writeCount++;
			if ( ThrowOnWrite == writeCount ) {
				throw new IOException( "Synthetic refresh output failure." );
			}

			text.Append( value );
			return ValueTask.CompletedTask;
		}
	}
}
