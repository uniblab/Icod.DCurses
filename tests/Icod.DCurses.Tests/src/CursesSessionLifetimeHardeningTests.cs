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

/// <summary>Verifies 0.8 session-lifetime cancellation and disposal semantics.</summary>
public sealed class CursesSessionLifetimeHardeningTests {
	[Fact]
	public async Task CallerCancellationCancelsOnlyTheWait() {
		BlockingInput input = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		using CancellationTokenSource cancellation = new();
		Task<CursesEvent> pending = session.ReadEventAsync(
			cancellation.Token
		).AsTask();
		await input.Started;

		cancellation.Cancel();

		OperationCanceledException exception =
			await Assert.ThrowsAsync<OperationCanceledException>(
				() => pending
			);
		Assert.Equal( cancellation.Token, exception.CancellationToken );

		CursesEvent timedOut = await session.ReadEventAsync( TimeSpan.Zero );
		Assert.Equal( CursesEventKind.Timeout, timedOut.Kind );
	}

	[Fact]
	public async Task DisposalUnblocksPendingEventWaitAsObjectDisposed() {
		BlockingInput input = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input );
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		Task<CursesEvent> pending = session.ReadEventAsync().AsTask();
		await input.Started;

		Task disposal = session.DisposeAsync().AsTask();

		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => pending
		);
		await disposal;
	}

	[Fact]
	public async Task WaitOperationsStartedAfterDisposalThrowObjectDisposed() {
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new BlockingInput()
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		await session.DisposeAsync();

		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => session.ReadEventAsync().AsTask()
		);
		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => session.ReadEventAsync( TimeSpan.Zero ).AsTask()
		);
		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => session.ReadEventAsync( DateTimeOffset.UtcNow ).AsTask()
		);
		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => session.ReadLifecycleEventAsync().AsTask()
		);
	}

	[Fact]
	public async Task RepeatedDisposalSharesOneRestorationOperation() {
		BlockingInput input = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input );
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		Task<CursesEvent> pending = session.ReadEventAsync().AsTask();
		await input.Started;

		Task first = session.DisposeAsync().AsTask();
		Task second = session.DisposeAsync().AsTask();

		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => pending
		);
		await Task.WhenAll( first, second );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		ITerminalInput input
	) {
		ArgumentNullException.ThrowIfNull( input );
		return TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			input,
			new EmptyOutput(),
			new TerminalSessionOptions {
				TerminalOverride = TerminalProfiles.Dumb,
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private sealed class BlockingInput : ITerminalInput {
		private readonly TaskCompletionSource started = new(
			TaskCreationOptions.RunContinuationsAsynchronously
		);

		internal Task Started {
			get {
				return this.started.Task;
			}
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			this.started.TrySetResult();
			await Task.Delay(
				Timeout.InfiniteTimeSpan,
				cancellationToken
			).ConfigureAwait( false );
			return 0;
		}
	}

	private sealed class EmptyOutput : ITerminalOutput {
		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
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
			return TerminalControlMutationResult.Success();
		}
	}
}
