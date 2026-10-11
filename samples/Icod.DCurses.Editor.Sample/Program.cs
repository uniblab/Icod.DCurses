/*
	Icod.DCurses.Editor.Sample
	Interactive editor-foundation acceptance sample for Icod.DCurses.
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

using Icod.DCurses;
using Icod.DCurses.Editor.Sample;

await using CursesSession session = await CursesSession.OpenAsync(
	EditorSampleInteraction.CreateSessionOptions()
);
await using CursesCursorStyleLease? caretStyle = await TryAcquireCaretStyleAsync( session );
CursesScreen screen = session.Screen;
using EditorSampleInteraction interaction = new( screen );
EditorSampleClipboard clipboard = new();
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
		_ = state.ProjectVisible( document );
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
			case EditorSampleInteraction.CopyCommandName:
				_ = await clipboard.CopyAsync( session, state );
				break;
			case EditorSampleInteraction.PasteCommandName:
				_ = await clipboard.PasteAsync( session, state );
				break;
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

	CursesInteractionResult? routed = sequenceResult.Fallback;
	CursesInputEvent? routedInput = routed?.Input;
	if ( routedInput?.Kind == CursesInputEventKind.Mouse
		&& routed is not null
		&& routed.PointerGesture is CursesPointerGesture gesture
		&& gesture.Button == CursesMouseButton.Primary
		&& gesture.Target is CursesPointerTarget target ) {
		if ( interaction.IsDocumentTarget( routed ) ) {
			if ( gesture.Kind == CursesPointerGestureKind.Press ) {
				state.SetCaretFromPointer(
					target.LocalRow,
					target.LocalColumn,
					extendSelection: false
				);
			} else if ( gesture.Kind is CursesPointerGestureKind.DragStart
				or CursesPointerGestureKind.DragMove
				or CursesPointerGestureKind.DragEnd ) {
				state.SetCaretFromPointer(
					target.LocalRow,
					target.LocalColumn,
					extendSelection: true
				);
				if ( gesture.Kind == CursesPointerGestureKind.DragEnd ) {
					state.EndPointerSelection();
				}
			}
		} else if ( interaction.IsScrollbarTarget( routed )
			&& gesture.Kind is CursesPointerGestureKind.Press
				or CursesPointerGestureKind.DragStart
				or CursesPointerGestureKind.DragMove
				or CursesPointerGestureKind.DragEnd ) {
			state.ScrollbarTo( target.LocalRow );
		}
		continue;
	}
	if ( routedInput?.Kind == CursesInputEventKind.Paste
		&& routedInput.Paste is CursesPasteEvent {
			Phase: CursesPastePhase.Data,
			Text: string pasteText
		}
		&& !interaction.IsPromptActive ) {
		_ = state.Insert( pasteText );
		continue;
	}
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

static async ValueTask<CursesCursorStyleLease?> TryAcquireCaretStyleAsync(
	CursesSession session
) {
	try {
		return await session.AcquireCursorStyleAsync(
			CursesCursorStyle.SteadyBar,
			TimeSpan.FromMilliseconds( 250 )
		);
	} catch ( Exception exception ) when ( exception is TimeoutException
		or NotSupportedException
		or InvalidOperationException
		or FormatException
		or IOException ) {
		return null;
	}
}

static void WriteLine( CursesWindow window, string value ) {
	if ( window.Rows > 0 && window.Columns > 0 ) {
		window.Move( 0, 0 );
		window.Write( value[ ..Math.Min( value.Length, window.Columns ) ] );
	}
}
