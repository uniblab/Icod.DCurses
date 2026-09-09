# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Stable compatibility floor:** `1.0.0`  
**Active development package:** `1.1.0-alpha.1`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Planning status:** approved release train; 1.1 implementation active

---

## 1. Purpose

The 1.0 release established a stable curses-style terminal UI substrate. The next four minor releases grow that substrate upward into semantic content, composition, layout, and interaction without taking raw terminal-protocol ownership away from `Icod.Terminal` and without prematurely turning the core into a widget toolkit.

The approved release sequence is:

```text
1.1.0  semantic cell metadata + hyperlinks
1.2.0  panels/layers + z-order composition
1.3.0  layout + resize primitives
1.4.0  focus/interaction/key gestures/hit testing/pointer semantics
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes their geometry manageable; 1.4 routes semantic input to those logical regions.

---

## 2. Terminal relationship

The current DCurses development floor is Terminal 1.6.0.

Terminal 1.5 established normalized control-family framing, capability/evidence state, semantic backend registration, and deterministic routing. Terminal 1.6 consolidates complete CSI grammar and adds internal terminal/cell pixel-geometry infrastructure without adding a new public surface.

Terminal's graphics roadmap continues with:

```text
Terminal 1.7  DCS foundation + Sixel + common raw raster model
Terminal 1.8  APC foundation + Kitty Graphics + raster backend routing
```

This establishes the desired future layering:

```text
DCurses semantic raster placement
            |
            v
Terminal common raster operation
       /                \
   Sixel/DCS        Kitty Graphics/APC
```

Therefore DCurses does not introduce `DrawSixel(...)`, `DrawKittyImage(...)`, a private graphics capability probe, or raw DCS/APC emission while Terminal is building the common semantic layer intended to own those details.

---

## 3. Release 1.1.0 — semantic cell metadata and hyperlinks

### Goal

Add non-visual semantic information to retained screen content, beginning with hyperlinks, while keeping `CursesStyle` exclusively concerned with visual rendition.

### Core requirements

Semantic metadata must participate in:

- logical equality and inspection;
- retained physical refresh planning;
- editing and scrolling;
- copy and overlay;
- windows and subwindows;
- pads and viewports;
- resize/clipping and wide-cell repair;
- lifecycle invalidation and safe output recovery.

Hyperlink output composes through Terminal's typed OSC 8 ownership APIs. DCurses does not construct raw OSC 8 and does not expose `TerminalHyperlinkLease` publicly.

### Representation gate

Before public API freeze, 1.1 must measure and choose among viable storage models such as:

- optional immutable metadata reference per cell;
- compact/interned surface-owned token;
- sparse sidecar semantic spans.

The selected representation must justify ordinary-cell and large-pad memory cost as well as equality, copying, editing, inspection, wide-cell semantics, and future extensibility.

### Outcome

Applications can associate a stable URI and optional hyperlink identifier with retained logical content. Adjacent equivalent links are rendered as semantic runs through Terminal rather than per-cell protocol churn.

Detailed roadmap:

- `Icod.DCurses-1.1.0-Development-Roadmap.md`

---

## 4. Release 1.2.0 — panels, layers, visibility, and z-order

### Goal

Introduce first-class overlapping logical surfaces without introducing widgets.

### Required mechanics

The layer/panel model should support:

- attaching a logical window/surface;
- show/hide;
- raise/lower;
- move above/below another layer;
- deterministic top/bottom ordering;
- z-order enumeration;
- clipping to destination geometry;
- occlusion-aware composition;
- explicitly defined transparent-cell behavior;
- invalidation when visibility/order/position/content changes.

### Acceptance shapes

- modal help overlay;
- command palette;
- completion popup;
- context-menu surface;
- temporary status/error overlay;
- movable dialog over a retained screen.

Moving or hiding a layer must expose the correct underlying retained cells without forcing the application to reconstruct the entire screen manually.

### Non-goal

No `Button`, `TextBox`, `Menu`, `Dialog`, or other widget class belongs in the core merely because the layer substrate can support one.

---

## 5. Release 1.3.0 — layout and resize primitives

### Goal

Remove routine terminal-geometry arithmetic from applications while keeping layout deterministic and curses-oriented.

### Candidate concepts

Public names remain a later design decision, but the release should cover concepts equivalent to:

- rectangles/bounds;
- insets/margins/padding;
- horizontal and vertical splits;
- fixed-size plus remainder allocation;
- proportional allocation;
- minimum/maximum sizes;
- top/bottom/left/right docking;
- clipping and empty-layout behavior;
- deterministic recomputation after resize.

### Acceptance shape

A `top`-style screen should be expressible conceptually as:

```text
summary       fixed 5 rows
header        fixed 1 row
task list     remaining rows
status        fixed 1 row
```

without hand-coded resize arithmetic spread through application logic.

### Non-goals

No CSS, browser-style flexbox, general constraint solver, animation system, or declarative widget tree is required in the core.

---

## 6. Release 1.4.0 — focus and interaction mechanics

### Goal

Add higher-level dispatch mechanics for the semantic events DCurses already receives from Terminal.

The question this layer answers is:

> Which logical part of the terminal UI should receive this event?

### Required mechanics

The release should consider stable concepts for:

- focusable logical regions;
- explicit focus ownership;
- forward/backward traversal;
- keyboard gestures and command mapping;
- mouse hit testing;
- interaction rectangles/regions;
- pointer-shape requests associated with regions;
- focus repair when a region disappears or becomes unavailable;
- resize-aware hit testing;
- deterministic precedence where regions overlap.

### Terminal relationship

DCurses owns geometry, focus, target selection, and semantic interaction policy. Terminal owns the authoritative input stream and physical pointer-shape protocol state.

For example:

```text
DCurses hyperlink region      -> semantic pointer request
DCurses column divider        -> resize pointer request
DCurses editable region       -> text pointer request
                                  |
                                  v
                         Icod.Terminal
                         physical protocol
