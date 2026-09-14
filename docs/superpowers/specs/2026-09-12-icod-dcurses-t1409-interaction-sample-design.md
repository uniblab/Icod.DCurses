# T1409 Interaction Acceptance Sample Design

**Project:** `Icod.DCurses`  
**Release:** `1.4.0`  
**Tranche:** T1409  
**Branch:** `1.4.0-interaction-routing`  
**Sample project:** `samples/Icod.DCurses.Interaction.Sample`  
**Status:** approved design; implementation pending plan approval

---

## Purpose

T1409 proves that the 1.4 interaction primitives compose into a realistic application without introducing a widget framework, hidden event loop, automatic focus policy, or raw terminal-protocol dependency.

The sample is a teaching and acceptance artifact. It shows an application author how to combine:

- explicit layout recomputation;
- ordinary and panel-associated interaction regions;
- logical focus and deterministic forward/backward traversal;
- region-local and global semantic command bindings;
- mouse hit testing with region-local coordinates;
- retained panel precedence;
- pointer-shape preferences plus explicit lease application;
- terminal resize and repaint handling;
- terminal focus reporting without conflating it with logical application focus.

The sample must consume only public `Icod.DCurses` APIs.

## Why this is a new sample

`Icod.DCurses.Layout.Sample` remains intentionally narrow: it teaches explicit 1.3 layout recomputation after resize. Expanding it into a 1.4 interaction demonstration would blur that purpose and make the simpler layout story harder to follow.

T1409 therefore introduces a new project:

```text
samples/Icod.DCurses.Interaction.Sample/
    Icod.DCurses.Interaction.Sample.csproj
    Program.cs
```

The project is added to `Icod.DCurses.sln` and documented in `samples/README.md`.

## Project contract

The sample project follows repository conventions:

- executable sample;
- GPL-3.0-or-later source/header convention used by existing executable samples;
- `TargetFrameworks` = `net8.0;net9.0;net10.0`;
- `Configurations` = `Debug;Staging;Release`;
- `IsPackable` = `false`;
- project reference only to the repository `Icod.DCurses.csproj`;
- no direct `Icod.Terminal` or `Icod.TermInfo` references;
- no internal APIs, reflection, or friend-assembly access.

The sample must build under the normal solution Staging matrix, which provides compile-time proof that the public DCurses surface is sufficient on all three target frameworks.

## User-visible layout

The application presents four logical areas:

```text
+------------------------------------------------------------+
| header / status                                            |
+----------------------------+-------------------------------+
|                            |                               |
| left pane                  | right pane                    |
|                            |                               |
|                            |                               |
+----------------------------+-------------------------------+
| footer / help                                               |
+------------------------------------------------------------+
```

A retained popup panel can be shown over the body panes:

```text
+------------------------------------------------------------+
| header / status                                            |
+----------------------------+-------------------------------+
|                            |                               |
| left pane          +----------------------+                 |
|                    | retained popup       |                 |
|                    |                      |                 |
|                    +----------------------+                 |
+----------------------------+-------------------------------+
| footer / help                                               |
+------------------------------------------------------------+
```

The popup exists specifically to demonstrate panel-over-ordinary hit precedence and panel-relative interaction coordinates.

## Minimum terminal size

The minimum supported geometry is **64 columns x 16 rows**.

After allocating one header row and two footer/help rows, this leaves thirteen body rows, two usable ordinary panes, and room for a retained popup without degenerate rectangles.

When the terminal is below that minimum, the application must:

1. hide the popup panel if shown;
2. render a simple resize-required message on the standard screen;
3. avoid constructing or applying invalid window/panel bounds;
4. continue receiving lifecycle/input events so the application can recover after the terminal grows;
5. preserve application command/interaction registrations rather than rebuilding the router merely because the terminal became small.

## Geometry model

The application owns layout explicitly.

One `ComputeLayout(...)` routine derives immutable `CursesRectangle` values from current `CursesScreen.Bounds`.

The layout is frozen as follows:

- dock one row from the top for the header/status area;
- dock two rows from the bottom for the footer/help area;
- split the remaining body into left/right panes using `CursesLayout.SplitColumnsProportional(...)` with equal weights;
- center a popup rectangle of **28 columns x 8 rows** within the body, clipping only through the minimum-size fallback rather than creating an undersized popup.

On every accepted resize/repaint boundary:

1. call `session.SynchronizeDimensions()`;
2. evaluate the 64x16 minimum-size rule;
3. recompute rectangles from the current `screen.Bounds`;
4. apply new window bounds with `SetBounds(...)`;
5. apply the popup panel bounds with `SetBounds(...)` when it can be shown;
6. update the application-owned ordinary interaction-region bounds with `SetBounds(...)`;
7. keep the popup interaction region panel-local and set to the full popup content rectangle;
8. invalidate/repaint explicitly.

