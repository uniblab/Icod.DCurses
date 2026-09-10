# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Accepted compatibility floor:** `1.1.0`  
**Active source package:** `1.2.0-alpha.1`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current declared runtime dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Planning status:** approved release train; 1.2 T1201–T1208 qualified; T1209 closure active

---

## 1. Purpose

The 1.0 release established a stable curses-style terminal UI substrate. The next four minor releases grow that substrate upward into semantic content, composition, layout, and interaction without taking raw terminal-protocol ownership away from `Icod.Terminal` and without prematurely turning the core into a widget toolkit.

The approved sequence is:

```text
1.1.0  semantic cell metadata + hyperlinks
1.2.0  panels/layers + z-order composition
1.3.0  layout + resize primitives
1.4.0  focus/interaction/key gestures/hit testing/pointer semantics
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes their geometry manageable; 1.4 routes semantic input to logical regions.

## 2. Terminal relationship

The current 1.2 branch declares `Icod.Terminal 1.8.1` and `Icod.TermInfo 1.10.0`.

DCurses depends on Terminal's semantic session/input/lifecycle/output contracts rather than on terminal-family protocol details. Terminal may continue evolving independently so long as its consumed API remains compatible; DCurses package validation therefore does not hard-code approved sibling dependency versions.

The architectural boundary remains:

```text
DCurses semantic UI concepts
            |
            v
Terminal live terminal semantics
            |
            v
