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

using System.Reflection;
using System.Runtime.CompilerServices;
using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Drives T1607 retained-raster lifecycle and disposal invalidation.</summary>
public sealed class CursesRasterLifecycleTests {
	[Fact]
	public async Task ResourceDisposalInvalidatesOwningSessionPhysicalKnowledge() {
		await using CursesSession session = await OpenCursesSessionAsync();
		CursesRefreshEngine engine = AttachKnownRefreshEngine( session );
		CursesRasterResource resource = new(
			session,
			(TerminalRasterResource)RuntimeHelpers.GetUninitializedObject(
				typeof( TerminalRasterResource )
			)
		);

		await resource.DisposeAsync();

		Assert.Equal( 1, ReadInvalidationRequested( engine ) );
	}

	[Fact]
	public async Task PlaceholderDisposalInvalidatesOwningSessionPhysicalKnowledge() {
		await using CursesSession session = await OpenCursesSessionAsync();
		CursesRefreshEngine engine = AttachKnownRefreshEngine( session );
		CursesRasterPlaceholder placeholder = CreateSyntheticPlaceholder( session );

		await placeholder.DisposeAsync();

		Assert.Equal( 1, ReadInvalidationRequested( engine ) );
	}

	private static CursesRefreshEngine AttachKnownRefreshEngine(
		CursesSession session
	) {
		ArgumentNullException.ThrowIfNull( session );
		CursesRefreshEngine engine = new(
			session.Terminal,
			new NullRefreshOutput()
		);
		FieldInfo engineField = Assert.IsAssignableFrom<FieldInfo>(
			typeof( CursesSession ).GetField(
				"refreshEngine",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		engineField.SetValue( session, engine );
		FieldInfo invalidationField = GetInvalidationField();
		invalidationField.SetValue( engine, 0 );
		return engine;
	}

	private static int ReadInvalidationRequested(
		CursesRefreshEngine engine
	) {
		return Assert.IsType<int>( GetInvalidationField().GetValue( engine ) );
	}

	private static FieldInfo GetInvalidationField() {
		return Assert.IsAssignableFrom<FieldInfo>(
			typeof( CursesRefreshEngine ).GetField(
				"invalidationRequested",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
	}

	private static CursesRasterPlaceholder CreateSyntheticPlaceholder(
		CursesSession owner
	) {
		ArgumentNullException.ThrowIfNull( owner );
		CursesRasterPlaceholder placeholder =
			(CursesRasterPlaceholder)RuntimeHelpers.GetUninitializedObject(
				typeof( CursesRasterPlaceholder )
			);
		FieldInfo ownerField = Assert.IsAssignableFrom<FieldInfo>(
			typeof( CursesRasterPlaceholder ).GetField(
				"owner",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		ownerField.SetValue( placeholder, owner );
		FieldInfo terminalPlaceholderField = Assert.IsAssignableFrom<FieldInfo>(
			typeof( CursesRasterPlaceholder ).GetField(
				"placeholder",
				BindingFlags.Instance | BindingFlags.NonPublic
			)
		);
		terminalPlaceholderField.SetValue(
			placeholder,
			(TerminalRasterPlaceholder)RuntimeHelpers.GetUninitializedObject(
				typeof( TerminalRasterPlaceholder )
			)
		);
		return placeholder;
	}

	private static async ValueTask<CursesSession> OpenCursesSessionAsync() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "raster-lifecycle-test" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.Build();
		TerminalSession terminalSession = await TerminalSession.OpenAsync(
			new RecordingTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new EmptyInput(),
			new NullRawOutput(),
			new TerminalSessionOptions {
				TerminalOverride = terminal,
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
				UseSynchronizedOutput = false
			}
		);
	}

	private sealed class NullRefreshOutput : Icod.DCurses.Terminal.ITerminalOutput {
		public ValueTask WriteTextAsync(
			string value,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.CompletedTask;
		}

		public ValueTask WriteTerminalStringAsync(
			string value,
			int affectedLines = 1,
			CancellationToken cancellationToken = default
		) {
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

	private sealed class NullRawOutput : Icod.Terminal.ITerminalOutput {
		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
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

	private sealed class EmptyInput : Icod.Terminal.ITerminalInput {
		public ValueTask<int> ReadAsync(
			Memory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult( 0 );
		}
	}

	private sealed class RecordingTerminalControlProvider : ITerminalControlProvider {
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
				new TerminalSize( 2, 1 )
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
			return TerminalControlMutationResult.Success();
		}
	}
}
