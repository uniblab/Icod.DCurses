# Icod.DCurses 0.2 Public API Baseline

**Project:** `Icod.DCurses`  
**Release line:** `0.2.x`  
**Baseline prepared in:** `0.2.0-alpha.3` / T207  
**Stable release:** `0.2.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Release-line source contract baseline

---

## 1. Purpose

This document records the public input-contract additions intentionally carried by
`Icod.DCurses 0.2` over the stable `0.1` baseline.

The `0.1` screen/window/session contract remains in force except where this document
adds new semantic-input members. The release does not intentionally remove or rename any
`0.1` public member.

The principal design decision is that DCurses remains a curses-owned facade over the
stable Terminal 1.0 input substrate. The richer Terminal semantic contract is mapped into
DCurses types rather than requiring ordinary consumers to adopt `TerminalInputEvent`.

## 2. Existing numeric values retained

The complete `0.1` `CursesKey` prefix remains numerically frozen:

```text
None      = 0
Character = 1
Enter     = 2
Space     = 3
Escape    = 4
Backspace = 5
Tab       = 6
Up        = 7
Down      = 8
Left      = 9
Right     = 10
Home      = 11
End       = 12
PageUp    = 13
PageDown  = 14
Insert    = 15
Delete    = 16
Function  = 17
```

The legacy modifier flags remain:

```text
None    = 0
Shift   = 1
Control = 2
Alt     = 4
```

Tests exercise these values directly so future source edits cannot silently renumber them.

## 3. `CursesKey` additions

The stable 0.2 additions are appended after `Function = 17`:

```text
CapsLock           = 18
ScrollLock         = 19
NumLock            = 20
PrintScreen        = 21
Pause              = 22
Menu               = 23
Keypad0            = 24
Keypad1            = 25
Keypad2            = 26
Keypad3            = 27
Keypad4            = 28
Keypad5            = 29
Keypad6            = 30
Keypad7            = 31
Keypad8            = 32
Keypad9            = 33
KeypadDecimal      = 34
KeypadDivide       = 35
KeypadMultiply     = 36
KeypadSubtract     = 37
KeypadAdd          = 38
KeypadEnter        = 39
KeypadEqual        = 40
KeypadSeparator    = 41
KeypadLeft         = 42
KeypadRight        = 43
KeypadUp           = 44
KeypadDown         = 45
KeypadPageUp       = 46
KeypadPageDown     = 47
KeypadHome         = 48
KeypadEnd          = 49
KeypadInsert       = 50
KeypadDelete       = 51
KeypadBegin        = 52
MediaPlay          = 53
MediaPause         = 54
MediaPlayPause     = 55
MediaReverse       = 56
MediaStop          = 57
MediaFastForward   = 58
MediaRewind        = 59
MediaTrackNext     = 60
MediaTrackPrevious = 61
MediaRecord        = 62
VolumeDown         = 63
VolumeUp           = 64
VolumeMute         = 65
LeftShift          = 66
LeftControl        = 67
LeftAlt            = 68
LeftSuper          = 69
LeftHyper          = 70
LeftMeta           = 71
RightShift         = 72
RightControl       = 73
RightAlt           = 74
RightSuper         = 75
RightHyper         = 76
RightMeta          = 77
IsoLevel3Shift     = 78
IsoLevel5Shift     = 79
Unrecognized       = 80
```

Function keys remain represented by `CursesKey.Function` plus
`FunctionKeyNumber` in the inclusive range `0..63`.

## 4. Key phase

`0.2` adds:

```text
CursesKeyEventPhase.Press   = 0
CursesKeyEventPhase.Repeat  = 1
CursesKeyEventPhase.Release = 2
```

`CursesInputEvent.KeyPhase` is nullable because ordinary direct text, mouse, focus,
paste, end-of-input, and other non-key input do not carry key phases.

Traditional Terminal key events which provide only press semantics surface as
`CursesKeyEventPhase.Press`.

## 5. Modifier additions

The 0.2 modifier mask is:

```text
None     = 0
Shift    = 1
Control  = 2
Alt      = 4
Super    = 8
Hyper    = 16
Meta     = 32
CapsLock = 64
NumLock  = 128
```

The production converter is tested against every mask from `0x00` through `0xff`.

## 6. Modern key payload

`CursesInputEvent` adds these read-only members:

```text
Rune? ShiftedCharacter
Rune? BaseLayoutCharacter
string? AssociatedText
CursesKeyEventPhase? KeyPhase
```

The existing members remain:

```text
CursesInputEventKind Kind
CursesKey Key
Rune? Character
CursesKeyModifiers Modifiers
int? FunctionKeyNumber
CursesMouseEvent? Mouse
CursesFocusEvent? Focus
CursesPasteEvent? Paste
```

For Character key events, Terminal-supplied character, shifted-layout character,
base-layout character, associated text, modifier state, and key phase are preserved.

Ordinary direct text remains `CursesInputEventKind.Text`; it is not rewritten into a
synthetic key event merely because negotiated keyboard protocols exist.

## 7. Keyboard reporting protocol

`0.2` adds:

```text
CursesKeyboardReportingMode.Disambiguated = 0
CursesKeyboardReportingMode.EventTypes    = 1
CursesKeyboardReportingMode.AllKeys       = 2
```

`CursesInputProtocolOptions` adds:

```text
CursesKeyboardReportingMode? KeyboardReportingMode { get; init; }
```

`CursesInputProtocolLease` adds:

```text
CursesKeyboardReportingMode? KeyboardReportingMode { get; }
```

Keyboard reporting composes with the existing bracketed-paste, focus, and mouse requests
inside one curses protocol lease. Negotiation, protocol stack composition, suspend/resume,
and exact restoration remain Terminal responsibilities.

## 8. Dependency-boundary result

The 0.2 input work introduces no new direct package dependency and no newly approved
Terminal/TermInfo type in a public DCurses signature.

The intentional upstream public-type allow-list remains:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

The existing reflection boundary test remains authoritative and will fail if another
upstream type leaks into the public API accidentally.

## 9. Ownership and decoding

The 0.2 release does not add a keyboard parser, response router, or second input loop.

`Icod.Terminal` remains responsible for:

- traditional terminfo key matching;
- Kitty/CSI-u negotiation and decoding;
- event-phase interpretation;
- keyboard protocol lifecycle;
- mouse/focus/paste decoding;
- query/response routing.

DCurses maps those semantic events into the public curses facade.

## 10. Deferred beyond 0.2

The 0.2 public baseline does not freeze the later 1.0-train feature families for:

- Unicode/grapheme and terminal-cell completion (`0.3.0`);
- richer window editing/composition (`0.4.0`);
- pads and large surfaces (`0.5.0`);
- rendition/drawing expansion (`0.6.0`);
- refresh/output optimization (`0.7.0`);
- final concurrency and production-hardening policy (`0.8.0`).

Those releases may add new APIs before the `0.9.0` contract freeze.
