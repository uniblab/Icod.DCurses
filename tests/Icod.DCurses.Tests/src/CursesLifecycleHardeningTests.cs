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
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises resize and suspend/resume hardening at the Terminal lifecycle boundary.</summary>
public sealed class CursesLifecycleHardeningTests {
	[Fact]
	public async Task ResizeStormSynchronizesLogicalScreenDimensions() {
		MutableTerminalControlProvider provider = new();
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);

		for ( int index = 0; 64 > index; ++index ) {
			TerminalSize size = new(
				80 + ( index % 7 ),
				24 + ( index % 5 )
			);
			provider.Size = size;
			await session.RefreshAsync();
			Assert.Equal( size.Columns, session.Screen.Columns );
			Assert.Equal( size.Rows, session.Screen.Rows );
		}
	}

	[Fact]
	public async Task CancelledSuspendWaitDoesNotLeakLifecycleOrActivityGate() {
		GatedOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new MutableTerminalControlProvider(),
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Write( "X" );
		output.BlockNextWrite();
		Task refresh = session.RefreshAsync().AsTask();
		await output.BlockedWriteStarted;
		using CancellationTokenSource cancellation = new();
		Task suspend = session.LifecycleParticipant.PrepareForTerminalSuspendAsync(
			cancellation.Token
		).AsTask();
		await Task.Yield();
		Assert.False( suspend.IsCompleted );

		cancellation.Cancel();

		_ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => suspend
		);
		output.ReleaseBlockedWrite();
		await refresh;

		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
		await session.RefreshAsync();
	}

	[Fact]
	public async Task DisposalWhileSuspendedReleasesBlockedTerminalActivity() {
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new MutableTerminalControlProvider(),
			new RecordingOutput()
		);
		CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		Task blockedRefresh = session.RefreshAsync().AsTask();
		await Task.Yield();
		Assert.False( blockedRefresh.IsCompleted );

		Task disposal = session.DisposeAsync().AsTask();

		_ = await Assert.ThrowsAsync<ObjectDisposedException>(
			() => blockedRefresh
		);
		await disposal;
	}

	[Fact]
	public async Task RepeatedSuspendResumeCyclesAlwaysReleaseBlockedRefresh() {
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			new MutableTerminalControlProvider(),
			new RecordingOutput()
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		session.StandardScreen.Write( "X" );
		await session.RefreshAsync();

		for ( int index = 0; 16 > index; ++index ) {
			await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
			Task blockedRefresh = session.RefreshAsync().AsTask();
			await Task.Yield();
			Assert.False( blockedRefresh.IsCompleted );

			await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
			await blockedRefresh;
		}
	}

	private static CursesSessionOptions NoPresentationOptions() {
		return new CursesSessionOptions {
			UseAlternateScreen = false,
			EnableKeypad = false,
			HideCursor = false
		};
	}

	private static ValueTask<TerminalSession> OpenTerminalSessionAsync(
		MutableTerminalControlProvider provider,
		ITerminalOutput output
	) {
		ArgumentNullException.ThrowIfNull( provider );
		ArgumentNullException.ThrowIfNull( output );
		return TerminalSession.OpenAsync(
			provider,
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EmptyInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "hardening-lifecycle" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
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

	private class RecordingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.UTF8.GetString( this.bytes.ToArray() );
				}
			}
		}

		public virtual ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			lock ( this.sync ) {
				this.bytes.AddRange( buffer.ToArray() );
			}
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class GatedOutput : RecordingOutput {
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
				this.blockedWriteStarted!.TrySetResult();
				await this.releaseBlockedWrite!.Task.WaitAsync(
					cancellationToken
				).ConfigureAwait( false );
			}

			await base.WriteAsync( buffer, cancellationToken ).ConfigureAwait( false );
		}
	}

	private sealed class MutableTerminalControlProvider : ITerminalControlProvider {
		private readonly object sync = new();
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
		private TerminalSize size = new( 80, 24 );

		internal TerminalSize Size {
			get {
				lock ( this.sync ) {
					return this.size;
				}
			}
			set {
				lock ( this.sync ) {
					this.size = value;
				}
			}
		}

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
			return TerminalControlResult<TerminalSize>.Available( this.Size );
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
