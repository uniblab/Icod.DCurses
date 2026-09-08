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

	/// <summary>A normalized terminal mouse event.</summary>
	Mouse,

	/// <summary>A terminal focus-in or focus-out event.</summary>
	Focus,

	/// <summary>One framed bracketed-paste event.</summary>
	Paste,

	/// <summary>The terminal input endpoint reached end-of-file or disconnected.</summary>
	EndOfInput
}

/// <summary>
/// Identifies a terminal-independent key.
/// </summary>
public enum CursesKey {
	/// <summary>No key is associated with this event.</summary>
	None = 0,

	/// <summary>A printable or control-modified character.</summary>
	Character = 1,

	/// <summary>The Enter key.</summary>
	Enter = 2,

	/// <summary>The Space key.</summary>
	Space = 3,

	/// <summary>The Escape key.</summary>
	Escape = 4,

	/// <summary>The Backspace key.</summary>
	Backspace = 5,

	/// <summary>The Tab key.</summary>
	Tab = 6,

	/// <summary>The up-arrow key.</summary>
	Up = 7,

	/// <summary>The down-arrow key.</summary>
	Down = 8,

	/// <summary>The left-arrow key.</summary>
	Left = 9,

	/// <summary>The right-arrow key.</summary>
	Right = 10,

	/// <summary>The Home key.</summary>
	Home = 11,

	/// <summary>The End key.</summary>
	End = 12,

	/// <summary>The Page Up key.</summary>
	PageUp = 13,

	/// <summary>The Page Down key.</summary>
	PageDown = 14,

	/// <summary>The Insert key.</summary>
	Insert = 15,

	/// <summary>The Delete key.</summary>
	Delete = 16,

	/// <summary>A numbered function key.</summary>
	Function = 17,

	/// <summary>The Caps Lock key.</summary>
	CapsLock = 18,

	/// <summary>The Scroll Lock key.</summary>
	ScrollLock = 19,

	/// <summary>The Num Lock key.</summary>
	NumLock = 20,

	/// <summary>The Print Screen key.</summary>
	PrintScreen = 21,

	/// <summary>The Pause key.</summary>
	Pause = 22,

	/// <summary>The Menu/Application key.</summary>
	Menu = 23,

	/// <summary>Keypad digit 0.</summary>
	Keypad0 = 24,

	/// <summary>Keypad digit 1.</summary>
	Keypad1 = 25,

	/// <summary>Keypad digit 2.</summary>
	Keypad2 = 26,

	/// <summary>Keypad digit 3.</summary>
	Keypad3 = 27,

	/// <summary>Keypad digit 4.</summary>
	Keypad4 = 28,

	/// <summary>Keypad digit 5.</summary>
	Keypad5 = 29,

	/// <summary>Keypad digit 6.</summary>
	Keypad6 = 30,

	/// <summary>Keypad digit 7.</summary>
	Keypad7 = 31,

	/// <summary>Keypad digit 8.</summary>
	Keypad8 = 32,

	/// <summary>Keypad digit 9.</summary>
	Keypad9 = 33,

	/// <summary>Keypad decimal separator.</summary>
	KeypadDecimal = 34,

	/// <summary>Keypad division operator.</summary>
	KeypadDivide = 35,

	/// <summary>Keypad multiplication operator.</summary>
	KeypadMultiply = 36,

	/// <summary>Keypad subtraction operator.</summary>
	KeypadSubtract = 37,

	/// <summary>Keypad addition operator.</summary>
	KeypadAdd = 38,

	/// <summary>Keypad Enter.</summary>
	KeypadEnter = 39,

	/// <summary>Keypad equals.</summary>
	KeypadEqual = 40,

	/// <summary>Keypad separator.</summary>
	KeypadSeparator = 41,

	/// <summary>Keypad Left.</summary>
	KeypadLeft = 42,

	/// <summary>Keypad Right.</summary>
	KeypadRight = 43,

	/// <summary>Keypad Up.</summary>
	KeypadUp = 44,

	/// <summary>Keypad Down.</summary>
	KeypadDown = 45,

	/// <summary>Keypad Page Up.</summary>
	KeypadPageUp = 46,

