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

using System.Diagnostics;
using System.Text;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;
using Xunit.Abstractions;

/// <summary>Qualifies style-only damage, physical refresh, mapping, and transition ordering.</summary>
public sealed class CursesColorSurfaceRefreshTests {
	private readonly ITestOutputHelper testOutput;

	public CursesColorSurfaceRefreshTests( ITestOutputHelper testOutput ) {
		this.testOutput = testOutput;
	}

	private static readonly CursesStyle InitialStyle = new(
		CursesColor.Indexed( 1 ),
		CursesColor.Indexed( 2 ),
		CursesTextAttributes.Bold
	);

	[Fact]
	public void ForegroundOnlyChangeMarksCellDirty() {
		AssertStyleAxisCreatesDamage(
			InitialStyle.WithForeground( CursesColor.Indexed( 3 ) )
		);
	}

	[Fact]
	public void BackgroundOnlyChangeMarksCellDirty() {
		AssertStyleAxisCreatesDamage(
			InitialStyle.WithBackground( CursesColor.Indexed( 4 ) )
		);
	}

	[Fact]
	public void AttributeOnlyChangeMarksCellDirty() {
		AssertStyleAxisCreatesDamage(
			InitialStyle.WithAttributes( CursesTextAttributes.Underline )
		);
	}

	[Fact]
	public void EqualCompleteStyleRewriteRemainsClean() {
		CursesVirtualScreen screen = new( 1, 1 );
		CursesCell cell = new( "A", InitialStyle );
		screen[ 0, 0 ] = cell;
		screen.MarkClean();

		screen[ 0, 0 ] = cell;

		Assert.False( screen.IsDirty( 0, 0 ) );
		Assert.Equal( 0, screen.DirtyCellCount );
	}

