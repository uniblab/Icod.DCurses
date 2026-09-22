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
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Hardens retained-raster refresh failure and caller-driven retry semantics.</summary>
public sealed class CursesRasterFailureAtomicityTests {
	private const string RasterPlaceholder = "\U0010EEEE";

	[Fact]
	public async Task RasterOutputFailureDoesNotCommitPhysicalStateAndCallerRetryReemitsExactlyOnce() {
		FailOnceRasterOutput output = new();
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = refreshContext.Engine;
		CursesScreen screen = CreateRasterScreen( refreshContext.Session );

		await Assert.ThrowsAsync<IOException>(
			() => engine.RefreshAsync(
				screen,
				0,
				0
			).AsTask()
		);

		Assert.Equal( 1, output.RasterAttemptCount );
		Assert.Equal( 0, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 1, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 1, output.RasterSuccessCount );
	}

	[Fact]
	public async Task CancellationDuringRasterEmissionRequiresExplicitCallerRetry() {
		using CancellationTokenSource cancellation = new();
		CancelOnceRasterOutput output = new( cancellation );
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = refreshContext.Engine;
		CursesScreen screen = CreateRasterScreen( refreshContext.Session );

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => engine.RefreshAsync(
				screen,
				0,
				0,
				cancellation.Token
			).AsTask()
		);

		Assert.Equal( 1, output.RasterAttemptCount );
		Assert.Equal( 0, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0,
			CancellationToken.None
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 1, output.RasterSuccessCount );
	}

	[Fact]
	public async Task FlushFailureAfterRasterCommitInvalidatesPhysicalStateForExplicitRetry() {
		FailOnceFlushOutput output = new();
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = refreshContext.Engine;
		CursesScreen screen = CreateRasterScreen( refreshContext.Session );

		await Assert.ThrowsAsync<IOException>(
			() => engine.RefreshAsync(
				screen,
				0,
				0
			).AsTask()
		);

		Assert.Equal( 1, output.RasterAttemptCount );
		Assert.Equal( 1, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 2, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 2, output.RasterSuccessCount );
	}

	[Fact]
	public async Task FlushCancellationAfterRasterCommitInvalidatesPhysicalStateForExplicitRetry() {
		using CancellationTokenSource cancellation = new();
		CancelOnceFlushOutput output = new( cancellation );
		await using CursesRefreshEngineTestContext refreshContext =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesRefreshEngine engine = refreshContext.Engine;
		CursesScreen screen = CreateRasterScreen( refreshContext.Session );

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => engine.RefreshAsync(
				screen,
				0,
				0,
				cancellation.Token
			).AsTask()
		);

		Assert.Equal( 1, output.RasterAttemptCount );
		Assert.Equal( 1, output.RasterSuccessCount );

		await engine.RefreshAsync(
			screen,
			0,
			0,
			CancellationToken.None
		);

		Assert.Equal( 2, output.RasterAttemptCount );
		Assert.Equal( 2, output.RasterSuccessCount );
	}

	private static CursesScreen CreateRasterScreen( TerminalSession terminalSession ) {
		ArgumentNullException.ThrowIfNull( terminalSession );
		CursesScreen screen = new(
			3,
			1
		);
		screen.VirtualScreen.SetRasterCell(
			0,
			1,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell(
				terminalSession
			)
		);
		return screen;
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "raster-failure-atomicity" )
			.SetString(
				StringCapability.CursorAddress,
				"<cup:%p1%d,%p2%d>"
			)
			.Build();
	}

	private abstract class RasterOutputBase : ITerminalOutput {
		internal int RasterAttemptCount {
			get;
			set;
		}

		internal int RasterSuccessCount {
			get;
			set;
		}

		public virtual ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( IsRasterWrite( buffer ) ) {
				this.RasterAttemptCount++;
				this.RasterSuccessCount++;
			}
			return ValueTask.CompletedTask;
		}

		public virtual ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		protected static bool IsRasterWrite( ReadOnlyMemory<byte> buffer ) {
			return Encoding.UTF8.GetString( buffer.Span ).Contains(
				RasterPlaceholder,
				StringComparison.Ordinal
			);
		}
	}

	private sealed class FailOnceRasterOutput : RasterOutputBase {
		private bool failurePending = true;

		public override ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( !IsRasterWrite( buffer ) ) {
				return ValueTask.CompletedTask;
			}
			this.RasterAttemptCount++;
			if ( this.failurePending ) {
				this.failurePending = false;
				throw new IOException( "injected raster output failure" );
			}
			this.RasterSuccessCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CancelOnceRasterOutput : RasterOutputBase {
		private readonly CancellationTokenSource cancellation;
		private bool cancellationPending = true;

		internal CancelOnceRasterOutput( CancellationTokenSource cancellation ) {
			ArgumentNullException.ThrowIfNull( cancellation );
			this.cancellation = cancellation;
		}

		public override ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			if ( !IsRasterWrite( buffer ) ) {
				return ValueTask.CompletedTask;
			}
			this.RasterAttemptCount++;
			if ( this.cancellationPending ) {
				this.cancellationPending = false;
				this.cancellation.Cancel();
				throw new OperationCanceledException( this.cancellation.Token );
			}
			this.RasterSuccessCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FailOnceFlushOutput : RasterOutputBase {
		private bool failurePending = true;

		public override ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( this.failurePending ) {
				this.failurePending = false;
				throw new IOException( "injected post-raster flush failure" );
			}
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CancelOnceFlushOutput : RasterOutputBase {
		private readonly CancellationTokenSource cancellation;
		private bool cancellationPending = true;

		internal CancelOnceFlushOutput( CancellationTokenSource cancellation ) {
			ArgumentNullException.ThrowIfNull( cancellation );
			this.cancellation = cancellation;
		}

		public override ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			if ( this.cancellationPending ) {
				this.cancellationPending = false;
				this.cancellation.Cancel();
				throw new OperationCanceledException( this.cancellation.Token );
			}
			return ValueTask.CompletedTask;
		}
	}
}
