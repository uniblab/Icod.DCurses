namespace Icod.DCurses.Editor.Sample;

using Icod.DCurses;
using System.Buffers;
using System.Text;

// A sparse, fixed-record synthetic document. This is application policy, not a DCurses buffer.
internal sealed class EditorSampleState {
	internal const int DocumentRows = 10_000_000;
	internal const int RecordVisualRows = 4;
	internal const int ContentColumns = 1024;
	internal const int MaximumRecordLength = 100;
	private readonly Dictionary<int, string> edits = [];
	private int? selectionAnchor;
	private int preferredColumn;

	internal EditorSampleState( int rows, int columns ) {
		Viewport = new CursesViewport( DocumentRows * RecordVisualRows,
			ContentColumns, rows, columns );
	}

	internal int Row { get; private set; }
	internal int Offset { get; private set; }
	internal bool Wrap { get; private set; } = true;
	internal CursesViewport Viewport { get; private set; }
	internal int EditedRecordCount => edits.Count;
	internal bool Selecting => selectionAnchor.HasValue;
	internal int? SelectionAnchor => selectionAnchor;

	internal string GetRecord( int row ) {
		if ( row < 0 || row >= DocumentRows ) {
			throw new ArgumentOutOfRangeException( nameof( row ) );
		}
		return edits.TryGetValue( row, out string? value ) ? value
			: $"{row:D7}  Text 👩‍💻  café  界\tEdit this record; arrows scroll the document.";
	}

	internal CursesTextLayout LayoutRecord( int row ) => CursesTextLayout.Create(
		GetRecord( row ), new CursesTextLayoutOptions( Wrap ? Math.Max( 1, Viewport.Columns ) : ContentColumns ) {
			WrapMode = Wrap ? CursesTextWrapMode.TextElement : CursesTextWrapMode.NoWrap,
			MaximumRows = RecordVisualRows
		} );

	internal (int FirstRow, int Count) VisibleRecords() {
		int first = Viewport.OriginRow / RecordVisualRows;
		int last = Math.Min( DocumentRows - 1,
			( Viewport.OriginRow + Math.Max( 0, Viewport.Rows - 1 ) ) / RecordVisualRows );
		return ( first, last - first + 1 );
	}

	internal bool TryCaret( out CursesCellPosition position ) {
		CursesTextLayout layout = LayoutRecord( Row );
		CursesTextVisualPosition visual = layout.GetVisualPosition( new CursesTextPosition( Offset ) );
		return Viewport.TryContentToViewport(
			new CursesCellPosition( Row * RecordVisualRows + visual.Line, Math.Min( ContentColumns - 1, visual.Column ) ),
			out position );
	}

	internal CursesRectangle[] SelectionRectangles( int row, CursesTextLayout layout ) {
		if ( row != Row || selectionAnchor is not int anchor || anchor == Offset ) {
			return [];
		}
		return layout.GetSelectionRectangles(
			new CursesTextSelection( new CursesTextPosition( anchor ), new CursesTextPosition( Offset ) ),
			0, layout.Lines.Count );
	}

	internal void Resize( int rows, int columns ) {
		Viewport = Viewport.WithViewportExtent( rows, columns );
		EnsureCaret();
	}

	internal void ToggleWrap() {
		Wrap = !Wrap;
		Viewport = Viewport.MoveTo( Viewport.OriginRow, 0 );
		EnsureCaret();
	}

	internal void ToggleSelection() => selectionAnchor = selectionAnchor.HasValue ? null : Offset;

	internal void GoToRow( int row ) {
		Row = Math.Clamp( row, 0, DocumentRows - 1 );
		Offset = 0;
		selectionAnchor = null;
		preferredColumn = 0;
		EnsureCaret();
	}

	internal void MoveHorizontal( int direction ) {
		CursesTextLayout layout = LayoutRecord( Row );
		CursesTextPosition position = new( Offset );
		if ( direction < 0 ) {
			Offset = layout.GetPreviousPosition( position ).Offset;
		} else if ( direction > 0 ) {
			Offset = layout.GetNextPosition( position ).Offset;
		}
		preferredColumn = layout.GetVisualPosition( new CursesTextPosition( Offset ) ).Column;
		EnsureCaret();
	}

	internal void MoveVertical( int direction ) {
		if ( direction == 0 ) {
			return;
		}
		CursesTextLayout layout = LayoutRecord( Row );
		CursesTextVisualPosition visual = layout.GetVisualPosition( new CursesTextPosition( Offset ) );
		int target = visual.Line + Math.Sign( direction );
		if ( target >= 0 && target < layout.Lines.Count ) {
			CursesTextVisualPosition moved = layout.MoveVertically( visual, Math.Sign( direction ), preferredColumn );
			Offset = layout.HitTest( moved.Line, moved.Column ).Position.Offset;
		} else {
			int next = (int)Math.Clamp( (long)Row + Math.Sign( direction ), 0, DocumentRows - 1 );
			if ( next == Row ) {
				return;
			}
			Row = next;
			selectionAnchor = null;
			layout = LayoutRecord( Row );
			int line = direction < 0 ? layout.Lines.Count - 1 : 0;
			Offset = layout.HitTest( line, preferredColumn ).Position.Offset;
		}
		EnsureCaret();
	}

