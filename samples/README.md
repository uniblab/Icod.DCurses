# Icod.DCurses Samples

The repository contains three executable samples. They are intentionally separate
so the minimal session lifecycle stays easy to copy without mixing it with the
interactive and input-focused showcases.

All sample projects target `net10.0`.

## Icod.DCurses.Sample

`Icod.DCurses.Sample` is the minimal quick-start demonstration. It opens a
`CursesSession`, writes through the standard screen, refreshes the terminal, waits
for input, repaints after resize, and restores terminal state through asynchronous
disposal.

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

`Icod.DCurses.Input.Showcase` is the live Icod.Terminal 0.2 rich-input acceptance
consumer. It independently requests bracketed paste, focus reporting, and mouse
button reporting through `CursesSession.AcquireInputProtocolsAsync`, then shows
whether each protocol is available for the selected terminal profile.

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

These samples complement, but do not replace, the T12 ProcPs acceptance work.
`top`, `slabtop`, and `watch` remain the real application acceptance vehicles.
