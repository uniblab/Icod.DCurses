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

using Icod.DCurses.Internal;
using Icod.Terminal;
using Icod.TermInfo;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Qualifies whole-refresh behavior at Terminal's retained-item limit.</summary>
public sealed class CursesTransactionCapacityIntegrationTests {
	[Fact]
	public async Task FragmentedOverflowIsFailureAtomicAndCoalescibleRetrySucceeds() {
		CountingOutput output = new();
		await using CursesRefreshEngineTestContext context =
			await CursesRefreshEngineTestContext.OpenAsync(
				CreateTerminal(),
				output
			);
		CursesScreen screen = new( 257, 128 );
		PopulateFragmentedScreen( screen );

		InvalidOperationException exception =
			await Assert.ThrowsAsync<InvalidOperationException>(
				() => context.Engine.RefreshAsync( screen, 0, 0 ).AsTask()
			);

		Assert.Equal(
			"The screen-output transaction exceeds its item-count limit.",
			exception.Message
		);
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
		AssertDamageState( screen, expectedDirty: true );

		PopulateCoalescibleScreen( screen );
		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );

		Assert.True( 0 < output.WriteCount );
		Assert.Equal( 1, output.FlushCount );
		AssertDamageState( screen, expectedDirty: false );

		output.Clear();
		await context.Engine.RefreshAsync( screen, 0, 0 );
		Assert.Equal( 0, output.WriteCount );
		Assert.Equal( 0, output.FlushCount );
	}

	private static void PopulateFragmentedScreen(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		CursesStyle bold = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Bold
		);
		CursesCellMetadata firstLink = new(
			new CursesHyperlink(
				"https://example.test/t2005/overflow/a",
				"overflow-a"
			)
		);
		CursesCellMetadata secondLink = new(
			new CursesHyperlink(
				"https://example.test/t2005/overflow/b",
				"overflow-b"
			)
		);

		for ( int row = 0; row < screen.Rows; ++row ) {
			for ( int column = 0; column < screen.Columns; ++column ) {
				bool alternate = 0 != ( ( row * screen.Columns + column ) & 1 );
				screen.VirtualScreen[ row, column ] = new CursesCell(
					alternate ? "B" : "A",
					alternate ? bold : CursesStyle.Default
				);
				screen.VirtualScreen.SetMetadata(
					row,
					column,
					alternate ? secondLink : firstLink
				);
			}
		}
	}

	private static void PopulateCoalescibleScreen(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		for ( int row = 0; row < screen.Rows; ++row ) {
			for ( int column = 0; column < screen.Columns; ++column ) {
				screen.VirtualScreen[ row, column ] = new CursesCell( "S" );
				screen.VirtualScreen.SetMetadata( row, column, null );
			}
		}
	}

	private static void AssertDamageState(
		CursesScreen screen,
		bool expectedDirty
	) {
		ArgumentNullException.ThrowIfNull( screen );
		for ( int row = 0; row < screen.Rows; ++row ) {
			for ( int column = 0; column < screen.Columns; ++column ) {
				Assert.Equal(
					expectedDirty,
					screen.VirtualScreen.IsDirty( row, column )
				);
			}
		}
	}

	private static TerminalDescription CreateTerminal() {
		return new TerminalDescriptionBuilder( "transaction-capacity-integration" )
			.SetString( StringCapability.CursorAddress, "<cup:%p1%d,%p2%d>" )
			.SetString( StringCapability.ExitAttributeMode, "<sgr0>" )
			.SetString( StringCapability.OriginalColorPair, "<op>" )
			.SetString( StringCapability.EnterBoldMode, "<bold>" )
			.Build();
	}

	private sealed class CountingOutput : ITerminalOutput {
		private long writeCount;
		private int flushCount;

		internal long WriteCount => Interlocked.Read( ref this.writeCount );

		internal int FlushCount => Volatile.Read( ref this.flushCount );

		internal void Clear() {
			Interlocked.Exchange( ref this.writeCount, 0 );
			Interlocked.Exchange( ref this.flushCount, 0 );
		}

		public ValueTask WriteAsync(
			ReadOnlyMemory<byte> buffer,
			CancellationToken cancellationToken = default
		) {
			_ = buffer;
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment( ref this.writeCount );
			return ValueTask.CompletedTask;
		}

		public ValueTask FlushAsync(
			CancellationToken cancellationToken = default
		) {
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment( ref this.flushCount );
			return ValueTask.CompletedTask;
		}
	}
}
