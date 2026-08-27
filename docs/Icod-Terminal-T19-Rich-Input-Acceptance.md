# Icod.Terminal T19 — Rich-Input Acceptance

**Project:** `Icod.DCurses`
**DCurses development version:** `0.1.0-Alpha-15`
**Terminal acceptance target:** `Icod.Terminal 0.2.0-alpha.6`
**Status:** First acceptance slice implemented; validation pending

---

## 1. Purpose

This checkpoint exercises the rich-input substrate supplied by
`Icod.Terminal 0.2.0-alpha.6` through the active `Icod.DCurses` integration
boundary.

The objective is not to add a second terminal protocol implementation to
DCurses. It is to prove that DCurses can consume the richer Terminal contract
while retaining its curses-shaped application facade.

## 2. Accepted mechanisms

Alpha-15 accepts the following Terminal mechanisms:

- normalized mouse events;
- focus-in and focus-out events;
- framed bracketed-paste events;
- richer traditional modified-key events;
- reversible bracketed-paste reporting leases;
- reversible focus-reporting leases;
- reversible mouse-tracking leases;
- lifecycle suspension and re-entry of active rich-input protocols;
- session disposal cleanup for undisposed protocol leases.

DCurses maps Terminal event payloads into `CursesInputEvent` payloads and
delegates protocol ownership to `TerminalSession.AcquireInputProtocolsAsync`.

## 3. Deliberate non-implementation

DCurses does not add:

- a mouse escape parser;
- a bracketed-paste reader;
- hard-coded mouse/focus/paste enable or disable escape strings;
- a second terminal input loop;
- terminal-family detection for rich input.

Those responsibilities remain in `Icod.Terminal` and `Icod.TermInfo`.

## 4. Acceptance coverage

The non-interactive acceptance tests prove that:

1. focus, paste, mouse, and modified-key events arrive through the same
   `CursesSession.ReadEventAsync` stream;
2. mouse coordinates remain normalized to zero-based terminal cells;
3. Terminal key modifiers survive the DCurses translation layer;
4. protocol requests made through DCurses are acquired by Terminal;
5. controlled capability absence remains a controlled unavailable result;
6. disposing the curses session restores protocol state even when a caller has
   not disposed the individual curses protocol lease first.

Terminal's own lifecycle tests remain the authority for the protocol-manager
suspend/re-enter ordering; DCurses acceptance verifies that it delegates protocol
ownership rather than creating competing lifecycle state.

## 5. Gate

This first acceptance slice is complete when the repository builds and tests
successfully against the published `Icod.Terminal 0.2.0-alpha.6` package.

The broader Terminal T19 gate remains open until the DCurses sample/acceptance
