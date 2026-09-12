/*
	Icod.DCurses
	Managed, cross-platform curses-style terminal UI library for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.DCurses;

using System.Text;

/// <summary>
/// Represents one immutable terminal-independent semantic keyboard gesture.
/// </summary>
public readonly record struct CursesKeyGesture {
	private const CursesKeyModifiers BindingModifiers =
		CursesKeyModifiers.Shift
		| CursesKeyModifiers.Control
		| CursesKeyModifiers.Alt
		| CursesKeyModifiers.Super
		| CursesKeyModifiers.Hyper
		| CursesKeyModifiers.Meta;

	private CursesKeyGesture(
		CursesKey key,
		Rune? character,
		CursesKeyModifiers modifiers,
		CursesKeyEventPhase phase,
		int? functionKeyNumber
	) {
		this.Key = key;
		this.Character = character;
		this.Modifiers = modifiers;
		this.Phase = phase;
		this.FunctionKeyNumber = functionKeyNumber;
	}

	/// <summary>Gets the semantic key identity.</summary>
	public CursesKey Key {
		get;
	}

	/// <summary>Gets the Unicode scalar identity for a character gesture.</summary>
	public Rune? Character {
		get;
	}

	/// <summary>Gets the normalized binding modifiers.</summary>
	public CursesKeyModifiers Modifiers {
		get;
	}

	/// <summary>Gets the semantic key-event phase.</summary>
	public CursesKeyEventPhase Phase {
		get;
	}

	/// <summary>Gets the function-key number for a function-key gesture.</summary>
	public int? FunctionKeyNumber {
		get;
	}

	/// <summary>Creates a gesture for one named semantic key.</summary>
	/// <param name="key">The named semantic key.</param>
	/// <param name="modifiers">The binding modifiers.</param>
	/// <param name="phase">The key-event phase.</param>
	/// <returns>The immutable gesture.</returns>
	public static CursesKeyGesture ForKey(
		CursesKey key,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None,
		CursesKeyEventPhase phase = CursesKeyEventPhase.Press
	) {
		if ( !Enum.IsDefined( key ) ) {
			throw new ArgumentOutOfRangeException( nameof( key ) );
		}
		ValidateModifiers( modifiers );
		ValidatePhase( phase );
		if ( CursesKey.None == key
			|| CursesKey.Character == key
			|| CursesKey.Function == key
			|| CursesKey.Space == key
			|| CursesKey.Unrecognized == key ) {
			throw new ArgumentException(
				"The key must be a bindable named-key identity.",
				nameof( key )
			);
		}

		return new CursesKeyGesture(
			key,
			character: null,
			modifiers,
			phase,
			functionKeyNumber: null
		);
	}

	/// <summary>Creates a gesture for one Unicode character identity.</summary>
	/// <param name="character">The Unicode scalar identity.</param>
	/// <param name="modifiers">The binding modifiers.</param>
	/// <param name="phase">The key-event phase.</param>
	/// <returns>The immutable gesture.</returns>
	public static CursesKeyGesture ForCharacter(
		Rune character,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None,
		CursesKeyEventPhase phase = CursesKeyEventPhase.Press
	) {
		ValidateModifiers( modifiers );
		ValidatePhase( phase );

		return new CursesKeyGesture(
			CursesKey.Character,
			character,
			modifiers,
			phase,
			functionKeyNumber: null
		);
	}

	/// <summary>Creates a gesture for one numbered function key.</summary>
	/// <param name="functionKeyNumber">The function-key number in the existing DCurses range 0 through 63.</param>
	/// <param name="modifiers">The binding modifiers.</param>
	/// <param name="phase">The key-event phase.</param>
	/// <returns>The immutable gesture.</returns>
	public static CursesKeyGesture ForFunctionKey(
		int functionKeyNumber,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None,
		CursesKeyEventPhase phase = CursesKeyEventPhase.Press
	) {
		if ( functionKeyNumber is < 0 or > 63 ) {
			throw new ArgumentOutOfRangeException( nameof( functionKeyNumber ) );
		}
		ValidateModifiers( modifiers );
		ValidatePhase( phase );

		return new CursesKeyGesture(
			CursesKey.Function,
			character: null,
			modifiers,
			phase,
			functionKeyNumber
		);
	}

	/// <summary>Determines whether one normalized input event matches this gesture.</summary>
	/// <param name="input">The normalized DCurses input event.</param>
	/// <returns><see langword="true"/> when the event has this semantic gesture identity.</returns>
	internal bool Matches(
		CursesInputEvent input
	) {
		ArgumentNullException.ThrowIfNull( input );

		if ( CursesInputEventKind.Text == input.Kind ) {
			return CursesKey.Character == this.Key
				&& CursesKeyEventPhase.Press == this.Phase
				&& CursesKeyModifiers.None == this.Modifiers
				&& input.Character == this.Character;
		}
		if ( CursesInputEventKind.Key != input.Kind ) {
			return false;
		}

		CursesKeyModifiers modifiers = input.Modifiers & BindingModifiers;
		if ( modifiers != this.Modifiers
			|| input.KeyPhase != this.Phase ) {
			return false;
		}

		if ( CursesKey.Character == this.Key ) {
			if ( CursesKey.Character == input.Key ) {
				return input.Character == this.Character;
			}

			return CursesKey.Space == input.Key
				&& this.Character == new Rune( ' ' );
		}

		if ( CursesKey.Function == this.Key ) {
			return CursesKey.Function == input.Key
				&& input.FunctionKeyNumber == this.FunctionKeyNumber;
		}

		return input.Key == this.Key;
	}

	/// <summary>Gets whether this value was created as a valid bindable gesture.</summary>
	internal bool IsBindable => CursesKey.None != this.Key;

	private static void ValidateModifiers(
		CursesKeyModifiers modifiers
	) {
		if ( CursesKeyModifiers.None != ( modifiers & ~BindingModifiers ) ) {
			throw new ArgumentOutOfRangeException( nameof( modifiers ) );
		}
	}

	private static void ValidatePhase(
		CursesKeyEventPhase phase
	) {
		if ( !Enum.IsDefined( phase ) ) {
			throw new ArgumentOutOfRangeException( nameof( phase ) );
		}
	}
}
