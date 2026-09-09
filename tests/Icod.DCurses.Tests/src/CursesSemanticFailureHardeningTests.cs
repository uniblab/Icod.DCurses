using System.Text;
using Icod.DCurses.Terminal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;
using RawTerminalOutput = Icod.Terminal.ITerminalOutput;

namespace Icod.DCurses.Tests;

/// <summary>Exercises T1106 semantic-output failure, cancellation, cleanup, and recovery rules.</summary>
public sealed class CursesSemanticFailureHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string HyperlinkBegin = "\u001b]8;id=docs;https://example.test/docs\u001b\\";
	private const string HyperlinkEnd = "\u001b]8;;\u001b\\";

	[Fact]
	public async Task HyperlinkTextAndCleanupFailuresRemainVisibleThroughCursesRefresh() {
		SelectiveFailingRawOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			CreateCursesOptions()
		);
		output.Clear();
		WriteLinkedText( session );
		output.FailOnceWhen(
			value => "link" == value,
			new IOException( "hyperlink text failure" )
		);
		output.FailOnceWhen(
			value => HyperlinkEnd == value,
			new IOException( "hyperlink cleanup failure" )
		);

		AggregateException exception = await Assert.ThrowsAsync<AggregateException>(
			() => session.RefreshAsync().AsTask()
		);

		Assert.Equal( 2, exception.InnerExceptions.Count );
		Assert.Contains(
			exception.InnerExceptions,
			current => "hyperlink text failure" == current.Message
		);
		Assert.Contains(
			exception.InnerExceptions,
			current => "hyperlink cleanup failure" == current.Message
		);
		Assert.Contains( HyperlinkBegin, output.Text );

		output.Clear();
		await session.DisposeAsync();

		Assert.Contains( HyperlinkEnd, output.Text );
	}

	[Fact]
	public async Task HyperlinkCleanupFailureFailsClosedUntilSessionDisposal() {
		SelectiveFailingRawOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			CreateCursesOptions()
		);
		output.Clear();
		WriteLinkedText( session );
		output.FailOnceWhen(
			value => HyperlinkEnd == value,
			new IOException( "hyperlink cleanup failure" )
		);

		IOException firstFailure = await Assert.ThrowsAsync<IOException>(
			() => session.RefreshAsync().AsTask()
		);
		Assert.Equal( "hyperlink cleanup failure", firstFailure.Message );

		output.Clear();
		InvalidOperationException secondFailure = await Assert.ThrowsAsync<InvalidOperationException>(
			() => session.RefreshAsync().AsTask()
		);

		Assert.Contains(
			"prior Terminal hyperlink operation failed",
			secondFailure.Message,
			StringComparison.Ordinal
		);
		Assert.IsType<IOException>( secondFailure.InnerException );
		Assert.DoesNotContain( HyperlinkBegin, output.Text );
		Assert.DoesNotContain( "link", output.Text, StringComparison.Ordinal );

		output.Clear();
		await session.DisposeAsync();

		Assert.Contains( HyperlinkEnd, output.Text );
	}

	[Fact]
	public async Task CallerCancellationBeforeHyperlinkTransmissionDoesNotPoisonSemanticOutput() {
		SelectiveFailingRawOutput output = new();
		await using TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		TerminalSessionCursesOutput cursesOutput = new( terminalSession );
		CursesHyperlink hyperlink = new(
			"https://example.test/docs",
			"docs"
		);
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		_ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => cursesOutput.WriteHyperlinkTextAsync(
				"link",
				hyperlink,
				cancellation.Token
			).AsTask()
		);
		Assert.DoesNotContain( HyperlinkBegin, output.Text );

		output.Clear();
		await cursesOutput.WriteHyperlinkTextAsync(
			"link",
			hyperlink
		);

		Assert.Contains( HyperlinkBegin, output.Text );
		Assert.Contains( "link", output.Text, StringComparison.Ordinal );
		Assert.Contains( HyperlinkEnd, output.Text );
	}

	[Fact]
	public async Task SynchronizedOutputEndFailureIsRetriedBeforeNextRefresh() {
		SelectiveFailingRawOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			CreateCursesOptions( useSynchronizedOutput: true )
		);
		output.Clear();
		WriteLinkedText( session );
		output.FailOnceWhen(
			value => SynchronizedOutputEnd == value,
			new IOException( "synchronized-output end failure" )
		);

		IOException firstFailure = await Assert.ThrowsAsync<IOException>(
			() => session.RefreshAsync().AsTask()
		);
		Assert.Equal( "synchronized-output end failure", firstFailure.Message );

		output.Clear();
		await session.RefreshAsync();

		string text = output.Text;
		int cleanupRetry = text.IndexOf(
			SynchronizedOutputEnd,
			StringComparison.Ordinal
		);
		int newBegin = text.IndexOf(
			SynchronizedOutputBegin,
			cleanupRetry + SynchronizedOutputEnd.Length,
			StringComparison.Ordinal
		);
		int hyperlinkBegin = text.IndexOf(
			HyperlinkBegin,
			newBegin + SynchronizedOutputBegin.Length,
			StringComparison.Ordinal
		);
		int payload = text.IndexOf(
			"link",
			hyperlinkBegin + HyperlinkBegin.Length,
			StringComparison.Ordinal
		);
		int hyperlinkEnd = text.IndexOf(
			HyperlinkEnd,
			payload + 4,
			StringComparison.Ordinal
		);
		int finalEnd = text.IndexOf(
			SynchronizedOutputEnd,
			hyperlinkEnd + HyperlinkEnd.Length,
			StringComparison.Ordinal
		);

		Assert.Equal( 0, cleanupRetry );
		Assert.True( cleanupRetry < newBegin );
		Assert.True( newBegin < hyperlinkBegin );
		Assert.True( hyperlinkBegin < payload );
		Assert.True( payload < hyperlinkEnd );
		Assert.True( hyperlinkEnd < finalEnd );
	}

	[Fact]
	public async Task RepeatedSynchronizedOutputCleanupFailureBlocksNewRefreshBody() {
		SelectiveFailingRawOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			CreateCursesOptions( useSynchronizedOutput: true )
		);
		output.Clear();
		WriteLinkedText( session );
		output.FailOnceWhen(
			value => SynchronizedOutputEnd == value,
			new IOException( "first synchronized-output end failure" )
		);
		output.FailOnceWhen(
			value => SynchronizedOutputEnd == value,
			new IOException( "retry synchronized-output end failure" )
		);

		_ = await Assert.ThrowsAsync<IOException>(
			() => session.RefreshAsync().AsTask()
		);
		output.Clear();

		IOException retryFailure = await Assert.ThrowsAsync<IOException>(
			() => session.RefreshAsync().AsTask()
		);

		Assert.Equal( "retry synchronized-output end failure", retryFailure.Message );
		Assert.DoesNotContain( SynchronizedOutputBegin, output.Text );
		Assert.DoesNotContain( HyperlinkBegin, output.Text );
		Assert.DoesNotContain( "link", output.Text, StringComparison.Ordinal );

		output.Clear();
		await session.RefreshAsync();

		Assert.StartsWith(
			SynchronizedOutputEnd,
			output.Text,
			StringComparison.Ordinal
		);
		Assert.Contains( SynchronizedOutputBegin, output.Text );
		Assert.Contains( HyperlinkBegin, output.Text );
		Assert.Contains( "link", output.Text, StringComparison.Ordinal );
	}

	[Fact]
	public async Task SuspendResumeInvalidatesRetainedLinkedContentForRepaint() {
		SelectiveFailingRawOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			CreateCursesOptions()
		);
		output.Clear();
		WriteLinkedText( session );
		await session.RefreshAsync();

		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
		output.Clear();

		await session.RefreshAsync();

		Assert.Contains( HyperlinkBegin, output.Text );
		Assert.Contains( "link", output.Text, StringComparison.Ordinal );
		Assert.Contains( HyperlinkEnd, output.Text );
	}

	private static void WriteLinkedText( CursesSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		session.StandardScreen.Move(
			0,
			0
		);
		session.StandardScreen.Write(
			"link",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/docs",
					"docs"
				)
			)
		);
	}

	private static CursesSessionOptions CreateCursesOptions(
		bool useSynchronizedOutput = false
	) {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false,
			UseSynchronizedOutput = useSynchronizedOutput
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		RawTerminalOutput output
	) {
		ArgumentNullException.ThrowIfNull( output );
		return TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EndOfInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "semantic-failure-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class EndOfInput : ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class SelectiveFailingRawOutput : RawTerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];
		private readonly List<FailureRule> rules = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.UTF8.GetString( this.bytes.ToArray() );
				}
			}
		}

		internal void Clear() {
			lock ( this.sync ) {
				this.bytes.Clear();
			}
		}

		internal void FailOnceWhen(
			Func<string, bool> predicate,
			Exception exception
		) {
			ArgumentNullException.ThrowIfNull( predicate );
			ArgumentNullException.ThrowIfNull( exception );
			lock ( this.sync ) {
				this.rules.Add(
					new FailureRule(
						predicate,
						exception
					)
				);
			}
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			string value = Encoding.UTF8.GetString( buffer.Span );
			lock ( this.sync ) {
				for ( int index = 0; index < this.rules.Count; index++ ) {
					FailureRule rule = this.rules[ index ];
					if ( !rule.Predicate( value ) ) {
						continue;
					}

					this.rules.RemoveAt( index );
					return ValueTask.FromException( rule.Exception );
				}

				this.bytes.AddRange( buffer.ToArray() );
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		private sealed record FailureRule(
			Func<string, bool> Predicate,
			Exception Exception
		);
	}

	private sealed class TestTerminalControlProvider : ITerminalControlProvider {
		private readonly TerminalModeSnapshot baseline = TerminalModeSnapshot.CreatePosix(
			0,
			0,
			0,
			0x0002UL,
			new byte[ 32 ],
			0,
			32,
			0,
			new TerminalSpeed( 13, 9600 ),
			new TerminalSpeed( 13, 9600 )
		);

		public TerminalControlResult<TerminalEndpointObservation> Observe(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalEndpointObservation>.Available(
				new TerminalEndpointObservation(
					true,
					null,
					TerminalPlatformKind.PosixTermios,
					TerminalControlCapabilities.Attachment
						| TerminalControlCapabilities.LiveSize
						| TerminalControlCapabilities.ModeRead
						| TerminalControlCapabilities.ModeWrite
				)
			);
		}

		public TerminalControlResult<TerminalSize> GetSize(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalSize>.Available(
				new TerminalSize( 8, 2 )
			);
		}

		public TerminalControlResult<TerminalModeSnapshot> GetMode(
			TerminalEndpoint endpoint
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			return TerminalControlResult<TerminalModeSnapshot>.Available( this.baseline );
		}

		public TerminalControlMutationResult SetMode(
			TerminalEndpoint endpoint,
			TerminalModeSnapshot mode,
			TerminalModeApplyTiming timing
		) {
			ArgumentNullException.ThrowIfNull( endpoint );
			ArgumentNullException.ThrowIfNull( mode );
			if ( !Enum.IsDefined( timing ) ) {
				throw new ArgumentOutOfRangeException( nameof( timing ) );
			}

			return TerminalControlMutationResult.Success();
		}
	}
}
