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

using System.Diagnostics;
using Icod.DCurses.Editor.Sample;
using Xunit;
using Xunit.Abstractions;

/// <summary>Qualifies viewport-local editor projection through the public DCurses surface.</summary>
public sealed class CursesEditorViewport24Tests {
	private readonly ITestOutputHelper output;

	public CursesEditorViewport24Tests( ITestOutputHelper output ) {
		this.output = output;
	}

	[Fact]
	public void TenMillionRecordDocumentProjectsOnlyVisibleSlices() {
		EditorSampleState state = new( 12, 40 );
		CursesScreen screen = new( 40, 12 );

		EditorViewportProjectionResult first = state.ProjectVisible(
			screen.StandardWindow
		);
		state.GoToRow( EditorSampleState.DocumentRows - 1 );
		screen.VirtualScreen.MarkClean();
		EditorViewportProjectionResult last = state.ProjectVisible(
			screen.StandardWindow
		);

		Assert.Equal( 10_000_000, EditorSampleState.DocumentRows );
		Assert.InRange( first.RecordCount, 1, 5 );
		Assert.InRange( last.RecordCount, 1, 5 );
		Assert.InRange( first.SourceCharacterCount, 1, 500 );
		Assert.InRange( last.SourceCharacterCount, 1, 500 );
		Assert.InRange( screen.VirtualScreen.DirtyCellCount, 1, 40 * 12 );
	}

	[Fact]
	public void LongSingleLineUsesCallerPreparedBoundedHorizontalSlice() {
		string documentLine = new( 'a', 100_000 );
		const int sourceStart = 99_960;
		string visibleSlice = documentLine.AsSpan( sourceStart, 40 ).ToString();
		CursesScreen screen = new( 40, 1 );

		long before = GC.GetAllocatedBytesForCurrentThread();
		Stopwatch stopwatch = Stopwatch.StartNew();
		CursesTextLayout layout = CursesTextLayout.Create(
			visibleSlice,
			new CursesTextLayoutOptions( 40 ) {
				WrapMode = CursesTextWrapMode.NoWrap,
				StartingColumn = sourceStart
			}
		);
		screen.StandardWindow.PresentTextLayout( layout, 0, 1, 0, 0 );
		stopwatch.Stop();
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		this.output.WriteLine(
			$"bounded long-line slice: allocated={allocated} bytes; elapsed={stopwatch.Elapsed.TotalMilliseconds:F3} ms"
		);

		Assert.Equal( 100_000, documentLine.Length );
		Assert.Equal( 40, layout.Text.Length );
		Assert.Equal( 40, screen.VirtualScreen.DirtyCellCount );
		Assert.All(
			Enumerable.Range( 0, 40 ),
			column => Assert.Equal( "a", screen.VirtualScreen[ 0, column ].Content )
		);
		Assert.InRange( allocated, 0, 512_000 );
		Assert.True( stopwatch.Elapsed < TimeSpan.FromSeconds( 2 ) );
	}

	[Fact]
	public void UnicodeCaretHitAndSelectionGeometryRemainLegal() {
		const string text = "e\u0301界\t👩‍💻\ncontrol";
		CursesTextLayout layout = CursesTextLayout.Create(
			text,
			new CursesTextLayoutOptions( 12 ) {
				WrapMode = CursesTextWrapMode.TextElement,
				TabInterval = 4
			}
		);

		for ( CursesTextPosition position = new( 0 ); ; ) {
			CursesTextVisualPosition visual = layout.GetVisualPosition( position );
			CursesTextHitTestResult hit = layout.HitTest( visual.Line, visual.Column );
			Assert.InRange( visual.Line, 0, layout.Lines.Count - 1 );
			Assert.True( 0 <= visual.Column );
			Assert.True( hit.Position.Offset <= position.Offset );
			CursesTextPosition next = layout.GetNextPosition( position );
			if ( next == position ) {
				break;
			}
			position = next;
		}

		CursesRectangle[] rectangles = layout.GetSelectionRectangles(
			new CursesTextSelection(
				new CursesTextPosition( 0 ),
				new CursesTextPosition( text.IndexOf( '\n' ) )
			),
			0,
			layout.Lines.Count
		);
		Assert.NotEmpty( rectangles );
		Assert.All( rectangles, rectangle => {
			Assert.True( 0 <= rectangle.Row );
			Assert.True( 0 <= rectangle.Column );
			Assert.True( 0 < rectangle.Columns );
		} );
	}

	[Fact]
	public void LargePasteIsRejectedBeforeLayoutOrDocumentMutation() {
		EditorSampleState state = new( 12, 40 );
		string before = state.GetRecord( 0 );
		string paste = new( 'x', 1_000_000 );

		Stopwatch stopwatch = Stopwatch.StartNew();
		bool inserted = state.Insert( paste );
		stopwatch.Stop();

		Assert.False( inserted );
		Assert.Equal( before, state.GetRecord( 0 ) );
		Assert.Equal( 0, state.EditedRecordCount );
		Assert.True( stopwatch.Elapsed < TimeSpan.FromSeconds( 1 ) );
	}

	[Fact]
	public void HorizontalScrollNeverSplitsWideSelectionAndRepaintClearsOldHighlight() {
		CursesCellMetadata metadata = new(
			new CursesHyperlink( "https://example.org/editor" )
		);
		CursesStyle selected = CursesStyle.Default.WithAttributes(
			CursesTextAttributes.Reverse
		);
		CursesTextLayout selectedLayout = CursesTextLayout.Create(
			"a界\tb",
			new CursesTextLayoutOptions( 16 ) {
				WrapMode = CursesTextWrapMode.NoWrap,
				TabInterval = 4
			},
			[
				new CursesTextSpan( new CursesTextPosition( 0 ), 1, CursesStyle.Default, metadata ),
				new CursesTextSpan( new CursesTextPosition( 1 ), 1, selected, metadata ),
				new CursesTextSpan( new CursesTextPosition( 2 ), 2, CursesStyle.Default, metadata )
			]
		);
		CursesScreen screen = new( 4, 1 );

		screen.StandardWindow.PresentTextLayout( selectedLayout, 0, 1, 0, -1 );

		Assert.Equal( "界", screen.VirtualScreen[ 0, 0 ].Content );
		Assert.True( screen.VirtualScreen[ 0, 1 ].IsContinuation );
		Assert.True( screen.VirtualScreen[ 0, 0 ].Style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.True( screen.VirtualScreen[ 0, 1 ].Style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.Equal( metadata, screen.StandardWindow.GetMetadata( 0, 0 ) );
		Assert.Equal( metadata, screen.StandardWindow.GetMetadata( 0, 1 ) );

		CursesTextLayout plainLayout = CursesTextLayout.Create(
			"a界\tb",
			selectedLayout.Options,
			[ new CursesTextSpan( new CursesTextPosition( 0 ), 4, CursesStyle.Default, metadata ) ]
		);
		screen.StandardWindow.Clear();
		screen.StandardWindow.PresentTextLayout( plainLayout, 0, 1, 0, -1 );

		Assert.False( screen.VirtualScreen[ 0, 0 ].Style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.False( screen.VirtualScreen[ 0, 1 ].Style.Attributes.HasFlag( CursesTextAttributes.Reverse ) );
		Assert.Equal( metadata, screen.StandardWindow.GetMetadata( 0, 0 ) );
		Assert.Equal( metadata, screen.StandardWindow.GetMetadata( 0, 1 ) );
	}
}
