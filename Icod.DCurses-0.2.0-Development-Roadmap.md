# Icod.DCurses 0.2.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.2.0`  
**Stable baseline:** `0.1.1`  
**Development dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Terminal 1.0 input semantic parity  
**Current development version:** `0.2.0`  
**Current tranche:** T208 — stable release closure  
**Status:** Stable source closure prepared; PR validation active

---

## 1. Release Objective

`Icod.DCurses 0.2.0` SHALL close the semantic-input gap between the stable
DCurses `0.1` facade and the stable `Icod.Terminal 1.0.0` input contract.

The current DCurses adapter recognizes only the earlier Terminal key vocabulary
and modifier set. `Icod.Terminal 1.0.0` now supplies a broader semantic contract,
including:

- press, repeat, and release key phases;
- a larger named-key vocabulary;
- keypad keys;
- lock keys;
- media keys;
- left/right modifier keys;
- Super, Hyper, Meta, CapsLock, and NumLock modifier state;
- shifted-layout character identity;
- base-layout character identity;
- associated text carried by modern keyboard protocols;
- an explicit `Unrecognized` semantic key;
- negotiated keyboard-reporting modes.

A valid Terminal 1.0 event SHALL NOT become an unexpected
`ArgumentOutOfRangeException` merely because the DCurses facade predates that
Terminal semantic addition.

The release remains a curses-owned API. Ordinary DCurses consumers SHALL not be
required to switch to `TerminalInputEvent`, `TerminalKey`, or
`TerminalInputProtocolOptions` to obtain the richer semantics.

---

## 2. Design Rules

### 2.1 Preserve the curses facade

DCurses SHALL continue to expose:

```text
CursesEvent
CursesInputEvent
CursesKey
CursesKeyModifiers
CursesInputProtocolOptions
CursesInputProtocolLease
```

Terminal types remain an implementation substrate except for the limited public
Terminal/TermInfo types already accepted by the dependency-boundary baseline.

### 2.2 Preserve existing numeric enum values

Existing `CursesKey` and `CursesKeyModifiers` values SHALL retain their current
numeric values. New values SHALL be appended or assigned explicitly so the
pre-0.2 public vocabulary does not drift accidentally.

### 2.3 No second keyboard decoder

All byte-stream decoding, terminfo key matching, modern keyboard negotiation,
query/response routing, and protocol lifecycle remain owned by
`Icod.Terminal`.

DCurses maps semantic Terminal events into curses-shaped semantic events.

### 2.4 Exhaustive mapping is a testable contract

The Terminal-to-DCurses conversion SHALL be covered by tests which enumerate the
stable Terminal key/modifier vocabulary. A future Terminal key addition SHALL
cause an intentional test failure until the DCurses mapping decision is made.

---

## 3. Development Sequence

```text
T201  0.2 package/version and contract foundation
  -> T202  complete key vocabulary and modifier parity
  -> T203  key-phase and modern key payload parity
  -> T204  keyboard-reporting protocol lease
  -> T205  exhaustive conversion and compatibility tests
  -> T206  rich-input showcase/sample update
  -> T207  package/public API baseline and documentation gate
  -> T208  0.2.0 stable closure
```

T201-T207 are complete. The `0.2.0-rc.1` gate passed on Windows, Linux, macOS,
and the canonical package/fresh-consumer job. T208 has therefore assigned the
stable `0.2.0` source identity; merge, main Release validation, tagging, and
publication remain after the PR gate.

---

# 4. T201 — 0.2 Package and Contract Foundation

T201 begins active feature development.

Required work:

- set `<Version>` and `<PackageVersion>` to `0.2.0-alpha.1`;
- set assembly version consistently with the repository's pre-1.0 versioning
  policy;
- update package release notes for the active alpha;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0` package references;
- publish this `0.2.0` roadmap;
- link the `1.0.0` release-train roadmap from active project documentation;
- establish the first input-parity tests before widening the production facade.

**Gate T201:** the repository builds, tests, packs, and validates as
`0.2.0-alpha.1` without changing the stable dependency baseline.

---

# 5. T202 — Complete Key Vocabulary and Modifier Parity

## 5.1 `CursesKey`

Append curses semantic equivalents for the stable Terminal 1.0 key vocabulary
that is not represented in `0.1`, including:

```text
CapsLock
ScrollLock
NumLock
PrintScreen
Pause
Menu