TermInfo immutable capability descriptions
```

DCurses does not introduce private OSC/CSI/DCS/APC emitters, terminal-family capability probes, or a second live input/lifecycle owner.

## 3. Release 1.1.0 — semantic cell metadata and hyperlinks

Version 1.1 established semantic metadata attached to retained content, beginning with hyperlinks, while keeping `CursesStyle` exclusively visual.

The accepted 1.1 contract is:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

The two 1.1 exported types are `CursesHyperlink` and `CursesCellMetadata`. Metadata uses a lazily allocated row-sparse plane, follows surviving content through editing/composition/pads/resize, remains coherent across wide-cell footprints, and is rendered through Terminal-owned bounded hyperlink operations.

The detailed 1.1 roadmap and T1101–T1109 records remain historical compatibility authorities.

## 4. Release 1.2.0 — panels, layers, visibility, and z-order

### Goal

Introduce first-class overlapping retained logical surfaces without introducing widgets or changing ordinary `CursesWindow` shared-view semantics.

### Accepted architecture

`CursesPanel` owns an independent retained surface edited through the existing `CursesWindow` model. A `CursesScreen` owns panel identity and deterministic bottom-to-top order.

The 1.2 candidate provides:

- screen-owned panel creation;
- independent retained content;
- show/hide with remembered z-order;
- movement and relative/top/bottom ordering;
- opaque composition by default;
- explicit `BlankCellsTransparent` composition;
- clipping without mutating panel content;
- damage-bounded incremental recomposition;
- Unicode width-two, line-glyph, style, and semantic-metadata coherence;
- live `CursesSession.RefreshAsync()` integration;
- retained content across destination resize and suspend/resume;
- deterministic one-way `IDisposable` removal for transient panels;
- no-panel fast path without panel snapshot allocation.

Current candidate fingerprint:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1: `CursesPanel` and `CursesPanelTransparency`.

### Application/resource acceptance

T1208 covers modal help, command-palette movement, completion-popup transparency, context-menu ordering, hidden transient status updates, sparse visible damage, allocation-free no-panel presence checking, and retained-frame reuse under repeated steady-state composition.

T1208 exact head `bb00707779cf3dc6c2222455a6f942469d036881` passed workflow #596 / `34522859308` across all seven jobs; the Linux ARM64 evidence leg reported 551/551 passing tests on each supported TFM with zero warnings/errors.

### Regret gate

T1209 discovered that hide-only lifetime semantics would retain every transient panel in the owning screen. The accepted correction makes `CursesPanel` implement `IDisposable`; disposal permanently removes the panel from its screen and rejects later manipulation. Reattachment/transfer is intentionally not introduced.

The fingerprint-complete lifetime/API head `866497c9d5015f3a149580d67eacefaf7e121aaf` passed workflow #600 / `34524054785` across all seven jobs.

### Non-goals

No `Button`, `TextBox`, `Menu`, `Dialog`, widget tree, general panel resizing, layout solver, or focus dispatch belongs in 1.2. Panel size remains fixed; general layout/resize primitives belong to 1.3.

Detailed roadmap:

- `Icod.DCurses-1.2.0-Development-Roadmap.md`

Permanent acceptance/closure records:

- `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/Public-API-Baseline-1.2.md`

## 5. Release 1.3.0 — layout and resize primitives

### Goal

Remove routine terminal-geometry arithmetic from applications while keeping layout deterministic and curses-oriented.

Candidate concepts remain subject to their own design/regret gate and include rectangles/bounds, insets/margins/padding, horizontal and vertical splits, fixed-size plus remainder allocation, proportional allocation, minimum/maximum sizes, docking, clipping/empty-layout behavior, and deterministic recomputation after resize.

A `top`-style screen should be expressible conceptually as:

```text
summary       fixed 5 rows
header        fixed 1 row
task list     remaining rows
status        fixed 1 row
```

without hand-coded resize arithmetic spread through application logic.

No CSS, browser-style flexbox, general constraint solver, animation system, or declarative widget tree is required in the core.

## 6. Release 1.4.0 — focus and interaction mechanics

### Goal

Add higher-level dispatch mechanics for semantic events already received through Terminal: focusable logical regions, focus ownership/traversal, keyboard gestures/commands, mouse hit testing, interaction regions, pointer-shape requests, focus repair, resize-aware hit testing, and deterministic overlap precedence.

DCurses owns geometry, focus, target selection, and semantic interaction policy. Terminal remains the authoritative input stream and physical pointer/protocol owner.

Version 1.4 provides interaction substrate, not widgets.

## 7. Terminal features are wrapped only when DCurses adds meaning

| Terminal semantic feature | DCurses core treatment |
|---|---|
| OSC 8 hyperlinks | Yes — attached to retained logical content |
| Pointer shape | Yes — when associated with DCurses interaction regions |
| Pixel geometry | Later — when required by an accepted raster/layout abstraction |
| Sixel / Kitty Graphics | Later — through one semantic raster abstraction |
| Notifications | Usually application-level; no automatic wrapper |
| Shell integration metadata | No |
| Clipboard | No automatic wrapper unless a future editing abstraction adds real value |
| Terminal titles | No automatic wrapper |

The rule is: **DCurses wraps meaning, not method names.**

## 8. Future satellite packages

Potential later packages remain:

- `Icod.DCurses.Compat` — native-curses-shaped compatibility facade;
- `Icod.DCurses.Widgets` — controls built after layer/layout/interaction substrate matures;
- `Icod.DCurses.Graphics` — optional raster-oriented layer if graphics concepts should remain outside every core consumer.

## 9. Cross-release compatibility and validation rules

Each compatible 1.x release must satisfy:

1. additive public API by default;
2. exact compiler-derived public-API fingerprint for every intentional delta;
3. retained 1.0/1.1 compatibility unless explicitly reconsidered;
4. no public raw Terminal protocol/routing types;
5. no competing terminal input reader;
6. no private terminal capability database;
7. preserved Unicode/wide-cell/line-glyph/metadata semantics;
8. conservative failure/cancellation/lifecycle recovery with Terminal-authoritative restoration;
9. measured memory/allocation impact when retained data structures change;
10. fresh NuGet-only consumer validation on net8/net9/net10;
11. Windows/Linux/macOS x64/ARM64 runtime validation before stable promotion;
12. package/symbol/XML/dependency/readme validation;
13. README/current-roadmap/sample/package documentation audit before RC;
14. historical tranche records remain historical.

## 10. Immediate next step

Finish the T1209 documentation/sample/package closure on one exact head. If that head is green across the seven-job PR matrix, proceed to T1210 RC and stable-source qualification. Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
