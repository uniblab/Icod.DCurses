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
	Alt = 4
}

/// <summary>
/// Represents one terminal-independent input event.
/// </summary>
public sealed class CursesInputEvent {
	private CursesInputEvent(
		CursesInputEventKind kind,
		CursesKey key,
		Rune? character,
		CursesKeyModifiers modifiers,
		int? functionKeyNumber,
		CursesMouseEvent? mouse,
		CursesFocusEvent? focus,
		CursesPasteEvent? paste
	) {
		this.Kind = kind;
		this.Key = key;
		this.Character = character;
		this.Modifiers = modifiers;
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
			CursesKeyModifiers.None,
			null,
			null,
			null,
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
		int? functionKeyNumber = null
	) {
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
			functionKeyNumber,
			null,
			null,
			null
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
			null,
			mouse.Modifiers,
			null,
			mouse,
			null,
			null
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
			null,
			CursesKeyModifiers.None,
			null,
			null,
			focus,
			null
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
			null,
			CursesKeyModifiers.None,
			null,
			null,
			null,
			paste
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
			null,
			null,
			null,
			null
		);
	}
}