using Icod.DCurses;
using Icod.DCurses.Internal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Verifies wide-cell leader/continuation invariants through structural screen operations.
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
	public void VirtualScreenRejectsOrphanContinuation() {
		CursesVirtualScreen screen = new( 3, 1 );

		Assert.Throws<ArgumentException>(
			() => screen[ 0, 1 ] = CursesCell.Continuation()
		);
		CursesCellFootprint.Validate( screen );
	}

	[Fact]
	public void VirtualScreenRejectsWideLeaderInFinalColumn() {
		CursesScreen source = new( 4, 1 );
		source.StandardWindow.Write( "\u754C" );
		CursesCell leader = source.VirtualScreen[ 0, 0 ];
		CursesVirtualScreen destination = new( 2, 1 );

		Assert.Throws<ArgumentException>(
			() => destination[ 0, 1 ] = leader
		);
		CursesCellFootprint.Validate( destination );
	}

	[Fact]
	public void CopyingWideLeaderInstallsMatchingContinuation() {
		CursesScreen source = new( 4, 1 );
		source.StandardWindow.Write( "\u754C" );
		CursesCell leader = source.VirtualScreen[ 0, 0 ];
		CursesVirtualScreen destination = new( 4, 1 );

		destination[ 0, 1 ] = leader;

		Assert.Equal( leader, destination[ 0, 1 ] );
		Assert.True( destination[ 0, 2 ].IsContinuation );
		Assert.Equal( leader.Style, destination[ 0, 2 ].Style );
		CursesCellFootprint.Validate( destination );
	}

	[Fact]
	public void ReplacingContinuationRepairsLeader() {
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.Write( "\u754C" );

		screen.VirtualScreen[ 0, 1 ] = new CursesCell( "X" );

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 1 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void ReplacingLeaderRepairsContinuation() {
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.Write( "\u754C" );

		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "X" );

		Assert.Equal( "X", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		CursesCellFootprint.Validate( screen.VirtualScreen );
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
}
