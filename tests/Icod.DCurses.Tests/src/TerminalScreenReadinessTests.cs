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
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

/// <summary>Qualifies the published Terminal screen boundary required by DCurses 2.0.</summary>
public sealed class TerminalScreenReadinessTests {
	private const int MaximumTransactionItemCount = 65_536;
	private const int MaximumTransactionPayloadByteCount = 64 * 1024 * 1024;

	[Fact]
	public async Task UnknownRenditionCanEstablishAVisibleSafeBaseline() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync( output );
		TerminalScreenOperationPlan plan = session.Screen.PlanRenditionBaseline()
			?? throw new InvalidOperationException(
				"The selected profile cannot establish a rendition baseline."
			);
		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		transaction.Add( plan );

		await transaction.CommitAsync();

		Assert.Equal( "<sgr0><op>", output.Text );
	}

	[Fact]
	public async Task TransactionItemBoundaryRejectsBeforeAnyOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync( output );
		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		for ( int count = 0; count < MaximumTransactionItemCount; ++count ) {
			transaction.WriteText( "x" );
		}

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => transaction.WriteText( "x" )
		);

		Assert.Equal(
			"The screen-output transaction exceeds its item-count limit.",
			exception.Message
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task TransactionPayloadBoundaryRejectsBeforeAnyOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync( output );
		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		transaction.WriteText( new string( 'x', MaximumTransactionPayloadByteCount ) );

		InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
			() => transaction.WriteText( "x" )
		);

		Assert.Equal(
			"The screen-output transaction exceeds its application-payload limit.",
			exception.Message
		);
		Assert.Equal( string.Empty, output.Text );
	}

	[Fact]
	public async Task StaleTransactionCannotReplayAfterSessionOutput() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync( output );
		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		transaction.WriteText( "transaction" );
		await session.WriteTextAsync( "outside" );

		InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => transaction.CommitAsync().AsTask()
		);

		Assert.Equal(
			"The screen-output transaction is stale because intervening session output occurred.",
			exception.Message
		);
		Assert.Equal( "outside", output.Text );
	}

	[Fact]
	public async Task CursorPlannerReportsAndEmitsTheCheapestLiteralPlan() {
		TerminalDescription terminal = new TerminalDescriptionBuilder(
			"dcurses-2-cursor-cost"
		)
			.SetString( StringCapability.CursorAddress, "<A:%p1%d,%p2%d>" )
			.SetString( StringCapability.CursorDownOne, "d" )
			.SetString( StringCapability.CursorRightOne, "r" )
			.Build();
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync(
			output,
			terminal
		);
		TerminalScreenOperationPlan plan = session.Screen.PlanCursorMove(
			new TerminalScreenPosition( 1, 2 ),
			new TerminalScreenPosition( 3, 5 )
		) ?? throw new InvalidOperationException(
			"The selected profile cannot move the cursor."
		);

		Assert.Equal( 5, plan.ByteCount );
		Assert.Equal( string.Empty, output.Text );

		TerminalScreenOutputTransaction transaction =
			session.CreateScreenOutputTransaction();
		transaction.Add( plan );
		await transaction.CommitAsync();

		Assert.Equal( "ddrrr", output.Text );
	}

	[Fact]
	public async Task SynchronizedOwnerRejectsFramedTransactionBeforeItsBytes() {
		RecordingTerminalOutput output = new();
		await using TerminalSession session = await OpenSessionAsync( output );
		TerminalSynchronizedOutputLease owner =
			await session.AcquireSynchronizedOutputAsync();
		TerminalScreenOutputTransaction blocked =
			session.CreateScreenOutputTransaction(
				new TerminalScreenOutputTransactionOptions {
					UseSynchronizedOutput = true
				}
			);
		blocked.WriteText( "blocked" );
		string ownerOutput = output.Text;

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => blocked.CommitAsync().AsTask()
		);

		Assert.Equal( "\u001b[?2026h", ownerOutput );
		Assert.Equal( ownerOutput, output.Text );

		await owner.DisposeAsync();
		TerminalScreenOutputTransaction recovery =
			session.CreateScreenOutputTransaction(
				new TerminalScreenOutputTransactionOptions {
					UseSynchronizedOutput = true
				}
			);
		recovery.WriteText( "recovery" );
		await recovery.CommitAsync();

		Assert.Equal(
			"\u001b[?2026h\u001b[?2026l\u001b[?2026hrecovery\u001b[?2026l",
			output.Text
		);
	}

	private static ValueTask<TerminalSession> OpenSessionAsync(
		RecordingTerminalOutput output,
		TerminalDescription? terminalOverride = null
	) {
		ArgumentNullException.ThrowIfNull( output );
		TerminalDescription terminal = terminalOverride ?? new TerminalDescriptionBuilder(
			"dcurses-2-readiness"
		)
			.SetNumber( NumericCapability.Colors, 16 )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.SetForegroundColor, "<fg:%p1%d>" )
			.SetString( StringCapability.SetBackgroundColor, "<bg:%p1%d>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();

		return TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new TestTerminalInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = terminal,
				ObserveLifecycleEvents = false
			}
		);
	}

	private sealed class TestTerminalInput : ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class RecordingTerminalOutput : ITerminalOutput {
		private readonly List<byte> bytes = [];

		internal string Text => Encoding.UTF8.GetString( [ .. this.bytes ] );

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

	private sealed class TestTerminalControlProvider : ITerminalControlProvider {
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
						| TerminalControlCapabilities.ModeRead
						| TerminalControlCapabilities.ModeWrite
						| TerminalControlCapabilities.LiveSize
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
			return TerminalControlResult<TerminalModeSnapshot>.Available(
				this.baseline
			);
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
