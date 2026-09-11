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

/// <summary>Hardens Unicode, semantic metadata, transparency, and repeated retained resize behavior.</summary>
public sealed class CursesLayoutUnicodeResizeTests {
	private const string WideText = "\u4E00";

	[Fact]
	public void RepeatedGrowShrinkPreservesSurvivingWideFootprintAndMetadata() {
		CursesScreen screen = new( 12, 8 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 2, 4 );
		CursesWindow content = panel.ContentWindow;
		CursesCellMetadata asciiMetadata = new(
			new CursesHyperlink( "https://example.test/t1308-ascii" )
		);
		CursesCellMetadata wideMetadata = new(
			new CursesHyperlink( "https://example.test/t1308-wide" )
		);
		content.WriteWithMetadata(
			"A",
			asciiMetadata
		);
		content.Move( 1, 1 );
		content.WriteWithMetadata(
			WideText,
			wideMetadata
		);

		( int Rows, int Columns )[] dimensions = [
			( 3, 6 ),
			( 2, 4 ),
			( 4, 5 ),
			( 2, 3 ),
			( 3, 5 ),
			( 2, 4 )
		];
		foreach ( ( int rows, int columns ) in dimensions ) {
			panel.Resize(
				rows,
				columns
			);

			Assert.Equal( "A", content.GetCell( 0, 0 ).Content );
			Assert.Equal( asciiMetadata, content.GetMetadata( 0, 0 ) );
			Assert.Equal( WideText, content.GetCell( 1, 1 ).Content );
			Assert.True( content.GetCell( 1, 2 ).IsContinuation );
			Assert.Equal( wideMetadata, content.GetMetadata( 1, 1 ) );
			Assert.Equal( wideMetadata, content.GetMetadata( 1, 2 ) );
			CursesCellFootprint.Validate( panel.VirtualScreen );
		}
	}

	[Fact]
	public void ClippedWideFootprintDoesNotReappearAfterRegrow() {
		CursesScreen screen = new( 8, 4 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 3 );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1308-clipped" )
		);
		panel.ContentWindow.Move( 0, 1 );
		panel.ContentWindow.WriteWithMetadata(
			WideText,
			metadata
		);
		CursesCellFootprint.Validate( panel.VirtualScreen );

		panel.Resize(
			1,
			2
		);

		Assert.True( panel.ContentWindow.GetCell( 0, 1 ).IsBlank );
		Assert.Null( panel.ContentWindow.GetMetadata( 0, 1 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );

		panel.Resize(
			1,
			3
		);

		Assert.True( panel.ContentWindow.GetCell( 0, 1 ).IsBlank );
		Assert.True( panel.ContentWindow.GetCell( 0, 2 ).IsBlank );
		Assert.Null( panel.ContentWindow.GetMetadata( 0, 1 ) );
		Assert.Null( panel.ContentWindow.GetMetadata( 0, 2 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
	}

	[Fact]
	public void TransparentBlankGrowthContinuesToRevealUnderlyingBaseCells() {
		CursesScreen screen = new( 10, 4 );
		screen.StandardWindow.Move( 1, 2 );
		screen.StandardWindow.Write( "ABCD" );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 1 );
		panel.Transparency = CursesPanelTransparency.BlankCellsTransparent;
		panel.ContentWindow.Write( "P" );
		CursesVirtualScreen composed = screen.ComposePanels();
		Assert.Equal( "PBCD", ReadText( composed, 1, 2, 4 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
		CursesCellFootprint.Validate( composed );

		panel.Resize(
			1,
			4
		);
		CursesVirtualScreen grown = screen.ComposePanels();

		Assert.Same( composed, grown );
		Assert.Equal( "PBCD", ReadText( grown, 1, 2, 4 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
		CursesCellFootprint.Validate( grown );

		panel.ContentWindow.Move( 0, 2 );
		panel.ContentWindow.Write( "Q" );
		CursesVirtualScreen written = screen.ComposePanels();

		Assert.Same( composed, written );
		Assert.Equal( "PBQD", ReadText( written, 1, 2, 4 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
		CursesCellFootprint.Validate( written );

		panel.Resize(
			1,
			2
		);
		CursesVirtualScreen shrunk = screen.ComposePanels();

		Assert.Same( composed, shrunk );
		Assert.Equal( "PBCD", ReadText( shrunk, 1, 2, 4 ) );
		CursesCellFootprint.Validate( panel.VirtualScreen );
		CursesCellFootprint.Validate( shrunk );
	}

	[Fact]
	public void RepeatedResizeCompositionCyclesRemainWideCellCoherent() {
		CursesScreen screen = new( 12, 5 );
		CursesPanel panel = screen.CreatePanel( 1, 2, 1, 4 );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.test/t1308-cycle" )
		);
		panel.ContentWindow.WriteWithMetadata(
			WideText,
			metadata
		);
		CursesVirtualScreen composed = screen.ComposePanels();
		CursesCellFootprint.Validate( panel.VirtualScreen );
		CursesCellFootprint.Validate( composed );

		for ( int cycle = 0; cycle < 24; cycle++ ) {
			int columns = 0 == ( cycle % 2 )
				? 6
				: 2
			;
			panel.Resize(
				1,
				columns
			);
			CursesVirtualScreen current = screen.ComposePanels();

			Assert.Same( composed, current );
			Assert.Equal( WideText, panel.ContentWindow.GetCell( 0, 0 ).Content );
			Assert.True( panel.ContentWindow.GetCell( 0, 1 ).IsContinuation );
			Assert.Equal( metadata, panel.ContentWindow.GetMetadata( 0, 0 ) );
			Assert.Equal( metadata, panel.ContentWindow.GetMetadata( 0, 1 ) );
			CursesCellFootprint.Validate( panel.VirtualScreen );
			CursesCellFootprint.Validate( current );
		}
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

		return string.Concat(
			Enumerable.Range(
				column,
				length
			).Select(
				current => screen.GetCell( row, current ).Content
			)
		);
	}
}
