# Icod.DCurses Samples

The repository contains six executable samples. They are intentionally separate
so the minimal session lifecycle stays easy to copy without mixing it with the
interactive and acceptance-focused showcases.

All sample projects target `net8.0`, `net9.0`, and `net10.0` and consume the
repository `Icod.DCurses` project, which currently uses `Icod.Terminal 1.6.0` and
`Icod.TermInfo 1.10.0`.

## 0.8 concurrency and lifetime contract

The samples follow the production-hardening ownership model frozen for 0.8:

- logical `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport`
  mutation is single-writer unless an API explicitly documents otherwise;
- one `CursesSession` event wait may coexist with refresh/output activity;
- applications should not create competing independent `ReadEventAsync(...)`
  and `ReadLifecycleEventAsync(...)` reader loops over the same session;
- terminal-mutating presentation, protocol, refresh, cursor, alert, suspend, and
  disposal activity remains serialized by DCurses/Terminal;
- disposing a `CursesSession` prevents new terminal activity and unblocks
  pending DCurses input/lifecycle waits while Terminal remains responsible for
  authoritative terminal restoration;
- canceling one public input wait does not discard Terminal's underlying decoder
  state or a fragmented input sequence.

The samples therefore use one ordinary application event loop and do not add a
second byte reader, per-cell locking, or a private terminal scheduler.

## Icod.DCurses.Sample

`Icod.DCurses.Sample` is the minimal quick-start demonstration. It opens a
`CursesSession`, writes styled content through the standard screen, updates a
small moving marker through timed event waits, repaints after resize, accepts
input, and restores terminal state through asynchronous disposal.

```text
dotnet run --project samples/Icod.DCurses.Sample/Icod.DCurses.Sample.csproj
```

Press any key to exit.

## Icod.DCurses.Showcase

`Icod.DCurses.Showcase` is the interactive 0.1 API demonstration. It exercises a
timed event loop, retained refreshes, resize repainting, named keys, Unicode cell
widths, alert fallback, cursor presentation, and explicit physical-screen
invalidation.

Controls:

```text
Arrow keys   Move the @ marker
B            Request an audible alert, with visual fallback
C            Cycle physical cursor visibility
I            Invalidate retained physical-screen knowledge
Space        Request an immediate refresh
Q / Escape   Exit
```

```text
dotnet run --project samples/Icod.DCurses.Showcase/Icod.DCurses.Showcase.csproj
```

The Unicode row includes ASCII, precomposed and combining text, a
supplementary-plane scalar value, and a known two-column character. It is a visual
demonstration rather than a Unicode-conformance test.

## Icod.DCurses.Input.Showcase

`Icod.DCurses.Input.Showcase` is the live rich-input inspector for the current
`Icod.Terminal 1.6.0` / `Icod.TermInfo 1.10.0` baseline. During the `0.2.0`
development line it also demonstrates the expanded Terminal 1.0 keyboard
semantics carried through the curses facade.

At startup the showcase independently requests:

- keyboard event-type reporting;
- bracketed-paste reporting;
- focus reporting;
- mouse button reporting.

Keyboard reporting uses `CursesKeyboardReportingMode.EventTypes` rather than
`AllKeys` so a supporting terminal can report Press/Repeat/Release information
without forcing every ordinary text-producing key into a key-event escape
sequence. Every protocol request remains optional. Unsupported requests are
reported as controlled unavailable results and do not prevent the remaining
showcase from running.

All input arrives through the ordinary `CursesSession.ReadEventAsync` stream.
The inspector displays:

- ordinary text and the complete semantic key vocabulary;
- Press, Repeat, and Release key phases where reported;
- Shift, Control, Alt, Super, Hyper, Meta, CapsLock, and NumLock modifier state;
- numbered function keys;
- character, shifted-layout character, and base-layout character identities;
- associated text supplied by modern keyboard protocols;
- normalized mouse action/button/modifier data and zero-based cell coordinates;
- focus gained/lost events;
- bracketed-paste begin/data/end framing;
- lifecycle notifications such as resize.

Useful interactions to try depend on the active terminal and negotiated keyboard
protocol. Representative examples include:

