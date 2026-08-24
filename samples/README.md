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

`Icod.DCurses.Input.Showcase` is an interactive keyboard and event inspector. It
shows exactly what the current DCurses decoder produced for each input event,
including the semantic key, Unicode scalar value, modifier flags, and function-key
number. Lifecycle notifications are reported alongside keyboard input.

This is deliberately an observation tool rather than a modifier emulator. For
example, a terminal may currently expose `Alt+R` as an Escape event followed by
ordinary `R`, or a modified function-key sequence as a numbered function key
without a separate Shift modifier. The sample records those events as DCurses
actually decoded them.

Useful keys to try:

```text
Shift+Tab
Ctrl+R
F1 through F12
Shift+F7
Alt+R
Escape
```

`Q` exits the inspector. Escape deliberately does not exit because Escape itself,
and Escape-prefixed input, are useful decoder observations.

```text
dotnet run --project samples/Icod.DCurses.Input.Showcase/Icod.DCurses.Input.Showcase.csproj
```

These samples complement, but do not replace, the T12 ProcPs acceptance work.
`top`, `slabtop`, and `watch` remain the real application acceptance vehicles.
