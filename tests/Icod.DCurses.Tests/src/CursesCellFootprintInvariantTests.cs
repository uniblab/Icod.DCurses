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
	public void RepairBlanksOrphanedContinuation() {
		CursesVirtualScreen screen = new( 3, 1 );
		screen[ 0, 1 ] = CursesCell.Continuation();

		Assert.Throws<InvalidOperationException>(
			() => CursesCellFootprint.Validate( screen )
		);

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