	internal void MovePage( int pages ) {
		int records = Math.Max( 1, Viewport.Rows / RecordVisualRows );
		GoToRow( (int)Math.Clamp( (long)Row + (long)pages * records, 0, DocumentRows - 1 ) );
	}

	internal void MoveLineBoundary( bool end ) {
		CursesTextLayout layout = LayoutRecord( Row );
		Offset = end ? layout.Text.Length : 0;
		preferredColumn = layout.GetVisualPosition( new CursesTextPosition( Offset ) ).Column;
		EnsureCaret();
	}

	internal bool Insert( string value ) {
		ArgumentNullException.ThrowIfNull( value );
		if ( value.Length == 0 ) {
			return false;
		}
		ReadOnlySpan<char> remaining = value.AsSpan();
		while ( !remaining.IsEmpty ) {
			if ( Rune.DecodeFromUtf16( remaining, out Rune rune, out int consumed ) != OperationStatus.Done
				|| Rune.IsControl( rune ) && rune.Value is not ( '\t' or '\n' ) ) {
				return false;
			}
			remaining = remaining[ consumed.. ];
		}
		string before = GetRecord( Row );
		int start = selectionAnchor is int anchor ? Math.Min( anchor, Offset ) : Offset;
		int end = selectionAnchor is int selected ? Math.Max( selected, Offset ) : Offset;
		if ( before.Length - ( end - start ) + value.Length > MaximumRecordLength ) {
			return false;
		}
		CursesTextLayout layout = LayoutRecord( Row );
		_ = layout.GetVisualPosition( new CursesTextPosition( Offset ) );
		string next = before.Remove( start, end - start ).Insert( start, value );
		CursesTextLayout nextLayout;
		try {
			nextLayout = CursesTextLayout.Create( next, layout.Options );
			if ( nextLayout.IsTruncated || CursesTextLayout.Create( next,
				new CursesTextLayoutOptions( 30 ) {
					WrapMode = CursesTextWrapMode.TextElement,
					MaximumRows = RecordVisualRows
				} ).IsTruncated ) {
				return false;
			}
			_ = nextLayout.GetVisualPosition( new CursesTextPosition( start + value.Length ) );
		} catch ( ArgumentException ) {
			return false;
		}
		edits[ Row ] = next;
		Offset = start + value.Length;
		preferredColumn = nextLayout.GetVisualPosition( new CursesTextPosition( Offset ) ).Column;
		selectionAnchor = null;
		EnsureCaret();
		return true;
	}

	internal bool Delete( bool backwards ) {
		string before = GetRecord( Row );
		CursesTextLayout layout = LayoutRecord( Row );
		if ( selectionAnchor is int anchor && anchor != Offset ) {
			int first = Math.Min( anchor, Offset );
			edits[ Row ] = before.Remove( first, Math.Abs( anchor - Offset ) );
			Offset = first;
			selectionAnchor = null;
			preferredColumn = LayoutRecord( Row ).GetVisualPosition( new CursesTextPosition( Offset ) ).Column;
			EnsureCaret();
			return true;
		}
		int other = backwards
			? layout.GetPreviousPosition( new CursesTextPosition( Offset ) ).Offset
			: layout.GetNextPosition( new CursesTextPosition( Offset ) ).Offset;
		if ( other == Offset ) {
			return false;
		}
		int start = Math.Min( other, Offset );
		edits[ Row ] = before.Remove( start, Math.Abs( other - Offset ) );
		Offset = start;
		selectionAnchor = null;
		preferredColumn = LayoutRecord( Row ).GetVisualPosition( new CursesTextPosition( Offset ) ).Column;
		EnsureCaret();
		return true;
	}

	private void EnsureCaret() {
		CursesTextLayout layout = LayoutRecord( Row );
		CursesTextVisualPosition visual = layout.GetVisualPosition( new CursesTextPosition( Offset ) );
		Viewport = Viewport.EnsureVisible( new CursesCellPosition(
			Row * RecordVisualRows + visual.Line,
			Wrap ? 0 : Math.Min( ContentColumns - 1, visual.Column ) ) );
	}
}

internal readonly record struct EditorRegions(
	CursesRectangle Document, CursesRectangle Status, CursesRectangle Prompt );

internal static class EditorSampleLayout {
	internal static bool TryArrange( CursesRectangle bounds, out EditorRegions regions ) {
		if ( bounds.Rows < 6 || bounds.Columns < 30 ) {
			regions = default;
			return false;
		}
		CursesRectangle[] rows = CursesLayout.ArrangeRows( bounds,
			[ CursesTrack.Weighted( minimum: 1 ), CursesTrack.Fixed( 1 ), CursesTrack.Fixed( 1 ) ] );
		regions = new EditorRegions( rows[ 0 ], rows[ 1 ], rows[ 2 ] );
		return true;
	}
}