	/// <summary>Keypad Page Down.</summary>
	KeypadPageDown = 47,

	/// <summary>Keypad Home.</summary>
	KeypadHome = 48,

	/// <summary>Keypad End.</summary>
	KeypadEnd = 49,

	/// <summary>Keypad Insert.</summary>
	KeypadInsert = 50,

	/// <summary>Keypad Delete.</summary>
	KeypadDelete = 51,

	/// <summary>Keypad Begin.</summary>
	KeypadBegin = 52,

	/// <summary>Media Play.</summary>
	MediaPlay = 53,

	/// <summary>Media Pause.</summary>
	MediaPause = 54,

	/// <summary>Media Play/Pause.</summary>
	MediaPlayPause = 55,

	/// <summary>Media Reverse.</summary>
	MediaReverse = 56,

	/// <summary>Media Stop.</summary>
	MediaStop = 57,

	/// <summary>Media Fast Forward.</summary>
	MediaFastForward = 58,

	/// <summary>Media Rewind.</summary>
	MediaRewind = 59,

	/// <summary>Next media track.</summary>
	MediaTrackNext = 60,

	/// <summary>Previous media track.</summary>
	MediaTrackPrevious = 61,

	/// <summary>Media Record.</summary>
	MediaRecord = 62,

	/// <summary>Volume Down.</summary>
	VolumeDown = 63,

	/// <summary>Volume Up.</summary>
	VolumeUp = 64,

	/// <summary>Volume Mute.</summary>
	VolumeMute = 65,

	/// <summary>Left Shift.</summary>
	LeftShift = 66,

	/// <summary>Left Control.</summary>
	LeftControl = 67,

	/// <summary>Left Alt.</summary>
	LeftAlt = 68,

	/// <summary>Left Super.</summary>
	LeftSuper = 69,

	/// <summary>Left Hyper.</summary>
	LeftHyper = 70,

	/// <summary>Left Meta.</summary>
	LeftMeta = 71,

	/// <summary>Right Shift.</summary>
	RightShift = 72,

	/// <summary>Right Control.</summary>
	RightControl = 73,

	/// <summary>Right Alt.</summary>
	RightAlt = 74,

	/// <summary>Right Super.</summary>
	RightSuper = 75,

	/// <summary>Right Hyper.</summary>
	RightHyper = 76,

	/// <summary>Right Meta.</summary>
	RightMeta = 77,

	/// <summary>ISO Level 3 Shift.</summary>
	IsoLevel3Shift = 78,

	/// <summary>ISO Level 5 Shift.</summary>
	IsoLevel5Shift = 79,

	/// <summary>A syntactically valid key identity not recognized by this library version.</summary>
	Unrecognized = 80
}

/// <summary>
/// Identifies one semantic key-event phase.
/// </summary>
public enum CursesKeyEventPhase {
	/// <summary>The key was pressed.</summary>
	Press = 0,

	/// <summary>The key press repeated while held.</summary>
	Repeat = 1,

	/// <summary>The key was released.</summary>
	Release = 2
}

/// <summary>
/// Identifies modifiers carried by a decoded key or mouse event.
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
	Alt = 4,

	/// <summary>The Super modifier is present.</summary>
	Super = 8,

	/// <summary>The Hyper modifier is present.</summary>
	Hyper = 16,

	/// <summary>The Meta modifier is present.</summary>
	Meta = 32,

	/// <summary>Caps Lock is active.</summary>
	CapsLock = 64,

	/// <summary>Num Lock is active.</summary>
	NumLock = 128
}