The sample does not introduce a retained layout tree and does not expect the router to relayout regions automatically.

## Interaction router ownership

The application creates exactly one `CursesInteractionRouter` for the session's materialized `CursesScreen` and owns it for the lifetime of the sample.

The router registers exactly these interaction regions:

- header/status region — non-focusable;
- left pane region — focusable;
- right pane region — focusable;
- footer/help region — non-focusable;
- popup panel-associated region — focusable.

The popup region remains registered for the entire sample lifetime. Panel visibility controls whether it is currently eligible.

All router and region lifetimes are explicit through normal `using`/`Dispose` ownership.

## Focus model

Logical focus is application state routed through DCurses; terminal focus reports remain a separate concept.

The sample demonstrates:

- explicit initial logical focus on the left pane;
- Tab -> `MoveFocus(Forward)`;
- Shift+Tab -> `MoveFocus(Backward)`;
- popup focus when F2 opens the popup;
- deterministic focus repair when Escape hides a focused popup;
- terminal Focused/Unfocused reports update status text only and do not clear or replace `router.FocusedRegion`.

Mouse hits do not mutate logical focus. The sample intentionally leaves click-to-focus absent so the distinction between target routing and focus policy remains visible.

## Command vocabulary and bindings

The sample uses semantic `CursesCommand` identities rather than callbacks registered in the router.

The sample-private command names are frozen as:

```text
focus.next
focus.previous
popup.toggle
escape
left.action
right.action
global.x
quit
```

Bindings are frozen as follows:

- global `Tab` -> `focus.next`;
- global `Shift+Tab` -> `focus.previous`;
- global `F2` -> `popup.toggle`;
- global `Escape` -> `escape`;
- left-pane local character `x` -> `left.action`;
- right-pane local character `r` -> `right.action`;
- global character `x` -> `global.x`;
- global character `q` -> `quit`.

This deliberately binds `x` both locally and globally. When the left pane has logical focus, `left.action` must win. When another focusable region has focus, the same `x` gesture resolves to `global.x`.

The `escape` command is interpreted by application state: when the popup is visible it hides the popup; otherwise it exits the application. This keeps one deterministic binding while demonstrating that command execution policy belongs to the application.

## Event loop

The application owns one explicit event loop:

```text
while running
    draw current application state
    RefreshAsync
    ReadEventAsync
    if input:
        router.Route(input)
        application interprets result
        application mutates its own state
        application explicitly applies focus/pointer changes when desired
    if lifecycle:
        update lifecycle/status state
        on resize/resume repaint boundary:
            synchronize dimensions
            recompute layout
    repeat
```

The sample never starts a second terminal reader and never consumes raw escape sequences.

## Routing behavior demonstrated

### Keyboard/text

The sample passes normalized `CursesInputEvent` values to `router.Route(...)` and handles `CursesInteractionResult` as data.

For a matched command, the application switches on command identity and performs the action itself. For targeted-but-unmatched input, the status line displays the target region and normalized input kind. Unhandled input remains inert.

### Mouse

Mouse events are routed through `router.Route(...)`.

The status area displays:

- the selected region name;
- screen row/column from the original mouse event;
- region-local row/column from `CursesInteractionHit`;
- the hit's pointer-shape preference, if any.

When the popup overlaps a body pane, a mouse event inside the overlap must resolve to the popup region, proving T1403 panel precedence in a real application.

## Pointer-shape policy

Pointer preferences are frozen as:

- header/status -> `Default`;
- left pane -> `Text`;
- right pane -> `Crosshair`;
- footer/help -> `Pointer`;
- popup -> `Move`.

Routing/hit testing only reports the preferred shape. The application owns physical application of that preference.

The sample maintains at most one active `CursesPointerShapeLease` for the shape it most recently chose to apply.

When a routed mouse hit requests a shape different from the currently applied shape:

1. acquire the new shape with `session.AcquirePointerShapeAsync(...)`;
2. only after successful acquisition, dispose the prior DCurses lease;
3. remember the new lease and shape.

When a mouse event has no hit/preferred shape, the application disposes its current DCurses pointer lease and leaves terminal policy authoritative.

On sample exit, any remaining pointer lease is disposed before session disposal through normal structured ownership.

The sample never calls `Icod.Terminal` pointer APIs directly.

## Popup behavior

The popup is a retained `CursesPanel` created once after startup and reused.

Application state controls whether it is visible.

