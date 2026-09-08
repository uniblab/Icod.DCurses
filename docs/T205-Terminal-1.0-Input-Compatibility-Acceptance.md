# T205 — Terminal 1.0 Input Compatibility Acceptance

**Project:** `Icod.DCurses`  
**Development line:** `0.2.0`  
**Development version:** `0.2.0-alpha.2`  
**Tranche:** T205 — exhaustive conversion and compatibility tests  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Implemented; Staging validation pending

---

## 1. Purpose

T205 proves that the expanded DCurses input facade is not merely vocabulary-compatible
with Terminal 1.0. It exercises the actual production conversion functions and a negotiated
modern-keyboard session through public `CursesSession` APIs.

The tranche closes the accidental exception path that existed when a valid stable
`TerminalKey` was outside the earlier DCurses switch.

## 2. Exhaustive production mapping tests

`CursesInputSemanticParityTests` now drives the production mapping helpers for:

- every stable `TerminalKey` value;
- every 8-bit combination of the stable Terminal modifier flags;
- every stable `TerminalKeyEventPhase` value.

The mapping helpers remain `internal`; they do not expand the public DCurses surface.
The existing `InternalsVisibleTo` test seam is used so the tests exercise the exact code
used by `CursesSession.ReadEventAsync(...)` rather than duplicating the mapping table in
test-only code.

The test suite continues to freeze the legacy DCurses numeric contract:

```text
CursesKey.None      = 0
...
CursesKey.Function  = 17

CursesKeyModifiers.Shift    = 1
CursesKeyModifiers.Control  = 2
CursesKeyModifiers.Alt      = 4
```

New vocabulary remains appended after the existing values.

## 3. Function-key range

The curses event contract continues to represent function keys as:

```text
CursesKey.Function
FunctionKeyNumber = 0 ... 63
```

T205 adds explicit boundary coverage for both F0 and F63 and also drives advertised
`kf0` and `kf63` sequences through a real Terminal session into public DCurses events.

## 4. Negotiated modern-keyboard acceptance

`CursesModernKeyboardAcceptanceTests` opens a real test `TerminalSession`, transfers
ownership to `CursesSession`, and requests one combined curses protocol lease containing:

- `CursesKeyboardReportingMode.AllKeys`;
- bracketed paste;
- focus reporting;
- mouse button reporting.

The test transport answers Terminal's keyboard-support probe and verifies that the
Terminal-owned keyboard mode is pushed before input is consumed.

Input is then supplied through the real Terminal decoder and observed only through
`CursesSession.ReadEventAsync(...)`.

Acceptance covers:

- character identity;
- shifted-layout character identity;
- base-layout character identity;
- associated text;
- repeat phase;
- all eight stable modifier bits simultaneously;
- media key identity;
- lock-key identity;
- left and right modifier-key identities;
- keypad key identity;
- `Unrecognized` fallback identity;
- F0 and F63;
- focus events;
- bracketed-paste begin/data/end;
- mouse events.

The event stream remains singular: DCurses does not add another input reader or keyboard
parser.

## 5. Cleanup ownership

The same acceptance test verifies that disposing the curses session releases:

- keyboard reporting;
- mouse reporting;
- focus reporting;
- bracketed-paste reporting.

Disposing the already-released curses protocol lease afterward emits no duplicate
terminal cleanup. Terminal remains the authority for protocol lifecycle and exact host
restoration.

## 6. Gate

T205 is complete when:

1. every stable Terminal key reaches the production DCurses mapping successfully;
2. every valid stable modifier combination maps without loss;
3. Press, Repeat, and Release phases map without loss;
4. F0-F63 remain valid curses function-key numbers;
5. the negotiated modern-keyboard acceptance test passes through public DCurses APIs;
6. keyboard input coexists with mouse, focus, and paste on the same event stream;
7. protocol disposal/restoration remains authoritative through Terminal;
8. `net8.0`, `net9.0`, and `net10.0` remain green on Windows, Linux, and macOS;
9. the package candidate and fresh-consumer validation remain green as
   `0.2.0-alpha.2`.

After this gate, development proceeds to T206 — showcase and consumer acceptance.