```text
Shift+Tab / Ctrl+R / Shift+F7
Keypad and media keys when the host reports them
Modifier-key combinations including Super/Meta where supported
Paste several lines of text
Click and use the mouse wheel
Move focus away from the terminal and back
Resize the terminal
Escape
```

`Q` exits the inspector whether it arrives as ordinary text or as a semantic
Character key event. Escape deliberately does not exit because Escape itself,
and Escape-prefixed input, remain useful decoder observations.

The showcase never installs a private keyboard parser, mouse parser, paste
reader, protocol escape emitter, or fallback negotiation path. All byte-stream
decoding and protocol lifecycle remain owned by `Icod.Terminal`.

```text
dotnet run --project samples/Icod.DCurses.Input.Showcase/Icod.DCurses.Input.Showcase.csproj
```

## Icod.DCurses.Watch.Acceptance

`Icod.DCurses.Watch.Acceptance` is the first T12 application-shaped acceptance
harness. It deliberately uses synthetic, already-interpreted child-output
snapshots so the sample exercises DCurses mechanisms rather than becoming a
process runner or ANSI parser.

The harness demonstrates:

- periodic refresh through timed `ReadEventAsync` waits without busy polling;
- Space-triggered immediate refresh;
- resize/resume invalidation and repaint;
- title/no-title layouts;
- application-selected wrap or clip behavior;
- reverse-video highlighting for changed output lines;
- semantic child colors represented through `CursesStyle`;
- optional alert-on-command-failure behavior;
- a paused/preserved presentation that remains unchanged across timer ticks.

Controls:

```text
Space        Immediate refresh
T            Toggle title/no-title
W            Toggle wrap/clip
C            Toggle interpreted child colors
B            Toggle alert on failure
P            Pause/resume while preserving the current presentation
F            Jump directly to a simulated failed command result
Q            Exit
```

```text
dotnet run --project samples/Icod.DCurses.Watch.Acceptance/Icod.DCurses.Watch.Acceptance.csproj
```

## Icod.DCurses.Slabtop.Acceptance

`Icod.DCurses.Slabtop.Acceptance` is the second T12 application-shaped acceptance
harness. Synthetic slab-cache snapshots keep Linux `/proc/slabinfo` observation
and slab policy outside DCurses while exercising the live screen/input contract.

The harness demonstrates:

- periodic resampling through timed `ReadEventAsync` waits;
- Space-triggered immediate resampling;
- resize/resume repaint of the current snapshot without another sample;
- all ten documented sort keys: `a`, `b`, `c`, `l`, `v`, `n`, `o`, `p`, `s`,
  and `u`;
- semantic summary/table styling through `CursesStyle`;
- retained refresh so unchanged cells need not be rewritten physically;
- `q`/`Q` exit and session-owned terminal restoration.

```text
dotnet run --project samples/Icod.DCurses.Slabtop.Acceptance/Icod.DCurses.Slabtop.Acceptance.csproj
```

## Icod.DCurses.Top.Acceptance

`Icod.DCurses.Top.Acceptance` is the third and largest T12 application-shaped
acceptance harness. Synthetic task snapshots keep process observation and command
policy outside DCurses while exercising the full interactive screen contract.

The harness demonstrates:

- separate summary, task, task-header/body, and status windows;
- rapid retained refresh without application-owned terminal clearing;
- Enter/Space immediate refresh;
- Up/Down, Page Up/Page Down, Home/End, Left/Right navigation;
- Tab and Shift+Tab focus traversal plus Ctrl+L invalidation;
- ordinary command keys and semantic sort changes;
- a logical help view and an in-screen editable delay prompt;
- bold, reverse, underline, standout, foreground, and background styles;
- resize-driven complete relayout and lifecycle repaint;
- physical cursor show/position during the prompt and hide afterward.

```text
dotnet run --project samples/Icod.DCurses.Top.Acceptance/Icod.DCurses.Top.Acceptance.csproj
```

These samples complement, but do not replace, final validation in the actual
ProcPs applications. Alpha-17, Alpha-18, and Alpha-19 provide the focused
`watch`, `slabtop`, and `top` checkpoints before the migrated commands are used
as final acceptance vehicles.
