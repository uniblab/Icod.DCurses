using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// End-to-end acceptance coverage for negotiated Terminal 1.0 keyboard semantics through DCurses.
/// </summary>
public sealed class CursesModernKeyboardAcceptanceTests {
	private const string ProbeRequest = "\u001b[?u\u001b[c";
	private const string PushAllKeys = "\u001b[>31u";
	private const string PopKeyboard = "\u001b[<u";

	[Fact]
	public async Task NegotiatedModernKeyboardAndRichInputRoundTripThroughCursesSession() {
		DuplexTerminalTransport transport = new();
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new RecordingTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			transport,
			transport,
			new TerminalSessionOptions {
				TerminalOverride = CreateRichInputTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);

		TerminalControlResult<CursesInputProtocolLease> result =
			await session.AcquireInputProtocolsAsync(
				new CursesInputProtocolOptions {
					BracketedPaste = true,
					FocusReporting = true,
					MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents,
					KeyboardReportingMode = CursesKeyboardReportingMode.AllKeys
				}
			);

		Assert.True( result.IsAvailable );
		CursesInputProtocolLease lease = result.GetRequiredValue();
		Assert.True( lease.BracketedPaste );
		Assert.True( lease.FocusReporting );
		Assert.Equal( CursesMouseTrackingMode.ButtonEvents, lease.MouseTrackingMode );
		Assert.Equal( CursesKeyboardReportingMode.AllKeys, lease.KeyboardReportingMode );
		Assert.Contains( ProbeRequest, transport.Text );
		Assert.Contains( PushAllKeys, transport.Text );
		Assert.Contains( "<P+>", transport.Text );
		Assert.Contains( "<F+>", transport.Text );
		Assert.Contains( "\u001b[?1006h", transport.Text );
		Assert.Contains( "\u001b[?1000h", transport.Text );

		transport.QueueInput(
			Encoding.UTF8.GetBytes(
				"\u001b[97:65:113;6:2;120u"
					+ "\u001b[57430;256:1u"
					+ "\u001b[57358;1:1u"
					+ "\u001b[57441;1:1u"
					+ "\u001b[57452;1:1u"
					+ "\u001b[57414;1:3u"
					+ "\u001b[57455;17:3u"
					+ "<f0><f63>"
					+ "\u001b[I"
					+ "\u001b[200~ok\u001b[201~"
					+ "\u001b[<0;3;4M"
			)
		);
		using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 5 ) );

		CursesInputEvent character = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesInputEventKind.Key, character.Kind );
		Assert.Equal( CursesKey.Character, character.Key );
		Assert.Equal( new Rune( 'a' ), character.Character );
		Assert.Equal( new Rune( 'A' ), character.ShiftedCharacter );
		Assert.Equal( new Rune( 'q' ), character.BaseLayoutCharacter );
		Assert.Equal( "x", character.AssociatedText );
		Assert.Equal(
			CursesKeyModifiers.Shift | CursesKeyModifiers.Control,
			character.Modifiers
		);
		Assert.Equal( CursesKeyEventPhase.Repeat, character.KeyPhase );

		CursesInputEvent media = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		CursesKeyModifiers allModifiers =
			CursesKeyModifiers.Shift
				| CursesKeyModifiers.Control
				| CursesKeyModifiers.Alt
				| CursesKeyModifiers.Super
				| CursesKeyModifiers.Hyper
				| CursesKeyModifiers.Meta
				| CursesKeyModifiers.CapsLock
				| CursesKeyModifiers.NumLock;
		Assert.Equal( CursesKey.MediaPlayPause, media.Key );
		Assert.Equal( allModifiers, media.Modifiers );
		Assert.Equal( CursesKeyEventPhase.Press, media.KeyPhase );

		CursesInputEvent lockKey = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.CapsLock, lockKey.Key );
		Assert.Equal( CursesKeyEventPhase.Press, lockKey.KeyPhase );

		CursesInputEvent leftModifier = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.LeftShift, leftModifier.Key );
		Assert.Equal( CursesKeyEventPhase.Press, leftModifier.KeyPhase );

		CursesInputEvent rightModifier = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.RightMeta, rightModifier.Key );
		Assert.Equal( CursesKeyEventPhase.Press, rightModifier.KeyPhase );

		CursesInputEvent keypad = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.KeypadEnter, keypad.Key );
		Assert.Equal( CursesKeyEventPhase.Release, keypad.KeyPhase );
		Assert.Null( keypad.Character );

		CursesInputEvent unrecognized = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.Unrecognized, unrecognized.Key );
		Assert.Equal( CursesKeyModifiers.Hyper, unrecognized.Modifiers );
		Assert.Equal( CursesKeyEventPhase.Release, unrecognized.KeyPhase );

		CursesInputEvent function0 = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		CursesInputEvent function63 = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesKey.Function, function0.Key );
		Assert.Equal( 0, function0.FunctionKeyNumber );
		Assert.Equal( CursesKeyEventPhase.Press, function0.KeyPhase );
		Assert.Equal( CursesKey.Function, function63.Key );
		Assert.Equal( 63, function63.FunctionKeyNumber );
		Assert.Equal( CursesKeyEventPhase.Press, function63.KeyPhase );

		CursesInputEvent focus = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesInputEventKind.Focus, focus.Kind );
		Assert.Equal( CursesFocusState.Focused, focus.Focus!.State );

		CursesInputEvent pasteBegin = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		CursesInputEvent pasteData = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		CursesInputEvent pasteEnd = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesPastePhase.Begin, pasteBegin.Paste!.Phase );
		Assert.Equal( CursesPastePhase.Data, pasteData.Paste!.Phase );
		Assert.Equal( "ok", pasteData.Paste.Text );
		Assert.Equal( CursesPastePhase.End, pasteEnd.Paste!.Phase );

		CursesInputEvent mouse = RequireInput(
			await session.ReadEventAsync( timeout.Token )
		);
		Assert.Equal( CursesInputEventKind.Mouse, mouse.Kind );
		Assert.Equal( CursesMouseAction.Press, mouse.Mouse!.Action );
		Assert.Equal( CursesMouseButton.Primary, mouse.Mouse.Button );
		Assert.Equal( 2, mouse.Mouse.Column );
		Assert.Equal( 3, mouse.Mouse.Row );

		transport.ClearWrites();
		await session.DisposeAsync();

		Assert.Contains( PopKeyboard, transport.Text );
		Assert.Contains( "\u001b[?1000l", transport.Text );
		Assert.Contains( "\u001b[?1006l", transport.Text );
		Assert.Contains( "<F->", transport.Text );
		Assert.Contains( "<P->", transport.Text );

		transport.ClearWrites();
		await lease.DisposeAsync();
		Assert.Empty( transport.Text );
	}

	private static CursesInputEvent RequireInput(
		CursesEvent cursesEvent
	) {
		ArgumentNullException.ThrowIfNull( cursesEvent );
		if ( CursesEventKind.Input != cursesEvent.Kind || cursesEvent.Input is null ) {
			throw new InvalidOperationException(
				"Expected a curses input event from the acceptance transport."
			);
		}

		return cursesEvent.Input;
	}

	private static TerminalDescription CreateRichInputTerminal() {
		return new TerminalDescriptionBuilder( "dcurses-modern-keyboard-test" )
			.SetString( StringCapability.KeyF0, "<f0>" )
			.SetString( StringCapability.KeyF63, "<f63>" )
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
			.Build();
	}

	private sealed class DuplexTerminalTransport : ITerminalInput, ITerminalOutput {
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly object sync = new();
		private readonly List<string> writes = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return string.Concat( this.writes );
				}
			}
		}

		internal void QueueInput(
			byte[] value
		) {
			ArgumentNullException.ThrowIfNull( value );
			if ( !this.input.Writer.TryWrite( value.ToArray() ) ) {
				throw new InvalidOperationException(
					"The acceptance input could not be queued."
				);
			}
		}

		internal void ClearWrites() {
			lock ( this.sync ) {
				this.writes.Clear();
			}
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			byte[] value = await this.input.Reader.ReadAsync(
				cancellationToken
			).ConfigureAwait( false );
			if ( value.Length > buffer.Length ) {
				throw new InvalidOperationException(
					"The acceptance input exceeds the terminal read buffer."
				);
			}

			value.AsSpan().CopyTo( buffer.Span );
			return value.Length;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			string value = Encoding.Latin1.GetString( buffer.Span );
			lock ( this.sync ) {
				this.writes.Add( value );
			}

			if ( value.Contains( ProbeRequest, StringComparison.Ordinal ) ) {
				this.QueueInput(
					Encoding.ASCII.GetBytes( "\u001b[?0u\u001b[?1;2c" )
				);
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
