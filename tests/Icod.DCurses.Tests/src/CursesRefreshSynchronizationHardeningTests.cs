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

namespace Icod.DCurses.Tests;

using System.Text;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Verifies Terminal-owned refresh leases, serialization, framing, and flush.</summary>
public sealed class CursesRefreshSynchronizationHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string HyperlinkEnd = "\u001b]8;;\u001b\\";
	private const string RasterPlaceholder = "\U0010EEEE";

	[Fact]
	public async Task ActiveHyperlinkLeaseRejectsRefreshBeforeBodyAndFreshRetrySucceeds() {
		BlockingRecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.WriteWithMetadata(
			"A",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/refresh",
					"refresh"
				)
			)
		);
		TerminalHyperlinkLease outer = await context.Session.AcquireHyperlinkAsync(
			"https://example.test/outer",
			"outer"
		);
		output.Clear();

		InvalidOperationException conflict;
		try {
			conflict = await Assert.ThrowsAsync<InvalidOperationException>(
				() => context.Engine.RefreshAsync( screen, 0, 0 ).AsTask()
			);
			Assert.Equal( string.Empty, output.Text );
			Assert.Equal( 0, output.FlushCount );
			Assert.True( screen.VirtualScreen.IsDirty( 0, 0 ) );
		} finally {
			await outer.DisposeAsync();
		}

		Assert.Contains(
			"conflicts with existing hyperlink ownership",
			conflict.Message,
			StringComparison.Ordinal
		);
		Assert.Equal( HyperlinkEnd, output.Text );
		Assert.Equal( 0, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal(
			1,
			CountOccurrences(
				output.Text,
				"\u001b]8;id=refresh;https://example.test/refresh\u001b\\"
			)
		);
		Assert.Equal( 1, CountOccurrences( output.Text, "A" ) );
		Assert.Equal( 1, CountOccurrences( output.Text, HyperlinkEnd ) );
		Assert.Equal( 1, output.FlushCount );
		Assert.False( screen.VirtualScreen.IsDirty( 0, 0 ) );
	}

	[Fact]
	public async Task SessionOutputCannotInterleaveWithMixedSynchronizedRefresh() {
		BlockingRecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output,
				useSynchronizedOutput: true
			);
		CursesScreen screen = CreateMixedScreen( context.Session );
		output.BlockNextWrite();

		Task refresh = context.Engine.RefreshAsync( screen, 0, 0 ).AsTask();
		await output.FirstWriteStarted;
		Task outside = context.Session.WriteTextAsync( "outside" ).AsTask();
		await Task.Yield();
		try {
			Assert.False( refresh.IsCompleted );
			Assert.False( outside.IsCompleted );
			Assert.DoesNotContain( "outside", output.Text, StringComparison.Ordinal );
		} finally {
			output.ReleaseFirstWrite();
		}
		await Task.WhenAll( refresh, outside );

		string text = output.Text;
		int begin = text.IndexOf( SynchronizedOutputBegin, StringComparison.Ordinal );
		int ordinary = text.IndexOf( "T", StringComparison.Ordinal );
		int hyperlinkBegin = text.IndexOf(
			"\u001b]8;id=batch;https://example.test/batch\u001b\\",
			StringComparison.Ordinal
		);
		int linked = text.IndexOf( "L", StringComparison.Ordinal );
		int hyperlinkEnd = text.IndexOf(
			HyperlinkEnd,
			StringComparison.Ordinal
		);
		int raster = text.IndexOf(
			RasterPlaceholder,
			StringComparison.Ordinal
		);
		int tail = text.IndexOf( "Z", StringComparison.Ordinal );
		int end = text.IndexOf(
			SynchronizedOutputEnd,
			StringComparison.Ordinal
		);
		int external = text.IndexOf( "outside", StringComparison.Ordinal );

		Assert.True( 0 <= begin );
		Assert.True( begin < ordinary );
		Assert.True( ordinary < hyperlinkBegin );
		Assert.True( hyperlinkBegin < linked );
		Assert.True( linked < hyperlinkEnd );
		Assert.True( hyperlinkEnd < raster );
		Assert.True( raster < tail );
		Assert.True( tail < end );
		Assert.True( end < external );
		Assert.Equal( 1, CountOccurrences( text, SynchronizedOutputBegin ) );
		Assert.Equal( 1, CountOccurrences( text, SynchronizedOutputEnd ) );
		Assert.Equal( 1, CountOccurrences( text, RasterPlaceholder ) );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task MixedSynchronizedRefreshFlushesOnceAndUnchangedRefreshIsNoOp() {
		BlockingRecordingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output,
				useSynchronizedOutput: true
			);
		CursesScreen screen = CreateMixedScreen( context.Session );

		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( 1, CountOccurrences( output.Text, SynchronizedOutputBegin ) );
		Assert.Equal( 1, CountOccurrences( output.Text, SynchronizedOutputEnd ) );
		Assert.Equal( 1, CountOccurrences( output.Text, RasterPlaceholder ) );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	private static CursesScreen CreateMixedScreen( TerminalSession session ) {
		ArgumentNullException.ThrowIfNull( session );
		CursesScreen screen = new( 5, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "T" );
		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "L" );
		screen.StandardWindow.SetMetadata(
			0,
			1,
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/batch",
					"batch"
				)
			)
		);
		screen.VirtualScreen.SetRasterCell(
			0,
			2,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				session
			)
		);
		screen.VirtualScreen[ 0, 3 ] = new CursesCell( "Z" );
		return screen;
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "refresh-synchronization-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
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
			int index = source.IndexOf(
				value,
				offset,
				StringComparison.Ordinal
			);
			if ( 0 > index ) {
				return count;
			}
			count++;
			offset = index + value.Length;
		}
	}

	private sealed class BlockingRecordingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];
		private TaskCompletionSource? firstWriteStarted;
		private TaskCompletionSource? releaseFirstWrite;
		private int blockNextWrite;
		private int flushCount;

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.UTF8.GetString( this.bytes.ToArray() );
				}
			}
		}

		internal int FlushCount => Volatile.Read( ref this.flushCount );

		internal Task FirstWriteStarted => this.firstWriteStarted?.Task
			?? throw new InvalidOperationException(
				"No write is configured to block."
			);

		internal void BlockNextWrite() {
			this.firstWriteStarted = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			this.releaseFirstWrite = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			Interlocked.Exchange( ref this.blockNextWrite, 1 );
		}

		internal void ReleaseFirstWrite() {
			this.releaseFirstWrite?.TrySetResult();
		}

		internal void Clear() {
			lock ( this.sync ) {
				this.bytes.Clear();
			}
			Interlocked.Exchange( ref this.flushCount, 0 );
		}

		public async ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
			}
			if ( 1 == Interlocked.Exchange( ref this.blockNextWrite, 0 ) ) {
				this.firstWriteStarted!.TrySetResult();
				await this.releaseFirstWrite!.Task.WaitAsync(
					cancellationToken
				).ConfigureAwait( false );
			}
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment( ref this.flushCount );
			return ValueTask.CompletedTask;
		}
	}
}