Keypad0 ... Keypad9
KeypadDecimal
KeypadDivide
KeypadMultiply
KeypadSubtract
KeypadAdd
KeypadEnter
KeypadEqual
KeypadSeparator
KeypadLeft
KeypadRight
KeypadUp
KeypadDown
KeypadPageUp
KeypadPageDown
KeypadHome
KeypadEnd
KeypadInsert
KeypadDelete
KeypadBegin

MediaPlay
MediaPause
MediaPlayPause
MediaReverse
MediaStop
MediaFastForward
MediaRewind
MediaTrackNext
MediaTrackPrevious
MediaRecord
VolumeDown
VolumeUp
VolumeMute

LeftShift
LeftControl
LeftAlt
LeftSuper
LeftHyper
LeftMeta
RightShift
RightControl
RightAlt
RightSuper
RightHyper
RightMeta
IsoLevel3Shift
IsoLevel5Shift

Unrecognized
```

The existing `Function` + `FunctionKeyNumber` representation remains the
function-key contract.

## 5.2 `CursesKeyModifiers`

Retain:

```text
None = 0
Shift = 1
Control = 2
Alt = 4
```

and add stable flags equivalent to Terminal 1.0:

```text
Super = 8
Hyper = 16
Meta = 32
CapsLock = 64
NumLock = 128
```

## 5.3 Conversion

`CursesSession` SHALL convert every stable Terminal key and modifier flag into a
curses semantic equivalent.

No valid stable Terminal 1.0 key SHALL fall into an accidental default exception
path.

**Gate T202:** exhaustive conversion tests prove complete key and modifier
coverage while the existing 0.1 numeric values remain unchanged.

---

# 6. T203 — Key Phase and Modern Key Payload Parity

Introduce:

```text
CursesKeyEventPhase
    Press
    Repeat
    Release
```

`CursesInputEvent` SHALL preserve the following Terminal key-event information:

- `KeyPhase`;
- `Character`;
- `ShiftedCharacter`;
- `BaseLayoutCharacter`;
- `AssociatedText`;
- `Modifiers`;
- `FunctionKeyNumber`.

The event contract SHALL validate combinations predictably:

- function-key numbers are valid only with `CursesKey.Function`;
- character identities are valid only where the semantic key contract allows
  them;
- associated text is null or non-empty;
- key phases use defined values;
- modifier masks contain only known flags.

Traditional key input remains represented as a Press event where Terminal
reports only press semantics.

Ordinary direct text events remain `CursesInputEventKind.Text` and do not become
synthetic key events merely because modern protocols can report all keys.

**Gate T203:** modern keyboard events round-trip through Terminal-to-DCurses
mapping without loss of key phase, character identities, associated text, or
modifier information.

---

# 7. T204 — Keyboard Reporting Protocol Lease

Introduce a curses-shaped keyboard reporting request:

```text
CursesKeyboardReportingMode
    Disambiguated
    EventTypes
    AllKeys
```

Extend `CursesInputProtocolOptions` with:

```text
CursesKeyboardReportingMode? KeyboardReportingMode
```

and extend `CursesInputProtocolLease` with the effective requested mode.

Mapping semantics SHALL delegate to Terminal:

```text
CursesKeyboardReportingMode.Disambiguated
    -> TerminalKeyboardReportingMode.Disambiguated

CursesKeyboardReportingMode.EventTypes
    -> TerminalKeyboardReportingMode.EventTypes

CursesKeyboardReportingMode.AllKeys
    -> TerminalKeyboardReportingMode.AllKeys
