# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Baseline:** merged `1.0.0` source with `Icod.Terminal 1.5.0` and `Icod.TermInfo 1.10.0`  
**Latest published DCurses release at planning time:** `0.9.0`  
**Current package metadata at planning time:** `1.0.0`  
**Planning status:** approved release train; implementation begins with 1.1.0  
**Version policy for this planning PR:** no package or assembly version bump

---

## 1. Purpose

The 1.0 release established a stable curses-style terminal UI substrate. The next four minor releases should grow that substrate upward, not sideways into raw terminal protocol ownership and not prematurely into a widget framework.

The approved release sequence is:

```text
1.1.0  semantic cell metadata + hyperlinks
1.2.0  panels/layers + z-order composition
1.3.0  layout + resize primitives
1.4.0  focus/interaction/key gestures/hit testing/pointer semantics
```

This order deliberately develops the semantic and composition model before raster graphics and before high-level widgets.

---

## 2. Why graphics is not the next DCurses feature

`Icod.Terminal 1.5.0` establishes the normalized internal control-language/capability-routing foundation without adding a new public DCurses-facing graphics contract.

At planning time, Terminal's 1.6 work is consolidating complete CSI grammar and terminal/cell pixel geometry, while its published roadmap reserves later work for:

```text
Terminal 1.7  DCS foundation + Sixel + common raw raster model
Terminal 1.8  APC foundation + Kitty Graphics + common raster backend routing
```

The correct future layering is:

```text
DCurses semantic raster placement
            |
            v
Terminal common raster operation
       /                \
   Sixel/DCS        Kitty Graphics/APC
```

DCurses should therefore not introduce `DrawSixel(...)`, `DrawKittyImage(...)`, a private raster capability probe, or raw DCS/APC output while Terminal is actively building the common semantic layer intended to own those details.

Raster graphics remain an approved later direction once the required Terminal public contract is stable.

---

## 3. Release 1.1.0 — semantic cell metadata and hyperlinks

### Goal

Add non-visual semantic information to retained screen content, beginning with hyperlinks, while keeping `CursesStyle` exclusively concerned with rendition.

### Core direction

A logical screen cell or span should be able to carry semantic metadata which participates in:

- retained logical equality;
- physical refresh planning;
- editing and scrolling;
- copy and overlay;
- windows and subwindows;
- pads and viewports;
- resize/clipping and wide-cell repair;
- lifecycle invalidation and safe output recovery.

Hyperlink output must compose through the existing typed `Icod.Terminal` OSC 8 ownership API. DCurses must not encode OSC 8 directly and must not expose `TerminalHyperlinkLease` as part of its own public abstraction.

### Important design gate

The representation must be measured before public freeze. Candidate approaches include:

1. one optional immutable metadata reference carried by each `CursesCell`;
2. interned metadata tokens owned by a screen/surface;
3. sparse sidecar semantic spans.

The selected representation must preserve ordinary-cell efficiency and avoid a disproportionate permanent memory tax on large pads merely because hyperlink support exists.

### Expected outcome

Applications can associate a stable URI and optional hyperlink identifier with logical content. The renderer groups adjacent equivalent semantic runs and allows Terminal to own hyperlink begin/end protocol state.

---

## 4. Release 1.2.0 — panels, layers, visibility, and z-order

### Goal

Introduce first-class overlapping logical surfaces without turning the core into a widget toolkit.

### Required mechanics

The release should provide a stable layer/panel model capable of:

- attaching a logical window/surface;
- show/hide;
- raise/lower;
- move above/below another layer;
- top/bottom ordering;
- deterministic z-order enumeration;
- clipping to destination geometry;
- occlusion-aware composition;
- optional transparent-cell composition semantics;
- invalidation when visibility, order, position, or covered content changes.

### Acceptance shapes

Representative application shapes include:

- modal help overlay;
- command palette;
- completion popup;
- context menu surface;
- temporary status/error overlay;
- movable dialog over a large retained screen.

The release should prove that moving or hiding a layer exposes the correct underlying retained cells without requiring applications to manually reconstruct the entire screen.

### Non-goal

No `Button`, `TextBox`, `Menu`, `Dialog`, or other widget class belongs in the core merely because layers can support them.

---

## 5. Release 1.3.0 — layout and resize primitives

### Goal

Move routine terminal geometry arithmetic out of every application while keeping layout deterministic, small, and curses-oriented.

### Candidate concepts

Public names are not frozen by this roadmap, but the release should cover concepts equivalent to:

- rectangles/bounds;
- insets/margins/padding;
- horizontal and vertical splits;
- fixed-size plus remainder allocation;
- proportional allocation;
- minimum and maximum sizes;
- top/bottom/left/right docking;
- clipping and empty-layout behavior;
- deterministic recomputation after resize.

### Acceptance shape

