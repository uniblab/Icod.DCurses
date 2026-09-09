using System.Text;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Proves retained DCurses hyperlinks flow through Terminal's public semantic OSC 8 ownership.</summary>
public sealed class CursesTerminalHyperlinkIntegrationTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string HyperlinkBegin = "\u001b]8;id=docs;https://example.test/docs\u001b\\";
	private const string HyperlinkEnd = "\u001b]8;;\u001b\\";

	[Fact]
	public async Task SynchronizedRefreshUsesTerminalOwnedBoundedHyperlinkFraming() {
		RecordingTerminalOutput output = new();
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
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
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				UseSynchronizedOutput = true
			}
		);
		output.Clear();

		session.StandardScreen.Write(
			"link",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/docs",
					"docs"
				)
			)
		);
		await session.RefreshAsync();

		string text = output.Text;
		int synchronizedBegin = text.IndexOf(
			SynchronizedOutputBegin,
			StringComparison.Ordinal
		);
		int hyperlinkBegin = text.IndexOf(
			HyperlinkBegin,
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
		int synchronizedEnd = text.IndexOf(
			SynchronizedOutputEnd,
			hyperlinkEnd + HyperlinkEnd.Length,
			StringComparison.Ordinal
		);

		Assert.True( 0 <= synchronizedBegin );
		Assert.True( synchronizedBegin < hyperlinkBegin );
		Assert.True( hyperlinkBegin < payload );
		Assert.True( payload < hyperlinkEnd );
		Assert.True( hyperlinkEnd < synchronizedEnd );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "dcurses-hyperlink-integration" )
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

	private sealed class RecordingTerminalOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];

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

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			lock ( this.sync ) {
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
			return TerminalControlMutationResult.Success();
		}
	}
}
