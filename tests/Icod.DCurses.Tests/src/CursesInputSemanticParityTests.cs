using System.Text;
using Icod.Terminal;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>
/// Contract coverage for the Terminal 1.0 semantic-input vocabulary carried by DCurses 0.2.
/// </summary>
public sealed class CursesInputSemanticParityTests {
	[Fact]
	public void LegacyKeyNumericValuesRemainStable() {
		Assert.Equal( 0, (int)CursesKey.None );
		Assert.Equal( 1, (int)CursesKey.Character );
		Assert.Equal( 2, (int)CursesKey.Enter );
		Assert.Equal( 3, (int)CursesKey.Space );
		Assert.Equal( 4, (int)CursesKey.Escape );
		Assert.Equal( 5, (int)CursesKey.Backspace );
		Assert.Equal( 6, (int)CursesKey.Tab );
		Assert.Equal( 7, (int)CursesKey.Up );
		Assert.Equal( 8, (int)CursesKey.Down );
		Assert.Equal( 9, (int)CursesKey.Left );
		Assert.Equal( 10, (int)CursesKey.Right );
		Assert.Equal( 11, (int)CursesKey.Home );
		Assert.Equal( 12, (int)CursesKey.End );
		Assert.Equal( 13, (int)CursesKey.PageUp );
		Assert.Equal( 14, (int)CursesKey.PageDown );
		Assert.Equal( 15, (int)CursesKey.Insert );
		Assert.Equal( 16, (int)CursesKey.Delete );
		Assert.Equal( 17, (int)CursesKey.Function );
	}

	[Fact]
	public void CursesKeyVocabularyCoversEveryStableTerminalKey() {
		foreach ( TerminalKey terminalKey in Enum.GetValues<TerminalKey>() ) {
			Assert.True(
				Enum.TryParse(
					terminalKey.ToString(),
					out CursesKey cursesKey
				),
				$"CursesKey is missing TerminalKey.{terminalKey}."
			);
			Assert.True( Enum.IsDefined( cursesKey ) );
		}
	}

	[Fact]
	public void ModifierVocabularyPreservesLegacyBitsAndCoversTerminalFlags() {
		Assert.Equal( 0, (int)CursesKeyModifiers.None );
		Assert.Equal( 1, (int)CursesKeyModifiers.Shift );
		Assert.Equal( 2, (int)CursesKeyModifiers.Control );
		Assert.Equal( 4, (int)CursesKeyModifiers.Alt );

		foreach ( TerminalKeyModifiers terminalModifier in Enum.GetValues<TerminalKeyModifiers>() ) {
			Assert.True(
				Enum.TryParse(
					terminalModifier.ToString(),
					out CursesKeyModifiers cursesModifier
				),
				$"CursesKeyModifiers is missing TerminalKeyModifiers.{terminalModifier}."
			);
			Assert.Equal( (int)terminalModifier, (int)cursesModifier );
		}
	}

	[Fact]
	public void KeyPhaseVocabularyCoversEveryStableTerminalPhase() {
		foreach ( TerminalKeyEventPhase terminalPhase in Enum.GetValues<TerminalKeyEventPhase>() ) {
			Assert.True(
				Enum.TryParse(
					terminalPhase.ToString(),
					out CursesKeyEventPhase cursesPhase
				),
				$"CursesKeyEventPhase is missing TerminalKeyEventPhase.{terminalPhase}."
			);
			Assert.Equal( (int)terminalPhase, (int)cursesPhase );
		}
	}

	[Fact]
	public void ModernCharacterKeyPayloadIsPreserved() {
		CursesInputEvent input = CursesInputEvent.FromKey(
			CursesKey.Character,
			CursesKeyModifiers.Shift
				| CursesKeyModifiers.Control
				| CursesKeyModifiers.Super,
			new Rune( 'a' ),
			functionKeyNumber: null,
			CursesKeyEventPhase.Repeat,
			new Rune( 'A' ),
			new Rune( 'q' ),
			"x"
		);

		Assert.Equal( CursesInputEventKind.Key, input.Kind );
		Assert.Equal( CursesKey.Character, input.Key );
		Assert.Equal( new Rune( 'a' ), input.Character );
		Assert.Equal( new Rune( 'A' ), input.ShiftedCharacter );
		Assert.Equal( new Rune( 'q' ), input.BaseLayoutCharacter );
		Assert.Equal( "x", input.AssociatedText );
		Assert.Equal(
			CursesKeyModifiers.Shift
				| CursesKeyModifiers.Control
				| CursesKeyModifiers.Super,
			input.Modifiers
		);
		Assert.Equal( CursesKeyEventPhase.Repeat, input.KeyPhase );
		Assert.Null( input.FunctionKeyNumber );
	}

	[Fact]
	public void KeyboardReportingOptionsDelegateToTerminalVocabulary() {
		foreach ( CursesKeyboardReportingMode mode in Enum.GetValues<CursesKeyboardReportingMode>() ) {
			CursesInputProtocolOptions options = new() {
				KeyboardReportingMode = mode
			};

			TerminalInputProtocolOptions terminal = options.ToTerminalOptions();
			Assert.Equal(
				mode.ToString(),
				terminal.KeyboardReportingMode?.ToString()
			);
		}
	}
}
