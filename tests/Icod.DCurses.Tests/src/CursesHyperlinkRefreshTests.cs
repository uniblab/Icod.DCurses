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

/// <summary>Exercises retained physical hyperlink rendering through the Terminal semantic-output seam.</summary>
public sealed class CursesHyperlinkRefreshTests {
	[Fact]
	public async Task AdjacentEquivalentLinkedCellsUseOneBoundedHyperlinkRun() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 6, 1 );
		CursesCellMetadata metadata = LinkMetadata( "one" );

		screen.StandardWindow.WriteWithMetadata(
			"ABC",
			metadata
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "ABC", output.HyperlinkWrites[ 0 ].Text );
		Assert.Equal( "one", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
	}

	[Fact]
	public async Task UnchangedSemanticContentProducesNoSecondPayloadWrite() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 6, 1 );
		screen.StandardWindow.WriteWithMetadata(
			"ABC",
			LinkMetadata( "one" )
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Empty( output.HyperlinkWrites );
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task SemanticOnlyChangeRewritesSameVisibleCellsWithNewHyperlink() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WriteWithMetadata(
			"ABC",
			LinkMetadata( "one" )
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		CursesCellMetadata replacement = LinkMetadata( "two" );
		for ( int column = 0; column < 3; column++ ) {
			window.SetMetadata(
				0,
				column,
				replacement
			);
		}
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "ABC", output.HyperlinkWrites[ 0 ].Text );
		Assert.Equal( "two", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
	}

	[Fact]
	public async Task RemovingHyperlinkRewritesSameVisibleCellsAsOrdinaryText() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WriteWithMetadata(
			"ABC",
			LinkMetadata( "one" )
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		for ( int column = 0; column < 3; column++ ) {
			window.SetMetadata(
				0,
				column,
				null
			);
		}
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Empty( output.HyperlinkWrites );
		Assert.Contains( "ABC", output.Text );
	}

	[Fact]
	public async Task LinkedAndUnlinkedSegmentsRemainSeparateSemanticRuns() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 5, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABC" );
		window.SetMetadata(
			0,
			0,
			LinkMetadata( "left" )
		);
		window.SetMetadata(
			0,
			2,
			LinkMetadata( "right" )
		);

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 2, output.HyperlinkWrites.Count );
		Assert.Equal( "A", output.HyperlinkWrites[ 0 ].Text );
		Assert.Equal( "left", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
		Assert.Equal( "C", output.HyperlinkWrites[ 1 ].Text );
		Assert.Equal( "right", output.HyperlinkWrites[ 1 ].Hyperlink.Identifier );
		Assert.Contains( "B", output.Text );
	}

	[Fact]
	public async Task WideLinkedElementWritesOneSemanticPayload() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 5, 1 );

		screen.StandardWindow.WriteWithMetadata(
			"界",
			LinkMetadata( "wide" )
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "界", output.HyperlinkWrites[ 0 ].Text );
		Assert.Equal( "wide", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
	}

	[Fact]
	public async Task InvalidationRepaintsLinkedContentThroughSemanticOutput() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.WriteWithMetadata(
			"link",
			LinkMetadata( "again" )
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		engine.Invalidate();
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "link", output.HyperlinkWrites[ 0 ].Text );
	}

	[Fact]
	public async Task SemanticPresenceSuppressesEraseOptimizationUntilPropagationIsProven() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "xxxxxx" );
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		window.SetMetadata(
			0,
			0,
			LinkMetadata( "guard" )
		);
		for ( int column = 1; column < screen.Columns; column++ ) {
			screen.VirtualScreen[ 0, column ] = CursesCell.Blank();
		}
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.DoesNotContain( "<el>", output.Text );
		Assert.Single( output.HyperlinkWrites );
	}

	[Fact]
	public async Task SemanticPresenceSuppressesCharacterShiftOptimizationUntilTerminalEquivalenceIsPortable() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata( "character-shift" );
		window.WriteWithMetadata(
			"ABCDEFGH",
			metadata
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		window.Move( 0, 1 );
		window.InsertCells( 2 );
		await engine.RefreshAsync(
			screen,
			0,
			1
		);

		Assert.DoesNotContain( "<ich:2>", output.Text );
		Assert.Null( window.GetMetadata( 0, 1 ) );
		Assert.Null( window.GetMetadata( 0, 2 ) );
		Assert.Equal( metadata, window.GetMetadata( 0, 3 ) );
		Assert.NotEmpty( output.HyperlinkWrites );
	}

	[Fact]
	public async Task SemanticPresenceSuppressesLineShiftOptimizationUntilTerminalEquivalenceIsPortable() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 8, 4 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata moving = LinkMetadata( "line-shift" );
		window.Move( 0, 0 );
		window.Write( "AAAAAA" );
		window.Move( 1, 0 );
		window.Write( "BBBBBB" );
		window.Move( 2, 0 );
		window.WriteWithMetadata(
			"CCCCCC",
			moving
		);
		window.Move( 3, 0 );
		window.Write( "DDDDDD" );
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		window.Move( 1, 0 );
		window.DeleteLines();
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.DoesNotContain( "<dl:1>", output.Text );
		Assert.Equal( moving, window.GetMetadata( 1, 0 ) );
		Assert.NotEmpty( output.HyperlinkWrites );
	}

	private static CursesCellMetadata LinkMetadata( string identifier ) {
		ArgumentNullException.ThrowIfNull( identifier );
		return new CursesCellMetadata(
			new CursesHyperlink(
				"https://example.test/" + identifier,
				identifier
			)
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "hyperlink-refresh" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.InsertCharacters, "<ich:%p1%d>" )
			.SetString( StringCapability.DeleteLines, "<dl:%p1%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class SemanticRecordingOutput
		: ITerminalOutput,
		  ITerminalHyperlinkOutput {
		private readonly StringBuilder text = new();
		private readonly List<HyperlinkWrite> hyperlinkWrites = [];

		internal int FlushCount {
			get;
			private set;
		}

		internal IReadOnlyList<HyperlinkWrite> HyperlinkWrites => hyperlinkWrites;

		internal string Text => text.ToString();

		internal void Clear() {
			text.Clear();
			hyperlinkWrites.Clear();
			FlushCount = 0;
		}

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

		public ValueTask WriteHyperlinkTextAsync(
			string value,
			CursesHyperlink hyperlink,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			ArgumentNullException.ThrowIfNull( hyperlink );
			cancellationToken.ThrowIfCancellationRequested();
			hyperlinkWrites.Add(
				new HyperlinkWrite(
					value,
					hyperlink
				)
			);
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed record HyperlinkWrite(
		string Text,
		CursesHyperlink Hyperlink
	);
}
