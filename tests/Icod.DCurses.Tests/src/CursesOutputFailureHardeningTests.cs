using System.Text;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Promotes refresh/output failure recovery to session-level 0.8 acceptance.</summary>
public sealed class CursesOutputFailureHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";

	[Fact]
	public async Task PartialRefreshFailureInvalidatesPhysicalStateForCompleteRetry() {
		SelectiveFailingOutput output = new();
		CountingTerminalControlProvider provider = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output,
			CreateRenditionTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Write( "X" );
		session.StandardScreen.Write(
			"Y",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		output.FailOnceWhen(
			value => "<bold>" == value
		);

		_ = await Assert.ThrowsAsync<IOException>(
			() => session.RefreshAsync().AsTask()
		);

		output.Clear();
		await session.RefreshAsync();

		Assert.Contains( "<cup:0,0>", output.Text );
		Assert.Contains( "X", output.Text );
		Assert.Contains( "<bold>", output.Text );
		Assert.Contains( "Y", output.Text );
	}

	[Fact]
	public async Task SynchronizedRefreshPreservesBodyAndEndFailures() {
		SelectiveFailingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new CountingTerminalControlProvider(),
			output,
			CreateRenditionTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				UseSynchronizedOutput = true
			}
		);
		session.StandardScreen.Write( "X" );
		output.FailOnceWhen(
			value => value.Contains( "<cup:", StringComparison.Ordinal ),
			new IOException( "refresh body failure" )
		);
		output.FailOnceWhen(
			value => SynchronizedOutputEnd == value,
			new IOException( "synchronized-output end failure" )
		);

		AggregateException exception = await Assert.ThrowsAsync<AggregateException>(
			() => session.RefreshAsync().AsTask()
		);

		Assert.Equal( 2, exception.InnerExceptions.Count );
		Assert.Contains(
			exception.InnerExceptions,
			current => "refresh body failure" == current.Message
		);
		Assert.Contains(
			exception.InnerExceptions,
			current => "synchronized-output end failure" == current.Message
		);
		Assert.Contains( SynchronizedOutputBegin, output.Text );
	}

	[Fact]
	public async Task DisposalRestoresTerminalModeAfterRenditionResetFailure() {
		SelectiveFailingOutput output = new();
		CountingTerminalControlProvider provider = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output,
			CreateRenditionTerminal()
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Write(
			"X",
			new CursesStyle(
				CursesColor.Default,
				CursesColor.Default,
				CursesTextAttributes.Bold
			)
		);
		await session.RefreshAsync();
		output.FailOnceWhen(
			value => "<sgr0>" == value,
			new IOException( "rendition reset failure" )
		);

		Exception exception = await Assert.ThrowsAnyAsync<Exception>(
			() => session.DisposeAsync().AsTask()
		);

		Assert.Contains(
			"rendition reset failure",
			exception.ToString(),
			StringComparison.Ordinal
		);
		Assert.Equal( 2, provider.SetModeCount );
	}

	[Fact]
	public async Task EndOfInputRemainsAStableInputEvent() {
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new CountingTerminalControlProvider(),
			new SelectiveFailingOutput(),
			TerminalProfiles.Dumb,
			new EndOfInput()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		CursesEvent terminalEvent = await session.ReadEventAsync();

		Assert.Equal( CursesEventKind.Input, terminalEvent.Kind );
		Assert.Equal( CursesInputEventKind.EndOfInput, terminalEvent.Input!.Kind );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		CountingTerminalControlProvider provider,
		ITerminalOutput output,
		TerminalDescription terminal,
		ITerminalInput? input = null
	) {
		ArgumentNullException.ThrowIfNull( provider );
		ArgumentNullException.ThrowIfNull( output );
		ArgumentNullException.ThrowIfNull( terminal );
		return TerminalSession.OpenAsync(
			provider,
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			input ?? new EndOfInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = terminal,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static TerminalDescription CreateRenditionTerminal() {
		return new TerminalDescriptionBuilder( "hardening-output-failure" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
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

	private sealed class SelectiveFailingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];
		private readonly List<FailureRule> rules = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.Latin1.GetString( this.bytes.ToArray() );
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
			Exception? exception = null
		) {
			ArgumentNullException.ThrowIfNull( predicate );
			lock ( this.sync ) {
				this.rules.Add(
					new FailureRule(
						predicate,
						exception ?? new IOException( "injected output failure" )
					)
				);
			}
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			string value = Encoding.Latin1.GetString( buffer.Span );
			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
				for ( int index = 0; index < this.rules.Count; ++index ) {
					FailureRule rule = this.rules[ index ];
					if ( !rule.Predicate( value ) ) {
						continue;
					}

					this.rules.RemoveAt( index );
					throw rule.Exception;
				}
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

	private sealed class CountingTerminalControlProvider : ITerminalControlProvider {
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
		private int setModeCount;

		internal int SetModeCount {
			get {
				return Volatile.Read( ref this.setModeCount );
			}
		}

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
				new TerminalSize( 80, 24 )
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
			Interlocked.Increment( ref this.setModeCount );
			return TerminalControlMutationResult.Success();
		}
	}
}
