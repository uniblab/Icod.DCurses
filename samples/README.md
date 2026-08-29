# Icod.DCurses Samples

The repository contains six executable samples. They are intentionally separate
so the minimal session lifecycle stays easy to copy without mixing it with the
interactive and acceptance-focused showcases.

All sample projects target `net10.0`.

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

`Icod.DCurses.Input.Showcase` originated as the live Icod.Terminal 0.2 rich-input
acceptance consumer and remains the 0.1 rich-input showcase on
`Icod.Terminal 0.3.0`. It independently requests bracketed paste, focus
reporting, and mouse button reporting through
`CursesSession.AcquireInputProtocolsAsync`, then shows whether each protocol is
available for the selected terminal profile.

All input still arrives through the ordinary `CursesSession.ReadEventAsync`
stream. The showcase reports:

- ordinary text and named keys;
- Shift, Control, and Alt modifier combinations;
- numbered function keys;
- normalized mouse action/button/modifier data and zero-based cell coordinates;
- focus gained/lost events;
- bracketed-paste begin/data/end framing;
- lifecycle notifications such as resize.

Useful interactions to try:

```text
Shift+Tab / Ctrl+R / Shift+F7
Paste several lines of text
Click and use the mouse wheel
Move focus away from the terminal and back
Resize the terminal
Escape
```

`Q` exits the inspector. Escape deliberately does not exit because Escape itself,
and Escape-prefixed input, remain useful decoder observations.

A protocol reported as unavailable is not automatically a failure. DCurses uses
the controlled result from `Icod.Terminal`; the showcase does not install a
private parser or hard-coded fallback sequence for an unavailable protocol.

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
