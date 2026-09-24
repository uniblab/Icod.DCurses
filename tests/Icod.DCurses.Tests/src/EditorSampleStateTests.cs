using Icod.DCurses.Editor.Sample;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class EditorSampleStateTests {
	[Fact]
	public void SyntheticDocumentKeepsOnlyEditedRecordsAndVisibleLayouts() {
		EditorSampleState state = new( 14, 40 );
		Assert.Equal( EditorSampleState.DocumentRows * EditorSampleState.RecordVisualRows,
			state.Viewport.ContentRows );
		Assert.Equal( 0, state.EditedRecordCount );
		(int first, int count) = state.VisibleRecords();
		Assert.Equal( 0, first );
		Assert.InRange( count, 1, 5 );
		state.GoToRow( EditorSampleState.DocumentRows - 1 );
		Assert.True( state.TryCaret( out _ ) );
		(first, count) = state.VisibleRecords();
		Assert.InRange( count, 1, 5 );
		Assert.True( first >= EditorSampleState.DocumentRows - 5 );
		Assert.Equal( 0, state.EditedRecordCount );
		Assert.True( state.Insert( "X" ) );
		Assert.Equal( 1, state.EditedRecordCount );
		Assert.StartsWith( "X", state.GetRecord( state.Row ), StringComparison.Ordinal );
		Assert.StartsWith( "0000000", state.GetRecord( 0 ), StringComparison.Ordinal );
	}

	[Fact]
	public void UnicodeEditsUseTextElementBoundariesAndRejectMalformedInput() {
		EditorSampleState state = new( 12, 50 );
		Assert.True( state.Insert( "👩‍💻" ) );
		Assert.Equal( "👩‍💻".Length, state.Offset );
		state.MoveHorizontal( -1 );
		Assert.Equal( 0, state.Offset );
		state.MoveHorizontal( 1 );
		Assert.Equal( "👩‍💻".Length, state.Offset );
		Assert.True( state.Delete( true ) );
		Assert.Equal( 0, state.Offset );
		Assert.False( state.Insert( "\uD800" ) );
		Assert.False( state.Insert( "\r" ) );
		Assert.True( state.Insert( "e\u0301界\t" ) );
		Assert.Equal( 4, state.Offset );
		state.MoveHorizontal( -1 );
		Assert.Equal( 3, state.Offset );
		state.MoveHorizontal( -1 );
		Assert.Equal( 2, state.Offset );
		state.MoveHorizontal( -1 );
		Assert.Equal( 0, state.Offset );
		Assert.True( state.Insert( "\n" ) );
		Assert.True( state.LayoutRecord( state.Row ).Lines.Count >= 2 );
	}

	[Fact]
	public void SelectionReplacementAndDeletionRemainAtomicAtTextElementBoundaries() {
		EditorSampleState state = new( 12, 40 );
		Assert.True( state.Insert( "👩‍💻" ) );
		state.MoveHorizontal( -1 );
		state.ToggleSelection();
		state.MoveHorizontal( 1 );
		string before = state.GetRecord( state.Row );
		Assert.False( state.Insert( "\uD800" ) );
		Assert.Equal( before, state.GetRecord( state.Row ) );
		Assert.True( state.Selecting );
		Assert.True( state.Insert( "界" ) );
		Assert.StartsWith( "界", state.GetRecord( state.Row ), StringComparison.Ordinal );
		Assert.Equal( 1, state.Offset );
		Assert.False( state.Selecting );
		state.MoveHorizontal( -1 );
		state.ToggleSelection();
		state.MoveHorizontal( 1 );
		Assert.True( state.Delete( false ) );
		Assert.StartsWith( "0000000", state.GetRecord( state.Row ), StringComparison.Ordinal );
		Assert.Equal( 0, state.Offset );
	}

	[Fact]
	public void SelectionWrapHorizontalScrollAndResizePreserveLegalPositions() {
		EditorSampleState state = new( 12, 30 );
		state.ToggleSelection();
		state.MoveHorizontal( 1 );
		Assert.NotEmpty( state.SelectionRectangles( state.Row, state.LayoutRecord( state.Row ) ) );
		state.ToggleSelection();
		state.ToggleWrap();
		Assert.False( state.Wrap );
		state.MoveLineBoundary( true );
		Assert.True( state.Viewport.OriginColumn > 0 );
		Assert.True( state.TryCaret( out _ ) );
		state.Resize( 24, 42 );
		Assert.True( state.TryCaret( out _ ) );
		state.ToggleWrap();
		Assert.True( state.Wrap );
		Assert.Equal( 0, state.Viewport.OriginColumn );
		state.MovePage( int.MaxValue );
		Assert.Equal( EditorSampleState.DocumentRows - 1, state.Row );
	}

	[Fact]
	public void TracksRemainDisjointAtSupportedSizes() {
		Assert.False( EditorSampleLayout.TryArrange( new CursesRectangle( 0, 0, 5, 29 ), out _ ) );
		foreach ( CursesRectangle bounds in new[] {
			new CursesRectangle( 0, 0, 6, 30 ),
			new CursesRectangle( 0, 0, 24, 80 )
		} ) {
			Assert.True( EditorSampleLayout.TryArrange( bounds, out EditorRegions regions ) );
			Assert.True( bounds.Contains( regions.Document ) );
			Assert.True( bounds.Contains( regions.Status ) );
			Assert.True( bounds.Contains( regions.Prompt ) );
			Assert.True( regions.Document.Intersect( regions.Status ).IsEmpty );
			Assert.True( regions.Status.Intersect( regions.Prompt ).IsEmpty );
		}
	}
}
