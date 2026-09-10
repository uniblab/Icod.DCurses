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

/// <summary>Verifies panel composition through live refresh, resize, and suspend/resume lifecycle boundaries.</summary>
public sealed class CursesPanelLifecycleTests {
	[Fact]
	public async Task RefreshRendersVisiblePanelComposition() {
		MutableTerminalControlProvider provider = new() {
			Size = new TerminalSize( 20, 6 )
		};
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		CursesPanel panel = session.Screen.CreatePanel(
			1,
			2,
			1,
			5
		);
		panel.ContentWindow.Write( "PANEL" );

		await session.RefreshAsync();

		Assert.Contains( "PANEL", output.Text );
	}

	[Fact]
	public async Task ResizeClipsPanelWithoutDiscardingRetainedContent() {
		MutableTerminalControlProvider provider = new() {
			Size = new TerminalSize( 10, 5 )
		};
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		CursesPanel panel = session.Screen.CreatePanel(
			3,
			6,
			2,
			4
		);
		panel.ContentWindow.Write( "Z123" );
		panel.ContentWindow.Move( 1, 0 );
		panel.ContentWindow.Write( "Y456" );
		await session.RefreshAsync();
		output.Clear();

		provider.Size = new TerminalSize( 7, 4 );
		await session.RefreshAsync();

		Assert.Equal( 7, session.Screen.Columns );
		Assert.Equal( 4, session.Screen.Rows );
		Assert.Equal( 3, panel.Row );
		Assert.Equal( 6, panel.Column );
		Assert.Equal( 2, panel.Rows );
		Assert.Equal( 4, panel.Columns );
		Assert.Equal( "Z", panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.Equal( "3", panel.VirtualScreen.GetCell( 0, 3 ).Content );
		Assert.Equal( "Y", panel.VirtualScreen.GetCell( 1, 0 ).Content );
		Assert.Equal( "6", panel.VirtualScreen.GetCell( 1, 3 ).Content );
		Assert.Contains( "Z", output.Text );
	}

	[Fact]
	public async Task RestoringDestinationSizeRevealsRetainedPanelContent() {
		MutableTerminalControlProvider provider = new() {
			Size = new TerminalSize( 10, 4 )
		};
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		CursesPanel panel = session.Screen.CreatePanel(
			2,
			6,
			1,
			4
		);
		panel.ContentWindow.Write( "WXYZ" );
		await session.RefreshAsync();

		provider.Size = new TerminalSize( 8, 4 );
		await session.RefreshAsync();
		output.Clear();

		provider.Size = new TerminalSize( 10, 4 );
		await session.RefreshAsync();

		Assert.Equal( "W", panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.Equal( "Z", panel.VirtualScreen.GetCell( 0, 3 ).Content );
		Assert.Contains( "WXYZ", output.Text );
	}

	[Fact]
	public async Task ResumeRepaintsRetainedPanelWithoutContentReconstruction() {
		MutableTerminalControlProvider provider = new() {
			Size = new TerminalSize( 12, 4 )
		};
		RecordingOutput output = new();
		TerminalSession terminalSession = await OpenTerminalSessionAsync(
			provider,
			output
		);
		await using CursesSession session = await CursesSession.OpenAsync(
			terminalSession,
			NoPresentationOptions()
		);
		CursesPanel panel = session.Screen.CreatePanel(
			1,
			3,
			1,
			5
		);
		panel.ContentWindow.Write( "PANEL" );
		await session.RefreshAsync();

		await session.LifecycleParticipant.PrepareForTerminalSuspendAsync();
		await session.LifecycleParticipant.ResumeAfterTerminalSuspendAsync();
		output.Clear();
		await session.RefreshAsync();

		Assert.Equal( "P", panel.VirtualScreen.GetCell( 0, 0 ).Content );
		Assert.Equal( "L", panel.VirtualScreen.GetCell( 0, 4 ).Content );
		Assert.Contains( "PANEL", output.Text );
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
		return new TerminalDescriptionBuilder( "panel-lifecycle" )
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
