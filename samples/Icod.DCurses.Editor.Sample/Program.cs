using Icod.DCurses;
using Icod.DCurses.Editor.Sample;

await using CursesSession session = await CursesSession.OpenAsync();
CursesScreen screen = session.Screen;
CursesWindow document = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow prompt = screen.CreateWindow( 0, 0, 1, 1 );
foreach ( CursesWindow window in new[] { document, status, prompt } ) {
	window.WrapMode = CursesWrapMode.Clip;
}
EditorSampleState state = new( 1, 1 );
bool running = true;
bool goToPrompt = false;
string digits = string.Empty;

while ( running ) {
	if ( EditorSampleLayout.TryArrange( screen.Bounds, out EditorRegions regions ) ) {
		document.SetBounds( regions.Document );
		status.SetBounds( regions.Status );
		prompt.SetBounds( regions.Prompt );
		state.Resize( document.Rows, document.Columns );
		document.Clear();
		(int first, int count) = state.VisibleRecords();
		for ( int index = 0; index < count; index++ ) {
			int row = first + index;
			CursesTextLayout layout = state.LayoutRecord( row );
			CursesRectangle[] selection = state.SelectionRectangles( row, layout );
			if ( selection.Length > 0 && state.SelectionAnchor is int anchor ) {
				layout = CursesTextLayout.Create( layout.Text, layout.Options,
					[ new CursesTextSpan( new CursesTextPosition( Math.Min( anchor, state.Offset ) ),
						Math.Abs( anchor - state.Offset ),
						CursesStyle.Default.WithAttributes( CursesTextAttributes.Reverse ) ) ] );
			} else if ( row == state.Row ) {
				int next = layout.GetNextPosition( new CursesTextPosition( state.Offset ) ).Offset;
				if ( next > state.Offset ) {
					// Styling the complete source element keeps wide-glyph continuation cells intact.
					layout = CursesTextLayout.Create( layout.Text, layout.Options,
						[ new CursesTextSpan( new CursesTextPosition( state.Offset ),
							next - state.Offset,
							CursesStyle.Default.WithAttributes( CursesTextAttributes.Reverse ) ) ] );
				}
			}
			int visualRow = row * EditorSampleState.RecordVisualRows - state.Viewport.OriginRow;
			document.PresentTextLayout( layout, 0, layout.Lines.Count, visualRow,
				state.Wrap ? 0 : -state.Viewport.OriginColumn );
		}
		if ( state.Offset == state.GetRecord( state.Row ).Length
			&& state.TryCaret( out CursesCellPosition caret )
			&& screen.VirtualScreen.GetCell( regions.Document.Row + caret.Row,
				regions.Document.Column + caret.Column ).IsBlank ) {
			document.Move( caret.Row, caret.Column );
			document.Write( "|" );
		}
		status.Clear();
		WriteLine( status, $"Record {state.Row + 1}/{EditorSampleState.DocumentRows}  offset {state.Offset}  "
			+ $"{( state.Wrap ? "wrap" : "no-wrap" )}  edits {state.EditedRecordCount}  "
			+ $"view {state.Viewport.OriginRow},{state.Viewport.OriginColumn}" );
		prompt.Clear();
		WriteLine( prompt, goToPrompt ? $"Go to record (1-based): {digits}" :
			"Type to edit | Ctrl+W wrap | Ctrl+V select | Ctrl+G go to | Esc quit" );
	} else {
		session.StandardScreen.Clear();
		WriteLine( session.StandardScreen, "Resize to at least 30 columns x 6 rows; Esc quits." );
	}
	await session.RefreshAsync();
	CursesEvent current = await session.ReadEventAsync();
	if ( current.Kind == CursesEventKind.Lifecycle && current.Lifecycle is not null
		&& current.Lifecycle.Kind is CursesLifecycleEventKind.Interrupt
			or CursesLifecycleEventKind.Termination ) {
		break;
	}
	if ( current.RequiresRepaint ) {
		_ = session.SynchronizeDimensions();
		session.Invalidate();
	}
	if ( current.Kind != CursesEventKind.Input || current.Input is null ) {
		continue;
	}
	CursesInputEvent input = current.Input;
	if ( input.Kind == CursesInputEventKind.EndOfInput ) {
		break;
	}
	if ( goToPrompt ) {
		if ( input.Kind == CursesInputEventKind.Key ) {
			switch ( input.Key ) {
				case CursesKey.Escape: goToPrompt = false; break;
				case CursesKey.Backspace: if ( digits.Length > 0 ) digits = digits[ ..^1 ]; break;
				case CursesKey.Enter:
					if ( int.TryParse( digits, out int requested ) ) {
						state.GoToRow( requested - 1 );
					}
					goToPrompt = false;
					break;
			}
		} else if ( input.Kind == CursesInputEventKind.Text && input.Character.HasValue
			&& input.Character.Value.Value is >= '0' and <= '9' && digits.Length < 8 ) {
			digits += input.Character.Value.ToString();
		}
		continue;
	}
	if ( input.Kind == CursesInputEventKind.Key ) {
		switch ( input.Key ) {
			case CursesKey.Escape: running = false; break;
			case CursesKey.Left: state.MoveHorizontal( -1 ); break;
			case CursesKey.Right: state.MoveHorizontal( 1 ); break;
			case CursesKey.Up: state.MoveVertical( -1 ); break;
			case CursesKey.Down: state.MoveVertical( 1 ); break;
			case CursesKey.PageUp: state.MovePage( -1 ); break;
			case CursesKey.PageDown: state.MovePage( 1 ); break;
			case CursesKey.Home: state.MoveLineBoundary( false ); break;
			case CursesKey.End: state.MoveLineBoundary( true ); break;
			case CursesKey.Backspace: state.Delete( true ); break;
			case CursesKey.Delete: state.Delete( false ); break;
			case CursesKey.Enter: state.Insert( "\n" ); break;
			case CursesKey.Tab: state.Insert( "\t" ); break;
		}
	} else if ( input.Kind == CursesInputEventKind.Text && input.Character.HasValue ) {
		int character = input.Character.Value.Value;
		if ( 0 != ( input.Modifiers & CursesKeyModifiers.Control ) ) {
			switch ( character ) {
				case 'w': case 'W': case 23: state.ToggleWrap(); break;
				case 'v': case 'V': case 22: state.ToggleSelection(); break;
				case 'g': case 'G': case 7: goToPrompt = true; digits = string.Empty; break;
			}
		} else {
			state.Insert( input.Character.Value.ToString() );
		}
	}
}

return 0;

static void WriteLine( CursesWindow window, string value ) {
	if ( window.Rows > 0 && window.Columns > 0 ) {
		window.Move( 0, 0 );
		window.Write( value[ ..Math.Min( value.Length, window.Columns ) ] );
	}
}
