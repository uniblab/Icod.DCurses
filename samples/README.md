# Icod.DCurses Samples

The repository contains nine executable samples. They are intentionally separate so the minimal session lifecycle stays easy to copy without mixing it with the interactive and acceptance-focused showcases.

All sample projects target `net8.0`, `net9.0`, and `net10.0` and consume the repository `Icod.DCurses` project, which currently declares `Icod.Terminal 1.13.0` and `Icod.TermInfo 1.12.0`.

## Which sample should I run?

| Goal | Sample |
| --- | --- |
| Minimal session lifecycle and retained drawing | `Icod.DCurses.Sample` |
| Retained panels, z-order, transparency, and disposal | `Icod.DCurses.Panel.Sample` |
| Explicit geometry/layout and resize recomputation | `Icod.DCurses.Layout.Sample` |
| Interaction regions, logical focus, commands, mouse routing, and pointer preferences | `Icod.DCurses.Interaction.Sample` |
| General interactive API showcase | `Icod.DCurses.Showcase` |
| Raw semantic input inspection | `Icod.DCurses.Input.Showcase` |
| Application-shaped periodic-command acceptance | `Icod.DCurses.Watch.Acceptance` |
| Application-shaped slab-table acceptance | `Icod.DCurses.Slabtop.Acceptance` |
| Larger application-shaped multi-window acceptance | `Icod.DCurses.Top.Acceptance` |

## Ownership model

The samples follow the production ownership model:

- logical `CursesScreen`, `CursesWindow`, `CursesPad`, `CursesPadViewport`, and `CursesPanel` mutation is single-writer unless an API explicitly documents otherwise;
- one `CursesSession` event wait may coexist with refresh/output activity;
- applications should not create competing independent event-reader loops over one session;
- terminal-mutating presentation, protocol, refresh, cursor, alert, suspend, and disposal activity remains serialized by DCurses/Terminal;
- Terminal remains responsible for authoritative terminal restoration;
- canceling one public input wait does not discard Terminal decoder state.

## Icod.DCurses.Sample

`Icod.DCurses.Sample` is the minimal quick-start demonstration. It opens a `CursesSession`, writes styled retained content, demonstrates one retained hyperlink, updates a small moving marker, repaints after resize, accepts input, and restores terminal state through asynchronous disposal.

```text
dotnet run --project samples/Icod.DCurses.Sample/Icod.DCurses.Sample.csproj
```

## Icod.DCurses.Panel.Sample

`Icod.DCurses.Panel.Sample` is the focused 1.2 panel demonstration. It keeps base-screen content retained while an independent panel is shown, hidden, moved, switched to blank-cell transparency, and finally disposed. Panel content is edited through the ordinary `CursesWindow` API and no Terminal protocol output is emitted directly by the sample.

```text
dotnet run --project samples/Icod.DCurses.Panel.Sample/Icod.DCurses.Panel.Sample.csproj
```

Press a key between each stage to observe the retained content beneath the panel. Disposal permanently removes the panel from the owning screen; hide/show remains the reversible visibility mechanism.

## Icod.DCurses.Layout.Sample

`Icod.DCurses.Layout.Sample` demonstrates the 1.3 explicit resize/recomputation model. It derives a header region, body region, and retained side panel from `session.Screen.Bounds`, then reapplies those rectangles with `CursesWindow.SetBounds(...)` and `CursesPanel.SetBounds(...)` after resize lifecycle repaint requests. No retained layout tree or automatic application-layout owner is introduced.

```text
dotnet run --project samples/Icod.DCurses.Layout.Sample/Icod.DCurses.Layout.Sample.csproj
```

Resize the terminal while the sample is running to see the two windows and retained panel recompute from the new screen bounds. Press `Q` or `Escape` to exit.

## Icod.DCurses.Interaction.Sample

`Icod.DCurses.Interaction.Sample` is the 1.4 interaction-routing acceptance sample. It composes public DCurses geometry, retained panels, focus traversal, semantic command bindings, mouse hit testing, pointer-shape preferences, and resize handling inside one application-owned event loop. The sample does not use a widget framework, direct Terminal APIs, callback-driven command execution, automatic layout ownership, or a second input reader.

