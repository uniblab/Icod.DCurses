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

using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises bounded production-scale surfaces and refresh frequency.</summary>
public sealed class CursesScaleHardeningTests {
	[Fact]
	public void LargePadSupportsRepeatedTwoAxisPanningWithStableFootprints() {
		CursesPad pad = new(
			2_048,
			256
		);
		for ( int row = 0; row < pad.Rows; row += 17 ) {
			pad.ContentWindow.FillRectangle(
				row,
				0,
				1,
				pad.Columns,
				new CursesCell( ((char)( 'A' + ( row % 26 ) )).ToString() )
			);
		}
		pad.ContentWindow.Move( 127, 1_020 );
		pad.ContentWindow.Write( "A界B" );

		CursesScreen screen = new(
			120,
			40
		);
		CursesPadViewport viewport = pad.CreateViewport(
			screen.StandardWindow,
			0,
			0,
			40,
			120,
			0,
			0
		);

		for ( int iteration = 0; 256 > iteration; ++iteration ) {
			viewport.SetSource(
				( iteration * 13 ) % ( pad.Rows - viewport.Rows + 1 ),
				( iteration * 29 ) % ( pad.Columns - viewport.Columns + 1 )
			);
			viewport.Present();
			CursesCellFootprint.Validate( screen.VirtualScreen );
		}

		viewport.SetSource( 127, 1_020 );
		viewport.Present();
		Assert.Equal( "A", screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "界", screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.True( screen.StandardWindow.GetCell( 0, 2 ).IsContinuation );
		Assert.Equal( "B", screen.StandardWindow.GetCell( 0, 3 ).Content );
	}

	[Fact]
	public async Task LargeScreenAndHighFrequencySparseRefreshRemainStable() {
		CountingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		for ( int row = 0; row < session.Screen.Rows; row++ ) {
			session.StandardScreen.Move( row, 0 );
			session.StandardScreen.Write(
				new string(
					(char)( 'A' + ( row % 26 ) ),
					session.Screen.Columns
				)
			);
		}
		await session.RefreshAsync();
		long writesAfterFullPaint = output.WriteCount;
		Assert.True( 0 < writesAfterFullPaint );

		for ( int iteration = 0; 1_000 > iteration; ++iteration ) {
			int row = iteration % session.Screen.Rows;
			int column = ( iteration * 17 ) % session.Screen.Columns;
			session.StandardScreen.Move( row, column );
			session.StandardScreen.Write(
				( iteration % 10 ).ToString( System.Globalization.CultureInfo.InvariantCulture )
			);
			await session.RefreshAsync();
		}

		CursesCell finalCell = session.StandardScreen.GetCell(
			999 % session.Screen.Rows,
			( 999 * 17 ) % session.Screen.Columns
		);
		Assert.Equal( "9", finalCell.Content );
		Assert.True( writesAfterFullPaint < output.WriteCount );
	}

	[Fact]
	public async Task RepeatedNoOpRefreshDoesNotGrowTerminalWriteCount() {
		CountingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Write( "stable" );
		await session.RefreshAsync();
		long baselineWrites = output.WriteCount;

		for ( int iteration = 0; 512 > iteration; ++iteration ) {
			await session.RefreshAsync();
		}

		Assert.Equal( baselineWrites, output.WriteCount );
		Assert.Equal( 513, output.FlushCount );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		ITerminalOutput output
	) {
		ArgumentNullException.ThrowIfNull( output );
		return TerminalSession.OpenAsync(
			new LargeTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EmptyInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = new TerminalDescriptionBuilder( "hardening-scale" )
					.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
					.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
					.SetString( StringCapability.OriginalColorPair, "<op>" )
					.Build(),
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
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class CountingOutput : ITerminalOutput {
		private long writeCount;
		private int flushCount;

		internal long WriteCount {
			get {
				return Interlocked.Read( ref this.writeCount );
			}
		}

		internal int FlushCount {
			get {
				return Volatile.Read( ref this.flushCount );
			}
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment( ref this.writeCount );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment( ref this.flushCount );
			return ValueTask.CompletedTask;
		}
	}

	private sealed class LargeTerminalControlProvider : ITerminalControlProvider {
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
				new TerminalSize( 160, 60 )
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
