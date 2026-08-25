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
	public async Task TrailingDefaultBlanksUseEraseToEndOfLine() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateTerminal(), output );
		CursesScreen screen = new( 6, 1 );
		for ( int column = 0; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = new CursesCell( "x" );
		}
		await engine.RefreshAsync( screen, 0, 0 );
		output.Clear();

		for ( int column = 3; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "<cup:0,3>", output.Text );
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
	public async Task RgbColorUsesExtendedTerminfoCapability() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "rgb-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetExtendedString( "setrgbf", "<rgbf:%p1%d,%p2%d,%p3%d>" )
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

		Assert.Contains( "<rgbf:12,34,56>", output.Text );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "refresh-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterDimMode, "<dim>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.SetString( StringCapability.EnterStandoutMode, "<standout>" )
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
			ArgumentNullException.ThrowIfNull( value );
			return this.WriteCoreAsync( value, cancellationToken );
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
			return this.WriteCoreAsync( value, cancellationToken );
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.FlushCount++;
			return ValueTask.CompletedTask;
		}

		private ValueTask WriteCoreAsync(
			string value,
			CancellationToken cancellationToken
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.writeCount++;
			if ( this.ThrowOnWrite == this.writeCount ) {
				throw new IOException( "Synthetic refresh output failure." );
			}

			this.text.Append( value );
			return ValueTask.CompletedTask;
		}
	}
}
