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

/// <summary>Freezes final 1.2 panel release transitions and integration boundaries.</summary>
public sealed class CursesPanelReleaseHardeningTests {
	private const string SynchronizedOutputBegin = "\u001b[?2026h";
	private const string SynchronizedOutputEnd = "\u001b[?2026l";
	private const string HyperlinkBegin = "\u001b]8;id=panel-docs;https://example.test/panel-docs\u001b\\";
	private const string HyperlinkEnd = "\u001b]8;;\u001b\\";

	[Fact]
	public async Task DisposingLastLivePanelRestoresBaseAndReturnsToCleanNoPanelRefresh() {
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
		session.StandardScreen.Move( 1, 2 );
		session.StandardScreen.Write( "BASE" );
		await session.RefreshAsync();
		output.Clear();

		CursesPanel panel = session.Screen.CreatePanel( 1, 2, 1, 4 );
		panel.ContentWindow.Write( "TOP!" );
		await session.RefreshAsync();

		Assert.Contains( "TOP!", output.Text );
		output.Clear();
		panel.Dispose();
		await session.RefreshAsync();

		Assert.Contains( "BASE", output.Text );
		Assert.DoesNotContain( "TOP!", output.Text );
		output.Clear();
		await session.RefreshAsync();

		Assert.DoesNotContain( "BASE", output.Text );
		Assert.DoesNotContain( "TOP!", output.Text );
	}

	[Fact]
	public async Task SynchronizedLivePanelHyperlinkUsesTerminalOwnedBoundedFraming() {
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
			new CursesSessionOptions {
				UseAlternateScreen = false,
				EnableKeypad = false,
				HideCursor = false,
				UseSynchronizedOutput = true
			}
		);
		output.Clear();
		using CursesPanel panel = session.Screen.CreatePanel( 1, 2, 1, 6 );
		panel.ContentWindow.WriteWithMetadata(
			"link",
			new CursesCellMetadata(
				new CursesHyperlink(
					"https://example.test/panel-docs",
					"panel-docs"
				)
			)
		);

		await session.RefreshAsync();

		string text = output.Text;
		int synchronizedBegin = text.IndexOf(
			SynchronizedOutputBegin,
			StringComparison.Ordinal
		);
		int hyperlinkBegin = text.IndexOf(
			HyperlinkBegin,
			StringComparison.Ordinal
		);
		int payload = text.IndexOf(
			"link",
			hyperlinkBegin + HyperlinkBegin.Length,
			StringComparison.Ordinal
		);
		int hyperlinkEnd = text.IndexOf(
			HyperlinkEnd,
			payload + 4,
			StringComparison.Ordinal
		);
		int synchronizedEnd = text.IndexOf(
			SynchronizedOutputEnd,
			hyperlinkEnd + HyperlinkEnd.Length,
			StringComparison.Ordinal
		);

		Assert.True( 0 <= synchronizedBegin );
		Assert.True( synchronizedBegin < hyperlinkBegin );
		Assert.True( hyperlinkBegin < payload );
		Assert.True( payload < hyperlinkEnd );
		Assert.True( hyperlinkEnd < synchronizedEnd );
	}

	[Fact]
	public void DisposingTopPanelRevealsLowerPanelWithBoundedDamage() {
		CursesScreen screen = new( 10, 4 );
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 3 );
		CursesPanel upper = screen.CreatePanel( 1, 2, 1, 3 );
		lower.ContentWindow.Write( "LOW" );
		upper.ContentWindow.Write( "TOP" );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.Equal( "TOP", ReadText( composed, 1, 2, 3 ) );
		composed.MarkClean();

		upper.Dispose();
		CursesVirtualScreen revealed = screen.ComposePanels();

		Assert.Same( composed, revealed );
		Assert.Equal( "LOW", ReadText( revealed, 1, 2, 3 ) );
		Assert.Equal( 2, revealed.DirtyCellCount );
	}

	[Fact]
	public void DisposingHiddenPanelDoesNotDirtyComposedFrame() {
		CursesScreen screen = new( 10, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		panel.ContentWindow.Write( "HID" );
		panel.Hide();
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		panel.Dispose();
		CursesVirtualScreen updated = screen.ComposePanels();

		Assert.Same( composed, updated );
		Assert.Equal( 0, updated.DirtyCellCount );
	}

	[Fact]
	public void BaseTouchVisibleThroughTransparentBlankPropagatesDamage() {
		CursesScreen screen = new( 8, 3 );
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "B" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		screen.VirtualScreen.TouchCell( 1, 2 );
		CursesVirtualScreen touched = screen.ComposePanels();

		Assert.Same( composed, touched );
		Assert.Equal( "B", touched.GetCell( 1, 2 ).Content );
		Assert.Equal( 1, touched.DirtyCellCount );
		Assert.True( touched.IsDirty( 1, 2 ) );
	}

	[Fact]
	public void LowerPanelTouchVisibleThroughTransparentUpperBlankPropagatesDamage() {
		CursesScreen screen = new( 8, 3 );
		CursesPanel lower = screen.CreatePanel( 1, 2, 1, 1 );
		lower.ContentWindow.Write( "L" );
		CursesPanel upper = screen.CreatePanel( 1, 2, 1, 1 );
		upper.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		CursesVirtualScreen composed = screen.ComposePanels();
		composed.MarkClean();

		lower.VirtualScreen.TouchCell( 0, 0 );
		CursesVirtualScreen touched = screen.ComposePanels();

		Assert.Same( composed, touched );
		Assert.Equal( "L", touched.GetCell( 1, 2 ).Content );
		Assert.Equal( 1, touched.DirtyCellCount );
		Assert.True( touched.IsDirty( 1, 2 ) );
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
		return new TerminalDescriptionBuilder( "panel-release-hardening" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.Build();
	}

	private static string ReadText(
		CursesVirtualScreen screen,
		int row,
		int column,
		int length
	) {
		ArgumentNullException.ThrowIfNull( screen );
		if ( 0 > row ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		if ( 0 > column ) {
			throw new ArgumentOutOfRangeException( nameof( column ) );
		}
		if ( 0 > length ) {
			throw new ArgumentOutOfRangeException( nameof( length ) );
		}

		StringBuilder result = new();
		for ( int index = 0; index < length; index++ ) {
			result.Append( screen.GetCell( row, column + index ).Content );
		}
		return result.ToString();
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
