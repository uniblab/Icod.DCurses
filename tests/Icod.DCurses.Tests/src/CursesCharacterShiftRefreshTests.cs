using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies physical character-shift optimization through the retained refresh engine.</summary>
public sealed class CursesCharacterShiftRefreshTests {
	[Fact]
	public async Task InsertCellsUsesPhysicalInsertCharactersAndRetainsExactRow() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = CreateScreen( "ABCDEFGH" );
		screen.StandardWindow.Move( 0, 1 );
		await engine.RefreshAsync( screen, 0, 1 );
		output.Clear();

		screen.StandardWindow.InsertCells( 2 );
		await engine.RefreshAsync( screen, 0, 1 );

		Assert.Equal( "I2", output.Text );
		Assert.Equal( "A  BCDEF", ReadRow( screen ) );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 1 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task DeleteCellsUsesPhysicalDeleteCharactersAndRetainsExactRow() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = CreateScreen( "ABCDEFGH" );
		screen.StandardWindow.Move( 0, 1 );
		await engine.RefreshAsync( screen, 0, 1 );
		output.Clear();

		screen.StandardWindow.DeleteCells( 2 );
		await engine.RefreshAsync( screen, 0, 1 );

		Assert.Equal( "D2", output.Text );
		Assert.Equal( "ADEFGH  ", ReadRow( screen ) );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await engine.RefreshAsync( screen, 0, 1 );
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task MissingCharacterShiftCapabilitiesFallsBackToOrdinaryRewrite() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder( "fallback" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.Build();
		CursesRefreshEngine engine = new( terminal, output );
		CursesScreen screen = CreateScreen( "ABCDEFGH" );
		screen.StandardWindow.Move( 0, 1 );
		await engine.RefreshAsync( screen, 0, 1 );
		output.Clear();

		screen.StandardWindow.InsertCells( 2 );
		await engine.RefreshAsync( screen, 0, 1 );

		Assert.DoesNotContain( "I2", output.Text );
		Assert.Contains( "BCDEF", output.Text );
		Assert.Equal( "A  BCDEF", ReadRow( screen ) );
	}

	[Fact]
	public async Task FailedCharacterShiftInvalidatesPhysicalKnowledgeBeforeRetry() {
		RecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = CreateScreen( "ABCDEFGH" );
		screen.StandardWindow.Move( 0, 1 );
		await engine.RefreshAsync( screen, 0, 1 );
		output.Clear();

		screen.StandardWindow.InsertCells( 2 );
		output.ThrowOnWrite = 1;
		await Assert.ThrowsAsync<IOException>(
			async () => await engine.RefreshAsync( screen, 0, 1 )
		);

		output.ThrowOnWrite = null;
		output.Clear();
		await engine.RefreshAsync( screen, 0, 1 );

		Assert.DoesNotContain( "I2", output.Text );
		Assert.Contains( "A  BCDEF", output.Text );
		Assert.Contains( "<cup:0,0>", output.Text );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "character-shift" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.InsertCharacters, "I%p1%d" )
			.SetString( StringCapability.DeleteCharacters, "D%p1%d" )
			.Build();
	}

	private static CursesScreen CreateScreen(
		string value
	) {
		ArgumentNullException.ThrowIfNull( value );
		CursesScreen screen = new( value.Length, 1 );
		for ( int column = 0; column < value.Length; column++ ) {
			screen.VirtualScreen[ 0, column ] = ' ' == value[ column ]
				? CursesCell.Blank()
				: new CursesCell( value[ column ].ToString() )
			;
		}
		return screen;
	}

	private static string ReadRow(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		StringBuilder result = new();
		for ( int column = 0; column < screen.Columns; column++ ) {
			CursesCell cell = screen.VirtualScreen[ 0, column ];
			if ( cell.IsBlank ) {
				result.Append( ' ' );
			} else {
				result.Append( cell.Content );
			}
		}
		return result.ToString();
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
				throw new IOException( "Synthetic character-shift output failure." );
			}
			text.Append( value );
			return ValueTask.CompletedTask;
		}
	}
}
