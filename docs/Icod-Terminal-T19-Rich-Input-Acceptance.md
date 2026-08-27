# Icod.Terminal T19 — Rich-Input Acceptance

**Project:** `Icod.DCurses`
**DCurses development version:** `0.1.0-Alpha-16`
**Terminal acceptance target:** `Icod.Terminal 0.2.0-alpha.6`
**Status:** Alpha-15 automated acceptance validated; Alpha-16 interactive acceptance current

---

## 1. Purpose

This checkpoint exercises the rich-input substrate supplied by
`Icod.Terminal 0.2.0-alpha.6` through the active `Icod.DCurses` integration
boundary.

The objective is not to add a second terminal protocol implementation to
DCurses. It is to prove that DCurses can consume the richer Terminal contract
while retaining its curses-shaped application facade.

## 2. Accepted mechanisms

DCurses accepts the following Terminal mechanisms:

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

## 4. Alpha-15 automated acceptance

The non-interactive acceptance tests prove that:

1. focus, paste, mouse, and modified-key events arrive through the same
   `CursesSession.ReadEventAsync` stream;
2. mouse coordinates remain normalized to zero-based terminal cells;
3. Terminal key modifiers survive the DCurses translation layer;
4. protocol requests made through DCurses are acquired by Terminal;
5. controlled capability absence remains a controlled unavailable result;
6. disposing the curses session restores protocol state even when a caller has
   not disposed the individual curses protocol lease first.

The Alpha-15 repository builds and tests successfully against the published
`Icod.Terminal 0.2.0-alpha.6` package.

Terminal's own lifecycle tests remain the authority for the protocol-manager
suspend/re-enter ordering; DCurses acceptance verifies that it delegates protocol
ownership rather than creating competing lifecycle state.

## 5. Alpha-16 interactive acceptance

`Icod.DCurses.Input.Showcase` is the live acceptance consumer for the rich-input
facade. At startup it requests three protocol families independently:

- bracketed paste;
- focus reporting;
- mouse button reporting.

Each request is made through `CursesSession.AcquireInputProtocolsAsync`. A
capability that is unavailable is reported as unavailable and does not prevent
the other protocol families from being exercised.

The showcase displays:

- protocol availability;
- ordinary text and named-key events;
- Shift, Control, and Alt modifier combinations;
- focus gained/lost events;
- paste begin/data/end framing;
- normalized mouse action, button, modifiers, and zero-based coordinates;
- resize and other lifecycle notifications.

No protocol escape string is emitted by the sample or by DCurses itself.

### Manual acceptance checklist

Run:

```text
dotnet run --project samples/Icod.DCurses.Input.Showcase/Icod.DCurses.Input.Showcase.csproj
```

On an interactive terminal, verify the protocol families reported as available:

1. type ordinary and modified keys and confirm semantic key/modifier events;
2. paste text and confirm `Paste Begin`, one or more `Paste Data`, and `Paste End`;
3. click or use the wheel and confirm normalized mouse events;
4. move focus away from the terminal and back and confirm focus events;
5. resize the terminal and confirm the display is repainted;
6. press `Q` to exit and confirm the host terminal is restored normally.

Capability absence is not itself a failure. The acceptance requirement is that
DCurses reports the controlled Terminal result and never installs a private
fallback parser or emitter.

## 6. Gate

The DCurses portion of Terminal T19 is accepted when Alpha-16 builds/tests in CI
and the interactive showcase confirms the available rich-input mechanisms on a
live terminal without leaving protocol or presentation state behind on exit.

After that checkpoint, Terminal T19 may close. DCurses T12 remains open until the
`top`, `slabtop`, and `watch` ProcPs acceptance work proves that no generic
full-screen terminal infrastructure remains necessary in `Icod.ProcPs.Shared`.
