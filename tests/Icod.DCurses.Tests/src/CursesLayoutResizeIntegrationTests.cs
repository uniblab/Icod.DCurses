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

/// <summary>Verifies explicit application relayout after live terminal dimension synchronization.</summary>
public sealed class CursesLayoutResizeIntegrationTests {
	[Fact]
	public async Task LiveResizeSynchronizesThenExplicitlyRecomputesAndRepaintsLayout() {
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
		CursesScreen screen = session.Screen;
		ComputeLayout(
			screen.Bounds,
			out CursesRectangle headerBounds,
			out CursesRectangle bodyBounds,
			out CursesRectangle panelBounds
		);
		CursesWindow header = screen.CreateWindow(
			headerBounds.Row,
			headerBounds.Column,
			headerBounds.Rows,
			headerBounds.Columns
		);
		CursesWindow body = screen.CreateWindow(
			bodyBounds.Row,
			bodyBounds.Column,
			bodyBounds.Rows,
			bodyBounds.Columns
		);
		using CursesPanel panel = screen.CreatePanel(
			panelBounds.Row,
			panelBounds.Column,
			panelBounds.Rows,
			panelBounds.Columns
		);
		header.WrapMode = CursesWrapMode.Clip;
		body.WrapMode = CursesWrapMode.Clip;
		panel.ContentWindow.WrapMode = CursesWrapMode.Clip;
		header.Write( "HEADER" );
		body.Write( "BODY" );
		panel.ContentWindow.Write( "PANEL" );
		await session.RefreshAsync();

		provider.Size = new TerminalSize( 14, 6 );
		await session.RefreshAsync();

		Assert.Equal(
			new CursesRectangle(
				0,
				0,
				6,
				14
			),
			screen.Bounds
		);
		ComputeLayout(
			screen.Bounds,
			out headerBounds,
			out bodyBounds,
			out panelBounds
		);
		header.SetBounds( headerBounds );
		body.SetBounds( bodyBounds );
		panel.SetBounds( panelBounds );
		header.Clear();
		body.Clear();
		panel.ContentWindow.Clear();
		header.Write( "H2" );
		body.Write( "B2" );
		panel.ContentWindow.Write( "P2" );
		output.Clear();
		session.Invalidate();

		await session.RefreshAsync();

		Assert.Equal( headerBounds, header.Bounds );
		Assert.Equal( bodyBounds, body.Bounds );
		Assert.Equal( panelBounds, panel.Bounds );
		Assert.Contains( "H2", output.Text, StringComparison.Ordinal );
		Assert.Contains( "B2", output.Text, StringComparison.Ordinal );
		Assert.Contains( "P2", output.Text, StringComparison.Ordinal );
	}

	private static void ComputeLayout(
		CursesRectangle bounds,
		out CursesRectangle header,
		out CursesRectangle body,
		out CursesRectangle panel
	) {
		CursesLayout.SplitTop(
			bounds,
			1,
			out header,
			out CursesRectangle content
		);
		int panelColumns = Math.Min(
			6,
			Math.Max(
				1,
				content.Columns / 3
			)
		);
		CursesLayout.SplitRight(
			content,
			panelColumns,
			out body,
			out panel
		);
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
		return new TerminalDescriptionBuilder( "layout-resize-integration" )
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

	private sealed class RecordingOutput : ITerminalOutput {
		private readonly object sync = new();
		private readonly List<byte> bytes = [];

		internal string Text {
			get {
				lock ( this.sync ) {
					return Encoding.UTF8.GetString( this.bytes.ToArray() );
				}
			}
		}

		internal void Clear() {
			lock ( this.sync ) {
				this.bytes.Clear();
			}
		}

		public ValueTask WriteAsync(
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

	private sealed class MutableTerminalControlProvider : ITerminalControlProvider {
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
		private TerminalSize size = new( 20, 8 );

		internal TerminalSize Size {
			get => this.size;
			set => this.size = value;
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
			return TerminalControlResult<TerminalSize>.Available( this.size );
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
