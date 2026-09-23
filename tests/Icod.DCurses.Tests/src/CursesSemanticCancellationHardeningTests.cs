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
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises cancellation that arrives after one bounded semantic run has completed.</summary>
public sealed class CursesSemanticCancellationHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string HyperlinkEnd = "\u001b]8;;\u001b\\";

	[Fact]
	public async Task PreCancelledRefreshEmitsNothingAndRetainsCompleteDamage() {
		RecordingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output,
				useSynchronizedOutput: true
			);
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.Write( "AB" );
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => refreshContext.Engine.RefreshAsync(
				screen,
				0,
				0,
				cancellation.Token
			).AsTask()
		);

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );

		await refreshContext.Engine.RefreshAsync( screen, 0, 0 );

		Assert.StartsWith( SynchronizedOutputBegin, output.Text );
		Assert.Contains( "AB", output.Text, StringComparison.Ordinal );
		Assert.EndsWith( SynchronizedOutputEnd, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task CancellationAfterLinkedRunDoesNotInterruptEnteredTransaction() {
		using CancellationTokenSource cancellation = new();
		CancelAfterHyperlinkOutput output = new( cancellation );
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output,
				useSynchronizedOutput: true
			);
		CursesRefreshEngine engine = refreshContext.Engine;
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.WriteWithMetadata(
			"A",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/cancel",
					"cancel"
				)
			)
		);
		window.Write( "B" );

		await engine.RefreshAsync(
			screen,
			0,
			0,
			cancellation.Token
		);
		Assert.True( cancellation.IsCancellationRequested );
		Assert.Equal( [ "A" ], output.HyperlinkWrites );
		Assert.Equal(
			[
				SynchronizedOutputBegin,
				"\u001b]8;id=cancel;https://example.test/cancel\u001b\\",
				"A",
				HyperlinkEnd,
				"B  ",
				SynchronizedOutputEnd
			],
			output.Writes.Where(
				value => SynchronizedOutputBegin == value
					|| SynchronizedOutputEnd == value
					|| value.StartsWith( "\u001b]8;", StringComparison.Ordinal )
					|| "A" == value
					|| value.StartsWith( "B", StringComparison.Ordinal )
			).ToArray()
		);
		Assert.Equal( 1, output.FlushCount );

		output.CancelAfterHyperlink = false;
		output.Clear();
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Empty( output.HyperlinkWrites );
		Assert.Empty( output.Writes );
		Assert.Equal( 0, output.FlushCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "semantic-cancellation-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class CancelAfterHyperlinkOutput : ITerminalOutput {
		private readonly CancellationTokenSource cancellation;
		private readonly List<string> hyperlinkWrites = [];
		private readonly List<string> writes = [];
		private bool hyperlinkActive;

		internal CancelAfterHyperlinkOutput(
			CancellationTokenSource cancellation
		) {
			ArgumentNullException.ThrowIfNull( cancellation );
			this.cancellation = cancellation;
		}

		internal bool CancelAfterHyperlink {
			get;
			set;
		} = true;

		internal IReadOnlyList<string> HyperlinkWrites => this.hyperlinkWrites;

		internal IReadOnlyList<string> Writes => this.writes;

		internal int FlushCount {
			get;
			private set;
		}

		internal void Clear() {
			this.hyperlinkWrites.Clear();
			this.writes.Clear();
			this.hyperlinkActive = false;
			this.FlushCount = 0;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			string value = Encoding.UTF8.GetString( buffer.Span );
			this.writes.Add( value );
			if ( "\u001b]8;;\u001b\\" == value ) {
				this.hyperlinkActive = false;
			} else if ( value.StartsWith( "\u001b]8;", StringComparison.Ordinal ) ) {
				this.hyperlinkActive = true;
			} else if ( this.hyperlinkActive ) {
				this.hyperlinkWrites.Add( value );
				if ( this.CancelAfterHyperlink ) {
					this.cancellation.Cancel();
				}
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			++this.FlushCount;
			return ValueTask.CompletedTask;
		}
	}
}
