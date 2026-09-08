# T201-T204 — Terminal 1.0 Input Semantic Foundation

**Project:** `Icod.DCurses`  
**Development version:** `0.2.0-alpha.1`  
**Branch:** `0.2.0`  
**Pull request:** `#13`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** API foundation implemented; validation in progress

---

## 1. Purpose

The first `0.2.0` implementation slice removes the mismatch between the stable
Terminal 1.0 semantic keyboard contract and the older DCurses 0.1 event facade.

Before this work, `CursesSession` converted only the earlier key vocabulary and
Shift/Control/Alt modifier set. A valid newer `TerminalKey` could therefore
reach the conversion switch and throw `ArgumentOutOfRangeException`.

The alpha.1 foundation expands the curses-owned semantic API without adding a
second input decoder or exposing Terminal's raw input contract directly to
ordinary DCurses consumers.

---

## 2. Version Foundation

The active package is advanced to:

```text
Version         0.2.0-alpha.1
PackageVersion  0.2.0-alpha.1
AssemblyVersion 0.2.0.0
```

The stable runtime dependencies remain:

```text
Icod.Terminal 1.0.0
Icod.TermInfo 1.10.0
```

No new package dependency is introduced.

---

## 3. Key Vocabulary

The existing `CursesKey` numeric values `0..17` are frozen unchanged through
`Function`.

New values are appended for the stable Terminal 1.0 vocabulary, covering:

- lock and system keys;
- keypad digits, operators, navigation, editing, and Begin;
- media transport and volume keys;
- left/right Shift, Control, Alt, Super, Hyper, and Meta keys;
- ISO Level 3/5 Shift;
- `Unrecognized`.

The function-key contract remains `CursesKey.Function` plus
`FunctionKeyNumber`.

---

## 4. Modifier Vocabulary

The legacy modifier bits remain:

```text
None    0
Shift   1
Control 2
Alt     4
```

The Terminal 1.0 semantic flags are appended with matching bits:

```text
Super    8
Hyper   16
Meta    32
CapsLock 64
NumLock 128
```

Mouse events use the same expanded `CursesKeyModifiers` validation contract so
shared modifier semantics do not diverge between keyboard and mouse payloads.

---

## 5. Key Event Payload

The alpha introduces:

```text
CursesKeyEventPhase
    Press
    Repeat
    Release
```

and extends `CursesInputEvent` with:

```text
ShiftedCharacter
BaseLayoutCharacter
AssociatedText
KeyPhase
```

The existing fields remain:

```text
Key
Character
Modifiers
FunctionKeyNumber
Mouse
Focus
Paste
```

Validation rejects impossible or malformed combinations, including unknown
modifier flags, undefined key phases, missing Character payload for a Character
key, misplaced function-key numbers, and empty associated text.

---

## 6. Terminal Conversion

`CursesSession` now maps the complete stable `TerminalKey` vocabulary into the
curses facade and preserves:

- complete modifier state;
- key phase;
- primary character identity;
- shifted-layout character identity;
- base-layout character identity;
- associated text;
- function-key number.

The conversion remains semantic. Terminal owns byte decoding, traditional
terminfo key matching, modern keyboard protocol parsing, ambiguity handling, and
query/response routing.

---

## 7. Keyboard Reporting Protocol

The curses protocol facade now includes:

```text
CursesKeyboardReportingMode
    Disambiguated
    EventTypes
    AllKeys
```

`CursesInputProtocolOptions.KeyboardReportingMode` maps directly to the stable
Terminal reporting modes and composes with existing requests for:

- bracketed paste;
- focus reporting;
- mouse tracking.

`CursesInputProtocolLease` records the requested keyboard-reporting mode while
Terminal continues to own physical negotiation and restoration.

---

## 8. Initial Contract Tests

The alpha.1 tests establish machine gates for:

- legacy `CursesKey` numeric stability;
- legacy modifier-bit stability;
- name coverage for every stable `TerminalKey`;
- bit/value coverage for every stable `TerminalKeyModifiers` value;
- coverage for every stable `TerminalKeyEventPhase`;
- preservation of modern Character-key metadata;
- mapping of every curses keyboard-reporting mode to Terminal.

T205 will deepen this into public end-to-end conversion acceptance through real
`TerminalSession` test transports and negotiated modern keyboard input.

---

## 9. Remaining 0.2 Work

The foundation does not close `0.2.0`.

Next work remains:

1. T205 — exhaustive public conversion and compatibility acceptance;
2. T206 — rich-input showcase and consumer demonstration;
3. T207 — public API/documentation/package regret gate;
4. T208 — stable `0.2.0` release closure.

The detailed release plan remains authoritative in
`Icod.DCurses-0.2.0-Development-Roadmap.md`.
