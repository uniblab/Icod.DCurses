# Icod.DCurses Samples

The repository contains eleven executable samples. They are intentionally separate so the minimal session lifecycle stays easy to copy without mixing it with the interactive and acceptance-focused showcases.

All sample projects target `net8.0`, `net9.0`, and `net10.0` and consume the repository `Icod.DCurses` project. Icod.DCurses 2.0 declares only `Icod.Terminal 1.18.0` directly. Terminal may restore TermInfo transitively. The previous 1.6 package keeps its historical direct dependency set. To migrate external applications, see [the 2.0 migration guide](../docs/2.0-Migration-Guide.md).

## Which sample should I run?

| Goal | Sample |
| --- | --- |
| Minimal session lifecycle and retained drawing | `Icod.DCurses.Sample` |
| Retained panels, z-order, transparency, and disposal | `Icod.DCurses.Panel.Sample` |
| Explicit geometry/layout and resize recomputation | `Icod.DCurses.Layout.Sample` |
| Retained text + hyperlink metadata + raster presentation, panning, panels, and interaction geometry | `Icod.DCurses.MixedMedia.Sample` |
| Virtualized large world, sparse movement, track layout, and retained help overlay | `Icod.DCurses.Roguelike.Sample` |
| Interaction scopes, capture, spatial focus, gestures, commands, and pointer preferences | `Icod.DCurses.Interaction.Sample` |
| General interactive API showcase | `Icod.DCurses.Showcase` |
| Semantic input inspection | `Icod.DCurses.Input.Showcase` |
| Application-shaped periodic-command acceptance | `Icod.DCurses.Watch.Acceptance` |
| Application-shaped slab-table acceptance | `Icod.DCurses.Slabtop.Acceptance` |
| Larger application-shaped multi-window acceptance | `Icod.DCurses.Top.Acceptance` |

## Ownership model

The samples follow the production ownership model:

- logical `CursesScreen`, `CursesWindow`, `CursesPad`, `CursesPadViewport`, and `CursesPanel` mutation is single-writer unless an API explicitly documents otherwise;
- one `CursesSession` event wait may coexist with refresh/output activity;
- applications should not create competing independent event-reader loops over one session;
- terminal-mutating presentation, protocol, refresh, cursor, alert, suspend, raster ownership, and disposal activity remains serialized by DCurses/Terminal;
- Terminal remains responsible for authoritative terminal restoration and live raster identity/lifecycle;
- canceling one public input wait does not discard Terminal decoder state.

Run the commands below from the repository root. The examples select `net10.0` explicitly because each project targets multiple frameworks; substitute `net8.0` or `net9.0` as needed.

## Icod.DCurses.Sample

`Icod.DCurses.Sample` is the minimal quick-start demonstration. It opens a `CursesSession`, reads its Terminal-owned `Profile` and `GetDimensions()` result, writes styled retained content, demonstrates one retained hyperlink, updates a small moving marker, repaints after resize, accepts input, and restores terminal state through asynchronous disposal. An unavailable live-size result is displayed without replacing the logical screen dimensions.

```text
dotnet run --project samples/Icod.DCurses.Sample/Icod.DCurses.Sample.csproj --framework net10.0
```

## Icod.DCurses.Panel.Sample

`Icod.DCurses.Panel.Sample` is the focused 1.2 panel demonstration. It keeps base-screen content retained while an independent panel is shown, hidden, moved, switched to blank-cell transparency, and finally disposed. Panel content is edited through the ordinary `CursesWindow` API and no Terminal protocol output is emitted directly by the sample.

```text
dotnet run --project samples/Icod.DCurses.Panel.Sample/Icod.DCurses.Panel.Sample.csproj --framework net10.0
```

Press a key between each stage to observe the retained content beneath the panel. Disposal permanently removes the panel from the owning screen; hide/show remains the reversible visibility mechanism.

## Icod.DCurses.Layout.Sample

`Icod.DCurses.Layout.Sample` demonstrates the 1.3 explicit resize/recomputation model. It derives a header region, body region, and retained side panel from `session.Screen.Bounds`, then reapplies those rectangles with `CursesWindow.SetBounds(...)` and `CursesPanel.SetBounds(...)` after resize lifecycle repaint requests. No retained layout tree or automatic application-layout owner is introduced.

```text
dotnet run --project samples/Icod.DCurses.Layout.Sample/Icod.DCurses.Layout.Sample.csproj --framework net10.0
```

Resize the terminal while the sample is running to see the two windows and retained panel recompute from the new screen bounds. Press `Q` or `Escape` to exit.

## Icod.DCurses.Roguelike.Sample

