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

using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Exercises T1105 semantic-metadata propagation through structural logical operations.</summary>
public sealed class CursesSemanticPropagationTests {
	[Fact]
	public void InsertCellsMovesMetadataWithSurvivingContent() {
		CursesScreen screen = new( 8, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCD" );
		CursesCellMetadata metadata = LinkMetadata( "insert" );
		window.SetMetadata(
			0,
			2,
			metadata
		);

		window.Move( 0, 1 );
		window.InsertCells( 2 );

		Assert.Equal( "C", window.GetCell( 0, 4 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 0, 4 ) );
		Assert.Null( window.GetMetadata( 0, 1 ) );
		Assert.Null( window.GetMetadata( 0, 2 ) );
	}

	[Fact]
	public void DeleteCellsMovesMetadataLeftAndClearsVacatedTail() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCD" );
		CursesCellMetadata metadata = LinkMetadata( "delete" );
		window.SetMetadata(
			0,
			3,
			metadata
		);

		window.Move( 0, 1 );
		window.DeleteCells( 2 );

		Assert.Equal( "D", window.GetCell( 0, 1 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 0, 1 ) );
		Assert.Null( window.GetMetadata( 0, 3 ) );
		Assert.Null( window.GetMetadata( 0, 5 ) );
	}

	[Fact]
	public void InsertAndDeleteLinesMoveMetadataWithRows() {
		CursesScreen screen = new( 4, 4 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata( "lines" );
		window.Move( 1, 0 );
		window.WriteWithMetadata( "L", metadata );

		window.Move( 0, 0 );
		window.InsertLines();

		Assert.Equal( "L", window.GetCell( 2, 0 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 2, 0 ) );
		Assert.Null( window.GetMetadata( 0, 0 ) );

		window.Move( 0, 0 );
		window.DeleteLines();

		Assert.Equal( "L", window.GetCell( 1, 0 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 1, 0 ) );
		Assert.Null( window.GetMetadata( 3, 0 ) );
	}

	[Fact]
	public void ScrollMovesMetadataWithSurvivingRows() {
		CursesScreen screen = new( 4, 4 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata( "scroll" );
		window.Move( 2, 1 );
		window.WriteWithMetadata( "S", metadata );

		window.ScrollUp();

		Assert.Equal( "S", window.GetCell( 1, 1 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 1, 1 ) );
		Assert.Null( window.GetMetadata( 3, 1 ) );

		window.ScrollDown();

		Assert.Equal( "S", window.GetCell( 2, 1 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 2, 1 ) );
		Assert.Null( window.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void WideCellEditingPreservesOnlyCoherentMetadataFootprints() {
		CursesScreen screen = new( 7, 1 );
		CursesWindow window = screen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata( "wide" );
		window.Write( "A" );
		window.WriteWithMetadata( "界", metadata );
		window.Write( "B" );

		window.Move( 0, 0 );
		window.InsertCells();

		Assert.Equal( "界", window.GetCell( 0, 2 ).Content );
		Assert.True( window.GetCell( 0, 3 ).IsContinuation );
		Assert.Equal( metadata, window.GetMetadata( 0, 2 ) );
		Assert.Equal( metadata, window.GetMetadata( 0, 3 ) );

		window.Move( 0, 3 );
		window.DeleteCells();

		Assert.True( window.GetCell( 0, 2 ).IsBlank );
		Assert.Null( window.GetMetadata( 0, 2 ) );
		Assert.Null( window.GetMetadata( 0, 3 ) );
	}

	[Fact]
	public void CopyRectangleTransfersMetadataIncludingSemanticBlank() {
		CursesScreen sourceScreen = new( 3, 1 );
		CursesScreen destinationScreen = new( 3, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		CursesCellMetadata metadata = LinkMetadata( "blank-copy" );
		source.SetMetadata(
			0,
			1,
			metadata
		);
		destination.Write( "XYZ" );

		source.CopyRectangleTo(
			destination,
			0,
			0,
			1,
			3,
			0,
			0
		);

		Assert.True( destination.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( metadata, destination.GetMetadata( 0, 1 ) );
	}

	[Fact]
	public void OverlayBlankLeavesDestinationCellAndMetadataUntouched() {
		CursesScreen sourceScreen = new( 1, 1 );
		CursesScreen destinationScreen = new( 1, 1 );
		CursesWindow source = sourceScreen.StandardWindow;
		CursesWindow destination = destinationScreen.StandardWindow;
		CursesCellMetadata sourceMetadata = LinkMetadata( "source" );
		CursesCellMetadata destinationMetadata = LinkMetadata( "destination" );
		source.SetMetadata(
			0,
			0,
			sourceMetadata
		);
		destination.WriteWithMetadata( "D", destinationMetadata );

		source.OverlayRectangleTo(
			destination,
			0,
			0,
			1,
			1,
			0,
			0
		);

		Assert.Equal( "D", destination.GetCell( 0, 0 ).Content );
		Assert.Equal( destinationMetadata, destination.GetMetadata( 0, 0 ) );
	}

	[Fact]
	public void OverlappingCopySnapshotsMetadataBeforeMutation() {
		CursesScreen screen = new( 6, 1 );
		CursesWindow window = screen.StandardWindow;
		window.Write( "ABCD" );
		CursesCellMetadata metadata = LinkMetadata( "overlap" );
		window.SetMetadata(
			0,
			1,
			metadata
		);

		window.CopyRectangleTo(
			window,
			0,
			0,
			1,
			4,
			0,
			1
		);

		Assert.Equal( "A", window.GetCell( 0, 1 ).Content );
		Assert.Null( window.GetMetadata( 0, 1 ) );
		Assert.Equal( "B", window.GetCell( 0, 2 ).Content );
		Assert.Equal( metadata, window.GetMetadata( 0, 2 ) );
	}

	[Fact]
	public void PadPresentationTransfersSemanticMetadata() {
		CursesPad pad = new( 5, 2 );
		CursesWindow content = pad.ContentWindow;
		CursesCellMetadata metadata = LinkMetadata( "pad" );
		content.Move( 1, 2 );
		content.WriteWithMetadata( "P", metadata );
		CursesScreen destinationScreen = new( 5, 2 );

		pad.PresentTo(
			destinationScreen.StandardWindow,
			0,
			0,
			2,
			5,
			0,
			0
		);

		Assert.Equal( "P", destinationScreen.StandardWindow.GetCell( 1, 2 ).Content );
		Assert.Equal(
			metadata,
			destinationScreen.StandardWindow.GetMetadata( 1, 2 )
		);
	}

	[Fact]
	public void PadViewportObservesAndPresentsSemanticOnlyChange() {
		CursesPad pad = new( 5, 2 );
		pad.ContentWindow.Move( 0, 1 );
		pad.ContentWindow.Write( "V" );
		CursesScreen destinationScreen = new( 5, 2 );
		CursesPadViewport viewport = pad.CreateViewport(
			destinationScreen.StandardWindow,
			0,
			0,
			2,
			5,
			0,
			0
		);
		viewport.Present();
		Assert.False( viewport.HasVisiblePadChanges );
		CursesCellMetadata metadata = LinkMetadata( "viewport" );

		pad.ContentWindow.SetMetadata(
			0,
			1,
			metadata
		);

		Assert.True( viewport.HasVisiblePadChanges );
		viewport.Present();
		Assert.Equal(
			metadata,
			destinationScreen.StandardWindow.GetMetadata( 0, 1 )
		);
		Assert.False( viewport.HasVisiblePadChanges );
	}

	[Fact]
	public void PreserveResizeKeepsSurvivingMetadata() {
		CursesScreen screen = new( 4, 2 );
		CursesCellMetadata metadata = LinkMetadata( "resize" );
		screen.StandardWindow.Move( 1, 1 );
		screen.StandardWindow.WriteWithMetadata( "R", metadata );

		screen.Resize(
			6,
			3,
			preserveContents: true
		);

		Assert.Equal( "R", screen.StandardWindow.GetCell( 1, 1 ).Content );
		Assert.Equal( metadata, screen.StandardWindow.GetMetadata( 1, 1 ) );
	}

	[Fact]
	public void PreserveResizeDropsMetadataWhenWideFootprintIsClipped() {
		CursesScreen screen = new( 4, 1 );
		CursesCellMetadata metadata = LinkMetadata( "resize-wide" );
		screen.StandardWindow.Move( 0, 2 );
		screen.StandardWindow.WriteWithMetadata( "界", metadata );

		screen.Resize(
			3,
			1,
			preserveContents: true
		);

		Assert.True( screen.StandardWindow.GetCell( 0, 2 ).IsBlank );
		Assert.Null( screen.StandardWindow.GetMetadata( 0, 2 ) );
	}

	private static CursesCellMetadata LinkMetadata( string identifier ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( identifier );
		return new CursesCellMetadata(
			new CursesHyperlink(
				$"https://example.test/{identifier}",
				identifier
			)
		);
	}
}
