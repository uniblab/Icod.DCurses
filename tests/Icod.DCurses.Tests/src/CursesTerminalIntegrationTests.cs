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
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies the active DCurses-to-Icod.Terminal integration boundary.</summary>
public sealed class CursesTerminalIntegrationTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";

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
	public async Task DisposalRestoresDefaultsAfterRichPresentation() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreateRenditionTerminal()
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		CursesTextAttributes attributes =
			CursesTextAttributes.Bold
			| CursesTextAttributes.Dim
			| CursesTextAttributes.Underline
			| CursesTextAttributes.Reverse
			| CursesTextAttributes.Standout
			| CursesTextAttributes.Italic
			| CursesTextAttributes.Blink
			| CursesTextAttributes.Conceal
			| CursesTextAttributes.Strikeout;
		session.StandardScreen.Write(
			"X",
			new CursesStyle(
				CursesColor.Indexed( 2 ),
				CursesColor.Indexed( 1 ),
				attributes
			)
		);
		await session.RefreshAsync();

		Assert.Contains( "<bold>", output.Text );
		Assert.Contains( "<dim>", output.Text );
		Assert.Contains( "<underline>", output.Text );
		Assert.Contains( "<reverse>", output.Text );
		Assert.Contains( "<standout>", output.Text );
		Assert.Contains( "<italic>", output.Text );
		Assert.Contains( "<blink>", output.Text );
		Assert.Contains( "<conceal>", output.Text );
		Assert.Contains( "<strikeout>", output.Text );
		Assert.Contains( "<fg:2>", output.Text );
		Assert.Contains( "<bg:1>", output.Text );

		output.Clear();
		await session.DisposeAsync();

		Assert.Contains( "<sgr0>", output.Text );
		Assert.Contains( "<op>", output.Text );
	}

	[Fact]
	public async Task SynchronizedOutputIsDisabledByDefault() {
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
		session.StandardScreen.Write( "X" );
		output.Clear();

		await session.RefreshAsync();

		Assert.DoesNotContain( SynchronizedOutputBegin, output.Text );
		Assert.DoesNotContain( SynchronizedOutputEnd, output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task SynchronizedOutputFramesCompleteRefreshWhenEnabled() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreateRenditionTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			SynchronizedOutputOptions()
		);
		session.StandardScreen.Write( "X" );
		output.Clear();

		await session.RefreshAsync();

		int beginIndex = output.Text.IndexOf(
			SynchronizedOutputBegin,
			StringComparison.Ordinal
		);
		int payloadIndex = output.Text.IndexOf(
			"X",
			StringComparison.Ordinal
		);
		int endIndex = output.Text.IndexOf(
			SynchronizedOutputEnd,
			StringComparison.Ordinal
		);
		Assert.True( 0 <= beginIndex );
		Assert.True( beginIndex < payloadIndex );
		Assert.True( payloadIndex < endIndex );
		Assert.Equal(
			1,
			CountOccurrences( output.Text, SynchronizedOutputBegin )
		);
		Assert.Equal(
			1,
			CountOccurrences( output.Text, SynchronizedOutputEnd )
		);
		Assert.Equal(
			16,
			Encoding.ASCII.GetByteCount(
				SynchronizedOutputBegin + SynchronizedOutputEnd
			)
		);
		Assert.Equal( 2, output.FlushCount );
	}

	[Fact]
	public async Task SynchronizedOutputComposesWithOuterTerminalLease() {
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			output,
			new EmptyInput(),
			CreateRenditionTerminal()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			SynchronizedOutputOptions()
		);
		session.StandardScreen.Write( "X" );
		output.Clear();
		TerminalSynchronizedOutputLease outer =
			await terminalSession.AcquireSynchronizedOutputAsync();

		await session.RefreshAsync();

		Assert.Equal(
			1,
			CountOccurrences( output.Text, SynchronizedOutputBegin )
		);
		Assert.Equal(
			0,
			CountOccurrences( output.Text, SynchronizedOutputEnd )
		);

		await outer.DisposeAsync();

		Assert.Equal(
			1,
			CountOccurrences( output.Text, SynchronizedOutputEnd )
		);
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

	private static CursesSessionOptions SynchronizedOutputOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false,
			UseSynchronizedOutput = true
		};
	}

	private static int CountOccurrences(
		string source,
		string value
	) {
		ArgumentNullException.ThrowIfNull( source );
		ArgumentException.ThrowIfNullOrEmpty( value );

		int count = 0;
		int offset = 0;
		while ( true ) {
			int match = source.IndexOf(
				value,
				offset,
				StringComparison.Ordinal
			);
			if ( 0 > match ) {
				return count;
			}

			count++;
			offset = match + value.Length;
		}
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
			.SetNumber( NumericCapability.Colors, 8 )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterDimMode, "<dim>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.SetString( StringCapability.EnterReverseMode, "<reverse>" )
			.SetString( StringCapability.EnterStandoutMode, "<standout>" )
			.SetString( StringCapability.EnterItalicMode, "<italic>" )
			.SetString( StringCapability.EnterBlinkMode, "<blink>" )
			.SetString( StringCapability.EnterInvisibleMode, "<conceal>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.SetExtendedString( "smxx", "<strikeout>" )
			.SetExtendedString( "rmxx", "</strikeout>" )
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

		internal int FlushCount {
			get;
			private set;
		}

		internal void Clear() {
			this.bytes.Clear();
			this.FlushCount = 0;
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
			this.FlushCount = checked( this.FlushCount + 1 );
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
