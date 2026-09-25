using System.Text;
using Icod.DCurses.Editor.Sample;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class EditorSampleInteractionTests {
	[Fact]
	public void DocumentContextRoutesDirectAndSequencePromptCommands() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		Assert.Contains( "Ctrl+G go", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.Contains( "Ctrl+K Ctrl+G go", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.Contains( "Ctrl+T select", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.DoesNotContain( "Ctrl+V", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );

		CursesCommandSequenceResult direct = interaction.Process( Control( 'g' ) );
		Assert.Equal( CursesCommandSequenceResultKind.Fallback, direct.Kind );
		Assert.Equal( EditorSampleInteraction.OpenPromptCommandName,
			direct.Fallback?.Command?.Name );

		CursesCommandSequenceResult pending = interaction.Process( Control( 'k' ) );
		Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
		Assert.True( interaction.HasPendingCommandSequence );
		CursesCommandSequenceResult completed = interaction.Process( Control( 'g' ) );
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( EditorSampleInteraction.OpenPromptCommandName,
			completed.Command?.Name );
	}

	[Theory]
	[InlineData( 'G', EditorSampleInteraction.OpenPromptCommandName )]
	[InlineData( 'T', EditorSampleInteraction.ToggleSelectionCommandName )]
	[InlineData( 'W', EditorSampleInteraction.ToggleWrapCommandName )]
	public void TraditionalControlKeysRouteDocumentCommands(
		char character,
		string commandName
	) {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesCommandSequenceResult result = interaction.Process(
			Control( character )
		);

		Assert.Equal( CursesCommandSequenceResultKind.Fallback, result.Kind );
		Assert.Equal( commandName, result.Fallback?.Command?.Name );
	}

	[Fact]
	public void TraditionalControlKThenGRoutesPromptCommand() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesCommandSequenceResult pending = interaction.Process( Control( 'K' ) );
		CursesCommandSequenceResult completed = interaction.Process( Control( 'G' ) );

		Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( EditorSampleInteraction.OpenPromptCommandName,
			completed.Command?.Name );
	}

	[Fact]
	public void TraditionalControlKThenCCancelsPrompt() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );
		interaction.OpenPrompt();

		CursesCommandSequenceResult pending = interaction.Process( Control( 'K' ) );
		CursesCommandSequenceResult completed = interaction.Process( Control( 'C' ) );

		Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( EditorSampleInteraction.CancelPromptCommandName,
			completed.Command?.Name );
	}

	[Fact]
	public void ControlVIsNotClaimedByEditor() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );

		CursesCommandSequenceResult result = interaction.Process( Control( 'V' ) );

		Assert.Equal( CursesCommandSequenceResultKind.Fallback, result.Kind );
		Assert.Null( result.Fallback?.Command );
	}

	[Fact]
	public void PromptScopeInvalidatesDocumentPrefixAndDiscoversPromptCommands() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );
		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( Control( 'k' ) ).Kind );

		interaction.OpenPrompt();

		Assert.True( interaction.IsPromptActive );
		Assert.False( interaction.HasPendingCommandSequence );
		Assert.DoesNotContain( "Ctrl+K Ctrl+G go", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.Contains( "Ctrl+K Ctrl+C cancel", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( Control( 'k' ) ).Kind );
		CursesCommandSequenceResult completed = interaction.Process( Control( 'c' ) );
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( EditorSampleInteraction.CancelPromptCommandName,
			completed.Command?.Name );

		interaction.ClosePrompt();
		Assert.False( interaction.IsPromptActive );
		Assert.Contains( "Ctrl+K Ctrl+G go", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
	}

	[Fact]
	public void SequenceMismatchReturnsTheUnicodeTypingEventToTheDocument() {
		CursesScreen screen = new( 80, 24 );
		using EditorSampleInteraction interaction = CreateInteraction( screen );
		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( Control( 'k' ) ).Kind );
		CursesInputEvent typed = CursesInputEvent.FromText( new Rune( '界' ) );

		CursesCommandSequenceResult mismatch = interaction.Process( typed );

		Assert.Equal( CursesCommandSequenceResultKind.Mismatch, mismatch.Kind );
		Assert.Same( typed, mismatch.Input );
		Assert.Same( typed, mismatch.Fallback?.Input );
		Assert.Equal( CursesInteractionResultKind.Targeted, mismatch.Fallback?.Kind );
		Assert.False( interaction.HasPendingCommandSequence );
	}

	private static EditorSampleInteraction CreateInteraction(
		CursesScreen screen
	) {
		EditorSampleInteraction interaction = new( screen );
		interaction.SetBounds(
			new CursesRectangle( 0, 0, 22, 80 ),
			new CursesRectangle( 23, 0, 1, 80 )
		);
		return interaction;
	}

	private static CursesInputEvent Control(
		char character
	) {
		return CursesInputEvent.FromKey(
			CursesKey.Character,
			CursesKeyModifiers.Control,
			character: new Rune( character )
		);
	}
}
