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

namespace Icod.DCurses.Editor.Sample;

using System.Text;
using Icod.DCurses;

internal sealed class EditorSampleInteraction : IDisposable {
	internal const string MoveLeftCommandName = "editor.move.left";
	internal const string MoveRightCommandName = "editor.move.right";
	internal const string MoveUpCommandName = "editor.move.up";
	internal const string MoveDownCommandName = "editor.move.down";
	internal const string PageUpCommandName = "editor.page.up";
	internal const string PageDownCommandName = "editor.page.down";
	internal const string LineStartCommandName = "editor.line.start";
	internal const string LineEndCommandName = "editor.line.end";
	internal const string DeleteBackCommandName = "editor.delete.back";
	internal const string DeleteForwardCommandName = "editor.delete.forward";
	internal const string InsertNewLineCommandName = "editor.insert.new-line";
	internal const string InsertTabCommandName = "editor.insert.tab";
	internal const string ToggleWrapCommandName = "editor.wrap.toggle";
	internal const string ToggleSelectionCommandName = "editor.selection.toggle";
	internal const string CopyCommandName = "editor.clipboard.copy";
	internal const string PasteCommandName = "editor.clipboard.paste";
	internal const string OpenPromptCommandName = "editor.prompt.open";
	internal const string CancelPromptCommandName = "editor.prompt.cancel";
	internal const string AcceptPromptCommandName = "editor.prompt.accept";
	internal const string PromptBackspaceCommandName = "editor.prompt.backspace";
	internal const string QuitCommandName = "editor.quit";

	private readonly CursesInteractionRouter router;
	private readonly CursesInteractionRegion documentRegion;
	private readonly CursesInteractionRegion scrollbarRegion;
	private readonly CursesInteractionScope promptScope;
	private readonly CursesInteractionRegion promptRegion;
	private CursesInteractionScopeLease? promptScopeLease;
	private CursesPointerCaptureLease? pointerCaptureLease;
	private bool disposed;