`Icod.DCurses.Roguelike.Sample` is the 2.1 application acceptance sample. It generates a ten-million-row world by coordinate without allocating a world-sized grid or pad. The application owns player movement, terrain and four recent messages. `CursesViewport` locates the visible slice, `CursesLayout.ArrangeRows`/`ArrangeColumns` divide the current screen, prepared `WriteCells` draws an initial map, and movement within the same viewport updates only the old and new player cells. A retained `CursesPanel` overlays help without changing the base map. Resize explicitly recomputes rectangles; opt-in refresh diagnostics report the previous attempt's prepared item count in the status row.

Run from the repository root on an interactive terminal:

```text
dotnet run --project samples/Icod.DCurses.Roguelike.Sample/Icod.DCurses.Roguelike.Sample.csproj --framework net10.0
```

Use arrows or WASD to move, `?` to show or hide help, and `Q` or `Escape` to exit. The minimum supported terminal size is 30 columns by 6 rows; resize above that limit to resume the map. This is application code using public DCurses APIs, not a game engine or a terminal protocol implementation.

## Icod.DCurses.MixedMedia.Sample

`Icod.DCurses.MixedMedia.Sample` demonstrates the retained mixed-media presentation introduced in 1.6 and carried forward in 2.0. It constructs a backend-neutral `TerminalRasterImage`, asks the owning `CursesSession` to create a raster resource and placeholder, retains placeholder cells inside a `CursesPad`, writes retained hyperlink metadata into that same pad, projects the combined retained state through a pannable `CursesPadViewport`, composes an independent blank-transparent `CursesPanel`, and registers interaction regions over the same logical geometry.

The sample deliberately keeps the three retained presentation axes visible together:

- ordinary text/cell content;
- terminal-independent semantic metadata (`CursesHyperlink` through `WriteWithMetadata`); and
- session-bound raster placeholder cells.

The layer boundary remains explicit:

- the application owns source-image bytes, logical meaning, hyperlink destination, and the decision to request raster presentation;
- DCurses owns retained coordinates, metadata/raster coexistence, pad/viewport projection, panel composition, damage, and refresh;
- Terminal owns live raster identity, acknowledgement, encoding, lifecycle, and protocol output;
- `TerminalRasterImage` is the only raster input type intentionally exposed through the DCurses 1.6 public boundary.

Resource or placeholder creation may report unavailable. The sample reports that condition and continues with ordinary text/metadata/panel presentation; it does not infer a backend from terminal identity, emit raw Kitty/Sixel commands, rank hidden fallbacks, retain a source-image cache for replay, or silently switch protocols. When raster ownership is available, the second frame pans the same retained mixed-media pad to exercise damage-driven sparse projection and refresh.

This sample never asks TermInfo to select a backend. Icod.DCurses 2.0 uses the Terminal-only direct production boundary; raster availability and resource ownership still come from the live Terminal session. The previous 1.6 package retains its original dependency architecture.

Press any input key after the first frame to pan the retained content one column, then press another input key to exit. End-of-input, interrupt, and termination events exit cleanly from either wait.

```text
dotnet run --project samples/Icod.DCurses.MixedMedia.Sample/Icod.DCurses.MixedMedia.Sample.csproj --framework net10.0
```

## Icod.DCurses.Interaction.Sample

`Icod.DCurses.Interaction.Sample` is the 1.5 advanced-interaction acceptance sample. It composes only public DCurses APIs inside one application-owned event loop: ordinary root interaction regions, a modal popup scope, a nested scope, sequential and spatial focus, scoped/global command identities, explicit pointer capture, clock-free pointer gesture snapshots, pointer-shape preferences, retained panel movement, and resize handling. It does not use direct `Icod.Terminal` or `Icod.TermInfo` APIs, internal DCurses APIs, callback-driven command execution, a widget framework, automatic layout ownership, or a second input reader.

Controls:

```text
Tab / Shift+Tab    Move logical focus forward/backward
Arrow keys         Move logical focus with deterministic spatial focus
F2                 Open/close the retained popup and its modal interaction scope
S                  Resolve the outer popup-scope command while that boundary is visible
N                  Enter/leave the nested scope; entering it excludes the outer S binding
x                  Left-local action while the left pane has focus; otherwise global x
r                  Right-pane local action
Primary mouse drag Drag the popup using explicit pointer capture and normalized gestures
Escape             Close the popup first; otherwise exit
q                  Exit
```

Opening the popup first shows the retained panel and then activates its explicit interaction scope. The popup region belongs to a child scope, so it remains eligible while the outer popup scope is active. Pressing `N` activates that nested scope; pressing it again disposes the nested lease and restores the saved popup focus. Closing the outer scope restores the previously saved eligible root focus. The `S` and `N` bindings demonstrate the scope-command boundary: the outer `S` binding resolves while the outer scope is active, but does not leak inward while the nested scope itself is the active modal boundary.

The arrow-key commands call `MoveFocus(Up/Down/Left/Right)` and therefore demonstrate spatial focus independently of the existing Tab/Shift+Tab sequential traversal. Mouse routing itself still does not change logical focus automatically.

