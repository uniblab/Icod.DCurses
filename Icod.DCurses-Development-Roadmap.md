# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`\
**Repository:** `https://github.com/uniblab/Icod.DCurses`\
**Published compatibility floor:** `1.0.0`\
**Current published package:** `2.0.0`\
**Current development source/package identity:** `2.0.0`; T2101 does not change package identity\
**Current development assembly version:** `2.0.0.0`\
**Current development runtime dependency:** direct `Icod.Terminal 1.18.0` only; TermInfo remains transitive\
**Planned 2.1 direct runtime dependency:** `Icod.Terminal 1.18.0` minimum; no direct `Icod.TermInfo` reference\
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`\
**Configurations:** `Debug`; `Staging`; `Release`\
**Active development target:** `2.1.0` — core presentation and text foundations\
**Status:** 2.0.0 published; 2.1.0 T2101 foundation acceptance candidate; T2102 implementation pending

**Planning snapshot:** 2026-09-23

---

## Current authorities

The active plan is [Icod.DCurses-2.1.0-Development-Roadmap.md](Icod.DCurses-2.1.0-Development-Roadmap.md). T2101 has produced the [2.0 presentation baseline](docs/T2101-2.0-Core-Presentation-Baseline.md), [accepted public API design](docs/2.1-Core-Presentation-and-Text-API-Design.md), [T2102-T2108 implementation plan](docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md), and [foundation gate](docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md). The gate becomes accepted only after its ordinary exact-head PR matrix is green.

The published 2.0 contract and migration history remain governed by [Icod.DCurses-2.0.0-Development-Roadmap.md](Icod.DCurses-2.0.0-Development-Roadmap.md), [docs/T2011-Stable-Source-Release-Gate.md](docs/T2011-Stable-Source-Release-Gate.md), [docs/Public-API-Fingerprint-2.0.json](docs/Public-API-Fingerprint-2.0.json), [docs/Public-API-Baseline-2.0.md](docs/Public-API-Baseline-2.0.md), and the [2.0 migration guide](docs/2.0-Migration-Guide.md).

The published [Terminal 1.18.0 contract](https://github.com/uniblab/Icod.Terminal/releases/tag/v1.18.0) supplies the semantic profile, dimensions, screen planner, session-bound output transaction, and safe unknown-rendition baseline used by DCurses 2.x. Any later integration gap must be fixed and released in the owning dependency before the affected DCurses gate advances; it must not be bypassed with TermInfo calls or raw terminal strings.

The published [DCurses 1.6.0 release](https://github.com/uniblab/Icod.DCurses/releases/tag/v1.6.0) is the behavioral migration baseline. Its implementation and release closure are governed by:

- `Icod.DCurses-1.6.0-Development-Roadmap.md`;
- `docs/superpowers/specs/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation-design.md`;
- `docs/superpowers/plans/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation.md`;
- T1601-T1611 tranche evidence;
- `docs/Public-API-Fingerprint-1.6.json` and `docs/Public-API-Baseline-1.6.md`;
- the published `Icod.Terminal 1.15.0` persistent-raster/Unicode-placeholder contract;
- the published `Icod.TermInfo 1.14.0` capability/planning contract.

The published 1.5 contract remains frozen by:

- `Icod.DCurses-1.5.0-Development-Roadmap.md`;
- `docs/T159-RC-and-Stable-1.5.0-Closure.md`;
- `docs/Public-API-Fingerprint-1.5.json`;
- tag/release `v1.5.0`.

Historical 1.0-1.6 roadmaps, tranche records, public-API baselines/fingerprints, plans, and release-closure documents remain historical authorities and are not rewritten to simulate later development state. The active roadmap supersedes their forward-looking suggestion of a 1.7 dependency review.

---

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Published historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Published |
| `1.2.0` | Retained panels/layers and deterministic z-order composition | Published |
| `1.3.0` | Geometry, layout, panel resize, and explicit resize recomputation | Published |
| `1.4.0` | Interaction regions, hit testing, focus, gestures, commands, pointer semantics | Published |
| `1.5.0` | Advanced interaction control: scopes, capture, spatial focus, pointer gestures, scoped commands | Published |
| `1.6.0` | Retained mixed-media presentation | Published; 1.x feature endpoint |
| `1.6.x` | Necessary maintenance only | As needed; no new feature track |
| `2.0.0` | Terminal-only integration and removal of direct TermInfo API/dependency coupling | **Current published release** |
| `2.1.0` | Core presentation and text foundations for editor and roguelike applications | **T2101 acceptance candidate; T2102 pending** |
| `2.2+` | Interaction conveniences, higher-level packages and later graphics work | Deferred until 2.1 evidence identifies the next boundary |

The post-1.0 progression is intentionally cumulative:

```text
1.1  retained cells can carry semantic meaning
1.2  independent retained surfaces overlap deterministically
1.3  surfaces have explicit geometry, layout primitives, and resize policy
1.4  normalized input can target logical application regions deterministically
1.5  interaction can be scoped, captured, spatially navigated, gesture-normalized, and scope-command aware
1.6  terminal-resident raster placeholder content participates in retained cell-grid composition and refresh
2.0  all terminal-facing work goes through Terminal; DCurses retains presentation and interaction policy
2.1  rich text, coordinate mapping, virtual viewports, bulk mutation, track layout and diagnostics support application-scale presentation
```

---

## Published 1.5 compatibility baseline

The published 1.5 contract is:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

`Icod.DCurses 1.5.0` was published with:

```text
AssemblyVersion  1.0.0.0
Icod.Terminal    1.15.0
Icod.TermInfo    1.14.0
Targets          net8.0; net9.0; net10.0
```

Version 1.6 is additive over this published contract. Existing 1.5 behavior remains the compatibility witness when mixed-media features are not used.

The published 1.6 contract is:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

---

## 1.6 objective — retained mixed-media presentation

The accepted 1.6 implementation integrates Terminal 1.15's opaque persistent-raster and Unicode-placeholder ownership with the presentation responsibilities DCurses already owns:

```text
Terminal 1.15
    opaque raster resources
    virtual raster placeholders
    self-contained placeholder cells
    lifecycle certainty
    serialized output
           |
           v
