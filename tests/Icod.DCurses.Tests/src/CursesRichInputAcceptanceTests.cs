using System.Text;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Acceptance coverage for rich terminal input through the DCurses facade.
/// </summary>
public sealed class CursesRichInputAcceptanceTests {
	[Fact]
	public async Task RichInputFamiliesAndModifiedKeysUseSingleCursesEventStream() {
		string inputText =
			"\u001b[I"
			+ "\u001b[200~hello\u001b[201~"
			+ "\u001b[<0;3;4M"
			+ "\u001b[1;5A";
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new RecordingOutput(),
			new ScriptedInput( Encoding.Latin1.GetBytes( inputText ) ),
			CreateRichInputTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 5 ) );

		CursesInputEvent focus = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		Assert.Equal( CursesInputEventKind.Focus, focus.Kind );
		Assert.Equal( CursesFocusState.Focused, focus.Focus!.State );

		CursesInputEvent pasteBegin = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		CursesInputEvent pasteData = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		CursesInputEvent pasteEnd = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		Assert.Equal( CursesPastePhase.Begin, pasteBegin.Paste!.Phase );
		Assert.Equal( CursesPastePhase.Data, pasteData.Paste!.Phase );
		Assert.Equal( "hello", pasteData.Paste.Text );
		Assert.Equal( CursesPastePhase.End, pasteEnd.Paste!.Phase );

		CursesInputEvent mouse = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		Assert.Equal( CursesInputEventKind.Mouse, mouse.Kind );
		Assert.Equal( CursesMouseAction.Press, mouse.Mouse!.Action );
		Assert.Equal( CursesMouseButton.Primary, mouse.Mouse.Button );
		Assert.Equal( 2, mouse.Mouse.Column );
		Assert.Equal( 3, mouse.Mouse.Row );

		CursesInputEvent modifiedKey = ( await session.ReadEventAsync( timeout.Token ) ).Input!;
		Assert.Equal( CursesInputEventKind.Key, modifiedKey.Kind );
		Assert.Equal( CursesKey.Up, modifiedKey.Key );
		Assert.Equal( CursesKeyModifiers.Control, modifiedKey.Modifiers );
	}

	[Fact]
	public async Task ProtocolLeaseIsDelegatedToTerminalAndReleasedWithSession() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreateRichInputTerminal()
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		TerminalControlResult<CursesInputProtocolLease> result =
			await session.AcquireInputProtocolsAsync(
				new CursesInputProtocolOptions {
					BracketedPaste = true,
					FocusReporting = true,
					MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents
				}
			);

		Assert.True( result.IsAvailable );
		CursesInputProtocolLease lease = result.GetRequiredValue();
		Assert.True( lease.BracketedPaste );
		Assert.True( lease.FocusReporting );
		Assert.Equal( CursesMouseTrackingMode.ButtonEvents, lease.MouseTrackingMode );
		Assert.Contains( "<P+>", output.Text );
		Assert.Contains( "<F+>", output.Text );
		Assert.Contains( "\u001b[?1006h", output.Text );
		Assert.Contains( "\u001b[?1000h", output.Text );

		output.Clear();
		await session.DisposeAsync();

		Assert.Contains( "\u001b[?1000l", output.Text );
		Assert.Contains( "\u001b[?1006l", output.Text );
		Assert.Contains( "<F->", output.Text );
		Assert.Contains( "<P->", output.Text );
		await lease.DisposeAsync();
	}

	[Fact]
	public async Task UnsupportedProtocolRequestRemainsControlled() {
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new RecordingOutput(),
			new EmptyInput(),
			TerminalProfiles.Dumb
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		TerminalControlResult<CursesInputProtocolLease> result =
			await session.AcquireInputProtocolsAsync(
				new CursesInputProtocolOptions {
					BracketedPaste = true
				}
			);

		Assert.False( result.IsAvailable );
		Assert.Equal( TerminalControlStatus.Unavailable, result.Status );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static TerminalDescription CreateRichInputTerminal() {
		return new TerminalDescriptionBuilder( "dcurses-rich-input-test" )
			.SetExtendedString( "BE", "<P+>" )
			.SetExtendedString( "BD", "<P->" )
			.SetExtendedString( "PS", "\u001b[200~" )
			.SetExtendedString( "PE", "\u001b[201~" )
			.SetExtendedString( "fe", "<F+>" )
			.SetExtendedString( "fd", "<F->" )
			.SetExtendedString( "kxIN", "\u001b[I" )
			.SetExtendedString( "kxOUT", "\u001b[O" )
			.SetString( StringCapability.KeyMouse, "\u001b[<" )
			.SetExtendedString(
				"XM",
				"\u001b[?1006;1000%?%p1%{1}%=%th%el%;"
			)
			.SetExtendedString(
				"xm",
				"\u001b[<%i%p3%d;%p1%d;%p2%d;%?%p4%tM%em%;"
			)
			.SetExtendedString( "kUP5", "\u001b[1;5A" )
			.Build();
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		RecordingOutput output,
		ITerminalInput input,
		TerminalDescription terminal
	) {
		ArgumentNullException.ThrowIfNull( output );
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( terminal );

		return TerminalSession.OpenAsync(
			new RecordingTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			input,
			output,
			new TerminalSessionOptions {
				TerminalOverride = terminal,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private sealed class EmptyInput : ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class ScriptedInput : ITerminalInput {
		private readonly byte[] bytes;
		private int offset;

		internal ScriptedInput(
			byte[] bytes
		) {
			ArgumentNullException.ThrowIfNull( bytes );
			this.bytes = bytes;
		}

		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( this.offset >= this.bytes.Length ) {
				return ValueTask.FromResult( 0 );
			}

			int count = Math.Min( buffer.Length, this.bytes.Length - this.offset );
			this.bytes.AsMemory( this.offset, count ).CopyTo( buffer );
			this.offset += count;
			return ValueTask.FromResult( count );
		}
	}

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly List<byte> bytes = [];

		internal string Text => Encoding.Latin1.GetString( this.bytes.ToArray() );

		internal void Clear() {
			this.bytes.Clear();
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.bytes.AddRange( buffer.ToArray() );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class RecordingTerminalControlProvider : ITerminalControlProvider {
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