/// <summary>
/// Represents one terminal-independent input event.
/// </summary>
public sealed class CursesInputEvent {
	private CursesInputEvent(
		CursesInputEventKind kind,
		CursesKey key,
		Rune? character,
		Rune? shiftedCharacter,
		Rune? baseLayoutCharacter,
		string? associatedText,
		CursesKeyModifiers modifiers,
		CursesKeyEventPhase? keyPhase,
		int? functionKeyNumber,
		CursesMouseEvent? mouse,
		CursesFocusEvent? focus,
		CursesPasteEvent? paste
	) {
		this.Kind = kind;
		this.Key = key;
		this.Character = character;
		this.ShiftedCharacter = shiftedCharacter;
		this.BaseLayoutCharacter = baseLayoutCharacter;
		this.AssociatedText = associatedText;
		this.Modifiers = modifiers;
		this.KeyPhase = keyPhase;
		this.FunctionKeyNumber = functionKeyNumber;
		this.Mouse = mouse;
		this.Focus = focus;
		this.Paste = paste;
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
	/// Gets the Unicode character identity for ordinary text or a character key.
	/// </summary>
	public Rune? Character {
		get;
	}

	/// <summary>Gets the shifted-layout character identity when reported by a modern keyboard protocol.</summary>
	public Rune? ShiftedCharacter {
		get;
	}

	/// <summary>Gets the base-layout character identity when reported by a modern keyboard protocol.</summary>
	public Rune? BaseLayoutCharacter {
		get;
	}

	/// <summary>Gets associated text produced by a key event when reported.</summary>
	public string? AssociatedText {
		get;
	}

	/// <summary>Gets key modifiers.</summary>
	public CursesKeyModifiers Modifiers {
		get;
	}

	/// <summary>Gets the semantic key-event phase for key events.</summary>
	public CursesKeyEventPhase? KeyPhase {
		get;
	}

	/// <summary>
	/// Gets the function-key number when <see cref="Key"/> is <see cref="CursesKey.Function"/>.
	/// </summary>
	public int? FunctionKeyNumber {
		get;
	}

	/// <summary>
	/// Gets the normalized mouse payload when <see cref="Kind"/> is
	/// <see cref="CursesInputEventKind.Mouse"/>.
	/// </summary>
	public CursesMouseEvent? Mouse {
		get;
	}

	/// <summary>
	/// Gets the focus payload when <see cref="Kind"/> is
	/// <see cref="CursesInputEventKind.Focus"/>.
	/// </summary>
	public CursesFocusEvent? Focus {
		get;
	}

	/// <summary>
	/// Gets the bracketed-paste payload when <see cref="Kind"/> is
	/// <see cref="CursesInputEventKind.Paste"/>.
	/// </summary>
	public CursesPasteEvent? Paste {
		get;
	}

	/// <summary>Creates an ordinary Unicode text-input event.</summary>
	/// <param name="character">The decoded Unicode scalar value.</param>
	/// <returns>The text-input event.</returns>
	internal static CursesInputEvent FromText(
		Rune character
	) {
		return new CursesInputEvent(
			CursesInputEventKind.Text,
			CursesKey.Character,
			character,
			shiftedCharacter: null,
			baseLayoutCharacter: null,
			associatedText: null,
			CursesKeyModifiers.None,
			keyPhase: null,
			functionKeyNumber: null,
			mouse: null,
			focus: null,
			paste: null
		);
	}

	/// <summary>Creates a named or modified key-input event.</summary>
	/// <param name="key">The terminal-independent key.</param>
	/// <param name="modifiers">The active key modifiers.</param>
	/// <param name="character">The optional character carried by a Character key event.</param>
	/// <param name="functionKeyNumber">The function-key number when <paramref name="key"/> is Function.</param>
	/// <param name="keyPhase">The semantic key-event phase.</param>
	/// <param name="shiftedCharacter">The shifted-layout character identity when reported.</param>
	/// <param name="baseLayoutCharacter">The base-layout character identity when reported.</param>
	/// <param name="associatedText">Associated text produced by the key event when reported.</param>
	/// <returns>The key-input event.</returns>
	internal static CursesInputEvent FromKey(
		CursesKey key,
		CursesKeyModifiers modifiers = CursesKeyModifiers.None,
		Rune? character = null,
		int? functionKeyNumber = null,
		CursesKeyEventPhase keyPhase = CursesKeyEventPhase.Press,
		Rune? shiftedCharacter = null,
		Rune? baseLayoutCharacter = null,
		string? associatedText = null
	) {
		if ( !Enum.IsDefined( key ) || CursesKey.None == key ) {
			throw new ArgumentOutOfRangeException( nameof( key ) );
		}
		ValidateModifiers( modifiers );
		if ( !Enum.IsDefined( keyPhase ) ) {
			throw new ArgumentOutOfRangeException( nameof( keyPhase ) );
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

		if ( CursesKey.Character == key ) {
			if ( !character.HasValue ) {
				throw new ArgumentNullException( nameof( character ) );
			}
		} else if ( character.HasValue
			|| shiftedCharacter.HasValue
			|| baseLayoutCharacter.HasValue ) {
			throw new ArgumentException(
				"Character identities are only valid for a Character key event.",
				nameof( character )
			);
		}

		if ( associatedText is not null && 0 == associatedText.Length ) {
			throw new ArgumentException(
				"Associated key text must be null or non-empty.",
				nameof( associatedText )
			);
		}

		return new CursesInputEvent(
			CursesInputEventKind.Key,
			key,
			character,
			shiftedCharacter,
			baseLayoutCharacter,
			associatedText,
			modifiers,
			keyPhase,
			functionKeyNumber,
			mouse: null,
			focus: null,
			paste: null
		);
	}

	/// <summary>Creates a normalized mouse-input event.</summary>
	/// <param name="mouse">The normalized mouse payload.</param>
	/// <returns>The mouse-input event.</returns>
	internal static CursesInputEvent FromMouse(
		CursesMouseEvent mouse
	) {
		ArgumentNullException.ThrowIfNull( mouse );
		return new CursesInputEvent(
			CursesInputEventKind.Mouse,
			CursesKey.None,
			character: null,
			shiftedCharacter: null,
			baseLayoutCharacter: null,
			associatedText: null,
			mouse.Modifiers,
			keyPhase: null,
			functionKeyNumber: null,
			mouse,
			focus: null,
			paste: null
		);
	}

	/// <summary>Creates a terminal-focus event.</summary>
	/// <param name="focus">The focus payload.</param>
	/// <returns>The focus-input event.</returns>
	internal static CursesInputEvent FromFocus(
		CursesFocusEvent focus
	) {
		ArgumentNullException.ThrowIfNull( focus );
		return new CursesInputEvent(
			CursesInputEventKind.Focus,
			CursesKey.None,
			character: null,
			shiftedCharacter: null,
			baseLayoutCharacter: null,
			associatedText: null,
			CursesKeyModifiers.None,
			keyPhase: null,
			functionKeyNumber: null,
			mouse: null,
			focus,
			paste: null
		);
	}

	/// <summary>Creates one bracketed-paste framing event.</summary>
	/// <param name="paste">The paste payload.</param>
	/// <returns>The paste-input event.</returns>
	internal static CursesInputEvent FromPaste(
		CursesPasteEvent paste
	) {
		ArgumentNullException.ThrowIfNull( paste );
		return new CursesInputEvent(
			CursesInputEventKind.Paste,
			CursesKey.None,
			character: null,
			shiftedCharacter: null,
			baseLayoutCharacter: null,
			associatedText: null,
			CursesKeyModifiers.None,
			keyPhase: null,
			functionKeyNumber: null,
			mouse: null,
			focus: null,
			paste
		);
	}

	/// <summary>Creates an end-of-input event.</summary>
	/// <returns>The end-of-input event.</returns>
	internal static CursesInputEvent EndOfInput() {
		return new CursesInputEvent(
			CursesInputEventKind.EndOfInput,
			CursesKey.None,
			character: null,
			shiftedCharacter: null,
			baseLayoutCharacter: null,
			associatedText: null,
			CursesKeyModifiers.None,
			keyPhase: null,
			functionKeyNumber: null,
			mouse: null,
			focus: null,
			paste: null
		);
	}

	private static void ValidateModifiers(
		CursesKeyModifiers modifiers
	) {
		const CursesKeyModifiers known =
			CursesKeyModifiers.Shift
			| CursesKeyModifiers.Control
			| CursesKeyModifiers.Alt
			| CursesKeyModifiers.Super
			| CursesKeyModifiers.Hyper
			| CursesKeyModifiers.Meta
			| CursesKeyModifiers.CapsLock
			| CursesKeyModifiers.NumLock;

		if ( 0 != ( modifiers & ~known ) ) {
			throw new ArgumentOutOfRangeException(
				nameof( modifiers ),
				modifiers,
				"The curses key modifiers contain an unknown flag."
			);
		}
	}
}
