using System.Text;
using Icod.DCurses.Roguelike.Sample;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class RoguelikeSampleInteractionTests {
	[Fact]
	public void MapContextRoutesMovementAndHelpCommands() {
		CursesScreen screen = new( 80, 24 );
		using CursesPanel help = screen.CreatePanel( 8, 16, 8, 48 );
		using RoguelikeSampleInteraction interaction = CreateInteraction( screen, help );

		CursesCommandSequenceResult movement = interaction.Process(
			CursesInputEvent.FromText( new Rune( 'w' ) )
		);
		Assert.Equal( RoguelikeSampleInteraction.MoveUpCommandName,
			movement.Fallback?.Command?.Name );
		Assert.Contains( "g h help", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );

		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind );
		CursesCommandSequenceResult completed = interaction.Process(
			CursesInputEvent.FromText( new Rune( 'h' ) )
		);
		Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
		Assert.Equal( RoguelikeSampleInteraction.OpenHelpCommandName,
			completed.Command?.Name );
	}

	[Fact]
	public void HelpScopeInvalidatesMapPrefixAndSuppressesMovement() {
		CursesScreen screen = new( 80, 24 );
		using CursesPanel help = screen.CreatePanel( 8, 16, 8, 48 );
		using RoguelikeSampleInteraction interaction = CreateInteraction( screen, help );
		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind );

		interaction.OpenHelp();

		Assert.True( interaction.IsHelpActive );
		Assert.False( interaction.HasPendingCommandSequence );
		Assert.True( help.IsVisible );
		Assert.DoesNotContain( "g h help", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		Assert.Contains( "g m map", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		CursesCommandSequenceResult movement = interaction.Process(
			CursesInputEvent.FromText( new Rune( 'w' ) )
		);
		Assert.Null( movement.Fallback?.Command );
		Assert.Equal( CursesInteractionResultKind.Targeted, movement.Fallback?.Kind );

		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind );
		CursesCommandSequenceResult completed = interaction.Process(
			CursesInputEvent.FromText( new Rune( 'm' ) )
		);
		Assert.Equal( RoguelikeSampleInteraction.CloseHelpCommandName,
			completed.Command?.Name );
	}

	[Fact]
	public void ClosingHelpInvalidatesOverlayPrefixAndRestoresMapDiscovery() {
		CursesScreen screen = new( 80, 24 );
		using CursesPanel help = screen.CreatePanel( 8, 16, 8, 48 );
		using RoguelikeSampleInteraction interaction = CreateInteraction( screen, help );
		interaction.OpenHelp();
		Assert.Equal( CursesCommandSequenceResultKind.Pending,
			interaction.Process( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind );

		interaction.CloseHelp();

		Assert.False( interaction.IsHelpActive );
		Assert.False( interaction.HasPendingCommandSequence );
		Assert.False( help.IsVisible );
		Assert.Contains( "g h help", interaction.GetShortcutSummary(),
			StringComparison.Ordinal );
		CursesCommandSequenceResult movement = interaction.Process(
			CursesInputEvent.FromKey( CursesKey.Up )
		);
		Assert.Equal( RoguelikeSampleInteraction.MoveUpCommandName,
			movement.Fallback?.Command?.Name );
	}

	private static RoguelikeSampleInteraction CreateInteraction(
		CursesScreen screen,
		CursesPanel help
	) {
		RoguelikeSampleInteraction interaction = new( screen, help );
		interaction.SetBounds(
			new CursesRectangle( 0, 0, 16, 62 ),
			new CursesRectangle( 0, 0, help.Rows, help.Columns )
		);
		return interaction;
	}
}
