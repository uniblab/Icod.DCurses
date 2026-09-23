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

/// <summary>Qualifies direct semantic operations at the public session boundary.</summary>
public sealed class CursesDirectSemanticOperationHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";

	[Theory]
	[InlineData( DirectOperation.Cursor )]
	[InlineData( DirectOperation.Alert )]
	[InlineData( DirectOperation.ResetRendition )]
	public async Task SuccessfulOperationUsesOneFramedCommitAndPublishesState(
		DirectOperation operation
	) {
		SelectiveFailingOutput output = new();
		await using CursesSession session = await OpenCursesSessionAsync( output );
		await EstablishKnownStateAsync( session, operation );
		output.Clear();

		Assert.True(
			await ExecuteAsync(
				session,
				operation,
				CancellationToken.None
			)
		);

		Assert.Equal(
			SynchronizedOutputBegin
				+ ExpectedOperationBody( operation )
				+ SynchronizedOutputEnd,
			output.Text
		);
		Assert.Equal( 1, output.FlushCount );

		output.Clear();
		if ( DirectOperation.ResetRendition == operation ) {
			session.StandardScreen.Write( "D" );
		}
		await session.RefreshAsync();

		if ( DirectOperation.ResetRendition == operation ) {
			Assert.Contains( "D", output.Text, StringComparison.Ordinal );
			Assert.DoesNotContain( "<sgr0>", output.Text, StringComparison.Ordinal );
			Assert.DoesNotContain( "<op>", output.Text, StringComparison.Ordinal );
		} else {
			Assert.Equal( string.Empty, output.Text );
			Assert.Equal( 0, output.FlushCount );
		}
	}

	[Theory]
	[InlineData( DirectOperation.Cursor )]
	[InlineData( DirectOperation.Alert )]
	[InlineData( DirectOperation.ResetRendition )]
	public async Task PreCancelledOperationEmitsNothingAndFreshRetrySucceeds(
		DirectOperation operation
	) {
		SelectiveFailingOutput output = new();
		await using CursesSession session = await OpenCursesSessionAsync( output );
		await EstablishKnownStateAsync( session, operation );
		int originalCursorRow = session.StandardScreen.CursorRow;
		int originalCursorColumn = session.StandardScreen.CursorColumn;
		output.Clear();
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => ExecuteAsync(
				session,
				operation,
				cancellation.Token
			).AsTask()
		);

		Assert.Equal( string.Empty, output.Text );
		Assert.Equal( 0, output.FlushCount );
		Assert.Equal( originalCursorRow, session.StandardScreen.CursorRow );
		Assert.Equal( originalCursorColumn, session.StandardScreen.CursorColumn );

		Assert.True(
			await ExecuteAsync(
				session,
				operation,
				CancellationToken.None
			)
		);
		Assert.Equal(
			SynchronizedOutputBegin
				+ ExpectedOperationBody( operation )
				+ SynchronizedOutputEnd,
			output.Text
		);
		Assert.Equal( 1, output.FlushCount );
	}

	[Theory]
	[InlineData( DirectOperation.Cursor )]
	[InlineData( DirectOperation.Alert )]
	[InlineData( DirectOperation.ResetRendition )]
	public async Task FailedOperationInvalidatesStateForCompleteRefreshRecovery(
		DirectOperation operation
	) {
		SelectiveFailingOutput output = new();
		await using CursesSession session = await OpenCursesSessionAsync( output );
		await EstablishKnownStateAsync( session, operation );
		output.Clear();
		output.FailOnceWhen(
			value => value.Contains(
				ExpectedOperationFailureMarker( operation ),
				StringComparison.Ordinal
			),
			new IOException( "injected direct-operation failure" )
		);

		IOException exception = await Assert.ThrowsAsync<IOException>(
			() => ExecuteAsync(
				session,
				operation,
				CancellationToken.None
			).AsTask()
		);
		Assert.Equal( "injected direct-operation failure", exception.Message );

		output.Clear();
		await session.RefreshAsync();

		Assert.Contains( SynchronizedOutputBegin, output.Text, StringComparison.Ordinal );
		Assert.Contains( "<cup:0,0>", output.Text, StringComparison.Ordinal );
		Assert.Contains( "K", output.Text, StringComparison.Ordinal );
		Assert.Contains( SynchronizedOutputEnd, output.Text, StringComparison.Ordinal );
		Assert.Equal( 1, output.FlushCount );
		if ( DirectOperation.Cursor == operation ) {
			Assert.Contains( "<cup:1,2>", output.Text, StringComparison.Ordinal );
		}
		if ( DirectOperation.ResetRendition == operation ) {
			Assert.Contains( "<bold>", output.Text, StringComparison.Ordinal );
		}
	}

	private static async ValueTask EstablishKnownStateAsync(
		CursesSession session,
		DirectOperation operation
	) {
		if ( DirectOperation.ResetRendition == operation ) {
			session.Screen.VirtualScreen[
				session.Screen.Rows - 1,
				session.Screen.Columns - 1
			] = new CursesCell(
				"K",
				new CursesStyle(
					CursesColor.Default,
					CursesColor.Default,
					CursesTextAttributes.Bold
				)
			);
		} else {
			session.StandardScreen.Write( "K" );
		}
		await session.RefreshAsync();
	}

	private static ValueTask<bool> ExecuteAsync(
		CursesSession session,
		DirectOperation operation,
		CancellationToken cancellationToken
	) {
		return operation switch {
			DirectOperation.Cursor => session.SetCursorPositionAsync(
				1,
				2,
				cancellationToken
			),
			DirectOperation.Alert => session.AlertAsync(
				CursesAlertKind.Audible,
				cancellationToken
			),
			DirectOperation.ResetRendition => session.ResetRenditionAsync(
				cancellationToken
			),
			_ => throw new ArgumentOutOfRangeException( nameof( operation ) )
		};
	}

	private static string ExpectedOperationBody(
		DirectOperation operation
	) {
		return operation switch {
			DirectOperation.Cursor => "<cup:1,2>",
			DirectOperation.Alert => "<bell>",
			DirectOperation.ResetRendition => "<sgr0>",
			_ => throw new ArgumentOutOfRangeException( nameof( operation ) )
		};
	}

	private static string ExpectedOperationFailureMarker(
		DirectOperation operation
	) {
		return operation switch {
			DirectOperation.Cursor => "<cup:1,2>",
			DirectOperation.Alert => "<bell>",
			DirectOperation.ResetRendition => "<sgr0>",
			_ => throw new ArgumentOutOfRangeException( nameof( operation ) )
		};
	}

	private static async ValueTask<CursesSession> OpenCursesSessionAsync(
		SelectiveFailingOutput output
	) {
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EndOfInput(),
			output,
			new TerminalSessionOptions {
				TerminalOverride = CreateTerminal(),
				ConfigureOutput = false,
				ObserveLifecycleEvents = false
			}
		);
		return await CursesSession.OpenAsync(
			terminalSession,
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				UseSynchronizedOutput = true
			}
		);
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "direct-semantic-operations" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.Bell, "<bell>" )
			.SetString( StringCapability.FlashScreen, "<flash>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.Build();
	}

	public enum DirectOperation {
		Cursor,
		Alert,
		ResetRendition
	}

	private sealed class EndOfInput : ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class SelectiveFailingOutput : ITerminalOutput {
		private readonly List<byte> bytes = [];
		private FailureRule? failure;

		internal string Text => Encoding.UTF8.GetString( this.bytes.ToArray() );

		internal int FlushCount {
			get;
			private set;
		}

		internal void Clear() {
			this.bytes.Clear();
			this.FlushCount = 0;
		}

		internal void FailOnceWhen(
			Func<string, bool> predicate,
			Exception exception
		) {
			ArgumentNullException.ThrowIfNull( predicate );
			ArgumentNullException.ThrowIfNull( exception );
			this.failure = new FailureRule( predicate, exception );
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			string value = Encoding.UTF8.GetString( buffer.Span );
			FailureRule? current = this.failure;
			if ( current is not null && current.Predicate( value ) ) {
				this.failure = null;
				throw current.Exception;
			}
			this.bytes.AddRange( buffer.ToArray() );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			this.FlushCount = checked( this.FlushCount + 1 );
			return ValueTask.CompletedTask;
		}

		private sealed record FailureRule(
			Func<string, bool> Predicate,
			Exception Exception
		);
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
