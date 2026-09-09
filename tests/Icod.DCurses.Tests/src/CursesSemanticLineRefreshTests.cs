using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies semantic line cells are resolved only at the physical refresh boundary.</summary>
public sealed class CursesSemanticLineRefreshTests {
	[Fact]
	public async Task ConsecutiveSemanticLineCellsShareOneAcsRun() {
		RecordingOutput output = new();
		TerminalDescription terminal = CreateAcsTerminal();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = new( 4, 1 );
		for ( int column = 0; column < 3; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Line(
				CursesLineGlyph.Horizontal
			);
		}

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, CountOccurrences( output.Text, "<smacs>" ) );
		Assert.Equal( 1, CountOccurrences( output.Text, "<rmacs>" ) );
		Assert.Contains( "<smacs>===<rmacs>", output.Text );
	}

	[Fact]
	public async Task OrdinaryBoxDrawingTextIsNotReinterpretedAsSemanticAcs() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new( CreateAcsTerminal(), output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "─" );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "─", output.Text );
		Assert.DoesNotContain( "<smacs>", output.Text );
		Assert.DoesNotContain( "<rmacs>", output.Text );
	}

	[Fact]
	public async Task MissingAcsUsesCanonicalUnicodeLineContent() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "unicode-line" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = CursesCell.Line( CursesLineGlyph.Crossing );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "┼", output.Text );
		Assert.DoesNotContain( "+", output.Text );
	}

	[Fact]
	public async Task UnsafeUnicodeLineWidthUsesAsciiFallback() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "ascii-line" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = new(
			2,
			1,
			new TwoColumnBoxDrawingWidthProvider()
		);
		screen.VirtualScreen[ 0, 0 ] = CursesCell.Line( CursesLineGlyph.UpperLeftCorner );

		await engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "+", output.Text );
		Assert.DoesNotContain( "┌", output.Text );
	}

	private static TerminalDescription CreateAcsTerminal() {
		return new TerminalDescriptionBuilder( "acs-line" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.EnterAlternateCharacterSetMode, "<smacs>" )
			.SetString( StringCapability.ExitAlternateCharacterSetMode, "<rmacs>" )
			.SetString( StringCapability.AlternateCharacterSet, "q=x|l+" )
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

	private sealed class TwoColumnBoxDrawingWidthProvider
		: ICursesTextWidthProvider {
		public int GetWidth( string textElement ) {
			ArgumentException.ThrowIfNullOrEmpty( textElement );
			return textElement[ 0 ] is >= '\u2500' and <= '\u257F'
				? 2
				: 1
			;
		}
	}

	private sealed class RecordingOutput
		: ITerminalOutput {
		private readonly StringBuilder text = new();

		internal string Text => text.ToString();

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
