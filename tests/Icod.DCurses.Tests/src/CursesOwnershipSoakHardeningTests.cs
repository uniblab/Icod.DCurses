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
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Runs repeated complete Terminal/DCurses ownership and rich-input handoff cycles.</summary>
[Collection( TerminalProtocolNegotiationCollection.Name )]
public sealed class CursesOwnershipSoakHardeningTests {
	private const int CycleCount = 8;
	private const string ProbeRequest = "\u001b[?u\u001b[c";
	private const string PushAllKeys = "\u001b[>31u";
	private const string PopKeyboard = "\u001b[<u";
	private const string EnterAlternateScreen = "<A+>";
	private const string ExitAlternateScreen = "<A->";
	private const string EnablePaste = "<P+>";
	private const string DisablePaste = "<P->";
	private const string EnableFocus = "<F+>";
	private const string DisableFocus = "<F->";
	private const string EnableSgrMouse = "\u001b[?1006h";
	private const string DisableSgrMouse = "\u001b[?1006l";
	private const string EnableButtonEvents = "\u001b[?1000h";
	private const string DisableButtonEvents = "\u001b[?1000l";

	[Fact]
	public async Task RepeatedOwnershipCyclesRestoreRichInputAndTerminalMode() {
		TerminalDescription terminal = CreateTerminal();
		RecordingTerminalControlProvider provider = new();

		for ( int cycle = 0; cycle < CycleCount; ++cycle ) {
			DuplexTerminalTransport transport = new();
			TerminalSession terminalSession = await TerminalSession.OpenAsync(
				provider,
				TerminalEndpoint.StandardInput,
				TerminalEndpoint.StandardOutput,
				transport,
				transport,
				new TerminalSessionOptions {
					TerminalOverride = terminal,
					ConfigureOutput = false,
					ObserveLifecycleEvents = false
				}
			);

			TerminalInputProtocolLease protocols = (
				await terminalSession.AcquireInputProtocolsAsync(
					new TerminalInputProtocolOptions {
						BracketedPaste = true,
						FocusReporting = true,
						MouseTrackingMode = TerminalMouseTrackingMode.ButtonEvents,
						KeyboardReportingMode = TerminalKeyboardReportingMode.AllKeys
					}
				)
			).GetRequiredValue();

			Assert.True( transport.ContainsWrite( ProbeRequest ) );
			Assert.True( transport.ContainsWrite( PushAllKeys ) );
			Assert.True( transport.ContainsWrite( EnablePaste ) );
			Assert.True( transport.ContainsWrite( EnableFocus ) );
			Assert.True( transport.ContainsWrite( EnableSgrMouse ) );
			Assert.True( transport.ContainsWrite( EnableButtonEvents ) );

			transport.ClearWrites();
			CursesSession curses = await CursesSession.OpenAsync(
				terminalSession,
				new CursesSessionOptions {
					UseAlternateScreen = true,
					EnableKeypad = false,
					HideCursor = false
				}
			);

			try {
				AssertOrdered(
					transport,
					PopKeyboard,
					EnterAlternateScreen,
					PushAllKeys
				);

				int beforeRefresh = transport.WriteCount;
				curses.StandardScreen.Write(
					cycle.ToString( System.Globalization.CultureInfo.InvariantCulture )
				);
				await curses.RefreshAsync();
				Assert.True( beforeRefresh < transport.WriteCount );

				transport.QueueInput(
					Encoding.UTF8.GetBytes(
						"\u001b[97:65:113;6:2;120u"
							+ "\u001b[I"
							+ "\u001b[200~ok\u001b[201~"
					)
				);

				CursesInputEvent key = RequireInput( await curses.ReadEventAsync() );
				Assert.Equal( CursesInputEventKind.Key, key.Kind );
				Assert.Equal( CursesKey.Character, key.Key );
				Assert.Equal( 'a', (char)key.Character!.Value.Value );
				Assert.Equal( CursesKeyEventPhase.Repeat, key.KeyPhase );

				CursesInputEvent focus = RequireInput( await curses.ReadEventAsync() );
				Assert.Equal( CursesInputEventKind.Focus, focus.Kind );
				Assert.Equal( CursesFocusState.Focused, focus.Focus!.State );

				CursesInputEvent pasteBegin = RequireInput( await curses.ReadEventAsync() );
				CursesInputEvent pasteData = RequireInput( await curses.ReadEventAsync() );
				CursesInputEvent pasteEnd = RequireInput( await curses.ReadEventAsync() );
				Assert.Equal( CursesPastePhase.Begin, pasteBegin.Paste!.Phase );
				Assert.Equal( CursesPastePhase.Data, pasteData.Paste!.Phase );
				Assert.Equal( "ok", pasteData.Paste.Text );
				Assert.Equal( CursesPastePhase.End, pasteEnd.Paste!.Phase );

				transport.ClearWrites();
			} finally {
				await curses.DisposeAsync();
			}

			AssertOrdered(
				transport,
				PopKeyboard,
				ExitAlternateScreen,
				PushAllKeys
			);
			Assert.True( 2 <= transport.CountOf( PopKeyboard ) );
			Assert.True( transport.ContainsWrite( DisableButtonEvents ) );
			Assert.True( transport.ContainsWrite( DisableSgrMouse ) );
			Assert.True( transport.ContainsWrite( DisableFocus ) );
			Assert.True( transport.ContainsWrite( DisablePaste ) );

			transport.ClearWrites();
			await protocols.DisposeAsync();
			Assert.Equal( 0, transport.WriteCount );
		}

		Assert.Equal( 2 * CycleCount, provider.SetModeCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "dcurses-hardening-soak" )
			.SetString(
				StringCapability.EnterCursorAddressingMode,
				EnterAlternateScreen
			)
			.SetString(
				StringCapability.ExitCursorAddressingMode,
				ExitAlternateScreen
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
			.SetString( StringCapability.KeyMouse, "\u001b[<" )
			.SetExtendedString( "BE", EnablePaste )
			.SetExtendedString( "BD", DisablePaste )
			.SetExtendedString( "PS", "\u001b[200~" )
			.SetExtendedString( "PE", "\u001b[201~" )
			.SetExtendedString( "fe", EnableFocus )
			.SetExtendedString( "fd", DisableFocus )
			.SetExtendedString( "kxIN", "\u001b[I" )
			.SetExtendedString( "kxOUT", "\u001b[O" )
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

	private static CursesInputEvent RequireInput(
		CursesEvent cursesEvent
	) {
		ArgumentNullException.ThrowIfNull( cursesEvent );
		if ( CursesEventKind.Input != cursesEvent.Kind || cursesEvent.Input is null ) {
			throw new InvalidOperationException( "Expected a curses input event." );
		}
		return cursesEvent.Input;
	}

	private static void AssertOrdered(
		DuplexTerminalTransport transport,
		string first,
		string second,
		string third
	) {
		ArgumentNullException.ThrowIfNull( transport );
		ArgumentNullException.ThrowIfNull( first );
		ArgumentNullException.ThrowIfNull( second );
		ArgumentNullException.ThrowIfNull( third );
		int firstIndex = transport.IndexOf( first );
		int secondIndex = transport.IndexOf( second );
		int thirdIndex = transport.IndexOf( third );
		Assert.True(
			0 <= firstIndex
				&& firstIndex < secondIndex
				&& secondIndex < thirdIndex
		);
	}

	private sealed class DuplexTerminalTransport : ITerminalInput, ITerminalOutput {
		private readonly Channel<byte[]> input = Channel.CreateUnbounded<byte[]>();
		private readonly object sync = new();
		private readonly List<string> writes = [];

		internal int WriteCount {
			get {
				lock ( this.sync ) {
					return this.writes.Count;
				}
			}
		}

		internal bool ContainsWrite(
			string expected
		) {
			ArgumentNullException.ThrowIfNull( expected );
			return 0 <= this.IndexOf( expected );
		}

		internal int CountOf(
			string expected
		) {
			ArgumentNullException.ThrowIfNull( expected );
			lock ( this.sync ) {
				return this.writes.Count(
					value => string.Equals(
						value,
						expected,
						StringComparison.Ordinal
					)
				);
			}
		}

		internal int IndexOf(
			string expected
		) {
			ArgumentNullException.ThrowIfNull( expected );
			lock ( this.sync ) {
				return this.writes.FindIndex(
					value => string.Equals(
						value,
						expected,
						StringComparison.Ordinal
					)
				);
			}
		}

		internal void ClearWrites() {
			lock ( this.sync ) {
				this.writes.Clear();
			}
		}

		internal void QueueInput(
			byte[] value
		) {
			ArgumentNullException.ThrowIfNull( value );
			if ( !this.input.Writer.TryWrite( value.ToArray() ) ) {
				throw new InvalidOperationException( "The soak input could not be queued." );
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
				throw new InvalidOperationException( "The soak input exceeds the terminal read buffer." );
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

			if ( string.Equals( ProbeRequest, value, StringComparison.Ordinal ) ) {
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