A `top`-style screen should be expressible as something conceptually equivalent to:

```text
summary       fixed 5 rows
header        fixed 1 row
task list     remaining rows
status        fixed 1 row
```

without hand-coded resize arithmetic distributed throughout application logic.

### Non-goals

The core does not need CSS, browser-style flexbox, constraint solvers, animation, or a declarative widget tree.

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

DCurses owns geometry, focus, target selection, and semantic interaction policy. Terminal owns physical pointer-shape protocol state and the authoritative input stream.

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

DCurses should not create OSC 22 or other raw pointer-shape sequences itself.

### Non-goal

This release provides interaction substrate, not a widget toolkit.

---

## 7. Features intentionally not wrapped one-for-one from Terminal

Terminal exposes or is developing a broad semantic surface. DCurses should absorb a Terminal feature only when it acquires meaningful curses-level semantics.

Examples:

| Terminal semantic feature | DCurses core treatment |
|---|---|
| OSC 8 hyperlinks | Yes — logical semantic metadata attached to retained content |
| Pointer shape | Yes — when associated with DCurses interaction regions |
| Pixel geometry | Yes later — when required for raster placement/layout semantics |
| Sixel / Kitty Graphics | Yes later — through one semantic raster abstraction |
| Notifications | Usually no — application-level operation already available through Terminal |
| Shell integration metadata | No — application/shell semantic operation |
| Clipboard | No automatic wrapper; use Terminal unless a future DCurses editing abstraction adds real value |
| Terminal titles | No automatic wrapper; direct Terminal semantic operation remains appropriate |

The architectural rule is: **DCurses wraps meaning, not method names.**

---

## 8. Future satellite packages

Two future packages are favored over expanding the stable core indefinitely.

### `Icod.DCurses.Compat`

A native-curses-shaped compatibility facade may eventually provide familiar conveniences such as `stdscr`, `newwin`, `wmove`, `waddstr`, and `wrefresh` semantics while remaining implemented over explicit `CursesSession` ownership.

The stable core remains instance-based and does not gain mandatory process-global state.

### `Icod.DCurses.Widgets`

A widget package may eventually provide controls such as labels, buttons, text fields, tables, menus, dialogs, scroll bars, and trees once the core layer/layout/interaction substrate is mature.

Widgets should evolve independently of the stable low-level core contract.

A future graphics package such as `Icod.DCurses.Graphics` may also be preferable if raster support would otherwise impose optional image-oriented concepts on every core consumer.

---

## 9. Cross-release compatibility and validation rules

Each 1.x release must satisfy all of the following:

1. Additive public API by default; no incidental breaking cleanup.
2. Exact compiled public-API fingerprint captured for every intentional delta.
3. Existing 1.0 signatures and enum values remain valid unless an explicit compatibility decision says otherwise.
4. Public metadata/layout/interaction APIs do not expose raw Terminal protocol frames or internal routing types.
5. No competing terminal input reader is introduced.
6. No private terminal capability database is introduced.
7. Unicode text elements, wide cells, continuation repair, semantic line cells, and terminal-column clipping remain correct.
8. Editing, copy/overlay, pads, viewports, and retained damage/refresh remain compositional with the new feature.
9. Failure/cancellation/lifecycle paths preserve safe recovery and authoritative Terminal restoration.
10. Large-pad/screen allocation and memory behavior are measured when data structures change.
11. Package-only consumers compile and execute the intentional new public surface on `net8.0`, `net9.0`, and `net10.0`.
12. Windows/Linux/macOS x64/ARM64 runtime validation remains required before stable promotion.
13. Package, symbols, XML documentation, exact dependency groups, and fresh-consumer validation remain release gates.
14. Every stable closure includes a documentation audit of README, current roadmap, API baseline, migration guidance, samples, package metadata, and release wording.
15. Historical tranche documents remain historical rather than being globally rewritten to current dependency versions.

---

## 10. Version and assembly policy

This planning PR intentionally leaves:

```text
Version         1.0.0
PackageVersion  1.0.0
AssemblyVersion 1.0.0.0
```

unchanged.

When 1.1 implementation begins, package/version metadata should move through `1.1.0-alpha.*`, `1.1.0-rc.1`, and stable `1.1.0` checkpoints as appropriate.

The recommended additive-1.x assembly policy is to retain:

```text
AssemblyVersion 1.0.0.0
```

through the 1.x line, allowing NuGet package versions and informational versions to advance without creating unnecessary assembly-identity churn for additive compatible releases. T1101 must explicitly ratify or reject that recommendation before the first 1.1 alpha.

---

## 11. Immediate next step

Begin the detailed 1.1.0 plan in `Icod.DCurses-1.1.0-Development-Roadmap.md`.

The first implementation tranche must freeze the semantic metadata/hyperlink contract and measure representation costs before the first new public API is accepted.
