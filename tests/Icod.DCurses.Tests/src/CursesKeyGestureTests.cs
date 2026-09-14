/*
	Icod.DCurses.Tests
	Automated test suite for Icod.DCurses.
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

using System.Text;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class CursesKeyGestureTests {
	[Fact]
	public void NamedKeyFactoryProducesFrozenIdentity() {
		CursesKeyGesture gesture = CursesKeyGesture.ForKey(
			CursesKey.Tab,
			CursesKeyModifiers.Control | CursesKeyModifiers.Alt,
			CursesKeyEventPhase.Release
		);

		Assert.Equal( CursesKey.Tab, gesture.Key );
		Assert.Null( gesture.Character );
		Assert.Equal(
			CursesKeyModifiers.Control | CursesKeyModifiers.Alt,
			gesture.Modifiers
		);
		Assert.Equal( CursesKeyEventPhase.Release, gesture.Phase );
		Assert.Null( gesture.FunctionKeyNumber );
	}

	[Fact]
	public void CharacterFactoryProducesFrozenIdentity() {
		Rune character = new( 0x03bb );
		CursesKeyGesture gesture = CursesKeyGesture.ForCharacter(
			character,
			CursesKeyModifiers.Shift | CursesKeyModifiers.Meta,
			CursesKeyEventPhase.Repeat
		);

		Assert.Equal( CursesKey.Character, gesture.Key );
		Assert.Equal( character, gesture.Character );
		Assert.Equal(
			CursesKeyModifiers.Shift | CursesKeyModifiers.Meta,
			gesture.Modifiers
		);
		Assert.Equal( CursesKeyEventPhase.Repeat, gesture.Phase );
		Assert.Null( gesture.FunctionKeyNumber );
	}

	[Fact]
	public void FunctionFactoryProducesFrozenIdentity() {
		CursesKeyGesture gesture = CursesKeyGesture.ForFunctionKey(
			12,
			CursesKeyModifiers.Super,
			CursesKeyEventPhase.Press
		);

		Assert.Equal( CursesKey.Function, gesture.Key );
		Assert.Null( gesture.Character );
		Assert.Equal( CursesKeyModifiers.Super, gesture.Modifiers );
		Assert.Equal( CursesKeyEventPhase.Press, gesture.Phase );
		Assert.Equal( 12, gesture.FunctionKeyNumber );
	}

	[Fact]
	public void FactoriesDefaultToPressWithoutModifiers() {
		CursesKeyGesture named = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesKeyGesture character = CursesKeyGesture.ForCharacter( new Rune( 'x' ) );
		CursesKeyGesture function = CursesKeyGesture.ForFunctionKey( 1 );

		Assert.Equal( CursesKeyEventPhase.Press, named.Phase );
		Assert.Equal( CursesKeyModifiers.None, named.Modifiers );
		Assert.Equal( CursesKeyEventPhase.Press, character.Phase );
		Assert.Equal( CursesKeyModifiers.None, character.Modifiers );
		Assert.Equal( CursesKeyEventPhase.Press, function.Phase );
		Assert.Equal( CursesKeyModifiers.None, function.Modifiers );
	}

	[Theory]
	[InlineData( CursesKey.None )]
	[InlineData( CursesKey.Character )]
	[InlineData( CursesKey.Function )]
	[InlineData( CursesKey.Space )]
	[InlineData( CursesKey.Unrecognized )]
	public void NamedKeyFactoryRejectsDedicatedOrNonBindableKeyForms(
		CursesKey key
	) {
		Assert.Throws<ArgumentException>(
			() => {
				_ = CursesKeyGesture.ForKey( key );
			}
		);
	}

	[Fact]
	public void NamedKeyFactoryRejectsUndefinedKey() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForKey( (CursesKey)999 );
			}
		);
	}

	[Theory]
	[InlineData( -1 )]
	[InlineData( 64 )]
	public void FunctionFactoryRejectsNumbersOutsideExistingRange(
		int functionKeyNumber
	) {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForFunctionKey( functionKeyNumber );
			}
		);
	}

	[Theory]
	[InlineData( CursesKeyModifiers.CapsLock )]
	[InlineData( CursesKeyModifiers.NumLock )]
	[InlineData( CursesKeyModifiers.Control | CursesKeyModifiers.CapsLock )]
	[InlineData( (CursesKeyModifiers)256 )]
	public void FactoriesRejectKeyboardStateAndUnknownModifierFlags(
		CursesKeyModifiers modifiers
	) {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForKey(
					CursesKey.Enter,
					modifiers
				);
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForCharacter(
					new Rune( 'x' ),
					modifiers
				);
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForFunctionKey(
					1,
					modifiers
				);
			}
		);
	}

	[Fact]
	public void FactoriesRejectUndefinedPhase() {
		CursesKeyEventPhase invalid = (CursesKeyEventPhase)99;

		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForKey(
					CursesKey.Enter,
					phase: invalid
				);
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForCharacter(
					new Rune( 'x' ),
					phase: invalid
				);
			}
		);
		Assert.Throws<ArgumentOutOfRangeException>(
			() => {
				_ = CursesKeyGesture.ForFunctionKey(
					1,
					phase: invalid
				);
			}
		);
	}

	[Fact]
	public void EqualityAndHashCodeUseOnlyFrozenSemanticIdentity() {
		CursesKeyGesture first = CursesKeyGesture.ForCharacter(
			new Rune( 'i' ),
			CursesKeyModifiers.Control,
			CursesKeyEventPhase.Release
		);
		CursesKeyGesture equal = CursesKeyGesture.ForCharacter(
			new Rune( 'i' ),
			CursesKeyModifiers.Control,
			CursesKeyEventPhase.Release
		);
		CursesKeyGesture differentCase = CursesKeyGesture.ForCharacter(
			new Rune( 'I' ),
			CursesKeyModifiers.Control,
			CursesKeyEventPhase.Release
		);

		Assert.Equal( first, equal );
		Assert.Equal( first.GetHashCode(), equal.GetHashCode() );
		Assert.NotEqual( first, differentCase );
	}

	[Fact]
	public void NamedKeyMatchingMasksLockStateButRequiresBindingModifiersAndPhase() {
		CursesKeyGesture gesture = CursesKeyGesture.ForKey(
			CursesKey.Tab,
			CursesKeyModifiers.Control,
			CursesKeyEventPhase.Repeat
		);
		CursesInputEvent matching = CursesInputEvent.FromKey(
			CursesKey.Tab,
			CursesKeyModifiers.Control
				| CursesKeyModifiers.CapsLock
				| CursesKeyModifiers.NumLock,
			keyPhase: CursesKeyEventPhase.Repeat
		);
		CursesInputEvent wrongModifier = CursesInputEvent.FromKey(
			CursesKey.Tab,
			CursesKeyModifiers.Alt,
			keyPhase: CursesKeyEventPhase.Repeat
		);
		CursesInputEvent wrongPhase = CursesInputEvent.FromKey(
			CursesKey.Tab,
			CursesKeyModifiers.Control,
			keyPhase: CursesKeyEventPhase.Press
		);

		Assert.True( gesture.Matches( matching ) );
		Assert.False( gesture.Matches( wrongModifier ) );
		Assert.False( gesture.Matches( wrongPhase ) );
	}

	[Fact]
	public void LockKeysRemainBindableNamedKeys() {
		CursesKeyGesture capsLock = CursesKeyGesture.ForKey( CursesKey.CapsLock );
		CursesKeyGesture numLock = CursesKeyGesture.ForKey( CursesKey.NumLock );

		Assert.True(
			capsLock.Matches(
				CursesInputEvent.FromKey(
					CursesKey.CapsLock,
					CursesKeyModifiers.CapsLock
				)
			)
		);
		Assert.True(
			numLock.Matches(
				CursesInputEvent.FromKey(
					CursesKey.NumLock,
					CursesKeyModifiers.NumLock
				)
			)
		);
	}

	[Fact]
	public void FunctionKeyMatchingRequiresExactFunctionNumber() {
		CursesKeyGesture gesture = CursesKeyGesture.ForFunctionKey(
			7,
			CursesKeyModifiers.Alt
		);

		Assert.True(
			gesture.Matches(
				CursesInputEvent.FromKey(
					CursesKey.Function,
					CursesKeyModifiers.Alt,
					functionKeyNumber: 7
				)
			)
		);
		Assert.False(
			gesture.Matches(
				CursesInputEvent.FromKey(
					CursesKey.Function,
					CursesKeyModifiers.Alt,
					functionKeyNumber: 8
				)
			)
		);
	}

	[Fact]
	public void CharacterPressMatchesTraditionalTextAndModernCharacterKey() {
		CursesKeyGesture gesture = CursesKeyGesture.ForCharacter( new Rune( 'x' ) );
		CursesInputEvent text = CursesInputEvent.FromText( new Rune( 'x' ) );
		CursesInputEvent modern = CursesInputEvent.FromKey(
			CursesKey.Character,
			character: new Rune( 'x' )
		);

		Assert.True( gesture.Matches( text ) );
		Assert.True( gesture.Matches( modern ) );
	}

	[Fact]
	public void CharacterSpacePressAlsoMatchesSemanticSpaceKey() {
		CursesKeyGesture gesture = CursesKeyGesture.ForCharacter( new Rune( ' ' ) );

		Assert.True(
			gesture.Matches(
				CursesInputEvent.FromText( new Rune( ' ' ) )
			)
		);
		Assert.True(
			gesture.Matches(
				CursesInputEvent.FromKey( CursesKey.Space )
			)
		);
	}

	[Fact]
	public void TraditionalTextDoesNotFabricateModifiersOrRepeatReleasePhase() {
		CursesInputEvent text = CursesInputEvent.FromText( new Rune( 'x' ) );

		Assert.False(
			CursesKeyGesture.ForCharacter(
				new Rune( 'x' ),
				CursesKeyModifiers.Control
			).Matches( text )
		);
		Assert.False(
			CursesKeyGesture.ForCharacter(
				new Rune( 'x' ),
				phase: CursesKeyEventPhase.Repeat
			).Matches( text )
		);
		Assert.False(
			CursesKeyGesture.ForCharacter(
				new Rune( 'x' ),
				phase: CursesKeyEventPhase.Release
			).Matches( text )
		);
	}

	[Fact]
	public void CharacterMatchingUsesOnlyPrimaryCharacterIdentity() {
		CursesInputEvent input = CursesInputEvent.FromKey(
			CursesKey.Character,
			CursesKeyModifiers.None,
			new Rune( 'a' ),
			functionKeyNumber: null,
			CursesKeyEventPhase.Press,
			new Rune( 'A' ),
			new Rune( 'q' ),
			"x"
		);

		Assert.True(
			CursesKeyGesture.ForCharacter( new Rune( 'a' ) ).Matches( input )
		);
		Assert.False(
			CursesKeyGesture.ForCharacter( new Rune( 'A' ) ).Matches( input )
		);
		Assert.False(
			CursesKeyGesture.ForCharacter( new Rune( 'q' ) ).Matches( input )
		);
		Assert.False(
			CursesKeyGesture.ForCharacter( new Rune( 'x' ) ).Matches( input )
		);
	}

	[Fact]
	public void CharacterRepeatAndReleaseRequireExplicitModernKeyPhases() {
		CursesKeyGesture repeat = CursesKeyGesture.ForCharacter(
			new Rune( 'x' ),
			phase: CursesKeyEventPhase.Repeat
		);
		CursesKeyGesture release = CursesKeyGesture.ForCharacter(
			new Rune( 'x' ),
			phase: CursesKeyEventPhase.Release
		);

		Assert.True(
			repeat.Matches(
				CursesInputEvent.FromKey(
					CursesKey.Character,
					character: new Rune( 'x' ),
					keyPhase: CursesKeyEventPhase.Repeat
				)
			)
		);
		Assert.True(
			release.Matches(
				CursesInputEvent.FromKey(
					CursesKey.Character,
					character: new Rune( 'x' ),
					keyPhase: CursesKeyEventPhase.Release
				)
			)
		);
	}

	[Fact]
	public void GestureDoesNotMatchNonKeyboardInput() {
		CursesKeyGesture gesture = CursesKeyGesture.ForKey( CursesKey.Enter );
		CursesInputEvent endOfInput = CursesInputEvent.EndOfInput();

		Assert.False( gesture.Matches( endOfInput ) );
	}
}
