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
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Verifies rectangular copy, overwrite, and blank-transparent overlay semantics.</summary>
public sealed class CursesWindowCompositionTests {
	[Fact]
	public void CopyRectangleIncludesSourceBlanks() {
		CursesScreen screen = new( 8, 4 );
		CursesWindow source = screen.CreateWindow(
			0,
			0,
			1,
			4
		);
		CursesWindow destination = screen.CreateWindow(
			2,
			2,
			1,
			4
		);
		source.Move( 0, 0 );
		source.Write( "A" );
		source.Move( 0, 2 );
		source.Write( "B" );
		destination.FillRectangle(
			0,
			0,
			1,
			4,
			new CursesCell( "D" )
		);

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			4,
			0,
			0
		);

		Assert.Equal( "A", destination.GetCell( 0, 0 ).Content );
		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( "B", destination.GetCell( 0, 2 ).Content );
		Assert.True( destination.GetCell( 0, 3 ).IsBlank );
	}

	[Fact]
	public void OverlayRectangleLeavesDestinationUnderSourceBlanks() {
		CursesScreen screen = new( 8, 4 );
		CursesWindow source = screen.CreateWindow(
			0,
			0,
			1,
			4
		);
		CursesWindow destination = screen.CreateWindow(
			2,
			2,
			1,
			4
		);
		source.Move( 0, 0 );
		source.Write( "A" );
		source.Move( 0, 2 );
		source.Write( "B" );
		destination.FillRectangle(
			0,
			0,
			1,
			4,
			new CursesCell( "D" )
		);

		source.OverlayRectangleTo(
			destination,
			0,
			0,
			1,
			4,
			0,
			0
		);

		Assert.Equal( "A", destination.GetCell( 0, 0 ).Content );
		Assert.Equal( "D", destination.GetCell( 0, 1 ).Content );
		Assert.Equal( "B", destination.GetCell( 0, 2 ).Content );
		Assert.Equal( "D", destination.GetCell( 0, 3 ).Content );
	}

	[Fact]
	public void SameWindowOverlappingCopyUsesSourceSnapshot() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCDE" );

		window.CopyRectangleTo(
			window,
			0,
			0,
			1,
			4,
			0,
			1
		);

		Assert.Equal( "A", window.GetCell( 0, 0 ).Content );
		Assert.Equal( "A", window.GetCell( 0, 1 ).Content );
		Assert.Equal( "B", window.GetCell( 0, 2 ).Content );
		Assert.Equal( "C", window.GetCell( 0, 3 ).Content );
		Assert.Equal( "D", window.GetCell( 0, 4 ).Content );
	}

	[Fact]
	public void CompleteWideFootprintCopiesAtomically() {
		CursesScreen screen = new( 9, 3 );
		CursesWindow source = screen.CreateWindow(
			0,
			0,
			1,
			4
		);
		CursesWindow destination = screen.CreateWindow(
			2,
			1,
			1,
			4
		);
		source.Write( "A\u754CB" );

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			4,
			0,
			0
		);

		Assert.Equal( "A", destination.GetCell( 0, 0 ).Content );
		Assert.Equal( "\u754C", destination.GetCell( 0, 1 ).Content );
		Assert.Equal( 2, destination.GetCell( 0, 1 ).DisplayWidth );
		Assert.True( destination.GetCell( 0, 2 ).IsContinuation );
		Assert.Equal( "B", destination.GetCell( 0, 3 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void SourceRectangleBeginningAtContinuationDoesNotCopyOrphan() {
		CursesScreen screen = new( 10, 3 );
		CursesWindow source = screen.CreateWindow(
			0,
			0,
			1,
			5
		);
		CursesWindow destination = screen.CreateWindow(
			2,
			4,
			1,
			2
		);
		source.Write( "A\u754CB" );
		destination.FillRectangle(
			0,
			0,
			1,
			2,
			new CursesCell( "D" )
		);

		source.CopyRectangleTo(
			destination,
			0,
			2,
			1,
			2,
			0,
			0
		);

		Assert.True( destination.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "B", destination.GetCell( 0, 1 ).Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void DestinationBoundaryRepairClearsWideLeaderOutsideCopyRectangle() {
		CursesScreen screen = new( 8, 2 );
		CursesWindow source = screen.CreateWindow(
			1,
			0,
			1,
			1
		);
		source.Write( "X" );
		screen.StandardWindow.Move(
			0,
			1
		);
		screen.StandardWindow.Write( "\u754C" );
		CursesWindow destination = screen.CreateWindow(
			0,
			2,
			1,
			1
		);

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			1,
			0,
			0
		);

		Assert.True( screen.VirtualScreen[ 0, 1 ].IsBlank );
		Assert.Equal( "X", screen.VirtualScreen[ 0, 2 ].Content );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void CompositionPreservesSourceAndDestinationCursors() {
		CursesScreen screen = new( 8, 4 );
		CursesWindow source = screen.CreateWindow( 0, 0, 2, 3 );
		CursesWindow destination = screen.CreateWindow( 2, 4, 2, 3 );
		source.Move( 1, 2 );
		destination.Move( 1, 1 );

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			2,
			0,
			0
		);

		Assert.Equal( 1, source.CursorRow );
		Assert.Equal( 2, source.CursorColumn );
		Assert.Equal( 1, destination.CursorRow );
		Assert.Equal( 1, destination.CursorColumn );
	}

	[Fact]
	public void OverlayWideFootprintRepairsDestinationFootprints() {
		CursesScreen screen = new( 10, 3 );
		CursesWindow source = screen.CreateWindow( 0, 0, 1, 3 );
		CursesWindow destination = screen.CreateWindow( 2, 4, 1, 3 );
		source.Write( "\u754C" );
		destination.Write( "\u754C" );
		destination.Move( 0, 0 );

		source.OverlayRectangleTo(
			destination,
			0,
			0,
			1,
			2,
			0,
			1
		);

		Assert.True( screen.VirtualScreen[ 2, 4 ].IsBlank );
		Assert.Equal( "\u754C", screen.VirtualScreen[ 2, 5 ].Content );
		Assert.True( screen.VirtualScreen[ 2, 6 ].IsContinuation );
		CursesCellFootprint.Validate( screen.VirtualScreen );
	}

	[Fact]
	public void CompositionRejectsNullDestination() {
		CursesScreen screen = new( 4, 2 );
		CursesWindow source = screen.StandardWindow;

		Assert.Throws<ArgumentNullException>(
			() => source.CopyRectangleTo(
				null!,
				0,
				0,
				1,
				1,
				0,
				0
			)
		);
	}

	[Fact]
	public void CompositionRejectsOutOfRangeDestinationRectangle() {
		CursesScreen screen = new( 6, 3 );
		CursesWindow source = screen.CreateWindow( 0, 0, 1, 2 );
		CursesWindow destination = screen.CreateWindow( 1, 1, 1, 2 );

		Assert.Throws<ArgumentOutOfRangeException>(
			() => source.OverlayRectangleTo(
				destination,
				0,
				0,
				1,
				2,
				0,
				1
			)
		);
	}
}
