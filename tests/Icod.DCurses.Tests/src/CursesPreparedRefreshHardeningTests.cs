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

using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Qualifies capacity and epoch behavior through the DCurses transaction wrapper.</summary>
public sealed class CursesPreparedRefreshHardeningTests {
	private const int MaximumTransactionItemCount = 65_536;
	private const int MaximumTransactionPayloadByteCount = 64 * 1024 * 1024;

	[Fact]
	public async Task ItemLimitRejectsTheNextPreparedItemBeforeOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			output
		);
		CursesPreparedRefresh prepared = new(
			session,
			useSynchronizedOutput: false
		);
		for ( int count = 0; count < MaximumTransactionItemCount; ++count ) {
			prepared.WriteText( string.Empty );
		}

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => prepared.WriteText( string.Empty )
		);

		Assert.Equal(
			"The screen-output transaction exceeds its item-count limit.",
			exception.Message
		);
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task PayloadLimitRejectsTheNextPreparedByteBeforeOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			output
		);
		CursesPreparedRefresh prepared = new(
			session,
			useSynchronizedOutput: false
		);
		string exactPayload = new(
			'\u00E9',
			MaximumTransactionPayloadByteCount / 2
		);
		prepared.WriteText( exactPayload );

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => prepared.WriteText( "x" )
		);

		Assert.Equal(
			"The screen-output transaction exceeds its application-payload limit.",
			exception.Message
		);
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
	}

	[Fact]
	public async Task StalePreparedRefreshCannotReplayAfterSessionOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			output
		);
		CursesPreparedRefresh stale = new(
			session,
			useSynchronizedOutput: false
		);
		stale.WriteText( "stale" );
		await session.WriteTextAsync( "outside" );
		int flushCountBeforeCommit = output.FlushCount;

		InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => stale.CommitAsync().AsTask()
		);

		Assert.Equal(
			"The screen-output transaction is stale because intervening session output occurred.",
			exception.Message
		);
		Assert.Equal( "outside", output.Text );
		Assert.Equal( flushCountBeforeCommit, output.FlushCount );

		CursesPreparedRefresh fresh = new(
			session,
			useSynchronizedOutput: false
		);
		fresh.WriteText( "fresh" );
		await fresh.CommitAsync();

		Assert.Equal( "outsidefresh", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}
}
