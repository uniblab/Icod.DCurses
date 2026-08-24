using System.Text;
using Icod.DCurses.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesPresentationTests {
	[Fact]
	public async Task AlertsPreferRequestedCapability() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync(
			CreateFullTerminal(),
			output
		);

		Assert.True(
			await session.AlertAsync( CursesAlertKind.Audible )
		);
		Assert.Equal( "<bell>", output.Text );

		output.Clear();

		Assert.True(
			await session.AlertAsync( CursesAlertKind.Visual )
		);
		Assert.Equal( "<flash>", output.Text );
	}

	[Fact]
	public async Task AudibleAlertFallsBackToVisualCapability() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder(
			"flash-only"
		)
			.SetString(
				StringCapability.FlashScreen,
				"<flash>"
			)
			.Build();

		await using CursesSession session = await OpenSessionAsync(
			terminal,
			output
		);

		Assert.True(
			await session.AlertAsync( CursesAlertKind.Audible )
		);
		Assert.Equal( "<flash>", output.Text );
	}

	[Fact]
	public async Task CursorPresentationUsesTerminfoCapabilities() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync(
			CreateFullTerminal(),
			output
		);

		Assert.True(
			await session.SetCursorVisibilityAsync(
				CursesCursorVisibility.Hidden
			)
		);
		Assert.Equal( "<hide>", output.Text );

		output.Clear();

		Assert.True(
			await session.SetCursorVisibilityAsync(
				CursesCursorVisibility.VeryVisible
			)
		);
		Assert.Equal( "<very>", output.Text );

		output.Clear();

		Assert.True(
			await session.SetCursorPositionAsync(
				2,
				5
			)
		);
		Assert.Equal( "<cup:2,5>", output.Text );
		Assert.Equal( 2, session.StandardScreen.CursorRow );
		Assert.Equal( 5, session.StandardScreen.CursorColumn );
	}

	[Fact]
	public async Task RenditionResetUsesAttributeAndColorResetCapabilities() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync(
			CreateFullTerminal(),
			output
		);

		Assert.True(
			await session.ResetRenditionAsync()
		);

		Assert.Contains( "<sgr0>", output.Text );
		Assert.Contains( "<op>", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task ReversiblePresentationModesUseMatchingCapabilities() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync(
			CreateFullTerminal(),
			output
		);

		Assert.True(
			await session.SetAlternateScreenAsync( true )
		);
		Assert.Equal( "<alternate>", output.Text );

		output.Clear();

		Assert.True(
			await session.SetAlternateScreenAsync( false )
		);
		Assert.Equal( "</alternate>", output.Text );

		output.Clear();

		Assert.True(
			await session.SetKeypadModeAsync( true )
		);
		Assert.Equal( "<keypad>", output.Text );

		output.Clear();

		Assert.True(
			await session.SetKeypadModeAsync( false )
		);
		Assert.Equal( "</keypad>", output.Text );
	}

	[Fact]
	public async Task OneWayModesAreNotEntered() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder(
			"one-way"
		)
			.SetString(
				StringCapability.EnterCursorAddressingMode,
				"<alternate>"
			)
			.SetString(
				StringCapability.EnterKeypadMode,
				"<keypad>"
			)
			.SetString(
				StringCapability.CursorInvisible,
				"<hide>"
			)
			.Build();

		await using CursesSession session = await OpenSessionAsync(
			terminal,
			output
		);

		Assert.False(
			await session.SetAlternateScreenAsync( true )
		);
		Assert.False(
			await session.SetKeypadModeAsync( true )
		);
		Assert.False(
			await session.SetCursorVisibilityAsync(
				CursesCursorVisibility.Hidden
			)
		);
		Assert.Empty( output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task MissingCapabilitiesReturnFalseWithoutOutput() {
		RecordingOutput output = new();
		TerminalDescription terminal = new TerminalDescriptionBuilder(
			"minimal"
		).Build();

		await using CursesSession session = await OpenSessionAsync(
			terminal,
			output
		);

		Assert.False(
			await session.AlertAsync()
		);
		Assert.False(
			await session.SetCursorVisibilityAsync(
				CursesCursorVisibility.Normal
			)
		);
		Assert.False(
			await session.SetCursorPositionAsync(
				0,
				0
			)
		);
		Assert.False(
			await session.ResetRenditionAsync()
		);
		Assert.False(
			await session.SetAlternateScreenAsync( true )
		);
		Assert.False(
			await session.SetKeypadModeAsync( true )
		);

		Assert.Empty( output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task AlternateScreenTransitionForcesSubsequentRepaint() {
		RecordingOutput output = new();
		await using CursesSession session = await OpenSessionAsync(
			CreateFullTerminal(),
			output
		);

		session.StandardScreen.Write( "A" );
		await session.RefreshAsync();
		output.Clear();

		Assert.True(
			await session.SetAlternateScreenAsync( true )
		);
		output.Clear();

		await session.RefreshAsync();

		Assert.Contains( "<cup:0,0>", output.Text );
		Assert.Contains( "A", output.Text );
	}

	private static ValueTask<CursesSession> OpenSessionAsync(
		TerminalDescription terminal,
		RecordingOutput output ) {
		ArgumentNullException.ThrowIfNull( terminal );
		ArgumentNullException.ThrowIfNull( output );

		return CursesSession.OpenAsync(
			new TerminalBackend(
				new TerminalEndpoint( "test input", true ),
				new TerminalEndpoint( "test output", true ),
				terminal,
				new FakeTerminalInput(),
				output,
				new FakeDimensionProvider(),
				new FakeSessionModeController()
			),
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false
			}
		);
	}

	private static TerminalDescription CreateFullTerminal() {
		return new TerminalDescriptionBuilder( "presentation-test" )
			.SetString(
				StringCapability.Bell,
				"<bell>"
			)
			.SetString(
				StringCapability.FlashScreen,
				"<flash>"
			)
			.SetString(
				StringCapability.CursorInvisible,
				"<hide>"
			)
			.SetString(
				StringCapability.CursorNormal,
				"<normal>"
			)
			.SetString(
				StringCapability.CursorVeryVisible,
				"<very>"
			)
			.SetString(
				StringCapability.CursorAddress,
				"<cup:%p1%d,%p2%d>"
			)
			.SetString(
				StringCapability.ExitAttributeMode,
				"<sgr0>"
			)
			.SetString(
				StringCapability.OriginalColorPair,
				"<op>"
			)
			.SetString(
				StringCapability.EnterCursorAddressingMode,
				"<alternate>"
			)
			.SetString(
				StringCapability.ExitCursorAddressingMode,
				"</alternate>"
			)
			.SetString(
				StringCapability.EnterKeypadMode,
				"<keypad>"
			)
			.SetString(
				StringCapability.ExitKeypadMode,
				"</keypad>"
			)
			.Build();
	}

	private sealed class FakeTerminalInput
		: ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class RecordingOutput
		: ITerminalOutput {
		private readonly List<byte> bytes = [];

		internal int FlushCount {
			get;
			private set;
		}

		internal string Text => Encoding.UTF8.GetString(
			bytes.ToArray()
		);

		internal void Clear() {
			bytes.Clear();
			FlushCount = 0;
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			bytes.AddRange( buffer.ToArray() );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeDimensionProvider
		: ITerminalDimensionProvider {
		public TerminalBackendResult<TerminalSize> GetDimensions() {
			return TerminalBackendResult<TerminalSize>.Available(
				new TerminalSize(
					8,
					4
				)
			);
		}
	}

	private sealed class FakeSessionModeController
		: ITerminalSessionModeController {
		public TerminalBackendResult<ITerminalModeState> CaptureMode() {
			return TerminalBackendResult<ITerminalModeState>.Available(
				new FakeTerminalModeState()
			);
		}

		public TerminalBackendMutationResult ApplySessionMode(
			ITerminalModeState baseline,
			CursesInputMode inputMode,
			bool echoInput ) {
			ArgumentNullException.ThrowIfNull( baseline );
			if ( !Enum.IsDefined( inputMode ) ) {
				throw new ArgumentOutOfRangeException( nameof( inputMode ) );
			}

			return TerminalBackendMutationResult.Success();
		}

		public TerminalBackendMutationResult RestoreMode(
			ITerminalModeState state,
			TerminalModeApplyTiming timing ) {
			ArgumentNullException.ThrowIfNull( state );
			if ( !Enum.IsDefined( timing ) ) {
				throw new ArgumentOutOfRangeException( nameof( timing ) );
			}

			return TerminalBackendMutationResult.Success();
		}
	}

	private sealed class FakeTerminalModeState
		: ITerminalModeState {
	}
}
