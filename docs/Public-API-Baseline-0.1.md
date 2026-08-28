# Icod.DCurses 0.1 Public API Baseline

**Project:** `Icod.DCurses`
**Release line:** `0.1.x`
**Baseline prepared in:** `0.1.0-Alpha-21`
**Target framework:** `net10.0`
**Status:** Release-line source contract baseline

---

## 1. Purpose

This document records the public contract intentionally carried into the
`Icod.DCurses 0.1` release line.

It is a regret-review baseline, not a promise that every future pre-1.0 release
is binary compatible. The generated XML documentation remains authoritative for
exact signatures, but additions/removals against this baseline should be
deliberate rather than accidental.

## 2. Session and lifecycle surface

The principal live-session type is:

```text
CursesSession : IAsyncDisposable
```

Its accepted public contract includes:

- `OpenAsync(CursesSessionOptions?, CancellationToken)`;
- advanced `OpenAsync(TerminalSession, CursesSessionOptions?, CancellationToken)`;
- selected terminal identity/endpoints through `Terminal`, `InputEndpoint`, and
  `OutputEndpoint`;
- `IsInteractive` and immutable `Options`;
- `Screen` and `StandardScreen`;
- `GetDimensions()` and `SynchronizeDimensions()`;
- `SupportsLifecycleEvents`, `TerminationToken`, and
  `ReadLifecycleEventAsync(...)`;
- `ReadEventAsync(...)` with indefinite, timeout, and deadline-shaped waits;
- `RefreshAsync(...)` and `Invalidate()`;
- `AlertAsync(...)`;
- cursor visibility/position control;
- rendition reset;
- alternate-screen and keypad mode control;
- reversible rich-input protocol acquisition;
- deterministic asynchronous disposal.

Lifecycle events use:

```text
CursesLifecycleEventKind
CursesLifecycleEvent
```

with resize, interrupt, termination, suspending, and resumed semantics.

## 3. Input contract

The accepted semantic input surface is:

```text
CursesEventKind
CursesEvent

CursesInputEventKind
CursesKey
CursesKeyModifiers
CursesInputEvent

CursesMouseAction
CursesMouseButton
CursesMouseEvent

CursesFocusState
CursesFocusEvent

CursesPastePhase
CursesPasteEvent
```

Coordinates are zero-based. Shift, Control, and Alt are semantic modifier flags.
Function-key events carry the function number separately.

Paste remains framed as Begin / one-or-more Data chunks / End. Applications
must not assume one Data event per paste operation.

## 4. Reversible rich-input protocols

The public curses-shaped lease contract is:

```text
CursesMouseTrackingMode
CursesInputProtocolOptions
CursesInputProtocolLease
CursesSession.AcquireInputProtocolsAsync(...)
```

DCurses owns the caller-facing curses abstraction; `Icod.Terminal` owns the
actual protocol state and restoration.

## 5. Screen, cell, style, and text contract

The accepted logical-display surface includes:

```text
CursesCell
CursesColorKind
CursesColor
CursesTextAttributes
CursesStyle

ICursesTextWidthProvider
UnicodeCursesTextWidthProvider

CursesVirtualScreen
CursesScreen
CursesScreenResizedEventArgs
CursesWrapMode
CursesWindow
```

Application coordinates are zero-based.

`CursesWindow` is the preferred text-writing API because it applies the owning
screen's text-width provider. The direct public `CursesCell(string, style)`
constructor is a low-level one-column cell constructor; multi-column and
combining behavior should normally be produced through a window write.

Windows are logical views over one shared screen rather than independent
compositing surfaces.

## 6. Session options and presentation enums

The accepted session/presentation policy types are:

```text
CursesInputMode
CursesSessionOptions
CursesAlertKind
CursesCursorVisibility
```

The default session uses cbreak input, no echo, alternate-screen entry, keypad
mode, and hidden cursor where the selected terminal supports reversible
capabilities.

## 7. Intentional Terminal/TermInfo types in public signatures

The 0.1 regret review accepts several lower-layer types in advanced or controlled
session APIs:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

This is intentional for `0.1`: DCurses does not duplicate endpoint,
capability-description, live-size, or controlled-result models merely to hide
its substrate packages.

Ordinary applications can remain on the curses-shaped `CursesSession`,
`CursesEvent`, screen/window, style, and input-protocol APIs.

## 8. Ownership rules frozen for 0.1

- A successfully opened `CursesSession` owns the presentation state it enters.
- Supplying an already-open `TerminalSession` transfers ownership only after
  successful curses initialization.
- Disposing the curses session restores DCurses and Terminal-owned state and
  disposes the owned Terminal session.
- Input-protocol leases are reversible and may be disposed independently.
- Session disposal remains authoritative cleanup even when a caller has not
  disposed every individual input-protocol lease.
- Applications do not need private terminal-mode, escape-decoder, resize-signal,
  alternate-screen, or cursor-lifecycle implementations.

## 9. Explicitly deferred

The 0.1 baseline does not freeze the later roadmap families for:

- mature move/resize/derived-window semantics;
- pads and large surfaces;
- complete Unicode/grapheme correctness;
- complete color/rendition portability;
- compatibility façades for native curses APIs;
- general terminal emulation;
- PTY/process hosting.

Those remain later-version concerns.