	[Fact]
	public async Task ForegroundOnlyChangeProducesRenditionTransition() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 1, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			InitialStyle.WithForeground( CursesColor.Indexed( 3 ) )
		);
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<fg:3>", output.Text );
		Assert.Contains( "A", output.Text );
	}

	[Fact]
	public async Task BackgroundOnlyChangeProducesRenditionTransition() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 1, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		screen.VirtualScreen[ 0, 0 ] = new CursesCell(
			"A",
			InitialStyle.WithBackground( CursesColor.Indexed( 4 ) )
		);
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<bg:4>", output.Text );
		Assert.Contains( "A", output.Text );
	}

	[Fact]
	public async Task ColorPairChangePreservesContentAndMetadata() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 1, 1 );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/color-pair" )
		);
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		screen.VirtualScreen.SetMetadata( 0, 0, metadata );
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();
		CursesStyle replacement = new(
			CursesColor.Indexed( 5 ),
			CursesColor.Indexed( 6 ),
			InitialStyle.Attributes
		);

		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", replacement );
		screen.VirtualScreen.SetMetadata( 0, 0, metadata );
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( "A", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( metadata, screen.VirtualScreen.GetMetadata( 0, 0 ) );
		Assert.Contains( "<fg:5>", output.Text );
		Assert.Contains( "<bg:6>", output.Text );
	}

	[Fact]
	public async Task EqualMulticolorFrameProducesNoCellOutput() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = CreateMulticolorScreen();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task BoundedColorRefreshWorkloadsRemainSparse() {
		const int iterations = 32;
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 12, 4 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B", new CursesStyle( CursesColor.Indexed( 3 ), CursesColor.Indexed( 4 ) )
		);
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		Stopwatch watch = Stopwatch.StartNew();
		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int i = 0; i < iterations; i++ ) {
			await context.Engine.RefreshAsync( screen, 0, 0 );
		}
		long cleanAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
		watch.Stop();
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.WriteCount );
		testOutput.WriteLine( $"clean: iterations={iterations} elapsedMs={watch.Elapsed.TotalMilliseconds:F3} allocatedBytes={cleanAllocated} writes=0 outputBytes=0" );

		foreach ( (string name, Func<int, CursesStyle> next) in new[] {
			("foreground", (Func<int, CursesStyle>)( i => InitialStyle.WithForeground( CursesColor.Indexed( 1 + ( i % 2 ) ) ) )),
			("background", (Func<int, CursesStyle>)( i => InitialStyle.WithBackground( CursesColor.Indexed( 2 + ( i % 2 ) ) ) ))
		} ) {
			int emitted = 0;
			int writes = 0;
			watch.Restart();
			before = GC.GetAllocatedBytesForCurrentThread();
			for ( int i = 0; i < iterations; i++ ) {
				output.Clear();
				screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", next( i + 1 ) );
				await context.Engine.RefreshAsync( screen, 0, 0 );
				Assert.Contains( "A", output.Text );
				emitted += Encoding.UTF8.GetByteCount( output.Text );
				writes += output.WriteCount;
			}
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			watch.Stop();
			Assert.InRange( emitted, iterations, iterations * 128 );
			testOutput.WriteLine( $"{name}: iterations={iterations} elapsedMs={watch.Elapsed.TotalMilliseconds:F3} allocatedBytes={allocated} writes={writes} outputBytes={emitted}" );
		}

		RecordingTerminalOutput panelOutput = new();
		TerminalSession panelTerminal = await TerminalScreenTestSession.OpenAsync(
			CreateTerminal(), panelOutput
		);
		await using CursesSession panelSession = await CursesSession.OpenAsync(
			panelTerminal,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
		using CursesPanel measuredPanel = panelSession.Screen.CreatePanel( 1, 1, 1, 2 );
		measuredPanel.ContentWindow.FillRectangle(
			0, 0, 1, 2, CursesCell.Blank( InitialStyle )
		);
		await panelSession.RefreshAsync();
		panelOutput.Clear();
		int panelBytes = 0;
		watch.Restart();
		before = GC.GetAllocatedBytesForCurrentThread();
		for ( int i = 0; i < iterations; i++ ) {
			panelOutput.Clear();
			measuredPanel.MoveTo( 1, 1 + ( i % 2 ) );
			await panelSession.RefreshAsync();
			panelBytes += Encoding.UTF8.GetByteCount( panelOutput.Text );
		}
		long panelAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
		watch.Stop();
		Assert.InRange( panelBytes, iterations, iterations * 256 );
		testOutput.WriteLine( $"panel move: iterations={iterations} elapsedMs={watch.Elapsed.TotalMilliseconds:F3} allocatedBytes={panelAllocated} outputBytes={panelBytes}" );
	}

	[Fact]
	public async Task TinyScreenRapidPaletteAndRepeatedInvalidationRemainCoherent() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		for ( int i = 0; i < 32; i++ ) {
			CursesStyle style = new(
				CursesColor.Indexed( 1 + ( i % 2 ) ),
				CursesColor.Indexed( 3 + ( i % 2 ) )
			);
			screen.StandardWindow.Move( 0, 0 );
			screen.StandardWindow.Write( "界", style );
			if ( i % 4 == 0 ) {
				screen.Resize( 1, 1, preserveContents: true );
				context.Engine.Invalidate();
				screen.Resize( 2, 1, preserveContents: true );
			}
			output.Clear();
			await context.Engine.RefreshAsync( screen, 0, 0 );
			CursesCellFootprint.Validate( screen.VirtualScreen );
			Assert.Equal( style, screen.VirtualScreen[ 0, 0 ].Style );
			Assert.Contains( "<fg:", output.Text );
			Assert.Contains( "<bg:", output.Text );
		}
	}

	[Fact]
	public async Task InvalidatedMulticolorFrameReemitsCompleteStyles() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = CreateMulticolorScreen();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		context.Engine.Invalidate();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<fg:1>", output.Text );
		Assert.Contains( "<bg:2>", output.Text );
		Assert.Contains( "<fg:3>", output.Text );
		Assert.Contains( "<bg:4>", output.Text );
		Assert.Contains( "A", output.Text );
		Assert.Contains( "B", output.Text );
	}

	[Fact]
	public void MapperPreservesIndependentForegroundAndBackground() {
		CursesStyle style = new(
			CursesColor.Indexed( 37 ),
			CursesColor.Rgb( 12, 34, 56 ),
			CursesTextAttributes.Dim
		);

		TerminalScreenRendition terminal = CursesTerminalScreenMapper.ToTerminal( style );

		Assert.Equal( TerminalScreenColor.Indexed( 37 ), terminal.Foreground );
		Assert.Equal( TerminalScreenColor.Rgb( 12, 34, 56 ), terminal.Background );
		Assert.Equal( style, CursesTerminalScreenMapper.ToCurses( terminal ) );
	}

	[Fact]
	public async Task ResetRestoresDefaultColorsBeforeLaterDefaultText() {
		RecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync( CreateTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "B", CursesStyle.Default );

		await context.Engine.RefreshAsync( screen, 0, 0 );

		int first = output.Text.IndexOf( "A", StringComparison.Ordinal );
		int restore = output.Text.IndexOf( "<op>", first + 1, StringComparison.Ordinal );
		int second = output.Text.IndexOf( "B", restore + 1, StringComparison.Ordinal );
		Assert.True( first < restore );
		Assert.True( restore < second );
	}

	[Fact]
	public void ReverseRemainsAttributeWithoutStoredColorSwap() {
		CursesStyle style = new(
			CursesColor.Indexed( 1 ),
			CursesColor.Indexed( 2 ),
			CursesTextAttributes.Reverse
		);

		TerminalScreenRendition terminal = CursesTerminalScreenMapper.ToTerminal( style );
		CursesStyle roundTrip = CursesTerminalScreenMapper.ToCurses( terminal );

		Assert.Equal( CursesColor.Indexed( 1 ), roundTrip.Foreground );
		Assert.Equal( CursesColor.Indexed( 2 ), roundTrip.Background );
		Assert.Equal( CursesTextAttributes.Reverse, roundTrip.Attributes );
	}

	private static void AssertStyleAxisCreatesDamage( CursesStyle replacement ) {
		CursesVirtualScreen screen = new( 1, 1 );
		screen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		screen.MarkClean();

		screen[ 0, 0 ] = new CursesCell( "A", replacement );

		Assert.True( screen.IsDirty( 0, 0 ) );
		Assert.Equal( 1, screen.DirtyCellCount );
	}

	private static CursesScreen CreateMulticolorScreen() {
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A", InitialStyle );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell(
			"B",
			new CursesStyle(
				CursesColor.Indexed( 3 ),
				CursesColor.Indexed( 4 ),
				CursesTextAttributes.Underline
			)
		);
		return screen;
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "color-surface-refresh" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterDimMode, "<dim>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.Build();
	}

	private sealed class RecordingOutput : ILegacyTerminalOutputFixture {
		private readonly StringBuilder text = new();

		internal string Text => text.ToString();

		internal int WriteCount { get; private set; }

		internal void Clear() {
			text.Clear();
			WriteCount = 0;
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			text.Append( value );
			WriteCount++;
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
			WriteCount++;
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
