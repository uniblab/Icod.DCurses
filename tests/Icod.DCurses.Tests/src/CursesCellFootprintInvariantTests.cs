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

using Icod.DCurses;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Verifies wide-cell leader/continuation invariants through structural screen and window operations.
/// </summary>
public sealed class CursesCellFootprintInvariantTests {
	[Fact]
	public void PreservedResizeRepairsWideLeaderClippedAtNewRightBoundary() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Move( 0, 2 );
		window.Write( "\u754C" );

		screen.Resize(
			3,
			1,
			preserveContents: true
		);

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void PreservedResizeKeepsCompleteWideFootprint() {
		CursesScreen screen = new( 5, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Move( 0, 1 );
		window.Write( "\u754C" );

		screen.Resize(
			4,
			1,
			preserveContents: true
		);

		Assert.Equal( "\u754C", screen.VirtualScreen[ 0, 1 ].Content );
		Assert.Equal( 2, screen.VirtualScreen[ 0, 1 ].DisplayWidth );
		Assert.True( screen.VirtualScreen[ 0, 2 ].IsContinuation );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void LowLevelVirtualScreenRemainsExactCellStorage() {
		CursesVirtualScreen screen = new( 3, 1 );
		CursesCell continuation = CursesCell.Continuation();

		screen[ 0, 1 ] = continuation;

		Assert.Equal( continuation, screen[ 0, 1 ] );
		Assert.Throws<InvalidOperationException>(
			() => CursesCellFootprint.Validate( screen )
		);
	}

	[Fact]
	public void RepairBlanksOrphanedContinuation() {
		CursesVirtualScreen screen = new( 3, 1 );
		screen[ 0, 1 ] = CursesCell.Continuation();

		CursesCellFootprint.Repair( screen );

		Assert.True( screen[ 0, 1 ].IsBlank );
		CursesCellFootprint.Validate( screen );
	}

	[Fact]
	public void RepairBlanksLeaderWithoutContinuation() {
		CursesScreen source = new( 4, 1 );
		source.StandardWindow.Write( "\u754C" );
		CursesCell leader = source.VirtualScreen[ 0, 0 ];
		CursesVirtualScreen damaged = new( 2, 1 );
		damaged[ 0, 1 ] = leader;

		Assert.Throws<InvalidOperationException>(
			() => CursesCellFootprint.Validate( damaged )
		);

		CursesCellFootprint.Repair( damaged );

		Assert.True( damaged[ 0, 1 ].IsBlank );
		CursesCellFootprint.Validate( damaged );
	}

	[Fact]
	public void ClearingWindowThatBeginsOnContinuationRepairsLeaderOutsideWindow() {
		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.Write( "\u754CAB" );
		CursesWindow window = screen.CreateWindow(
			0,
			1,
			1,
			2
		);

		window.Clear();

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void ClearingWindowThatEndsOnLeaderRepairsContinuationOutsideWindow() {
		CursesScreen screen = new( 5, 1 );
		CursesWindow standard = screen.StandardWindow;
		standard.Move( 0, 2 );
		standard.Write( "\u754C" );
		CursesWindow window = screen.CreateWindow(
			0,
			1,
			1,
			2
		);

		window.Clear();

		Assert.True( screen.VirtualScreen[ 0, 2 ].IsBlank );
		Assert.True( screen.VirtualScreen[ 0, 3 ].IsBlank );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void WritingAtSubwindowLeftBoundaryRepairsLeaderOutsideWindow() {
		CursesScreen screen = new( 5, 1 );
		screen.StandardWindow.Write( "\u754CAB" );
		CursesWindow window = screen.CreateWindow(
			0,
			1,
			1,
			3
		);

		window.Write( "X" );

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 1 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void WindowWideWritesRemainValidAcrossRepeatedScreenResizes() {
		CursesScreen screen = new( 12, 3 );
		CursesWindow window = screen.StandardWindow;
		string[] values = [
			"A\u754CB",
			"\u2764\uFE0FX",
			"\U0001F1FA\U0001F1F8Y",
			"1\uFE0F\u20E3Z"
		];

		for ( int iteration = 0; iteration < 32; iteration++ ) {
			window.Clear();
			window.Move( 0, 0 );
			window.Write( values[ iteration % values.Length ] );
			CursesCellFootprint.Validate( screen.VirtualScreen );

			int columns = 3 + ( iteration % 10 );
			int rows = 1 + ( iteration % 3 );
			screen.Resize(
				columns,
				rows,
				preserveContents: true
			);
			CursesCellFootprint.Validate( screen.VirtualScreen );
		}
	}

	[Fact]
	public void SeededWindowOperationsPreserveWideFootprints() {
		Random random = new( 0x3030 );
		CursesScreen screen = new( 12, 4 );
		string[] values = [
			"A",
			"\u754C",
			"\u2764\uFE0F",
			"\U0001F1FA\U0001F1F8",
			"1\uFE0F\u20E3",
			"XY"
		];

		for ( int iteration = 0; iteration < 128; iteration++ ) {
			CursesWindow standard = screen.StandardWindow;
			switch ( random.Next( 6 ) ) {
				case 0:
					standard.Move(
						random.Next( standard.Rows ),
						random.Next( standard.Columns )
					);
					standard.WrapMode = 0 == random.Next( 2 )
						? CursesWrapMode.Clip
						: CursesWrapMode.Wrap
					;
					standard.Write( values[ random.Next( values.Length ) ] );
					break;

				case 1:
					standard.Move(
						random.Next( standard.Rows ),
						random.Next( standard.Columns )
					);
					standard.ClearToEndOfLine();
					break;

				case 2:
					int windowColumns = random.Next( 1, screen.Columns + 1 );
					int windowRows = random.Next( 1, screen.Rows + 1 );
					int windowColumn = random.Next( 0, screen.Columns - windowColumns + 1 );
					int windowRow = random.Next( 0, screen.Rows - windowRows + 1 );
					CursesWindow window = screen.CreateWindow(
						windowRow,
						windowColumn,
						windowRows,
						windowColumns
					);
					window.Clear();
					break;

				case 3:
					standard.ScrollUp( random.Next( 1, standard.Rows + 1 ) );
					break;

				case 4:
					standard.ScrollDown( random.Next( 1, standard.Rows + 1 ) );
					break;

				default:
					screen.Resize(
						random.Next( 4, 13 ),
						random.Next( 2, 6 ),
						preserveContents: true
					);
					break;
			}

			CursesCellFootprint.Validate( screen.VirtualScreen );
		}
	}
}