```

The request SHALL compose with existing DCurses protocol requests for:

- bracketed paste;
- focus reporting;
- mouse tracking.

Protocol lifecycle, negotiation, screen ownership, suspend/resume, and cleanup
remain Terminal responsibilities.

**Gate T204:** a single DCurses lease can request keyboard + paste + focus + mouse
semantics, and disposal/restoration remains authoritative through Terminal.

---

# 8. T205 — Exhaustive Conversion and Compatibility Tests

The test suite SHALL add machine-checkable coverage for:

- every `TerminalKey` value;
- every `TerminalKeyModifiers` flag and combined masks;
- every `TerminalKeyEventPhase` value;
- character, shifted-character, base-layout-character, and associated-text
  preservation;
- F0-F63 semantics where supplied by Terminal;
- keypad navigation/editing keys;
- left/right modifier keys;
- lock-state modifiers;
- media keys;
- `Unrecognized` key handling;
- traditional Shift/Alt/Control combinations;
- modern negotiated keyboard events;
- coexistence with mouse, focus, paste, timeout, cancellation, and lifecycle
  events;
- the public dependency-boundary allow-list.

Where direct access to private conversion helpers would distort the production
API, tests SHOULD drive events through a real `TerminalSession` test transport
and public `CursesSession.ReadEventAsync(...)`.

**Gate T205:** no stable Terminal 1.0 semantic input can reach an untested DCurses
mapping path.

The completed checkpoint is recorded in
[`docs/T205-Terminal-1.0-Input-Compatibility-Acceptance.md`](docs/T205-Terminal-1.0-Input-Compatibility-Acceptance.md).

---

# 9. T206 — Showcase and Consumer Acceptance

Update the rich-input showcase so users can observe the expanded input contract.

The showcase SHOULD display:

- semantic key name;
- function-key number;
- key phase;
- complete modifier set;
- character;
- shifted character;
- base-layout character;
- associated text;
- existing mouse/focus/paste/lifecycle events.

Add controls or startup policy that allow the showcase to request a useful
keyboard-reporting mode without making modern keyboard support mandatory on
terminals which do not provide it.

Capability absence remains a controlled result rather than a failure.

**Gate T206:** the showcase remains usable on traditional terminals and can
demonstrate richer semantics on terminals supporting negotiated keyboard
reporting.

The completed checkpoint is recorded in
[`docs/T206-Rich-Input-Showcase-and-Consumer-Acceptance.md`](docs/T206-Rich-Input-Showcase-and-Consumer-Acceptance.md).

---

# 10. T207 — Public API, Documentation, and Package Gate

Before stable `0.2.0`:

- perform a focused API-regret review of every new key/event/protocol member;
- document numeric enum stability decisions;
- update XML documentation;
- update README input examples where useful;
- update the dependency baseline if the accepted public boundary changes;
- add/update the public API baseline for `0.2`;
- ensure package verifier and package-only smoke consumers compile the new
  surface;
- verify no new direct package dependency was introduced;
- run Staging validation on Windows/Linux/macOS;
- run the canonical package validation job from the generated `.nupkg`.

**Gate T207:** the complete new public input contract is intentional,
documented, package-consumable, and cross-platform green.

The completed checkpoint is recorded in
[`docs/T207-Public-API-Documentation-and-Package-Gate.md`](docs/T207-Public-API-Documentation-and-Package-Gate.md).
The `0.2.0-rc.1` Staging and package validation gate passed before T208 assigned
the stable source version.

---

# 11. T208 — Stable 0.2.0 Closure

T208 is release closure only.

Source closure work:

- `0.2.0-rc.1` Staging runtime and package validation passed;
- `<Version>` and `<PackageVersion>` are set to `0.2.0`;
- `AssemblyVersion` remains `0.2.0.0`;
- stable release notes describe the final semantic-input contract;
- the public API baseline is frozen as the `0.2.x` release-line source baseline;
- `docs/T208-0.2.0-Stable-Release-Closure.md` records the merge/publication gate.

Remaining release gate:

- require this stable source PR to pass Staging runtime and package validation;
- merge the release commit to `main`;
- require the six-runner `main` Release matrix to pass;
- create `v0.2.0` only after the matching `main` commit is green;
- publish through the tag-controlled release workflow;
- verify NuGet.org, GitHub Packages, GitHub Release assets, symbols, and
  checksums.

**0.2.0 completion criterion:** a package-only DCurses consumer can request and
consume the complete stable Terminal 1.0 semantic keyboard contract through the
curses facade without information loss or accidental conversion exceptions.

---

## 12. Explicit 0.2 Non-Goals

`0.2.0` does not include:

- Unicode/grapheme contract completion (`0.3.0`);
- new window editing/composition families (`0.4.0`);
- pads (`0.5.0`);
- new rendition families (`0.6.0`);
- refresh optimization (`0.7.0`);
- general terminal query/response APIs;
- a second keyboard parser;
- direct exposure of Terminal's raw decoder or protocol manager.

The release should be narrow: make DCurses a correct and complete consumer of
its stable Terminal 1.0 input substrate, then move on.
