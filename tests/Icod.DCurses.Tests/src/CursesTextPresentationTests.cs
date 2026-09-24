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

/// <summary>Specifies retained projection of immutable text layouts.</summary>
public sealed class CursesTextPresentationTests {
	[Fact]
	public void PresentTextLayoutWritesOnlyTheSelectedVisualLines() {
		CursesScreen screen = new( 8, 4 );
		CursesTextLayout layout = CursesTextLayout.Create(
			"one\ntwo",
			new CursesTextLayoutOptions( 3 )
		);

		screen.StandardWindow.PresentTextLayout(
			layout,
			1,
			1,
			2,
			2
		);

		Assert.Equal( "t", screen.VirtualScreen[ 2, 2 ].Content );
		Assert.Equal( "w", screen.VirtualScreen[ 2, 3 ].Content );
		Assert.Equal( "o", screen.VirtualScreen[ 2, 4 ].Content );
		Assert.True( screen.VirtualScreen[ 1, 2 ].IsBlank );
	}

	[Fact]
	public void RepeatingTheSameLayoutDoesNotDamageRetainedCells() {
		CursesScreen screen = new( 5, 2 );
		CursesTextLayout layout = CursesTextLayout.Create(
			"one",
			new CursesTextLayoutOptions( 3 )
		);
		CursesWindow window = screen.StandardWindow;
		window.PresentTextLayout( layout, 0, 1, 0, 1 );
		screen.VirtualScreen.MarkClean();

		window.PresentTextLayout( layout, 0, 1, 0, 1 );

		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
		Assert.Equal( "one", string.Concat(
			screen.VirtualScreen[ 0, 1 ].Content,
			screen.VirtualScreen[ 0, 2 ].Content,
			screen.VirtualScreen[ 0, 3 ].Content
		) );
	}

	[Fact]
	public void NegativeDestinationClipsWideElementAsOneFootprint() {
		CursesScreen screen = new( 3, 1 );
		CursesTextLayout layout = CursesTextLayout.Create(
			"界x",
			new CursesTextLayoutOptions( 3 )
		);

		screen.StandardWindow.PresentTextLayout( layout, 0, 1, 0, -1 );

		Assert.True( screen.VirtualScreen[ 0, 0 ].IsBlank );
		Assert.Equal( "x", screen.VirtualScreen[ 0, 1 ].Content );
	}

	[Fact]
	public void RepeatingMetadataLayoutPreservesMetadataWithoutDamage() {
		CursesScreen screen = new( 2, 1 );
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.org/" )
		);
		CursesTextLayout layout = CursesTextLayout.Create(
			"a",
			new CursesTextLayoutOptions( 1 ),
			[ new CursesTextSpan( new CursesTextPosition( 0 ), 1,
				CursesStyle.Default, metadata ) ]
		);
		CursesWindow window = screen.StandardWindow;
		window.PresentTextLayout( layout, 0, 1, 0, 0 );
		screen.VirtualScreen.MarkClean();

		window.PresentTextLayout( layout, 0, 1, 0, 0 );

		Assert.Equal( metadata, window.GetMetadata( 0, 0 ) );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}

	[Fact]
	public void ProjectionRemovesReplacedRasterCell() {
		CursesScreen screen = new( 2, 1 );
		CursesWindow window = screen.StandardWindow;
		window.SetRasterCell( 0, 0,
			CursesRasterRepresentationBaselineTests.CreateLogicalRasterCell() );
		CursesTextLayout layout = CursesTextLayout.Create(
			"a", new CursesTextLayoutOptions( 1 )
		);

		window.PresentTextLayout( layout, 0, 1, 0, 0 );

		Assert.Equal( "a", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Null( window.GetRasterCell( 0, 0 ) );
	}

	[Fact]
	public void WidthProviderFailureOnLaterLineDoesNotPartiallyPresent() {
		FailingPresentationWidthProvider provider = new();
		CursesTextLayout layout = CursesTextLayout.Create(
			"a\nb",
			new CursesTextLayoutOptions( 1 ) { WidthProvider = provider }
		);
		CursesScreen screen = new( 2, 2 );
		screen.VirtualScreen[ 0, 0 ] = new CursesCell( "old" );
		screen.VirtualScreen.MarkClean();
		provider.FailOnB = true;

		Assert.Throws<InvalidOperationException>(
			() => screen.StandardWindow.PresentTextLayout( layout, 0, 2, 0, 0 )
		);

		Assert.Equal( "old", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.Equal( 0, screen.VirtualScreen.DirtyCellCount );
	}

	private sealed class FailingPresentationWidthProvider : ICursesTextWidthProvider {
		public bool FailOnB { get; set; }

		public int GetWidth( string textElement ) {
			if ( FailOnB && "b" == textElement ) {
				throw new InvalidOperationException( "Width provider failure." );
			}
			return UnicodeCursesTextWidthProvider.Instance.GetWidth( textElement );
		}
	}
}