Controls:

```text
Tab          Move logical focus forward
Shift+Tab    Move logical focus backward
F2           Show/hide the retained popup; showing it explicitly focuses the popup
x            Left-local action while the left pane has focus; otherwise global x
r            Right-pane local action
Escape       Close the popup first; otherwise exit
q            Exit
Mouse        Report screen and region-local coordinates and apply the routed pointer preference
```

The left-pane local `x` binding deliberately shadows the router-global `x` binding, demonstrating focused local-command precedence. The retained popup overlaps the body panes and wins mouse routing through the normal panel z-order rules. Mouse routing itself does not change logical focus.

The application explicitly applies pointer-shape preferences with `CursesPointerShapeLease` rather than performing terminal I/O inside hit testing or routing. It also acquires keyboard event types, focus reporting, and mouse button events through DCurses input-protocol leases only.

Mouse, focus, keyboard-protocol behavior, and visible pointer-shape changes ultimately depend on terminal support. A terminal that does not visibly change the pointer shape can still be routing mouse hits and commands correctly; the sample's routed target/local-coordinate status is the relevant routing evidence.

Resize handling remains application-owned: the sample synchronizes the current terminal dimensions, recomputes header/footer and equal left/right pane rectangles, reapplies window/panel/interaction bounds, and repaints. If the terminal falls below `64x16`, retained router/binding identity survives while the application displays a resize message; growing the terminal restores the normal layout. Terminal focus reports update status text but remain distinct from logical `CursesInteractionRouter` focus.

```text
dotnet run --project samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj
```

## Icod.DCurses.Showcase

`Icod.DCurses.Showcase` is the interactive API demonstration. It exercises a timed event loop, retained refreshes, resize repainting, named keys, Unicode cell widths, alert fallback, cursor presentation, and explicit physical-screen invalidation.

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

## Icod.DCurses.Input.Showcase

`Icod.DCurses.Input.Showcase` is the live rich-input inspector. It demonstrates Terminal-owned keyboard event-type reporting, bracketed paste, focus reporting, mouse reporting, modifiers, function keys, character identities, associated text, lifecycle notifications, and the ordinary `CursesSession.ReadEventAsync` stream.

The showcase never installs a private keyboard parser, mouse parser, paste reader, protocol escape emitter, or fallback negotiation path.

```text
dotnet run --project samples/Icod.DCurses.Input.Showcase/Icod.DCurses.Input.Showcase.csproj
```

## Icod.DCurses.Watch.Acceptance

`Icod.DCurses.Watch.Acceptance` is an application-shaped acceptance harness using synthetic already-interpreted child-output snapshots so it exercises DCurses rather than becoming a process runner or ANSI parser.

It covers periodic retained refresh, immediate refresh, resize/resume repaint, title/no-title layouts, wrap/clip selection, semantic styles, failure alerts, and paused presentation.

```text
dotnet run --project samples/Icod.DCurses.Watch.Acceptance/Icod.DCurses.Watch.Acceptance.csproj
```

## Icod.DCurses.Slabtop.Acceptance

`Icod.DCurses.Slabtop.Acceptance` uses synthetic slab-cache snapshots to exercise periodic refresh, resize/resume repaint, all documented sort keys, styled summaries/tables, and retained terminal output without taking `/proc/slabinfo` observation into DCurses.

```text
dotnet run --project samples/Icod.DCurses.Slabtop.Acceptance/Icod.DCurses.Slabtop.Acceptance.csproj
```

## Icod.DCurses.Top.Acceptance

`Icod.DCurses.Top.Acceptance` is the largest application-shaped harness. It covers multiple windows, rapid retained refresh, navigation, focus-like application policy, help/prompt views, styling, resize-driven relayout, and cursor presentation.

```text
dotnet run --project samples/Icod.DCurses.Top.Acceptance/Icod.DCurses.Top.Acceptance.csproj
```

These samples complement, but do not replace, package-only consumer validation and application-specific downstream acceptance.
