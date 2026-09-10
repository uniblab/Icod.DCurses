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

/// <summary>Exercises application-shaped semantic workloads for the 1.1 acceptance gate.</summary>
public sealed class CursesSemanticApplicationAcceptanceTests {
	[Fact]
	public async Task EditorLikeMutationCoalescesWideLinkedSpanAfterInsertion() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 24, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata link = LinkMetadata( "editor" );
		window.Move( 1, 2 );
		window.WriteWithMetadata(
			"alpha界omega",
			link
		);

		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "alpha界omega", output.HyperlinkWrites[ 0 ].Text );
		output.Clear();

		window.Move( 1, 7 );
		window.InsertCells();
		window.WriteWithMetadata(
			"+",
			link
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.NotEmpty( output.HyperlinkWrites );
		Assert.All(
			output.HyperlinkWrites,
			write => Assert.Equal( "editor", write.Hyperlink.Identifier )
		);
		Assert.Equal(
			link,
			window.GetMetadata( 1, 7 )
		);
	}

	[Fact]
	public async Task EditorLikeStyleBeforeEditAndRepeatedRetargetingRemainIndependent() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 24, 3 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata stableLink = LinkMetadata( "stable" );
		CursesStyle bold = new(
			CursesColor.Default,
			CursesColor.Default,
			CursesTextAttributes.Bold
		);
		window.Move( 1, 4 );
		window.WriteWithMetadata(
			"linked",
			stableLink
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		output.Clear();

		window.Move( 1, 4 );
		window.Write(
			"linked",
			bold,
			stableLink
		);
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( "stable", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
		Assert.Contains( "<b>", output.Text );
		Assert.Equal( bold, window.GetCell( 1, 4 ).Style );
		Assert.Equal( stableLink, window.GetMetadata( 1, 4 ) );
		output.Clear();

		window.Move( 1, 2 );
		window.InsertCells( 2 );
		window.Write( ">>" );
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Null( window.GetMetadata( 1, 2 ) );
		Assert.Null( window.GetMetadata( 1, 3 ) );
		Assert.Equal( stableLink, window.GetMetadata( 1, 6 ) );
		Assert.Equal( bold, window.GetCell( 1, 6 ).Style );
		output.Clear();

		for ( int iteration = 0; iteration < 4; iteration++ ) {
			CursesCellMetadata replacement = LinkMetadata( $"retarget-{iteration}" );
			for ( int column = 6; column < 12; column++ ) {
				window.SetMetadata(
					1,
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
			Assert.Equal(
				$"retarget-{iteration}",
				output.HyperlinkWrites[ 0 ].Hyperlink.Identifier
			);
			Assert.Equal( "linked", output.HyperlinkWrites[ 0 ].Text );
			output.Clear();
		}
	}

	[Fact]
	public async Task PagerLikeManyLinksSettlesToNoOpRefresh() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 80, 20 );
		CursesWindow window = screen.StandardWindow;

		for ( int row = 0; row < 16; row++ ) {
			window.Move( row, 0 );
			window.Write( "topic " );
			window.WriteWithMetadata(
				$"link-{row:D2}",
				LinkMetadata( $"pager-{row:D2}" )
			);
		}

		await engine.RefreshAsync(
			screen,
			0,
			0
		);
		Assert.Equal( 16, output.HyperlinkWrites.Count );
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
	public void PagerLikeViewportMovementLineEditingAndScrollingPreserveLinkIdentity() {
		CursesPad pad = new(
			40,
			30
		);
		for ( int row = 0; row < pad.Rows; row++ ) {
			pad.ContentWindow.Move( row, 0 );
			pad.ContentWindow.Write( $"item-{row:D2} " );
			pad.ContentWindow.WriteWithMetadata(
				"docs",
				LinkMetadata( $"row-{row:D2}" )
			);
		}
		CursesScreen screen = new(
			20,
			5
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			5,
			0,
			5,
			20,
			0,
			0
		);

		viewport.Present();
		Assert.Equal(
			"row-05",
			screen.StandardWindow.GetMetadata( 0, 8 )!.Hyperlink!.Identifier
		);

		viewport.SetSource(
			10,
			0
		);
		viewport.Present();
		Assert.Equal(
			"row-10",
			screen.StandardWindow.GetMetadata( 0, 8 )!.Hyperlink!.Identifier
		);

		pad.ContentWindow.Move( 9, 0 );
		pad.ContentWindow.InsertLines();
		viewport.SetSource(
			11,
			0
		);
		viewport.Present();
		Assert.Equal(
			"row-10",
			screen.StandardWindow.GetMetadata( 0, 8 )!.Hyperlink!.Identifier
		);

		pad.ContentWindow.Move( 9, 0 );
		pad.ContentWindow.DeleteLines();
		viewport.SetSource(
			10,
			0
		);
		viewport.Present();
		Assert.Equal(
			"row-10",
			screen.StandardWindow.GetMetadata( 0, 8 )!.Hyperlink!.Identifier
		);

		pad.ContentWindow.ScrollUp();
		viewport.SetSource(
			9,
			0
		);
		viewport.Present();
		Assert.Equal(
			"row-10",
			screen.StandardWindow.GetMetadata( 0, 8 )!.Hyperlink!.Identifier
		);
		Assert.False( viewport.HasVisiblePadChanges );
	}

	[Fact]
	public void ReferenceLargePadPreservesSparseAndDenseSemanticRowsAcrossViewports() {
		const int Rows = 2_048;
		const int Columns = 256;
		CursesPad pad = new(
			Columns,
			Rows
		);
		CursesCellMetadata sparse = LinkMetadata( "sparse" );
		CursesCellMetadata dense = LinkMetadata( "dense" );

		pad.ContentWindow.Move( 17, 41 );
		pad.ContentWindow.WriteWithMetadata(
			"S",
			sparse
		);
		pad.ContentWindow.Move( 1_777, 0 );
		pad.ContentWindow.WriteWithMetadata(
			new string( 'D', Columns ),
			dense
		);

		CursesScreen firstScreen = new( 32, 4 );
		CursesScreen secondScreen = new( 32, 4 );
		CursesPadViewport first = pad.CreateViewport(
			firstScreen.StandardWindow,
			16,
			32,
			4,
			32,
			0,
			0
		);
		CursesPadViewport second = pad.CreateViewport(
			secondScreen.StandardWindow,
			1_776,
			112,
			4,
			32,
			0,
			0
		);

		first.Present();
		second.Present();

		Assert.Equal(
			sparse,
			firstScreen.StandardWindow.GetMetadata( 1, 9 )
		);
		for ( int column = 0; column < 32; column++ ) {
			Assert.Equal(
				dense,
				secondScreen.StandardWindow.GetMetadata( 1, column )
			);
		}
		Assert.False( first.HasVisiblePadChanges );
		Assert.False( second.HasVisiblePadChanges );
	}

	[Fact]
	public async Task DenseEquivalentLinkedRowUsesOneSemanticTransaction() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 256, 1 );
		CursesCellMetadata metadata = LinkMetadata( "dense-row" );
		screen.StandardWindow.WriteWithMetadata(
			new string( 'x', 256 ),
			metadata
		);

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Single( output.HyperlinkWrites );
		Assert.Equal( 256, output.HyperlinkWrites[ 0 ].Text.Length );
		Assert.Equal( "dense-row", output.HyperlinkWrites[ 0 ].Hyperlink.Identifier );
	}

	[Fact]
	public async Task ManyDistinctLinksRemainDistinctSemanticTransactions() {
		SemanticRecordingOutput output = new();
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
		CursesScreen screen = new( 64, 1 );
		CursesWindow window = screen.StandardWindow;

		for ( int column = 0; column < 32; column++ ) {
			window.Move( 0, column );
			window.WriteWithMetadata(
				"x",
				LinkMetadata( $"distinct-{column:D2}" )
			);
		}

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 32, output.HyperlinkWrites.Count );
		Assert.Equal(
			32,
			output.HyperlinkWrites
				.Select( write => write.Hyperlink.Identifier )
				.Distinct( StringComparer.Ordinal )
				.Count()
		);
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
		return new TerminalDescriptionBuilder( "semantic-application-acceptance" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ClearToEndOfLine, "<el>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.EnterBoldMode, "<b>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class SemanticRecordingOutput
		: ITerminalOutput,
		  ITerminalHyperlinkOutput {
		private readonly StringBuilder text = new();
		private readonly List<HyperlinkWrite> hyperlinkWrites = [];

		internal IReadOnlyList<HyperlinkWrite> HyperlinkWrites => hyperlinkWrites;

		internal string Text => text.ToString();

		internal void Clear() {
			text.Clear();
			hyperlinkWrites.Clear();
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
			return ValueTask.CompletedTask;
		}
	}

	private sealed record HyperlinkWrite(
		string Text,
		CursesHyperlink Hyperlink
	);
}
