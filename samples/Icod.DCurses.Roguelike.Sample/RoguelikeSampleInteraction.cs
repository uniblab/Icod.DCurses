namespace Icod.DCurses.Roguelike.Sample;

using System.Text;
using Icod.DCurses;

internal sealed class RoguelikeSampleInteraction : IDisposable {
	internal const string MoveUpCommandName = "roguelike.move.up";
	internal const string MoveDownCommandName = "roguelike.move.down";
	internal const string MoveLeftCommandName = "roguelike.move.left";
	internal const string MoveRightCommandName = "roguelike.move.right";
	internal const string OpenHelpCommandName = "roguelike.help.open";
	internal const string CloseHelpCommandName = "roguelike.help.close";
	internal const string QuitCommandName = "roguelike.quit";

	private readonly CursesPanel helpPanel;
	private readonly CursesInteractionRouter router;
	private readonly CursesInteractionRegion mapRegion;
	private readonly CursesInteractionScope helpScope;
	private readonly CursesInteractionRegion helpRegion;
	private CursesInteractionScopeLease? helpScopeLease;
	private bool disposed;

	internal RoguelikeSampleInteraction(
		CursesScreen screen,
		CursesPanel helpPanel
	) {
		ArgumentNullException.ThrowIfNull( screen );
		ArgumentNullException.ThrowIfNull( helpPanel );
		this.helpPanel = helpPanel;
		helpPanel.Hide();
		router = new CursesInteractionRouter( screen );
		helpScope = router.RegisterScope();
		mapRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions( default ) {
				IsFocusable = true
			}
		);
		helpRegion = router.RegisterRegion(
			new CursesInteractionRegionOptions( default ) {
				Panel = helpPanel,
				Scope = helpScope,
				IsFocusable = true
			}
		);

		BindMapCommands();
		BindHelpCommands();
		BindGlobalCommands();
	}

	internal bool IsHelpActive => helpScopeLease is not null;

	internal bool HasPendingCommandSequence => router.HasPendingCommandSequence;

	internal void SetBounds(
		CursesRectangle mapBounds,
		CursesRectangle helpBounds
	) {
		ThrowIfDisposed();
		mapRegion.SetBounds( mapBounds );
		helpRegion.SetBounds( helpBounds );
		_ = router.Focus( IsHelpActive ? helpRegion : mapRegion );
	}

	internal CursesCommandSequenceResult Process(
		CursesInputEvent input
	) {
		ThrowIfDisposed();
		return router.ProcessCommandSequence( input );
	}

	internal void OpenHelp() {
		ThrowIfDisposed();
		if ( helpScopeLease is not null ) {
			return;
		}

		helpPanel.Show();
		helpScopeLease = router.ActivateScope( helpScope );
		_ = router.Focus( helpRegion );
	}

	internal void CloseHelp() {
		ThrowIfDisposed();
		if ( helpScopeLease is null ) {
			return;
		}

		helpScopeLease.Dispose();
		helpScopeLease = null;
		helpPanel.Hide();
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
		if ( singles.Contains( OpenHelpCommandName ) ) {
			labels.Add( "? help" );
		}
		if ( sequences.Contains( OpenHelpCommandName ) ) {
			labels.Add( "g h help" );
		}
		if ( singles.Contains( CloseHelpCommandName ) ) {
			labels.Add( "? close" );
		}
		if ( sequences.Contains( CloseHelpCommandName ) ) {
			labels.Add( "g m map" );
		}
		if ( singles.Contains( QuitCommandName ) ) {
			labels.Add( "Q/Esc quit" );
		}
		return string.Join( " | ", labels );
	}

	public void Dispose() {
		if ( disposed ) {
			return;
		}

		helpScopeLease?.Dispose();
		helpScopeLease = null;
		router.Dispose();
		disposed = true;
	}

	private void BindMapCommands() {
		BindMapKey( CursesKey.Up, MoveUpCommandName );
		BindMapKey( CursesKey.Down, MoveDownCommandName );
		BindMapKey( CursesKey.Left, MoveLeftCommandName );
		BindMapKey( CursesKey.Right, MoveRightCommandName );
		BindMapCharacter( 'w', MoveUpCommandName );
		BindMapCharacter( 'W', MoveUpCommandName );
		BindMapCharacter( 's', MoveDownCommandName );
		BindMapCharacter( 'S', MoveDownCommandName );
		BindMapCharacter( 'a', MoveLeftCommandName );
		BindMapCharacter( 'A', MoveLeftCommandName );
		BindMapCharacter( 'd', MoveRightCommandName );
		BindMapCharacter( 'D', MoveRightCommandName );
		BindMapCharacter( '?', OpenHelpCommandName );
		mapRegion.BindGestureSequence(
			[ CharacterGesture( 'g' ), CharacterGesture( 'h' ) ],
			new CursesCommand( OpenHelpCommandName )
		);
		mapRegion.BindGestureSequence(
			[ CharacterGesture( 'G' ), CharacterGesture( 'H' ) ],
			new CursesCommand( OpenHelpCommandName )
		);
	}

	private void BindHelpCommands() {
		helpRegion.BindGesture(
			CharacterGesture( '?' ),
			new CursesCommand( CloseHelpCommandName )
		);
		helpRegion.BindGestureSequence(
			[ CharacterGesture( 'g' ), CharacterGesture( 'm' ) ],
			new CursesCommand( CloseHelpCommandName )
		);
		helpRegion.BindGestureSequence(
			[ CharacterGesture( 'G' ), CharacterGesture( 'M' ) ],
			new CursesCommand( CloseHelpCommandName )
		);
	}

	private void BindGlobalCommands() {
		router.BindGlobalGesture(
			CursesKeyGesture.ForKey( CursesKey.Escape ),
			new CursesCommand( QuitCommandName )
		);
		router.BindGlobalGesture(
			CharacterGesture( 'q' ),
			new CursesCommand( QuitCommandName )
		);
		router.BindGlobalGesture(
			CharacterGesture( 'Q' ),
			new CursesCommand( QuitCommandName )
		);
	}

	private void BindMapKey(
		CursesKey key,
		string commandName
	) {
		mapRegion.BindGesture(
			CursesKeyGesture.ForKey( key ),
			new CursesCommand( commandName )
		);
	}

	private void BindMapCharacter(
		char character,
		string commandName
	) {
		mapRegion.BindGesture(
			CharacterGesture( character ),
			new CursesCommand( commandName )
		);
	}

	private static CursesKeyGesture CharacterGesture(
		char character
	) {
		return CursesKeyGesture.ForCharacter( new Rune( character ) );
	}

	private void ThrowIfDisposed() {
		if ( disposed ) {
			throw new ObjectDisposedException( nameof( RoguelikeSampleInteraction ) );
		}
	}
}
