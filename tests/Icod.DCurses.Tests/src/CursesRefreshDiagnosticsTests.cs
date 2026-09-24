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

using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Specifies opt-in refresh diagnostics on a real Terminal-backed session.</summary>
public sealed class CursesRefreshDiagnosticsTests {
	[Fact]
	public async Task RefreshDiagnosticsAreDisabledAndEmptyByDefault() {
		CursesSessionOptions options = new() {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
		Assert.False( options.EnableRefreshDiagnostics );
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			new RecordingTerminalOutput()
		);
		await using CursesSession session = await CursesSession.OpenAsync( terminal, options );
		Assert.Null( session.LatestRefreshDiagnostics );
	}

	[Fact]
	public async Task EnabledRefreshPublishesOneBoundedSnapshotForEachAttempt() {
		RecordingTerminalOutput output = new();
		await using CursesSession session = await OpenAsync( output );
		session.StandardScreen.Write( "A" );
		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot first = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 1, first.Sequence );
		Assert.Equal( CursesRefreshOutcome.Succeeded, first.Outcome );
		Assert.True( first.IsFullRepaint );
		Assert.False( first.PhysicalStateInvalidated );
		Assert.True( first.LogicalStatePublished );
		Assert.True( first.LogicalCellsExamined > 0 );
		Assert.True( first.LogicalCellsChanged > 0 );
		Assert.True( first.DamagedRows > 0 );
		Assert.True( first.DamagedRegions > 0 );
		Assert.True( first.PreparedOutputItemCount > 0 );
		Assert.True( first.ApplicationPayloadCount > 0 );
		Assert.True( first.OperationKinds.HasFlag( CursesRefreshOperationKinds.Text ) );

		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot second = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 2, second.Sequence );
		Assert.Equal( CursesRefreshOutcome.Succeeded, second.Outcome );
		Assert.False( second.IsFullRepaint );
		Assert.True( second.LogicalStatePublished );
		Assert.Equal( 0, second.PreparedOutputItemCount );
		Assert.Equal( 1, first.Sequence );
	}

	[Fact]
	public async Task CancellationBeforeOutputRetainsPhysicalCertaintyAndPublishesNoWork() {
		await using CursesSession session = await OpenAsync( new RecordingTerminalOutput() );
		await session.RefreshAsync();
		using CancellationTokenSource source = new();
		source.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => session.RefreshAsync( source.Token ).AsTask() );
		CursesRefreshDiagnosticsSnapshot cancelled = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 2, cancelled.Sequence );
		Assert.Equal( CursesRefreshOutcome.Cancelled, cancelled.Outcome );
		Assert.False( cancelled.PhysicalStateInvalidated );
		Assert.False( cancelled.LogicalStatePublished );
		Assert.Equal( 0, cancelled.PreparedOutputItemCount );
	}

	[Fact]
	public async Task PartialWriteFailureInvalidatesThenRetriesAsFullRepaint() {
		FailingOutput output = new();
		await using CursesSession session = await OpenAsync( output );
		session.StandardScreen.Write( "A" );
		output.WrittenBytes = 0;
		output.FailAfterPrefix = true;
		await Assert.ThrowsAsync<IOException>( () => session.RefreshAsync().AsTask() );
		Assert.True( output.WrittenBytes > 0 );
		CursesRefreshDiagnosticsSnapshot failed = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( CursesRefreshOutcome.Failed, failed.Outcome );
		Assert.True( failed.PhysicalStateInvalidated );
		Assert.False( failed.LogicalStatePublished );
		Assert.True( failed.PreparedOutputItemCount > 0 );

		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot retry = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 2, retry.Sequence );
		Assert.Equal( CursesRefreshOutcome.Succeeded, retry.Outcome );
		Assert.True( retry.IsFullRepaint );
		Assert.False( retry.PhysicalStateInvalidated );
	}

	[Fact]
	public async Task FailureBeforeWriteAlsoFollowsExistingInvalidationRule() {
		FailingOutput output = new();
		await using CursesSession session = await OpenAsync( output );
		session.StandardScreen.Write( "A" );
		output.WrittenBytes = 0;
		output.FailNextWrite = true;
		await Assert.ThrowsAsync<IOException>( () => session.RefreshAsync().AsTask() );
		Assert.Equal( 0, output.WrittenBytes );
		CursesRefreshDiagnosticsSnapshot failed = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( CursesRefreshOutcome.Failed, failed.Outcome );
		Assert.True( failed.PhysicalStateInvalidated );
		Assert.False( failed.LogicalStatePublished );
	}

	[Fact]
	public async Task CancellationAfterFirstWriteInvalidatesAndDoesNotPublishLogicalState() {
		using CancellationTokenSource source = new();
		CancellingOutput output = new( source );
		await using CursesSession session = await OpenAsync( output );
		session.StandardScreen.Write( "A" );
		output.WrittenBytes = 0;
		output.Arm = true;
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => session.RefreshAsync( source.Token ).AsTask() );
		Assert.True( output.WrittenBytes > 0 );
		CursesRefreshDiagnosticsSnapshot cancelled = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( CursesRefreshOutcome.Cancelled, cancelled.Outcome );
		Assert.True( cancelled.PhysicalStateInvalidated );
		Assert.False( cancelled.LogicalStatePublished );
		Assert.True( cancelled.PreparedOutputItemCount > 0 );
	}

	[Fact]
	public async Task ExplicitInvalidationForcesRepaintWithoutChangingTheOutcome() {
		RecordingTerminalOutput output = new();
		await using CursesSession session = await OpenAsync( output );
		await session.RefreshAsync();
		output.Clear();
		session.Invalidate();
		await session.RefreshAsync();
		CursesRefreshDiagnosticsSnapshot snapshot = Assert.IsType<CursesRefreshDiagnosticsSnapshot>(
			session.LatestRefreshDiagnostics );
		Assert.Equal( 2, snapshot.Sequence );
		Assert.True( snapshot.IsFullRepaint );
		Assert.Equal( CursesRefreshOutcome.Succeeded, snapshot.Outcome );
		Assert.False( snapshot.PhysicalStateInvalidated );
		Assert.True( snapshot.PreparedOutputItemCount > 0 );
		Assert.NotEmpty( output.Text );
	}

	[Fact]
	public void SnapshotSurfaceContainsOnlyBoundedScalarData() {
		System.Reflection.PropertyInfo[] properties = typeof( CursesRefreshDiagnosticsSnapshot )
			.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
		Assert.Equal( 13, properties.Length );
		Assert.All( properties, property => {
			Assert.Null( property.SetMethod );
			Assert.True( property.PropertyType == typeof( int )
				|| property.PropertyType == typeof( long )
				|| property.PropertyType == typeof( bool )
				|| property.PropertyType == typeof( CursesRefreshOutcome )
				|| property.PropertyType == typeof( CursesRefreshOperationKinds ) );
		} );
	}

	private static async ValueTask<CursesSession> OpenAsync( ITerminalOutput output ) {
		TerminalDescription profile = new TerminalDescriptionBuilder( "refresh-diagnostics" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
		TerminalSession terminal = await TerminalScreenTestSession.OpenAsync( profile, output );
		return await CursesSession.OpenAsync( terminal, new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false,
			EnableRefreshDiagnostics = true
		} );
	}

	private sealed class FailingOutput : ITerminalOutput {
		internal bool FailNextWrite;
		internal bool FailAfterPrefix;
		internal int WrittenBytes;

		public ValueTask WriteAsync( ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( this.FailNextWrite ) {
				this.FailNextWrite = false;
				throw new IOException( "partial write" );
			}
			if ( this.FailAfterPrefix ) {
				this.FailAfterPrefix = false;
				this.WrittenBytes++;
				throw new IOException( "partial write" );
			}
			this.WrittenBytes += buffer.Length;
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync( CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CancellingOutput( CancellationTokenSource source ) : ITerminalOutput {
		internal bool Arm;
		internal int WrittenBytes;

		public ValueTask WriteAsync( ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			this.WrittenBytes += buffer.Length;
			if ( this.Arm ) {
				source.Cancel();
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync( CancellationToken cancellationToken = default ) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}
}
