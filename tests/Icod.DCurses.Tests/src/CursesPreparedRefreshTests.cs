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

/// <summary>Verifies the semantic DCurses wrapper over one Terminal screen transaction.</summary>
public sealed class CursesPreparedRefreshTests {
	[Fact]
	public async Task SemanticItemsRemainBufferedAndCommitInExactOrder() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "prepared" )
			.SetString( StringCapability.CursorAddress, "P" )
			.Build();
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			terminal,
			output
		);
		TerminalScreenOperationPlan cursor = Assert.IsType<TerminalScreenOperationPlan>(
			session.Screen.PlanCursorMove(
				null,
				new TerminalScreenPosition( 0, 0 )
			)
		);
		CursesPreparedRefresh prepared = new(
			session,
			useSynchronizedOutput: true
		);

		prepared.AddPlan( cursor );
		prepared.WriteText( "text" );
		prepared.WriteHyperlink(
			"linked",
			new CursesHyperlink(
				"https://example.invalid/",
				"screen"
			)
		);
		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );

		await prepared.CommitAsync();

		Assert.Equal(
			"\u001B[?2026hPtext"
				+ "\u001B]8;id=screen;https://example.invalid/\u001B\\"
				+ "linked"
				+ "\u001B]8;;\u001B\\"
				+ "\u001B[?2026l",
			output.Text
		);
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public async Task InvalidSemanticValuesAndMutationAfterCommitAreRejected() {
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			new RecordingTerminalOutput()
		);
		CursesPreparedRefresh prepared = new(
			session,
			useSynchronizedOutput: false
		);

		Assert.Throws<ArgumentException>(
			() => prepared.AddPlan( default )
		);
		Assert.Throws<InvalidOperationException>(
			() => prepared.WriteRasterPlaceholderCell( default )
		);
		Assert.Throws<ArgumentException>(
			() => prepared.WriteRasterPlaceholderCells(
				ReadOnlyMemory<CursesRasterCell>.Empty
			)
		);

		prepared.WriteText( "committed" );
		await prepared.CommitAsync();

		Assert.Throws<InvalidOperationException>(
			() => prepared.WriteText( "late" )
		);
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => prepared.CommitAsync().AsTask()
		);
	}

	[Fact]
	public async Task PreCancelledCommitEmitsNothingAndConsumesPreparedRefresh() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await TerminalScreenTestSession.OpenAsync(
			TerminalProfiles.Dumb,
			output
		);
		CursesPreparedRefresh cancelled = new(
			session,
			useSynchronizedOutput: true
		);
		cancelled.WriteText( "cancelled" );
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => cancelled.CommitAsync( cancellation.Token ).AsTask()
		);

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
		InvalidOperationException reused =
			await Assert.ThrowsAsync<InvalidOperationException>(
				() => cancelled.CommitAsync().AsTask()
			);
		Assert.Equal(
			"A screen-output transaction can be committed only once.",
			reused.Message
		);

		CursesPreparedRefresh recovery = new(
			session,
			useSynchronizedOutput: false
		);
		recovery.WriteText( "recovery" );
		await recovery.CommitAsync();

		Assert.Equal( "recovery", output.Text );
		Assert.Equal( 1, output.FlushCount );
	}

	[Fact]
	public void SurfaceDoesNotExposeRawControlsOrUnderlyingTransaction() {
		string[] methodNames = typeof( CursesPreparedRefresh )
			.GetMethods(
				System.Reflection.BindingFlags.Instance
					| System.Reflection.BindingFlags.NonPublic
					| System.Reflection.BindingFlags.DeclaredOnly
			)
			.Select( method => method.Name )
			.Order()
			.ToArray();

		Assert.Equal(
			[
				"AddPlan",
				"CommitAsync",
				"WriteHyperlink",
				"WriteRasterPlaceholderCell",
				"WriteRasterPlaceholderCells",
				"WriteText"
			],
			methodNames
		);
		Assert.DoesNotContain(
			typeof( CursesPreparedRefresh ).GetFields(
				System.Reflection.BindingFlags.Instance
					| System.Reflection.BindingFlags.NonPublic
			),
			field => typeof( TerminalSession ) == field.FieldType
		);
	}
}