	internal EditorSampleInteraction(
		CursesScreen screen
	) {
		ArgumentNullException.ThrowIfNull( screen );
		router = new CursesInteractionRouter( screen );
		promptScope = router.RegisterScope();
		documentRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions( default ) {
				IsFocusable = true
			}
		);
		scrollbarRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions( default ) {
				PointerShape = CursesPointerShape.Default
			}
		);
		promptRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions( default ) {
				Scope = promptScope,
				IsEnabled = false,
				IsFocusable = true
			}
		);

		BindDocumentCommands();
		BindPromptCommands();
	}

	internal bool IsPromptActive => promptScopeLease is not null;

	internal bool HasPendingCommandSequence => router.HasPendingCommandSequence;

	internal static CursesSessionOptions CreateSessionOptions() {
		return new CursesSessionOptions {
			InputMode = CursesInputMode.Raw,
			HideCursor = false
		};
	}

	internal void SetBounds(
		CursesRectangle documentBounds,
		CursesRectangle promptBounds
	) {
		ThrowIfDisposed();
		CursesRectangle editorBounds = documentBounds.Columns > 0
			? new CursesRectangle(
				documentBounds.Row,
				documentBounds.Column,
				documentBounds.Rows,
				Math.Max( 0, documentBounds.Columns - 1 )
			)
			: documentBounds;
		CursesRectangle scrollbarBounds = documentBounds.Columns > 0
			? new CursesRectangle(
				documentBounds.Row,
				documentBounds.RightExclusive - 1,
				documentBounds.Rows,
				1
			)
			: documentBounds;
		if ( documentRegion.Bounds != editorBounds
			|| scrollbarRegion.Bounds != scrollbarBounds
			|| promptRegion.Bounds != promptBounds ) {
			CancelPointerCapture();
		}
		documentRegion.SetBounds( editorBounds );
		scrollbarRegion.SetBounds( scrollbarBounds );
		promptRegion.SetBounds( promptBounds );
		_ = router.Focus( IsPromptActive ? promptRegion : documentRegion );
	}

	internal CursesCommandSequenceResult Process(
		CursesInputEvent input
	) {
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull( input );
		if ( input.Kind == CursesInputEventKind.Focus
			&& input.Focus?.State == CursesFocusState.Unfocused ) {
			CancelPointerCapture();
		}

		CursesCommandSequenceResult result = router.ProcessCommandSequence( input );
		if ( input.Kind == CursesInputEventKind.Mouse
			&& input.Mouse is CursesMouseEvent mouse ) {
			CursesInteractionResult? routed = result.Fallback;
			if ( mouse.Action == CursesMouseAction.Press
				&& mouse.Button == CursesMouseButton.Primary
				&& routed?.Kind == CursesInteractionResultKind.Targeted
				&& ( ReferenceEquals( routed.Region, documentRegion )
					|| ReferenceEquals( routed.Region, scrollbarRegion ) ) ) {
				CancelPointerCapture();
				pointerCaptureLease = router.CapturePointer(
					routed.Region,
					CursesMouseButton.Primary
				);
			} else if ( mouse.Action == CursesMouseAction.Release
				&& mouse.Button == CursesMouseButton.Primary ) {
				CancelPointerCapture();
			}
		}
		return result;
	}

	internal bool IsDocumentTarget( CursesInteractionResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		return ReferenceEquals( result.Region, documentRegion );
	}

	internal bool IsScrollbarTarget( CursesInteractionResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		return ReferenceEquals( result.Region, scrollbarRegion );
	}

	internal void OpenPrompt() {
		ThrowIfDisposed();
		if ( promptScopeLease is not null ) {
			return;
		}

		CancelPointerCapture();
		promptRegion.IsEnabled = true;
		promptScopeLease = router.ActivateScope( promptScope );
		_ = router.Focus( promptRegion );
	}

	internal void ClosePrompt() {
		ThrowIfDisposed();
		if ( promptScopeLease is null ) {
			return;
		}

		promptScopeLease.Dispose();
		promptScopeLease = null;
		promptRegion.IsEnabled = false;
	}

	internal string GetShortcutSummary() {
		ThrowIfDisposed();
		HashSet<string> singles = router.GetEffectiveGestureBindings()
			.Select( static binding => binding.Command.Name )
			.ToHashSet( StringComparer.Ordinal );
		HashSet<string> sequences = router.GetEffectiveGestureSequenceBindings()
			.Select( static binding => binding.Command.Name )
			.ToHashSet( StringComparer.Ordinal );
		List<string> labels = [];
		if ( singles.Contains( ToggleWrapCommandName ) ) {
			labels.Add( "Ctrl+W wrap" );
		}
		if ( singles.Contains( ToggleSelectionCommandName ) ) {
			labels.Add( "Ctrl+T select" );
		}
		if ( singles.Contains( OpenPromptCommandName ) ) {
			labels.Add( "Ctrl+G go" );
		}
		if ( singles.Contains( CopyCommandName ) ) {
			labels.Add( "Ctrl+C copy" );
		}
		if ( singles.Contains( PasteCommandName ) ) {
			labels.Add( "Ctrl+V paste" );
		}
		if ( sequences.Contains( OpenPromptCommandName ) ) {
			labels.Add( "Ctrl+K Ctrl+G go" );
		}
		if ( singles.Contains( AcceptPromptCommandName ) ) {
			labels.Add( "Enter go" );
		}
		if ( singles.Contains( CancelPromptCommandName ) ) {
			labels.Add( "Esc cancel" );
		}
		if ( sequences.Contains( CancelPromptCommandName ) ) {
			labels.Add( "Ctrl+K Ctrl+C cancel" );
		}
		return string.Join( " | ", labels );
	}

	public void Dispose() {
		if ( disposed ) {
			return;
		}

		promptScopeLease?.Dispose();
		promptScopeLease = null;
		CancelPointerCapture();
		router.Dispose();
		disposed = true;
	}

	private void CancelPointerCapture() {
		pointerCaptureLease?.Dispose();
		pointerCaptureLease = null;
	}

	private void BindDocumentCommands() {
		BindDocumentKey( CursesKey.Left, MoveLeftCommandName );
		BindDocumentKey( CursesKey.Right, MoveRightCommandName );
		BindDocumentKey( CursesKey.Up, MoveUpCommandName );
		BindDocumentKey( CursesKey.Down, MoveDownCommandName );
		BindDocumentKey( CursesKey.PageUp, PageUpCommandName );
		BindDocumentKey( CursesKey.PageDown, PageDownCommandName );
		BindDocumentKey( CursesKey.Home, LineStartCommandName );
		BindDocumentKey( CursesKey.End, LineEndCommandName );
		BindDocumentKey( CursesKey.Backspace, DeleteBackCommandName );
		BindDocumentKey( CursesKey.Delete, DeleteForwardCommandName );
		BindDocumentKey( CursesKey.Enter, InsertNewLineCommandName );
		BindDocumentKey( CursesKey.Tab, InsertTabCommandName );
		BindDocumentKey( CursesKey.Escape, QuitCommandName );
		BindControlGesture( documentRegion, 'w', ToggleWrapCommandName );
		BindControlGesture( documentRegion, 't', ToggleSelectionCommandName );
		BindControlGesture( documentRegion, 'g', OpenPromptCommandName );
		BindControlGesture( documentRegion, 'c', CopyCommandName );
		BindControlGesture( documentRegion, 'v', PasteCommandName );
		BindControlGestureSequence(
			documentRegion,
			'k',
			'g',
			OpenPromptCommandName
		);
	}

	private void BindPromptCommands() {
		promptRegion.BindGesture( CursesKeyGesture.ForKey( CursesKey.Escape ),
			new CursesCommand( CancelPromptCommandName ) );
		promptRegion.BindGesture( CursesKeyGesture.ForKey( CursesKey.Enter ),
			new CursesCommand( AcceptPromptCommandName ) );
		promptRegion.BindGesture( CursesKeyGesture.ForKey( CursesKey.Backspace ),
			new CursesCommand( PromptBackspaceCommandName ) );
		BindControlGestureSequence(
			promptRegion,
			'k',
			'c',
			CancelPromptCommandName
		);
	}

	private void BindDocumentKey(
		CursesKey key,
		string commandName
	) {
		documentRegion.BindGesture(
			CursesKeyGesture.ForKey( key ),
			new CursesCommand( commandName )
		);
	}

	private static CursesKeyGesture ControlGesture(
		char character
	) {
		return CursesKeyGesture.ForCharacter(
			new Rune( character ),
			CursesKeyModifiers.Control
		);
	}

	private static void BindControlGesture(
		CursesInteractionRegion region,
		char character,
		string commandName
	) {
		CursesCommand command = new( commandName );
		region.BindGesture( ControlGesture( character ), command );
		region.BindGesture(
			ControlGesture( char.ToUpperInvariant( character ) ),
			command
		);
	}

	private static void BindControlGestureSequence(
		CursesInteractionRegion region,
		char prefix,
		char completion,
		string commandName
	) {
		CursesCommand command = new( commandName );
		region.BindGestureSequence(
			[ ControlGesture( prefix ), ControlGesture( completion ) ],
			command
		);
		region.BindGestureSequence(
			[
				ControlGesture( char.ToUpperInvariant( prefix ) ),
				ControlGesture( char.ToUpperInvariant( completion ) )
			],
			command
		);
	}

	private void ThrowIfDisposed() {
		if ( disposed ) {
			throw new ObjectDisposedException( nameof( EditorSampleInteraction ) );
		}
	}
}
