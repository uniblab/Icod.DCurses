using Icod.DCurses;
using Icod.DCurses.Editor.Sample;

await using CursesSession session = await CursesSession.OpenAsync(
	EditorSampleInteraction.CreateSessionOptions()
);
CursesScreen screen = session.Screen;
using EditorSampleInteraction interaction = new( screen );
CursesWindow document = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow status = screen.CreateWindow( 0, 0, 1, 1 );
CursesWindow prompt = screen.CreateWindow( 0, 0, 1, 1 );
foreach ( CursesWindow window in new[] { document, status, prompt } ) {
	window.WrapMode = CursesWrapMode.Clip;
}
EditorSampleState state = new( 1, 1 );
bool running = true;
string digits = string.Empty;

while ( running ) {
	if ( EditorSampleLayout.TryArrange( screen.Bounds, out EditorRegions regions ) ) {
		document.SetBounds( regions.Document );
		status.SetBounds( regions.Status );
		prompt.SetBounds( regions.Prompt );
		interaction.SetBounds( regions.Document, regions.Prompt );
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
		string shortcutSummary = interaction.HasPendingCommandSequence
			? "Prefix Ctrl+K ..."
			: interaction.GetShortcutSummary();
		WriteLine( prompt, interaction.IsPromptActive
			? $"Go to record (1-based): {digits} | {shortcutSummary}"
			: $"Type to edit | {shortcutSummary}" );
	} else {
		interaction.SetBounds( default, default );
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
	CursesCommandSequenceResult sequenceResult = interaction.Process( input );
	if ( CursesCommandSequenceResultKind.Pending == sequenceResult.Kind ) {
		continue;
	}
	CursesCommand? command = sequenceResult.Command
		?? sequenceResult.Fallback?.Command;
	if ( command is not null ) {
		switch ( command.Name ) {
			case EditorSampleInteraction.MoveLeftCommandName: state.MoveHorizontal( -1 ); break;
			case EditorSampleInteraction.MoveRightCommandName: state.MoveHorizontal( 1 ); break;
			case EditorSampleInteraction.MoveUpCommandName: state.MoveVertical( -1 ); break;
			case EditorSampleInteraction.MoveDownCommandName: state.MoveVertical( 1 ); break;
			case EditorSampleInteraction.PageUpCommandName: state.MovePage( -1 ); break;
			case EditorSampleInteraction.PageDownCommandName: state.MovePage( 1 ); break;
			case EditorSampleInteraction.LineStartCommandName: state.MoveLineBoundary( false ); break;
			case EditorSampleInteraction.LineEndCommandName: state.MoveLineBoundary( true ); break;
			case EditorSampleInteraction.DeleteBackCommandName: _ = state.Delete( true ); break;
			case EditorSampleInteraction.DeleteForwardCommandName: _ = state.Delete( false ); break;
			case EditorSampleInteraction.InsertNewLineCommandName: _ = state.Insert( "\n" ); break;
			case EditorSampleInteraction.InsertTabCommandName: _ = state.Insert( "\t" ); break;
			case EditorSampleInteraction.ToggleWrapCommandName: state.ToggleWrap(); break;
			case EditorSampleInteraction.ToggleSelectionCommandName: state.ToggleSelection(); break;
			case EditorSampleInteraction.OpenPromptCommandName:
				digits = string.Empty;
				interaction.OpenPrompt();
				break;
			case EditorSampleInteraction.CancelPromptCommandName:
				digits = string.Empty;
				interaction.ClosePrompt();
				break;
			case EditorSampleInteraction.AcceptPromptCommandName:
				if ( int.TryParse( digits, out int requested ) ) {
					state.GoToRow( requested - 1 );
				}
				digits = string.Empty;
				interaction.ClosePrompt();
				break;
			case EditorSampleInteraction.PromptBackspaceCommandName:
				if ( digits.Length > 0 ) {
					digits = digits[ ..^1 ];
				}
				break;
			case EditorSampleInteraction.QuitCommandName: running = false; break;
		}
		continue;
	}

	CursesInputEvent? routedInput = sequenceResult.Fallback?.Input;
	if ( routedInput?.Kind == CursesInputEventKind.Text
		&& routedInput.Character.HasValue ) {
		if ( interaction.IsPromptActive ) {
			int character = routedInput.Character.Value.Value;
			if ( character is >= '0' and <= '9' && digits.Length < 8 ) {
				digits += routedInput.Character.Value.ToString();
			}
		} else {
			_ = state.Insert( routedInput.Character.Value.ToString() );
		}
	}
}

session.StandardScreen.Clear();
await session.RefreshAsync();
return 0;

static void WriteLine( CursesWindow window, string value ) {
	if ( window.Rows > 0 && window.Columns > 0 ) {
		window.Move( 0, 0 );
		window.Write( value[ ..Math.Min( value.Length, window.Columns ) ] );
	}
}
