# Icod.DCurses Color Sample

This public-only sample demonstrates independent foreground and background colors through retained cells, an opaque panel, styled blank padding, a border-like region, attributes, and a width-two grapheme. It reports `CursesPresentationCapabilities` and selects a conservative terminal-default, indexed, or direct-RGB request palette without inspecting terminal identity or emitting escape sequences.

Run from the repository root:

```text
dotnet run --project samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj -c Release -f net10.0
```

Press `Q` or `Escape` to exit. Resize causes dimension synchronization, explicit application repaint, and physical-state invalidation.

Visible fidelity depends on terminal capabilities and user configuration. The status line identifies the requested palette class; it does not claim that the host displayed a color the profile cannot safely represent. On constrained or monochrome profiles the sample remains usable with terminal-default colors and labels. `Reverse` is shown as an attribute, not as a promise that every host will produce the same visual colors.

The panel is intentionally `Opaque`: its styled blank cells own the visible background. `BlankCellsTransparent` is not appropriate for a surface whose blank padding carries color.

## Manual observation record

Record live observations separately from automated qualification:

```text
OS and version:
Terminal and version:
Architecture:
.NET runtime:
Commit:
Reported capabilities:
Requested palette class:
Observed default/indexed/RGB pairs:
Observed foreground-only/background-only changes:
Observed opaque padding/border/width-two cell:
Resize/repaint result:
Limitations or unverified behavior:
```