DCurses 1.6
    retained logical coordinates
    windows / pads / viewports
    panels / z-order / transparency
    clipping / scrolling / composition
    damage / sparse refresh / redraw order
```

The governing rule is:

> Terminal owns protocol identity and encoding; DCurses owns retained presentation coordinates, clipping, composition, damage, and refresh; applications own source-image durability and higher-level policy.

Version 1.6 is not a generic raster scene graph and does not make DCurses a graphics protocol library.

The detailed architecture, lifecycle, testing, tranche sequencing, and non-goals are defined in `Icod.DCurses-1.6.0-Development-Roadmap.md`.

---

## 1.6 architectural decisions

The following decisions are frozen and must not be weakened accidentally during release closure:

- `Icod.Terminal` remains the sole live terminal/protocol/raster identity authority.
- DCurses does not parse or construct Kitty/Sixel/APC/DCS graphics commands.
- DCurses does not expose protocol-private image, placement, placeholder, or session-generation ids.
- Mixed-media retained state is distinct from `CursesCellMetadata`; semantic metadata remains terminal-independent while raster ownership is live/session-bound.
- The accepted representation is a separate lazily allocated row-sparse retained-media plane.
- Ordinary no-media screens and large pads do not pay a permanent per-cell media-reference cost.
- DCurses does not retain a hidden arbitrary source-image cache merely to recreate Terminal resources after lifecycle loss.
- Suspend/resume or Terminal state invalidation may preserve logical intent but must never emit stale raster identity or silently recreate ownership.
- No automatic Sixel fallback or hidden backend ranking enters 1.6.
- Production DCurses does not turn TermInfo advisory planning into hidden routing policy.
- Existing event-loop, focus, command-execution, widget, and layout-policy ownership remain above DCurses.
- The direct `Icod.TermInfo` production dependency is part of the published 1.6 contract. The 2.0 track now explicitly replaces it with the strict `DCurses -> Terminal -> TermInfo` dependency path; this is not a compatible 1.x change.

---

## 1.6 tranche overview

```text
T1601  architecture / representation / API / dependency regret gate                 complete
T1602  session-owned raster resource and placeholder facade                         complete
T1603  sparse retained-media plane and core logical operations                      complete
T1604  editing / scrolling / copy / subwindow / pad / viewport propagation          complete
T1605  panel composition / transparency / clipping / z-order / resize coherence     complete
T1606  Terminal 1.15 placeholder refresh integration and physical/rendition state   complete
T1607  lifecycle / suspend-resume / stale-released ownership / disposal hardening   complete
T1608  application acceptance + package consumer                                    complete
T1609  adversarial / capacity / performance / allocation / failure-atomicity        complete
T1610  public API / package / docs / dependency / licensing regret gate             complete
T1611  RC and stable-source 1.6.0 closure                                            published as v1.6.0
```

No new feature family enters after T1610.

---

## Stable architectural boundaries

The following ownership boundaries carry forward into 2.0; the direct TermInfo coupling in 1.x is removed, not transferred into a new DCurses backend:

### TermInfo

`Icod.TermInfo` owns immutable terminal descriptions, capability evidence, expansion, and reusable/advisory inspection/planning. It does not own a live DCurses presentation or execute application graphics policy.

### Terminal

`Icod.Terminal` owns the live terminal conversation: endpoint state, native modes, input decoding, lifecycle, semantic profiles and dimensions, safe screen-operation planning and encoded-byte costs, semantic protocols, active queries, reversible terminal state, persistent raster ownership, opaque raster identity, acknowledgement correlation, and serialized output commitment.

### DCurses

`Icod.DCurses` owns retained terminal-cell presentation: screens, windows, pads, viewports, panels, cells, semantic metadata, retained raster coordinates, Unicode-aware text, geometry/layout primitives, composition, clipping, scrolling, damage/refresh, and deterministic interaction mechanisms.

### Applications / future higher layers

Applications own event loops, command execution, business/domain semantics, source-image lifetime, durable model state, high-level layout policy, and navigation policy.

A future widget layer may own controls, styling/themes, widget hierarchy, automatic focus policy, callback/event dispatch, and higher-level accessibility semantics without pushing those concerns down into DCurses core.

---

## Deliberate 1.6 exclusions

Version 1.6 does not add:

- a widget/control library;
- retained widget hierarchy;
- callback/event capture-bubble framework;
- automatic focus-on-click;
- timed double/triple-click policy;
- drag/drop payload semantics;
- retained flex/grid/constraint layout ownership;
- animation/frame scheduling;
- generic raster scene-graph ownership;
- automatic persistent-raster replay/re-upload;
- automatic Sixel fallback for persistent/placeholder content;
- image-file decoding/transcoding;
- raw Terminal graphics protocol access;
- terminal emulation;
- PTY/process hosting;
- accessibility-tree ownership.

These exclusions are deliberate scope control, not statements that the features are permanently undesirable.

---

## 2.0 objective — complete the Terminal boundary

The next release is a focused major-version migration, not another 1.x feature release. Removing TermInfo-bearing public signatures requires a source/binary compatibility break. There will be no TermInfo compatibility shim in DCurses 2.0.

The target is `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`:

- replace the public `CursesSession.Terminal` description with `CursesSession.Profile` of type `TerminalProfile`;
- use `TerminalDimensions` in `GetDimensions()`, `SynchronizeDimensions()`, and lifecycle dimensions;
- replace raw capability lookup/expansion, color/ACS interpretation, padding, and control-byte costing with Terminal profile/planner contracts;
- commit refresh text, operation plans, hyperlinks, and raster placeholders through one Terminal-owned output transaction;
- retain cells, Unicode width, clipping, composition, damage, optimization selection, and physical-screen certainty in DCurses;
- prove the boundary in source, public API, assembly metadata, package dependency groups, samples, and fresh package consumers.

TermInfo remains a legitimate transitive runtime dependency of Terminal. The release must not claim that TermInfo disappears from restore assets or from the application deployment.

### 2.0 tranche sequence

| Tranche | Deliverable |
|---|---|
| T2001 | Dependency/API inventory and Terminal readiness gate |
| T2002 | 2.0 development identity, Terminal upgrade, dimensions/profile public cutover |
| T2003 | Presentation/rendition/ACS/cursor/alert semantic adapters |
| T2004 | Erase/shift/scroll optimization using opaque operation plans and costs |
| T2005 | Transactional refresh, exhaustive capacity/cancellation/synchronization/publication hardening, and legacy-shim deletion |
| T2006 | Lifecycle, failure, output uncertainty, cleanup, and recovery qualification |
| T2007 | Final direct `Icod.TermInfo` package/reference removal, dependency leaks, and permanent guards |
| T2008 | Samples, package-only consumers, and 1.6-to-2.0 migration guide |
| T2009 | Behavioral parity, performance/allocation, platform and adversarial qualification |
| T2010 | Public API/package/XML/license/documentation freeze and regret gate |
| T2011 | RC, stable-source and exact-head release closure |

The approved staging for the T2003 vertical cutover is:

```text
T2003: transaction-backed vertical cutover for profile/rendition/ACS/cursor/alert and ordinary rewrite refresh
T2004: restore erase/character-shift/line-shift/scroll optimizations with Terminal plans and costs
T2005: exhaustive transaction/capacity/cancellation/synchronization/publication hardening and legacy-shim deletion
```

No T2003 or T2004 package is published. T2004 closes the temporary ordinary-rewrite difference for accepted erase and shift candidates.

T2007 retains final direct `Icod.TermInfo` package/reference removal; obsolete raw-output and capability-writer shims are deleted in T2005.

T2001-T2011 are accepted and version 2.0.0 is published. T2001 froze the dependency inventory and approved break manifest, qualified Terminal recovery/transaction/planner behavior, and captured the DCurses 1.6 behavioral baseline. T2002 established the 2.0 development identity and moved the public profile/dimensions boundary to Terminal-owned types. T2003-T2005 migrated semantic refresh, restored editing optimizations, and hardened transaction behavior. T2006-T2008 qualified failure recovery, removed the direct TermInfo dependency, and verified samples, package-only consumers, and a live packaged refresh. T2009 accepted parity and bounded-workload measurements; T2010 froze the API, package, and documentation; T2011 qualified the unchanged release source and artifacts. See the [2.0 roadmap](Icod.DCurses-2.0.0-Development-Roadmap.md) and its linked gates for evidence.

---

## 2.1 objective — core presentation and text foundations

Version 2.1 supplies the shared presentation mechanisms needed by a terminal-native roguelike and a screen editor in the style of `pico` or DOS `edit.exe`. Those two applications are acceptance witnesses, not new application frameworks inside DCurses.

The release covers:

- rich styled text layout with deterministic wrapping, alignment, tabs, clipping and ellipsis;
- bidirectional source-position, visual-position, caret, hit-testing and selection geometry;
- large-content viewport calculations that do not require a pad proportional to the document or map;
- benchmark-justified bulk retained-cell operations with explicit metadata, raster, clipping and damage semantics;
- additional stateless fixed/weighted/minimum/maximum track layout primitives;
- bounded opt-in refresh diagnostics and evidence-driven optimization;
- public-only roguelike and editor acceptance samples.

The shared coordinate contract is the center of the release: Unicode text elements, source positions, terminal columns, visual lines, cell rectangles, content coordinates and viewport coordinates must map deterministically. Text layout and geometry remain pure; Terminal continues to own all live terminal interaction and output.

The complete architecture, semantics, non-goals, tranche sequence and acceptance requirements are defined in [Icod.DCurses-2.1.0-Development-Roadmap.md](Icod.DCurses-2.1.0-Development-Roadmap.md).

### 2.1 tranche sequence

| Tranche | Deliverable | Status |
|---|---|---|
| T2101 | Architecture, 2.0 baseline, workload measurements, public API and regret gate | Acceptance candidate; exact-head matrix pending |
| T2102 | 2.1 development identity and text-coordinate/rich-span foundation | Pending |
| T2103 | Rich text visual-line and fragment layout | Pending |
| T2104 | Caret, hit-testing and selection geometry | Pending |
| T2105 | Retained layout presentation and benchmark-justified bulk mutation | Pending |
| T2106 | Large-content viewport and virtualization foundation | Pending |
| T2107 | Stateless track layout primitives | Pending |
| T2108 | Bounded refresh diagnostics and performance qualification | Pending |
| T2109 | Roguelike application acceptance sample | Pending |
| T2110 | Editor application acceptance sample | Pending |
| T2111 | Adversarial/package/documentation/public API freeze | Pending |
| T2112 | RC and stable-source exact-head release closure | Pending |

No production version or package identity changes in the planning tranche. T2101 must approve the detailed public API and implementation plan before T2102 advances `Version` and `PackageVersion` together to `2.1.0-alpha.1`. `AssemblyVersion` remains `2.0.0.0`.

---

## Later development sequence

### Interaction and application conveniences

After 2.1, the editor and roguelike evidence may justify contextual or multi-key command composition, command discovery, prompt helpers, timed pointer gestures, drag/drop payloads, frame-loop helpers or other repeated application mechanisms. DCurses must continue to expose mechanism without taking ownership of application commands or the event loop.

### Higher-level packages

A later `Icod.DCurses.Widgets` sibling package may build labels, buttons, text entry, lists, scrollbars and dialogs over the stable DCurses presentation and interaction mechanisms. Editor-specific document storage/undo/search facilities and game-specific world/entity systems belong in separate higher layers rather than DCurses core.

### Richer physical raster coordination

Terminal already owns persistent raster placements, cropping, relative placement, z-order and animation. A later DCurses track may evaluate retained physical placement coordination or tile/sprite helpers if application evidence requires capabilities beyond Unicode-placeholder cells. That work must define its scene and lifecycle model explicitly and preserve Terminal ownership.

---

## Quality and release policy

Every development tranche preserves the established process:

- tests written before or with the behavior they qualify;
- explicit red/green cycles for new migration contracts; use C#, PowerShell 5.1-compatible scripts, and cmd/sh, not Python;
- deterministic and bounded failure behavior;
- public API fingerprint/baseline review;
- public dependency-boundary tests;
- warnings-as-errors in Staging/Release;
- package candidate validation;
- isolated NuGet-only consumer execution;
- `net8.0`, `net9.0`, and `net10.0` qualification;
- Windows/Linux/macOS x64/ARM64 runtime matrix;
- exact-head qualification before advancing a checkpoint;
- explicit API/package/documentation regret gate before RC;
- merge, post-merge Release validation, tagging, and publication remain separate maintainer actions.

For 2.1, the published 2.0 API artifacts remain immutable historical evidence. The 2.1 public contract is additive unless a separate compatibility decision explicitly approves otherwise. T2101 must capture the baseline and freeze capacity/coordinate semantics before public implementation; T2111 must freeze and review the final 2.1 API and package delta. `Version` and `PackageVersion` advance together only when T2102 begins, while `AssemblyVersion` remains `2.0.0.0`.

---

## Immediate next step

Complete the T2101 exact-head acceptance matrix, then begin T2102 from the accepted implementation plan. T2102 advances `Version` and `PackageVersion` together to `2.1.0-alpha.1` while retaining `AssemblyVersion 2.0.0.0` and the direct `DCurses -> Terminal -> TermInfo` dependency path.

T2101 changes no product code, package identity or published 2.0 behavior. Any newly discovered live-terminal gap remains work for the owning Terminal dependency.
