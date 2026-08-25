using System.Text;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies the active DCurses-to-Icod.Terminal integration boundary.</summary>
public sealed class CursesTerminalIntegrationTests {
	[Fact]
	public async Task PresentationStateIsOwnedByTerminalLeases() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreatePresentationTerminal()
		);

		CursesSession session = await CursesSession.OpenAsync( terminalSession );
		Assert.Contains( "<alternate>", output.Text );
		Assert.Contains( "<keypad>", output.Text );
		Assert.Contains( "<hide>", output.Text );

		await session.DisposeAsync();
		Assert.Contains( "<show>", output.Text );
		Assert.Contains( "</keypad>", output.Text );
		Assert.Contains( "</alternate>", output.Text );
	}

	[Fact]
	public async Task TerminalInputIsMappedIntoCursesEventFacade() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new ScriptedInput( Encoding.UTF8.GetBytes( "q" ) ),
			TerminalProfiles.Dumb
		);
		CursesSessionOptions options = NoPresentationOptions();
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			options
		);

		CursesEvent input = await session.ReadEventAsync( TimeSpan.FromSeconds( 1 ) );
		Assert.Equal( CursesEventKind.Input, input.Kind );
		Assert.Equal( CursesInputEventKind.Text, input.Input!.Kind );
		Assert.Equal( 'q', (char)input.Input.Character!.Value.Value );
	}

	[Fact]
	public async Task DimensionsComeDirectlyFromTerminalSession() {
		RecordingTerminalControlProvider provider = new() {
			Size = new TerminalSize( 101, 37 )
		};
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new RecordingOutput(),
			new EmptyInput(),
			TerminalProfiles.Dumb,
			provider
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		TerminalControlResult<TerminalSize> dimensions = session.GetDimensions();
		Assert.True( dimensions.IsAvailable );
		Assert.Equal( new TerminalSize( 101, 37 ), dimensions.GetRequiredValue() );
	}

	[Fact]
	public async Task LifecycleParticipantNeutralizesRenditionAndBlocksRefreshUntilResume() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreateRenditionTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
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
		output.Clear();

		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		Assert.Contains( "<sgr0>", output.Text );
		Assert.Contains( "<op>", output.Text );

		Task blockedRefresh = session.RefreshAsync().AsTask();
		await Task.Yield();
		Assert.False( blockedRefresh.IsCompleted );

		await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
		await blockedRefresh;
		Assert.Contains( "<cup:0,0>", output.Text );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		RecordingOutput output,
		Icod.Terminal.ITerminalInput input,
		TerminalDescription terminal,
		RecordingTerminalControlProvider? provider = null
	) {
		ArgumentNullException.ThrowIfNull( output );
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( terminal );

		return TerminalSession.OpenAsync(
			provider ?? new RecordingTerminalControlProvider(),
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

	private static TerminalDescription CreatePresentationTerminal() {
		return new TerminalDescriptionBuilder( "integration-test" )
			.SetString( StringCapability.EnterCursorAddressingMode, "<alternate>" )
			.SetString( StringCapability.ExitCursorAddressingMode, "</alternate>" )
			.SetString( StringCapability.EnterKeypadMode, "<keypad>" )
			.SetString( StringCapability.ExitKeypadMode, "</keypad>" )
			.SetString( StringCapability.CursorInvisible, "<hide>" )
			.SetString( StringCapability.CursorNormal, "<show>" )
			.Build();
	}

	private static TerminalDescription CreateRenditionTerminal() {
		return new TerminalDescriptionBuilder( "rendition-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.Build();
	}

	private sealed class EmptyInput : Icod.Terminal.ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class ScriptedInput : Icod.Terminal.ITerminalInput {
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

	private sealed class RecordingOutput : Icod.Terminal.ITerminalOutput {
		private readonly List<byte> bytes = [];

		internal string Text => Encoding.UTF8.GetString( this.bytes.ToArray() );

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

		internal TerminalSize Size {
			get;
			init;
		} = new TerminalSize( 80, 24 );

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
			return TerminalControlResult<TerminalSize>.Available( this.Size );
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
			return TerminalControlMutationResult.Success();
		}
	}
}