```

### Non-goal

1.4 provides interaction substrate, not a widget toolkit.

---

## 7. Terminal features are wrapped only when DCurses adds meaning

DCurses does not mirror every `TerminalSession` method.

| Terminal semantic feature | DCurses core treatment |
|---|---|
| OSC 8 hyperlinks | Yes — attached to retained logical content |
| Pointer shape | Yes — when associated with DCurses interaction regions |
| Pixel geometry | Later — when needed by an accepted raster/layout abstraction |
| Sixel / Kitty Graphics | Later — through one semantic raster abstraction |
| Notifications | Usually no — application-level semantic operation |
| Shell integration metadata | No — shell/application semantic operation |
| Clipboard | No automatic wrapper; direct Terminal remains appropriate unless a future editing abstraction adds real value |
| Terminal titles | No automatic wrapper |

The architectural rule is: **DCurses wraps meaning, not method names.**

---

## 8. Future satellite packages

### `Icod.DCurses.Compat`

A native-curses-shaped compatibility facade may eventually provide familiar conveniences such as `stdscr`, `newwin`, `wmove`, `waddstr`, and `wrefresh`, implemented over explicit `CursesSession` ownership.

The stable core remains instance-based and does not gain mandatory process-global state.

### `Icod.DCurses.Widgets`

A widget package may eventually provide labels, buttons, text fields, tables, menus, dialogs, scroll bars, trees, and similar controls once the core layer/layout/interaction substrate is mature.

### `Icod.DCurses.Graphics`

A separate graphics package may be preferable if raster support would otherwise impose optional image-oriented concepts on every core consumer.

---

## 9. Cross-release compatibility and validation rules

Each 1.x release must satisfy:

1. additive public API by default;
2. exact compiled public-API fingerprint for every intentional delta;
3. retained 1.0 signature/enum compatibility unless explicitly reconsidered;
4. no public raw Terminal protocol frames or internal routing types;
5. no competing terminal input reader;
6. no private terminal capability database;
7. preserved Unicode/wide-cell/continuation/line-glyph semantics;
8. compositional editing/copy/overlay/pad/viewports behavior;
9. conservative failure/cancellation/lifecycle recovery and authoritative Terminal restoration;
10. measured large-screen/pad memory and allocation impact when data structures change;
11. fresh NuGet-only consumer validation on `net8.0`, `net9.0`, and `net10.0`;
12. Windows/Linux/macOS x64/ARM64 runtime validation before stable promotion;
13. package/symbol/XML/dependency/fresh-consumer validation;
14. README/current-roadmap/API/migration/sample/package documentation audit at stable closure;
15. historical tranche records remain historical.

---

## 10. Version policy

The active 1.1 checkpoint is:

```text
Version         1.1.0-alpha.1
PackageVersion  1.1.0-alpha.1
AssemblyVersion 1.0.0.0
```

Compatible additive 1.x releases retain `AssemblyVersion 1.0.0.0` while package versions advance normally.

---

## 11. Immediate next step

Complete T1101 validation once `Icod.Terminal 1.6.0` is available through NuGet, then proceed to T1102's representation and memory decision before adding public hyperlink/metadata types.
