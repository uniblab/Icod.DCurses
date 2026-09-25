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
	internal const string OpenPromptCommandName = "editor.prompt.open";
	internal const string CancelPromptCommandName = "editor.prompt.cancel";
	internal const string AcceptPromptCommandName = "editor.prompt.accept";
	internal const string PromptBackspaceCommandName = "editor.prompt.backspace";
	internal const string QuitCommandName = "editor.quit";

	private readonly CursesInteractionRouter router;
	private readonly CursesInteractionRegion documentRegion;
	private readonly CursesInteractionScope promptScope;
	private readonly CursesInteractionRegion promptRegion;
	private CursesInteractionScopeLease? promptScopeLease;
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

	internal void SetBounds(
		CursesRectangle documentBounds,
		CursesRectangle promptBounds
	) {
		ThrowIfDisposed();
		documentRegion.SetBounds( documentBounds );
		promptRegion.SetBounds( promptBounds );
		_ = router.Focus( IsPromptActive ? promptRegion : documentRegion );
	}

	internal CursesCommandSequenceResult Process(
		CursesInputEvent input
	) {
		ThrowIfDisposed();
		return router.ProcessCommandSequence( input );
	}

	internal void OpenPrompt() {
		ThrowIfDisposed();
		if ( promptScopeLease is not null ) {
			return;
		}

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
		router.Dispose();
		disposed = true;
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
