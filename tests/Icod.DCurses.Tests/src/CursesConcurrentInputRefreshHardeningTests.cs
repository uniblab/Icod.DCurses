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

using System.Text;
using System.Threading.Channels;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises the supported 0.8 input/refresh concurrency model.</summary>
public sealed class CursesConcurrentInputRefreshHardeningTests {
	[Fact]
	public async Task PendingInputWaitDoesNotBlockRepeatedRefresh() {
		ChannelInput input = new();
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input, output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		Task<CursesEvent> pendingInput = session.ReadEventAsync().AsTask();
		await input.WaitForReadAsync();

		for ( int index = 0; 32 > index; ++index ) {
			session.StandardScreen.Move( 0, 0 );
			session.StandardScreen.Write(
				( index % 10 ).ToString( System.Globalization.CultureInfo.InvariantCulture )
			);
			await session.RefreshAsync();
		}

		input.Queue( Encoding.UTF8.GetBytes( "q" ) );
		CursesEvent inputEvent = await pendingInput;

		Assert.Equal( CursesEventKind.Input, inputEvent.Kind );
		Assert.Equal( CursesInputEventKind.Text, inputEvent.Input!.Kind );
		Assert.Equal( 'q', (char)inputEvent.Input.Character!.Value.Value );
		Assert.Contains( "1", output.Text );
	}

	[Fact]
	public async Task CallerCancellationPreservesFragmentedUtf8ForNextWait() {
		ChannelInput input = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			input,
			new RecordingOutput()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		using CancellationTokenSource cancellation = new();
		Task<CursesEvent> firstWait = session.ReadEventAsync(
			cancellation.Token
		).AsTask();
		await input.WaitForReadAsync();

		byte[] encoded = Encoding.UTF8.GetBytes( "界" );
		input.Queue( [ encoded[ 0 ] ] );
		await input.WaitForReadAsync();
		cancellation.Cancel();

		OperationCanceledException exception =
			await Assert.ThrowsAsync<OperationCanceledException>(
				() => firstWait
			);
		Assert.Equal( cancellation.Token, exception.CancellationToken );

		Task<CursesEvent> secondWait = session.ReadEventAsync().AsTask();
		input.Queue( encoded[ 1.. ] );
		CursesEvent inputEvent = await secondWait;

		Assert.Equal( CursesEventKind.Input, inputEvent.Kind );
		Assert.Equal( CursesInputEventKind.Text, inputEvent.Input!.Kind );
		Assert.Equal( '界', (char)inputEvent.Input.Character!.Value.Value );
	}

	[Fact]
	public async Task CancellationDuringRefreshInvalidatesForSafeRetry() {
		ChannelInput input = new();
		BlockingRecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input, output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Move( 0, 0 );
		session.StandardScreen.Write( "X" );
		using CancellationTokenSource cancellation = new();
		output.BlockNextWrite();
		Task refresh = session.RefreshAsync( cancellation.Token ).AsTask();
		await output.BlockedWriteStarted;

		cancellation.Cancel();

		_ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => refresh
		);
		output.Clear();

		await session.RefreshAsync();

		Assert.Contains( "X", output.Text );
	}

	[Fact]
	public async Task ConcurrentRefreshCallsSerializeTerminalOutput() {
		ChannelInput input = new();
		GatedRecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync( input, output );
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Move( 0, 0 );
		session.StandardScreen.Write( "X" );
		output.BlockNextWrite();
		Task first = session.RefreshAsync().AsTask();
		await output.BlockedWriteStarted;

		Task second = session.RefreshAsync().AsTask();
		await Task.Yield();
		Assert.False( second.IsCompleted );
		Assert.Equal( 1, output.WriteCount );

		output.ReleaseBlockedWrite();
		await Task.WhenAll( first, second );
		Assert.True( 1 < output.WriteCount );
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		ITerminalInput input,
		ITerminalOutput output
	) {
		ArgumentNullException.ThrowIfNull( input );
		ArgumentNullException.ThrowIfNull( output );
		return TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			input,
			output,
			new TerminalSessionOptions {
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "hardening-concurrency" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private sealed class ChannelInput : ITerminalInput {
		private readonly Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
		private readonly SemaphoreSlim readSignals = new( 0 );

		internal void Queue(
			byte[] bytes
		) {
			ArgumentNullException.ThrowIfNull( bytes );
			if ( !this.channel.Writer.TryWrite( bytes.ToArray() ) ) {
				throw new InvalidOperationException( "Unable to queue terminal input." );
			}
		}

		internal Task WaitForReadAsync() {
			return this.readSignals.WaitAsync();
		}

		public async ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			this.readSignals.Release();
			byte[] bytes = await this.channel.Reader.ReadAsync(
				cancellationToken
			).ConfigureAwait( false );
			if ( bytes.Length > buffer.Length ) {
				throw new InvalidOperationException( "Queued terminal input exceeds the read buffer." );
			}
			bytes.AsSpan().CopyTo( buffer.Span );
			return bytes.Length;
		}
	}

	private class RecordingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];
		private int writeCount;

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.UTF8.GetString( this.bytes.ToArray() );
				}
			}
		}

		internal int WriteCount {
			get {
				return Volatile.Read( ref this.writeCount );
			}
		}

		internal void Clear() {
			lock ( this.sync ) {
				this.bytes.Clear();
			}
			Interlocked.Exchange( ref this.writeCount, 0 );
		}

		public virtual ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.Record( buffer );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		protected void Record(
			ReadOnlyMemory<byte> buffer
		) {
			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
			}
			Interlocked.Increment( ref this.writeCount );
		}
	}

	private sealed class BlockingRecordingOutput : RecordingOutput {
		private TaskCompletionSource? blockedWriteStarted;
		private int blockNext;

		internal Task BlockedWriteStarted {
			get {
				return this.blockedWriteStarted?.Task
					?? throw new InvalidOperationException( "No write is configured to block." );
			}
		}

		internal void BlockNextWrite() {
			this.blockedWriteStarted = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			Interlocked.Exchange( ref this.blockNext, 1 );
		}

		public override async ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			if ( 1 == Interlocked.Exchange( ref this.blockNext, 0 ) ) {
				this.blockedWriteStarted!.TrySetResult();
				await Task.Delay(
					Timeout.InfiniteTimeSpan,
					cancellationToken
				).ConfigureAwait( false );
			}

			await base.WriteAsync( buffer, cancellationToken ).ConfigureAwait( false );
		}
	}

	private sealed class GatedRecordingOutput : RecordingOutput {
		private TaskCompletionSource? blockedWriteStarted;
		private TaskCompletionSource? releaseBlockedWrite;
		private int blockNext;

		internal Task BlockedWriteStarted {
			get {
				return this.blockedWriteStarted?.Task
					?? throw new InvalidOperationException( "No write is configured to block." );
			}
		}

		internal void BlockNextWrite() {
			this.blockedWriteStarted = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			this.releaseBlockedWrite = new TaskCompletionSource(
				TaskCreationOptions.RunContinuationsAsynchronously
			);
			Interlocked.Exchange( ref this.blockNext, 1 );
		}

		internal void ReleaseBlockedWrite() {
			this.releaseBlockedWrite?.TrySetResult();
		}

		public override async ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			if ( 1 == Interlocked.Exchange( ref this.blockNext, 0 ) ) {
				this.Record( buffer );
				this.blockedWriteStarted!.TrySetResult();
				await this.releaseBlockedWrite!.Task.WaitAsync(
					cancellationToken
				).ConfigureAwait( false );
				return;
			}

			await base.WriteAsync( buffer, cancellationToken ).ConfigureAwait( false );
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
