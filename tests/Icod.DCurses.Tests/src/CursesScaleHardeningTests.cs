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
	[Theory]
	[InlineData( ApplicationShape.Roguelike )]
	[InlineData( ApplicationShape.FullScreenEditor )]
	[InlineData( ApplicationShape.PixelArt )]
	[InlineData( ApplicationShape.TileMap )]
	[InlineData( ApplicationShape.SpriteAndHud )]
	public async Task ApplicationShapedMixedWorkloadsFitOneBoundedTransaction(
		ApplicationShape shape
	) {
		CountingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateScaleTerminal(),
				output,
				useSynchronizedOutput: true
			);
		CursesScreen screen = new( 160, 60 );
		CursesRasterCell raster =
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				context.Session
			);
		PopulateApplicationFrame( screen, shape, raster );
		output.Clear();

		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.True( 0 < output.WriteCount );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );

		ApplySparseApplicationUpdate( screen, shape, raster );
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.True( 0 < output.WriteCount );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

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
		Assert.True( output.WriteCount <= writesAfterFullPaint + 3_000 );
		Assert.Equal( 1_001, output.FlushCount );
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
		Assert.Equal( 1, output.FlushCount );
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
				TerminalOverride = CreateScaleTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static TerminalDescription CreateScaleTerminal() {
		return new TerminalDescriptionBuilder( "hardening-scale" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.EnterUnderlineMode, "<underline>" )
			.Build();
	}

	private static void PopulateApplicationFrame(
		CursesScreen screen,
		ApplicationShape shape,
		CursesRasterCell raster
	) {
		ArgumentNullException.ThrowIfNull( screen );
		string palette = shape switch {
			ApplicationShape.Roguelike => ".#@+",
			ApplicationShape.FullScreenEditor => "edit",
			ApplicationShape.PixelArt => "0123",
			ApplicationShape.TileMap => "^~#.",
			ApplicationShape.SpriteAndHud => "HUD!",
			_ => throw new ArgumentOutOfRangeException( nameof( shape ) )
		};
		CursesStyle bold = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);
		for ( int row = 0; row < screen.Rows; ++row ) {
			for ( int column = 0; column < screen.Columns; ++column ) {
				int block = ( column / 8 ) + row + (int)shape;
				CursesStyle style = 0 == ( block & 1 )
					? CursesStyle.Default
					: bold;
				string content = palette[
					( row * 7 + column + (int)shape ) % palette.Length
				].ToString();
				screen.VirtualScreen[ row, column ] = new CursesCell(
					content,
					style
				);
			}
		}

		for ( int index = 0; 5 > index; ++index ) {
			int row = ( index * 11 + (int)shape * 3 ) % screen.Rows;
			int column = 7 + ( index * 29 % ( screen.Columns - 8 ) );
			screen.VirtualScreen.SetMetadata(
				row,
				column,
				CreateLinkMetadata( shape, $"frame-{index}" )
			);
		}

		for ( int index = 0; 3 > index; ++index ) {
			GetRasterPosition(
				screen,
				shape,
				index,
				out int row,
				out int column
			);
			screen.VirtualScreen.SetRasterCell( row, column, raster );
		}
	}

	private static void ApplySparseApplicationUpdate(
		CursesScreen screen,
		ApplicationShape shape,
		CursesRasterCell raster
	) {
		ArgumentNullException.ThrowIfNull( screen );
		CursesStyle underline = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Underline
		);
		for ( int index = 0; 12 > index; ++index ) {
			int row = ( index * 13 + (int)shape ) % screen.Rows;
			int column = ( index * 37 + (int)shape * 5 ) % screen.Columns;
			screen.VirtualScreen[ row, column ] = new CursesCell(
				"*",
				0 == ( index & 1 ) ? CursesStyle.Default : underline
			);
		}

		int linkRow = ( 17 + (int)shape * 7 ) % screen.Rows;
		int linkColumn = ( 31 + (int)shape * 19 ) % screen.Columns;
		screen.VirtualScreen.SetMetadata(
			linkRow,
			linkColumn,
			CreateLinkMetadata( shape, "sparse" )
		);

		GetRasterPosition(
			screen,
			shape,
			0,
			out int oldRasterRow,
			out int oldRasterColumn
		);
		screen.VirtualScreen.SetRasterCell(
			oldRasterRow,
			oldRasterColumn,
			null
		);
		int newRasterRow = ( oldRasterRow + 5 ) % screen.Rows;
		int newRasterColumn = ( oldRasterColumn + 17 ) % screen.Columns;
		screen.VirtualScreen.SetRasterCell(
			newRasterRow,
			newRasterColumn,
			raster
		);
	}

	private static CursesCellMetadata CreateLinkMetadata(
		ApplicationShape shape,
		string suffix
	) {
		return new CursesCellMetadata(
			new CursesHyperlink(
				$"https://example.test/t2005/{shape}/{suffix}",
				$"{shape}-{suffix}"
			)
		);
	}

	private static void GetRasterPosition(
		CursesScreen screen,
		ApplicationShape shape,
		int index,
		out int row,
		out int column
	) {
		row = ( 9 + (int)shape * 7 + index * 13 ) % screen.Rows;
		column = ( 23 + (int)shape * 17 + index * 41 ) % screen.Columns;
	}

	public enum ApplicationShape {
		Roguelike,
		FullScreenEditor,
		PixelArt,
		TileMap,
		SpriteAndHud
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

		internal void Clear() {
			Interlocked.Exchange( ref this.writeCount, 0 );
			Interlocked.Exchange( ref this.flushCount, 0 );
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