On a primary-button press over the popup, the application explicitly acquires `CursesPointerCaptureLease`. Captured moves keep targeting that region even when the pointer leaves its bounds and expose signed `CursesPointerTarget` coordinates. The first moved cell normalizes to `DragStart`, subsequent moves to `DragMove`, and release to `DragEnd`; matching release automatically ends router capture, after which disposing the stale lease remains safe. The sample chooses to move the retained popup in response to those results.

That split is intentional: DCurses provides the **mechanism**—scope boundaries, focus decisions, capture targets, gesture classification, and command identity—while the application owns **policy**, such as whether a drag should move a panel or what a command should do. No callback dispatcher, hidden event loop, automatic focus-on-click rule, or drag/drop policy is introduced.

The left-pane local `x` binding deliberately shadows the router-global `x` binding, preserving the 1.4 focused local-command precedence. The retained popup still wins ordinary mouse routing through panel z-order. Pointer-shape preferences remain explicit application behavior through `CursesPointerShapeLease`; routing itself performs no terminal I/O.

Mouse, focus, keyboard-protocol behavior, and visible pointer-shape changes ultimately depend on terminal support. A terminal that does not visibly change the pointer shape can still be routing mouse hits, capture, gestures, scopes, and commands correctly; the sample's routed target/local-coordinate and gesture status is the relevant routing evidence.

Resize handling remains application-owned: the sample synchronizes terminal dimensions, recomputes header/footer and equal left/right pane rectangles, reapplies window/panel/interaction bounds, and repaints. If the terminal falls below `64x16`, any live popup scope/capture is closed in LIFO order before geometry becomes unavailable. Growing the terminal restores the ordinary root layout. Terminal focus reports remain distinct from logical `CursesInteractionRouter` focus.

```text
dotnet run --project samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj --framework net10.0
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
dotnet run --project samples/Icod.DCurses.Showcase/Icod.DCurses.Showcase.csproj --framework net10.0
```

## Icod.DCurses.Input.Showcase

`Icod.DCurses.Input.Showcase` is the live rich-input inspector. It demonstrates Terminal-owned keyboard event-type reporting, bracketed paste, focus reporting, mouse reporting, modifiers, function keys, character identities, associated text, lifecycle notifications, and the ordinary `CursesSession.ReadEventAsync` stream.

The showcase never installs a private keyboard parser, mouse parser, paste reader, protocol escape emitter, or fallback negotiation path.

```text
dotnet run --project samples/Icod.DCurses.Input.Showcase/Icod.DCurses.Input.Showcase.csproj --framework net10.0
```

## Icod.DCurses.Watch.Acceptance

`Icod.DCurses.Watch.Acceptance` is an application-shaped acceptance harness using synthetic already-interpreted child-output snapshots so it exercises DCurses rather than becoming a process runner or ANSI parser.

It covers periodic retained refresh, immediate refresh, resize/resume repaint, title/no-title layouts, wrap/clip selection, semantic styles, failure alerts, and paused presentation.

Controls: `Space` samples immediately; `T` toggles the title; `W` toggles wrap/clip; `C` toggles color interpretation; `B` toggles failure alerts; `P` pauses/resumes periodic presentation; `F` selects a failing snapshot; `Q` exits.

```text
dotnet run --project samples/Icod.DCurses.Watch.Acceptance/Icod.DCurses.Watch.Acceptance.csproj --framework net10.0
```

## Icod.DCurses.Slabtop.Acceptance

`Icod.DCurses.Slabtop.Acceptance` uses synthetic slab-cache snapshots to exercise periodic refresh, resize/resume repaint, all documented sort keys, styled summaries/tables, and retained terminal output without taking `/proc/slabinfo` observation into DCurses.

Controls: `A/B/C/L/V/N/O/P/S/U` select the documented sort columns, `Space` samples immediately, and `Q` exits.

```text
dotnet run --project samples/Icod.DCurses.Slabtop.Acceptance/Icod.DCurses.Slabtop.Acceptance.csproj --framework net10.0
```

## Icod.DCurses.Top.Acceptance

`Icod.DCurses.Top.Acceptance` is the largest application-shaped harness. It covers multiple windows, rapid retained refresh, navigation, focus-like application policy, help/prompt views, styling, resize-driven relayout, and cursor presentation.

Controls: `P/M/N/T` select CPU/memory/PID/time sorting; arrow keys, `PageUp`, `PageDown`, `Home`, and `End` navigate; left/right also scroll horizontally; `Tab` changes focus; `C` toggles command display; `D` or `S` edits the refresh delay; `h` or `?` opens help; `Ctrl+L` invalidates physical-screen state; `Q` exits.

```text
dotnet run --project samples/Icod.DCurses.Top.Acceptance/Icod.DCurses.Top.Acceptance.csproj --framework net10.0
```

These samples complement, but do not replace, package-only consumer validation and application-specific downstream acceptance.