F2 toggles the popup:

- when hidden, show the panel and explicitly focus the popup region;
- when visible, hide the panel and allow existing router focus-repair semantics to select the next eligible region.

Escape behaves as follows:

- when the popup is visible, hide it and keep the application running;
- when the popup is hidden, exit the application.

Hiding the popup does not destroy/recreate its region or any command registrations.

The sample does not implement a modal event loop. The popup is retained application state inside the single normal event loop.

## Rendering/state model

The sample keeps one small sample-local application-state type containing only:

- `Running`;
- `PopupVisible`;
- latest status message;
- latest terminal focus state;
- latest routed target/local-coordinate description;
- current applied pointer-shape observation.

Rendering is deterministic from current state. Drawing code may be split into small helpers for header, panes, popup, and footer, but the sample remains intentionally lightweight and does not introduce widget abstractions.

## Resize and lifecycle behavior

The sample responds to DCurses lifecycle events, not Terminal types.

For `Resize` and `Resumed` lifecycle events:

- call `SynchronizeDimensions()`;
- recompute application layout explicitly;
- apply window/panel/region bounds;
- call `session.Invalidate()` before the next refresh.

For normalized terminal Focused/Unfocused input reports:

- update the header/status observation only;
- leave logical focus entirely controlled by `CursesInteractionRouter`.

For interrupt/termination lifecycle events, exit the event loop through the normal application shutdown path.

The sample contains no custom suspend/resume protocol handling. T1408 already proves DCurses/Terminal lifecycle ownership; T1409 consumes that public behavior only.

## Error handling

The sample remains pedagogical rather than wrapping every call in broad exception handling.

Policy is frozen as:

- startup/open failures propagate normally;
- pointer-shape acquisition failures are caught narrowly, reported in status text, and leave the prior successfully-owned pointer lease in place;
- cancellation/termination exits through the normal session lifecycle path;
- invalid geometry is prevented by the 64x16 minimum-size check and deterministic layout calculation rather than caught after the fact;
- no exception is swallowed silently.

## Files touched by T1409 implementation

Expected implementation scope:

```text
Icod.DCurses.sln
samples/README.md
samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj
samples/Icod.DCurses.Interaction.Sample/Program.cs
docs/T1409-Interaction-Acceptance-Sample.md
```

One additional sample-local `InteractionSampleState.cs` file is permitted if keeping the state type separate materially improves readability; no other helper project or framework layer is permitted.

No library production source file should change during T1409 unless the sample exposes a concrete public-API defect. Any such defect upgrades the work from sample implementation to a separately documented correction and must be requalified before T1409 can close.

## Automated qualification

T1409 qualification requires:

1. the sample project is part of `Icod.DCurses.sln`;
2. the sample builds successfully for `net8.0`, `net9.0`, and `net10.0` under Staging;
3. the existing full test suite remains green;
4. package candidate remains green;
5. Windows/Linux/macOS x64/ARM64 PR jobs remain green;
6. the 1.4 public API fingerprint remains exactly **62 exported types / 491 contract lines / SHA-256 `8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147`**;
7. `samples/README.md` documents how to run the sample and what 1.4 contracts it demonstrates.

## Manual acceptance checklist

A human run of the sample must demonstrate all of the following in one session:

- left/right logical focus is visibly distinguishable;
- Tab and Shift+Tab traverse focus deterministically;
- left-pane local `x` wins over global `x` while the left region is focused;
- global `x` works while the right or popup region has focus;
- F2 shows/hides the popup without a second event loop;
- the popup wins mouse routing when overlapping a body pane;
- mouse status reports region-local coordinates;
- pointer-shape preference changes are applied explicitly by the application;
- terminal resize triggers explicit recomputation of window, panel, and interaction geometry;
- shrinking below 64x16 does not destroy router/binding state;
- growing back restores the normal application layout;
- terminal focus reporting does not erase logical focus;
- Escape closes the popup before it quits the application;
- quit/termination cleans up the pointer lease and session normally.

## Non-goals

T1409 must not introduce:

- a widget/control hierarchy;
- callback registration for commands;
- automatic focus-on-click policy in the library;
- automatic layout ownership;
- modal nested event loops;
- raw `Icod.Terminal` protocol APIs in the sample;
- pointer capture or drag/drop semantics;
- accessibility framework ownership;
- generalized application navigation;
- raster graphics.

## Exit criterion

T1409 is complete when the approved sample exists, uses only public DCurses APIs, is documented, demonstrates the listed behaviors, and its exact documentation-complete head passes the repository's full PR package/runtime matrix without changing the accepted 1.4 public API contract.
