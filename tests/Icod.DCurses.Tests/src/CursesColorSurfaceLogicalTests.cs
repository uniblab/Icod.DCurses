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

namespace Icod.DCurses.Tests;

using System.Text;
using Icod.DCurses.Internal;
using Xunit;

/// <summary>Qualifies complete-style retention through logical color-surface mutations.</summary>
public sealed class CursesColorSurfaceLogicalTests {
	private static readonly CursesStyle PrimaryStyle = new(
		CursesColor.Indexed( 37 ),
		CursesColor.Rgb( 12, 34, 56 ),
		CursesTextAttributes.Bold | CursesTextAttributes.Underline
	);

	private static readonly CursesStyle BackgroundStyle = new(
		CursesColor.Rgb( 210, 80, 40 ),
		CursesColor.Indexed( 18 ),
		CursesTextAttributes.Dim | CursesTextAttributes.Italic
	);

	[Fact]
	public void WritesRetainCompleteStyle() {
		CursesScreen screen = new( 16, 2 );
		CursesWindow window = screen.StandardWindow;

		window.Write( "A", PrimaryStyle );
		window.Write( new Rune( 'B' ), PrimaryStyle );
		window.Write( "e\u0301", PrimaryStyle );
		window.Write( "\t", PrimaryStyle );
		window.CurrentStyle = PrimaryStyle;
		window.Write( "C" );

		for ( int column = 0; column < 9; column++ ) {
			Assert.Equal( PrimaryStyle, window.GetCell( 0, column ).Style );
		}
	}

	[Fact]
	public void BackgroundOperationsUseCompleteBackgroundStyle() {
		CursesScreen screen = new( 6, 3 );
		CursesWindow window = screen.StandardWindow;
		window.BackgroundCell = CursesCell.Blank( BackgroundStyle );
		window.FillRectangle(
			0,
			0,
			window.Rows,
			window.Columns,
			new CursesCell( "X", PrimaryStyle )
		);

		window.Move( 0, 2 );
		window.ClearToEndOfLine();
		window.Move( 1, 2 );
		window.ClearToBeginningOfLine();
		window.ScrollUp();

		for ( int column = 0; column < window.Columns; column++ ) {
			Assert.Equal( BackgroundStyle, window.GetCell( 2, column ).Style );
		}
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 0 ).Style );
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 1 ).Style );
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 2 ).Style );
	}

	[Fact]
	public void CopyAndLayoutProjectionRetainCompleteStyle() {
		CursesScreen sourceScreen = new( 4, 1 );
		CursesScreen destinationScreen = new( 8, 2 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		source.Write( "AB", PrimaryStyle );

		source.CopyRectangleTo( destination, 0, 0, 1, 2, 0, 1 );
		CursesTextLayout layout = CursesTextLayout.Create(
			"CD",
			new CursesTextLayoutOptions( 2 ),
			[
				new CursesTextSpan(
					new CursesTextPosition( 0 ),
					2,
					BackgroundStyle
				)
			]
		);
		destination.PresentTextLayout( layout, 0, 1, 1, 3 );

		Assert.Equal( PrimaryStyle, destination.GetCell( 0, 1 ).Style );
		Assert.Equal( PrimaryStyle, destination.GetCell( 0, 2 ).Style );
		Assert.Equal( BackgroundStyle, destination.GetCell( 1, 3 ).Style );
		Assert.Equal( BackgroundStyle, destination.GetCell( 1, 4 ).Style );
	}

	[Fact]
	public void PreparedCellsRetainIndependentColorAxes() {
		CursesScreen screen = new( 4, 1 );
		CursesCell[] prepared = [
			new CursesCell( "A", PrimaryStyle ),
			CursesCell.Blank( BackgroundStyle ),
			new CursesCell( "B", BackgroundStyle.WithAttributes( CursesTextAttributes.Reverse ) )
		];

		screen.StandardWindow.WriteCells( 0, 1, prepared );

		Assert.Equal( prepared[ 0 ], screen.VirtualScreen[ 0, 1 ] );
		Assert.Equal( prepared[ 1 ], screen.VirtualScreen[ 0, 2 ] );
		Assert.Equal( prepared[ 2 ], screen.VirtualScreen[ 0, 3 ] );
	}

	[Fact]
	public void EqualCellRewriteDoesNotCreateNewRevision() {
		CursesVirtualScreen screen = new( 2, 1 );
		screen.EnableChangeTracking();
		CursesCell cell = new( "A", PrimaryStyle );
		screen[ 0, 0 ] = cell;
		ulong revision = screen.ChangeRevision;
		screen.MarkClean();

		screen[ 0, 0 ] = cell;

		Assert.Equal( revision, screen.ChangeRevision );
		Assert.Equal( 0, screen.DirtyCellCount );
	}

	[Fact]
	public void WideLeaderAndContinuationCarryIdenticalStyle() {
		CursesScreen screen = new( 4, 1 );
		screen.StandardWindow.Write( "\u754C", PrimaryStyle );

		CursesCell leader = screen.VirtualScreen[ 0, 0 ];
		CursesCell continuation = screen.VirtualScreen[ 0, 1 ];
		Assert.Equal( 2, leader.DisplayWidth );
		Assert.True( continuation.IsContinuation );
		Assert.Equal( PrimaryStyle, leader.Style );
		Assert.Equal( PrimaryStyle, continuation.Style );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void OverwritingContinuationRepairsBothColumnsWithBackgroundStyle() {
		CursesScreen screen = new( 4, 1 );
		CursesWindow window = screen.StandardWindow;
		window.BackgroundCell = CursesCell.Blank( BackgroundStyle );
		window.Write( "\u754C", PrimaryStyle );
		window.Move( 0, 1 );

		window.WriteCell( CursesCell.Blank( BackgroundStyle ) );

		Assert.Equal( CursesCell.Blank( BackgroundStyle ), window.GetCell( 0, 0 ) );
		Assert.Equal( CursesCell.Blank( BackgroundStyle ), window.GetCell( 0, 1 ) );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void ClippedWideCopyLeavesNoStaleColoredHalf() {
		CursesScreen sourceScreen = new( 3, 1 );
		CursesScreen destinationScreen = new( 4, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		source.BackgroundCell = CursesCell.Blank( BackgroundStyle );
		source.Write( "\u754C", PrimaryStyle );
		destination.Write( "\u754C", PrimaryStyle );

		source.CopyRectangleTo( destination, 0, 0, 1, 1, 0, 1 );

		Assert.True( destination.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( PrimaryStyle, destination.GetCell( 0, 0 ).Style );
		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( BackgroundStyle, destination.GetCell( 0, 1 ).Style );
		CursesCellFootprint.Validate( destinationScreen.VirtualScreen );
	}

	[Fact]
	public void EditingAcrossWideFootprintUsesCompleteBackgroundStyle() {
		CursesScreen screen = new( 7, 1 );
		CursesWindow window = screen.StandardWindow;
		window.BackgroundCell = CursesCell.Blank( BackgroundStyle );
		window.Write( "A\u754CBC", PrimaryStyle );
		window.Move( 0, 1 );

		window.DeleteCells( 99 );

		Assert.True( window.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 1 ).Style );
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 5 ).Style );
		Assert.Equal( BackgroundStyle, window.GetCell( 0, 6 ).Style );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}
}
