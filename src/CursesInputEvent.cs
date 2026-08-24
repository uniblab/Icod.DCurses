namespace Icod.DCurses;

using System.Text;

/// <summary>
/// Identifies the semantic form of one decoded terminal-input event.
/// </summary>
public enum CursesInputEventKind {
	/// <summary>Ordinary Unicode text input.</summary>
	Text,

	/// <summary>A named key or modified character key.</summary>
	Key,

	/// <summary>The terminal input endpoint reached end-of-file or disconnected.</summary>
	EndOfInput
}

/// <summary>
/// Identifies a terminal-independent key.
/// </summary>
public enum CursesKey {
	/// <summary>No key is associated with this event.</summary>
	None,

	/// <summary>A printable or control-modified character.</summary>
	Character,

	/// <summary>The Enter key.</summary>
	Enter,

	/// <summary>The Space key.</summary>
	Space,

	/// <summary>The Escape key.</summary>
	Escape,

	/// <summary>The Backspace key.</summary>
	Backspace,

	/// <summary>The Tab key.</summary>
	Tab,

	/// <summary>The up-arrow key.</summary>
	Up,

	/// <summary>The down-arrow key.</summary>
	Down,

	/// <summary>The left-arrow key.</summary>
	Left,

	/// <summary>The right-arrow key.</summary>
	Right,

	/// <summary>The Home key.</summary>
	Home,

	/// <summary>The End key.</summary>
	End,

	/// <summary>The Page Up key.</summary>
	PageUp,

	/// <summary>The Page Down key.</summary>
	PageDown,

	/// <summary>The Insert key.</summary>
	Insert,

	/// <summary>The Delete key.</summary>
	Delete,

	/// <summary>A numbered function key.</summary>
	Function
}

/// <summary>
/// Identifies modifiers carried by a decoded key event.
/// </summary>
[Flags]
public enum CursesKeyModifiers {
	/// <summary>No modifier is present.</summary>
	None = 0,

	/// <summary>The Shift modifier is present.</summary>
	Shift = 1,

	/// <summary>The Control modifier is present.</summary>
	Control = 2,

	/// <summary>The Alt modifier is present.</summary>
	Alt = 4
}

/// <summary>
/// Represents one terminal-independent keyboard or end-of-input event.
/// </summary>
public sealed class CursesInputEvent {
	private CursesInputEvent(
		CursesInputEventKind kind,
		CursesKey key,
		Rune? character,
		CursesKeyModifiers modifiers,
		int? functionKeyNumber) {
		Kind = kind;
		Key = key;
		Character = character;
		Modifiers = modifiers;
		FunctionKeyNumber = functionKeyNumber;
	}

	/// <summary>Gets the semantic event kind.</summary>
	public CursesInputEventKind Kind {
		get;
	}

	/// <summary>
	/// Gets the terminal-independent key. Text input uses <see cref="CursesKey.Character"/>.
	/// </summary>
	public CursesKey Key {
		get;
	}

	/// <summary>
	/// Gets the Unicode character for ordinary text or a control-modified character key.
	/// </summary>
	public Rune? Character {
		get;
	}

	/// <summary>Gets key modifiers.</summary>
	public CursesKeyModifiers Modifiers {
		get;
	}

	/// <summary>
	/// Gets the function-key number when <see cref="Key"/> is <see cref="CursesKey.Function"/>.
	/// </summary>
	public int? FunctionKeyNumber {
		get;
	}

	/// <summary>Creates an ordinary Unicode text-input event.</summary>
	/// <param name="character">The decoded Unicode scalar value.</param>
	/// <returns>The text-input event.</returns>
	internal static CursesInputEvent FromText( Rune character ) {
		return new CursesInputEvent(
			CursesInputEventKind.Text,
			CursesKey.Character,
			character,
			CursesKeyModifiers.None,
			null
		);
	}

	/// <summary>Creates a named or modified key-input event.</summary>
	/// <param name="key">The terminal-independent key.</param>
	/// <param name="modifiers">The active key modifiers.</param>
	/// <param name="character">The optional character carried by the key event.</param>
	/// <param name="functionKeyNumber">The function-key number when <paramref name="key"/> is Function.</param>
	/// <returns>The key-input event.</returns>
	internal static CursesInputEvent FromKey(
		CursesKey key,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None,
		Rune? character = null,
		int? functionKeyNumber = null) {
		if ( !Enum.IsDefined( key ) ) {
			throw new ArgumentOutOfRangeException( nameof( key ) );
		}

		if ( CursesKey.Function == key ) {
			if ( functionKeyNumber is < 0 or > 63 ) {
				throw new ArgumentOutOfRangeException( nameof( functionKeyNumber ) );
			}
			if ( !functionKeyNumber.HasValue ) {
				throw new ArgumentNullException( nameof( functionKeyNumber ) );
			}
		} else if ( functionKeyNumber.HasValue ) {
			throw new ArgumentException(
				"A function-key number is only valid for a Function key event.",
				nameof( functionKeyNumber )
			);
		}

		return new CursesInputEvent(
			CursesInputEventKind.Key,
			key,
			character,
			modifiers,
			functionKeyNumber
		);
	}

	/// <summary>Creates an end-of-input event.</summary>
	/// <returns>The end-of-input event.</returns>
	internal static CursesInputEvent EndOfInput() {
		return new CursesInputEvent(
			CursesInputEventKind.EndOfInput,
			CursesKey.None,
			null,
			CursesKeyModifiers.None,
			null
		);
	}
}