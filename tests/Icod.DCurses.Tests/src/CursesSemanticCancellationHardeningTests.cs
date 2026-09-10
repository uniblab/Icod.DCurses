using System.Text;
using Icod.DCurses.Internal;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises cancellation that arrives after one bounded semantic run has completed.</summary>
public sealed class CursesSemanticCancellationHardeningTests {
	[Fact]
	public async Task CancellationAfterLinkedRunInvalidatesForCompleteLaterRepaint() {
		using CancellationTokenSource cancellation = new();
		CancelAfterHyperlinkOutput output = new( cancellation );
		CursesRefreshEngine engine = new(
			CreateTerminal(),
			output
		);
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

		_ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => engine.RefreshAsync(
				screen,
				0,
				0,
				cancellation.Token
			).AsTask()
		);
		Assert.Equal( [ "A" ], output.HyperlinkWrites );
		Assert.DoesNotContain( "B", output.Text, StringComparison.Ordinal );

		output.CancelAfterHyperlink = false;
		output.Clear();
		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( [ "A" ], output.HyperlinkWrites );
		Assert.Contains( "B", output.Text, StringComparison.Ordinal );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "semantic-cancellation-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class CancelAfterHyperlinkOutput
		: ITerminalOutput,
		  ITerminalHyperlinkOutput {
		private readonly CancellationTokenSource cancellation;
		private readonly StringBuilder text = new();
		private readonly List<string> hyperlinkWrites = [];

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

		internal string Text => this.text.ToString();

		internal void Clear() {
			this.text.Clear();
			this.hyperlinkWrites.Clear();
		}

		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			ArgumentNullException.ThrowIfNull( value );
			cancellationToken.ThrowIfCancellationRequested();
			this.text.Append( value );
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
			this.text.Append( value );
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
			this.hyperlinkWrites.Add( value );
			if ( this.CancelAfterHyperlink ) {
				this.cancellation.Cancel();
			}
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
