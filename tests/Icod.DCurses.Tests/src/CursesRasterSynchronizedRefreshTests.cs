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

using System.Reflection;
using System.Text;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies raster output remains inside the existing Terminal synchronized-output transaction.</summary>
public sealed class CursesRasterSynchronizedRefreshTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string RasterMarker = "\U0010EEEE";

	[Fact]
	public async Task MixedRasterAndTextStayInsideOneSynchronizedRefreshTransaction() {
		RecordingRawOutput rawOutput = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( rawOutput );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				UseSynchronizedOutput = true
			}
		);

		CursesScreen screen = session.Screen;
		CursesRasterCell rasterCell = CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
			terminalSession
		);
		screen.VirtualScreen.SetRasterCell( 0, 0, rasterCell );
		screen.VirtualScreen.SetCell( 0, 1, new CursesCell( "X" ) );

		CursesRefreshEngine refreshEngine = new(
			session.HostSession,
			useSynchronizedOutput: true
		);
		FieldInfo refreshEngineField = Assert.IsAssignableFrom<FieldInfo>(
			typeof( CursesSession ).GetField(
				"refreshEngine",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		refreshEngineField.SetValue( session, refreshEngine );
		rawOutput.Clear();

		await session.RefreshAsync();

		string output = rawOutput.Text;
		int beginIndex = output.IndexOf(
			SynchronizedOutputBegin,
			StringComparison.Ordinal
		);
		int rasterIndex = output.IndexOf(
			RasterMarker,
			StringComparison.Ordinal
		);
		int textIndex = output.IndexOf(
			"X",
			StringComparison.Ordinal
		);
		int endIndex = output.IndexOf(
			SynchronizedOutputEnd,
			StringComparison.Ordinal
		);

		Assert.True( 0 <= beginIndex );
		Assert.True( beginIndex < rasterIndex );
		Assert.True( rasterIndex < textIndex );
		Assert.True( textIndex < endIndex );
		Assert.Equal( 1, CountOccurrences( output, SynchronizedOutputBegin ) );
		Assert.Equal( 1, CountOccurrences( output, SynchronizedOutputEnd ) );
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		RecordingRawOutput output
	) {
		ArgumentNullException.ThrowIfNull( output );
		TerminalDescription terminal = new TerminalDescriptionBuilder( "raster-sync-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
		return TerminalSession.OpenAsync(
			new RecordingTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EmptyInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = terminal,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
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

	private sealed class RecordingRawOutput : Icod.Terminal.ITerminalOutput {
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

	private sealed class EmptyInput : Icod.Terminal.ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
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
				new TerminalSize( 2, 1 )
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
			return TerminalControlMutationResult.Success();
		}
	}
}
