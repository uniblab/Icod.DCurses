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

/// <summary>Verifies curses-shaped presentation observations over stable TermInfo profiles.</summary>
public sealed class CursesPresentationCapabilitiesTests {
	[Fact]
	public async Task DumbTerminalReportsNoRichPresentation() {
		CursesPresentationCapabilities capabilities =
			await ReadCapabilitiesAsync( TerminalProfiles.Dumb );

		Assert.Equal( 0, capabilities.IndexedColorCount );
		Assert.False( capabilities.SupportsColor );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.False( capabilities.SupportsForegroundColor );
		Assert.False( capabilities.SupportsBackgroundColor );
		Assert.False( capabilities.SupportsDefaultColorRestoration );
		Assert.Equal( CursesTextAttributes.None, capabilities.SupportedAttributes );
		Assert.Equal( CursesTextAttributes.None, capabilities.ColorRestrictedAttributes );
		Assert.False( capabilities.SupportsAlternateCharacterSet );
		Assert.False( capabilities.SupportsCursorHidden );
		Assert.False( capabilities.SupportsCursorNormal );
		Assert.False( capabilities.SupportsCursorVeryVisible );
	}

	[Fact]
	public async Task Sgr0WithoutOriginalColorPairDoesNotClaimDefaultColorRestoration() {
		TerminalDescription terminal = new TerminalDescriptionBuilder( "sgr0-only" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.Build();
		CursesPresentationCapabilities capabilities =
			await ReadCapabilitiesAsync( terminal );

		Assert.False( capabilities.SupportsDefaultColorRestoration );
	}

	[Fact]
	public async Task AnsiReportsIndexedColorAndNativeAttributeSubset() {
		CursesPresentationCapabilities capabilities =
			await ReadCapabilitiesAsync( TerminalProfiles.Ansi );

		Assert.Equal( 8, capabilities.IndexedColorCount );
		Assert.True( capabilities.SupportsColor );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.True( capabilities.SupportsForegroundColor );
		Assert.True( capabilities.SupportsBackgroundColor );
		Assert.True( capabilities.SupportsDefaultColorRestoration );
		Assert.True( capabilities.SupportsBold );
		Assert.False( capabilities.SupportsDim );
		Assert.True( capabilities.SupportsUnderline );
		Assert.True( capabilities.SupportsReverse );
		Assert.True( capabilities.SupportsStandout );
		Assert.False( capabilities.SupportsItalic );
		Assert.True( capabilities.SupportsBlink );
		Assert.True( capabilities.SupportsConceal );
		Assert.False( capabilities.SupportsStrikeout );
		Assert.Equal(
			CursesTextAttributes.Standout
				| CursesTextAttributes.Underline,
			capabilities.ColorRestrictedAttributes
		);
		Assert.False( capabilities.SupportsAlternateCharacterSet );
	}

	[Fact]
	public async Task Xterm256ReportsCompleteModernPresentationVocabulary() {
		CursesPresentationCapabilities capabilities =
			await ReadCapabilitiesAsync( TerminalProfiles.Xterm256Color );

		Assert.Equal( 256, capabilities.IndexedColorCount );
		Assert.False( capabilities.SupportsDirectRgb );
		Assert.True( capabilities.SupportsBold );
		Assert.True( capabilities.SupportsDim );
		Assert.True( capabilities.SupportsUnderline );
		Assert.True( capabilities.SupportsReverse );
		Assert.True( capabilities.SupportsStandout );
		Assert.True( capabilities.SupportsItalic );
		Assert.True( capabilities.SupportsBlink );
		Assert.True( capabilities.SupportsConceal );
		Assert.True( capabilities.SupportsStrikeout );
		Assert.True( capabilities.SupportsAlternateCharacterSet );
		Assert.True( capabilities.SupportsCursorHidden );
		Assert.True( capabilities.SupportsCursorNormal );
		Assert.True( capabilities.SupportsCursorVeryVisible );
	}

	[Fact]
	public async Task Direct256ReportsDirectRgbAndRetainedIndexedPrefix() {
		CursesPresentationCapabilities capabilities =
			await ReadCapabilitiesAsync( TerminalProfiles.XtermDirect256 );

		Assert.True( capabilities.SupportsDirectRgb );
		Assert.Equal( 256, capabilities.IndexedColorCount );
		Assert.True( capabilities.SupportsForegroundColor );
		Assert.True( capabilities.SupportsBackgroundColor );
	}

	private static async ValueTask<CursesPresentationCapabilities> ReadCapabilitiesAsync(
		TerminalDescription terminal
	) {
		ArgumentNullException.ThrowIfNull( terminal );
		await using TerminalSession session = await TerminalSession.OpenAsync(
			new TestTerminalControlProvider(),
			TerminalEndpoint.StandardInput,
			TerminalEndpoint.StandardOutput,
			new TestTerminalInput(),
			new DiscardingTerminalOutput(),
			new TerminalSessionOptions {
				TerminalOverride = terminal,
				ObserveLifecycleEvents = false
			}
		);

		return CursesPresentationCapabilities.Create( session.Profile.Screen );
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

	private sealed class DiscardingTerminalOutput : ITerminalOutput {
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
