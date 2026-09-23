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

/// <summary>Verifies refresh publication while Terminal commitment is blocked.</summary>
public sealed class CursesRefreshPublicationHardeningTests {
	[Fact]
	public async Task MutationDuringCommitRemainsDirtyAndRequiresSecondRefresh() {
		BlockingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A" );
		output.BlockNextWrite();

		Task firstRefresh = context.Engine.RefreshAsync( screen, 0, 0 ).AsTask();
		await output.FirstWriteStarted;
		try {
			Assert.False( firstRefresh.IsCompleted );
			ulong capturedRevision = screen.VirtualScreen.ChangeRevision;
			screen.VirtualScreen[ 0, 1 ] = new CursesCell( "B" );

			Assert.True(
				screen.VirtualScreen.GetCellChangeRevision( 0, 1 )
					> capturedRevision
			);
		} finally {
			output.ReleaseFirstWrite();
		}
		await firstRefresh;

		Assert.False( screen.VirtualScreen.IsDirty( 0, 0 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 1 ) );
		Assert.Equal( 1, screen.VirtualScreen.DirtyCellCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "B", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task InvalidationDuringCommitForcesNextFullRepaint() {
		BlockingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 2, 1 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "A" );
		output.BlockNextWrite();

		Task firstRefresh = context.Engine.RefreshAsync( screen, 0, 0 ).AsTask();
		await output.FirstWriteStarted;
		try {
			Assert.False( firstRefresh.IsCompleted );
			context.Engine.Invalidate();
		} finally {
			output.ReleaseFirstWrite();
		}
		await firstRefresh;

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "A", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task BlockedCommitFailurePublishesNothingAndRetriesCompleteDesiredScreen() {
		BlockingTerminalOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 2, 1 );
		screen.StandardWindow.Write( "AB" );
		IOException injected = new( "blocked commit failure" );
		output.BlockNextWrite( injected );

		Task firstRefresh = context.Engine.RefreshAsync( screen, 0, 0 ).AsTask();
		await output.FirstWriteStarted;
		Assert.False( firstRefresh.IsCompleted );
		output.ReleaseFirstWrite();

		IOException observed = await Assert.ThrowsAsync<IOException>(
			() => firstRefresh
		);

		Assert.Same( injected, observed );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 0 ) );
		Assert.True( screen.VirtualScreen.IsDirty( 0, 1 ) );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Contains( "AB", output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "refresh-publication-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class BlockingTerminalOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];
		private TaskCompletionSource? firstWriteStarted;
		private TaskCompletionSource? releaseFirstWrite;
		private Exception? blockedFailure;
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

		internal void BlockNextWrite( Exception? failure = null ) {
			this.firstWriteStarted = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			this.releaseFirstWrite = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			this.blockedFailure = failure;
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
			if ( 1 == Interlocked.Exchange( ref this.blockNextWrite, 0 ) ) {
				this.firstWriteStarted!.TrySetResult();
				await this.releaseFirstWrite!.Task.WaitAsync(
					cancellationToken
				).ConfigureAwait( false );
				if ( this.blockedFailure is not null ) {
					throw this.blockedFailure;
				}
			}

			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
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
