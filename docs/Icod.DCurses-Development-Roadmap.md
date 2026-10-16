# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`\
**Repository:** `https://github.com/uniblab/Icod.DCurses`\
**Published compatibility floor:** `1.0.0`\
**Published releases:** [GitHub Releases](https://github.com/uniblab/Icod.DCurses/releases) and [NuGet](https://www.nuget.org/packages/Icod.DCurses/)\
**Current stable source candidate:** `2.5.2` maintenance candidate on the active pull request\
**Latest published stable package:** `2.5.1`\
**Current development assembly version:** `2.0.0.0`\
**Current development runtime dependency:** direct stable `Icod.Terminal 1.29.2` only; TermInfo remains transitive\
**Stable 2.4 direct runtime dependency:** `Icod.Terminal 1.29.0`; no direct `Icod.TermInfo` reference\
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`\
**Configurations:** `Debug`; `Staging`; `Release`\
**Active feature track:** 2.5.2 maintenance update for Terminal 1.29.2 Windows console interoperability\
**Status:** Version 2.5.1 is published. Version 2.5.2 updates the sole direct Terminal dependency so Windows sessions inherit reversible UTF-8 output and reliable mouse ownership without changing the DCurses runtime or public API. The maintainer controls merge, tag and publication. See the [2.5 roadmap](Icod.DCurses-2.5.0-Development-Roadmap.md), [2.5.1 curated release notes](releases/2.5.1.md), and [2.5.2 curated release notes](releases/2.5.2.md).

**Planning snapshot:** 2026-10-16

---

## Current authorities

The [2.5 development roadmap](Icod.DCurses-2.5.0-Development-Roadmap.md), [approved color-surface design](superpowers/specs/2026-10-12-icod-dcurses-2.5-color-surface-hardening-design.md), and [implementation plan](superpowers/plans/2026-10-12-icod-dcurses-2.5-color-surface-hardening.md) govern the active feature track. It preserves the existing cell/style model and Terminal-owned color normalization while qualifying exact foreground/background behavior through every DCurses retained operation needed by Widgets.

The completed [2.4 development roadmap](Icod.DCurses-2.4.0-Development-Roadmap.md), [approved design](superpowers/specs/2026-10-10-icod-dcurses-2.4-widgets-editor-foundations-design.md), [implementation plan](superpowers/plans/2026-10-11-icod-dcurses-2.4-widgets-editor-foundations.md), and [v2.4.0 release](https://github.com/uniblab/Icod.DCurses/releases/tag/v2.4.0) are the historical authority for the published Widgets/dedit foundation. Terminal 1.29 clipboard/cursor integration and visible-editor/capture use remain implemented while higher-level policy stays in Widgets and dedit.

The completed [Icod.DCurses 2.3 roadmap](Icod.DCurses-2.3.0-Development-Roadmap.md), approved [`CursesRasterAtlas` design](superpowers/specs/2026-10-03-icod-dcurses-2.3-curses-raster-atlas-design.md), [implementation plan](superpowers/plans/2026-10-03-icod-dcurses-2.3-curses-raster-atlas.md), and [publication closure](T2310-2.3.0-Stable-Release-Closure.md) are the historical authority for the published 2.3 feature line. DCurses owns retained atlas coordinates, damage-oriented updates, front/back sequencing and refresh ordering; Terminal owns protocol encoding, opaque identities and the live terminal conversation; applications own source art, maps, actors, collision, visibility and scheduling.

The published 2.2 architecture and release evidence remain recorded in [the 2.2 roadmap](Icod.DCurses-2.2.0-Development-Roadmap.md), its [interaction design](superpowers/specs/2026-09-24-icod-dcurses-2.2-interaction-conveniences-design.md), and [the v2.2.0 release](https://github.com/uniblab/Icod.DCurses/releases/tag/v2.2.0). T2204 prompt state and T2205 timing helpers remain deferred; the 2.3 graphics work does not reopen them.

The published 2.0 contract and migration history remain governed by [Icod.DCurses-2.0.0-Development-Roadmap.md](Icod.DCurses-2.0.0-Development-Roadmap.md), [docs/T2011-Stable-Source-Release-Gate.md](T2011-Stable-Source-Release-Gate.md), [docs/Public-API-Fingerprint-2.0.json](Public-API-Fingerprint-2.0.json), [docs/Public-API-Baseline-2.0.md](Public-API-Baseline-2.0.md), and the [2.0 migration guide](2.0-Migration-Guide.md).

The published [Terminal 1.24.1 contract](https://github.com/uniblab/Icod.Terminal/releases/tag/v1.24.1) supplies the existing semantic/session baseline plus bounded pixel-geometry queries, exact derivation support, raster planning snapshots, intrinsic resource geometry, acknowledged frame composition and region replacement, frame selection, and focused operation evidence required by `CursesRasterAtlas`. Its persistent-raster verification distinguishes generic Kitty Graphics support from the nonzero terminal-assigned identity required by atlas creation, so malformed or zero identities become controlled capability evidence rather than escaping as parser failures. Any later integration gap must be fixed and released in the owning dependency before the affected DCurses gate advances; it must not be bypassed with TermInfo calls or raw terminal strings.

The published [DCurses 1.6.0 release](https://github.com/uniblab/Icod.DCurses/releases/tag/v1.6.0) is the behavioral migration baseline. Its implementation and release closure are governed by:

- `docs/Icod.DCurses-1.6.0-Development-Roadmap.md`;
- `docs/superpowers/specs/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation-design.md`;
- `docs/superpowers/plans/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation.md`;
- T1601-T1611 tranche evidence;
- `docs/Public-API-Fingerprint-1.6.json` and `docs/Public-API-Baseline-1.6.md`;
- the published `Icod.Terminal 1.15.0` persistent-raster/Unicode-placeholder contract;
- the published `Icod.TermInfo 1.14.0` capability/planning contract.

The published 1.5 contract remains frozen by:

- `docs/Icod.DCurses-1.5.0-Development-Roadmap.md`;
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
| `2.0.0` | Terminal-only integration and removal of direct TermInfo API/dependency coupling | Published |
| `2.1.0` | Core presentation and text foundations for editor and roguelike applications | Merged, tagged and released |
| `2.2.0` | Interaction and application conveniences for editor and roguelike applications | Published |
| `2.3.0` | First-class `CursesRasterAtlas` coordination for cell-aligned tile presentation | Published 2026-10-09 |
| `2.3.1` | Package documentation and release-state correction | Stable maintenance patch; no runtime or public API changes |
| `2.4.0` | Widgets/dedit foundations: Terminal 1.29 clipboard/cursor facades, qualified visible projection and capture | Published 2026-10-12 |
| `2.5.0` | Complete foreground/background color-surface retention, composition, damage and fallback qualification | Published 2026-10-14 |
| `2.5.1` | Terminal 1.29.1 raw shortcut-input compatibility | Published 2026-10-15 |
| `2.5.2` | Terminal 1.29.2 Windows UTF-8 output and mouse-input compatibility | Maintenance candidate; merge/tag/publication pending |
| Later | Higher-level `Icod.Widgets` themes and separately evidenced sprite/physical-placement work | Separate repositories/tracks; not implicitly in 2.5 |

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
2.2  caller-driven interaction mechanisms reduce repeated application input and command plumbing
2.3  retained tile atlases coordinate cell geometry, damage, double-buffered updates and refresh ordering
2.4  focused session facades and evidence-backed visible editor/interaction integration support Widgets and dedit
2.5  complete foreground/background styles survive retained editing, composition, damage and capability fallback
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

The detailed architecture, lifecycle, testing, tranche sequencing, and non-goals are defined in `docs/Icod.DCurses-1.6.0-Development-Roadmap.md`.

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

T2101–T2112 are complete. PR [#33](https://github.com/uniblab/Icod.DCurses/pull/33) merged to `main` as `5451c9ef08cbc5f91ba628a370de364a6b97dd10`; the v2.1.0 release contains the accepted samples and package artifacts. The [2.1 roadmap](Icod.DCurses-2.1.0-Development-Roadmap.md) remains historical release evidence; its pre-merge status text is not a live work queue.

---

## 2.2 objective — interaction and application conveniences

The agreed release order was 2.1 core text and presentation, **then Option 3: interaction and application conveniences**, then a possible higher-level package. Version 2.2 builds on the published 1.4/1.5 interaction router and the 2.1 editor and roguelike witnesses. It targets repeated interaction mechanisms such as contextual or multi-key command composition, discoverable bindings and bounded prompt input, with pointer timing or frame timing admitted only when a concrete application case and ownership boundary justify them.

The [2.2 roadmap](Icod.DCurses-2.2.0-Development-Roadmap.md) defines the candidate capabilities, evidence gates, tests and release sequence. T2201 froze the first discovery API, T2202 accepted its implementation, and T2203 accepted bounded command sequences. DCurses continues to expose mechanism without owning command execution or an application event loop. T2202 advanced `Version` and `PackageVersion` together to `2.2.0-alpha.1`; T2203 retained that identity. The [T2208 RC gate](T2208-RC-Qualification.md) accepted `2.2.0-rc.1`, the [stable-source gate](T2208-Stable-Source-Release-Gate.md) accepted the unchanged `2.2.0` candidate, and that source is now published as `v2.2.0`.

### 2.2 tranche sequence

| Tranche | Deliverable | Status |
|---|---|---|
| T2201 | 2.1 baseline, sample evidence, ownership and 2.2 public API design gate | Discovery foundation accepted; T2203 was subsequently accepted at its separate gate |
| T2202 | Development identity and effective-binding discovery | Accepted on exact head `1d6097d`; 7/7 PR Staging jobs green |
| T2203 | Bounded multi-key command composition | Accepted on exact head `2cdb89f`; 7/7 PR Staging jobs green |
| T2204 | Small prompt-input mechanism, if justified by T2201 | Deferred pending a second application witness |
| T2205 | Clock-fed pointer or frame timing mechanisms, if justified by T2201 | Deferred pending a concrete timing witness |
| T2206 | Public-only editor and roguelike acceptance and package consumers | Accepted at `ff6e446`; automated matrix and both live samples confirmed |
| T2207 | Adversarial, performance, API, documentation and dependency freeze | Accepted at `e5c5917`; 7/7 PR Staging jobs green |
| T2208 | RC and stable-source exact-head release closure | Accepted; RC `74f10d0`, stable-source executable head `42d08b7` |

---

## 2.3 objective — `CursesRasterAtlas`

The approved Option 1 is a generalized cell-aligned atlas coordinator, not an Ultima-specific engine. It makes the Terminal 1.24 raster mechanics usable through DCurses' existing retained windows, pads, viewports, panels, clipping, damage and refresh model.

Version 2.3 will provide:

- bounded, resize-sensitive cell-pixel geometry planning with exact fallback only;
- one session-owned atlas containing one raster resource, one Unicode placeholder grid and two known frames;
- immutable atlas/tile geometry and retained `CursesRasterCell` access without exposing Terminal ids;
- bulk atlas-region projection into a `CursesWindow` with the existing clipping, composition and damage rules;
- bounded, fully prevalidated tile updates in deterministic row-major order;
- a serialized present operation that copies the selected front frame into the unselected back frame, applies acknowledged RGB24/RGBA32 tile updates, selects only after all work succeeds, then swaps caller-visible front/back state;
- explicit controlled failure results and an indeterminate state after ambiguous committed failure; no blind retry, hidden upload cache or automatic replay;
- an Ultima-style public-only sample with text fallback and package-only acceptance evidence.

The full scope, non-goals, tranche gates and release policy are defined in [Icod.DCurses-2.3.0-Development-Roadmap.md](Icod.DCurses-2.3.0-Development-Roadmap.md). The design preserves the functional boundaries established by Terminal 1.24: DCurses coordinates presentation; it does not own game data, asset decoding, palette policy, collision, visibility, AI, save state, event loops or frame scheduling.

### 2.3 tranche sequence

| Tranche | Deliverable | Status |
|---|---|---|
| T2300 | Decision, roadmap, design, implementation plan, `2.3.0-alpha.1`, Terminal 1.24 reference | Accepted |
| T2301 | API-regret gate and permanent RED witnesses | Accepted |
| T2302 | Pixel geometry and advisory atlas planning | Accepted |
| T2303 | Atlas creation, ownership and rollback | Accepted |
| T2304 | Retained window/pad/viewport atlas projection | Accepted |
| T2305 | Double-buffered tile presentation and deterministic ordering | Accepted |
| T2306 | Refresh serialization, lifecycle and failure recovery | Accepted |
| T2307 | Adversarial, capacity, allocation and workload measurements | Accepted |
| T2308 | Public-only tile sample, supplied artwork, fallback, and package consumer | Accepted |
| T2309 | API, package, dependency, XML and documentation freeze | Accepted on implementation head `a6c580c`; 7/7 PR Staging jobs green |
| T2310 | Live acceptance, stable-source qualification, and publication | Closed; see `docs/T2310-2.3.0-Stable-Release-Closure.md` |

No production implementation begins until T2300 is reviewed and T2301 freezes the public contract.

---

## 2.4 objective — Widgets and dedit foundations

Version 2.4 is a focused DCurses mechanism release above stable 2.3.1 and Terminal 1.29. It exposes session-owned, bounded clipboard read/write and reversible cursor-style control without duplicating Terminal protocols. It proves visible-slice text projection, caret/selection and captured interaction for a Widgets editor, introducing a new DCurses viewport or gesture API only after an exact failing public-consumer witness and design amendment. Document storage, undo, tabs, dialogs, files, shortcut policy and scheduling remain with Widgets and dedit.

The [versioned roadmap](Icod.DCurses-2.4.0-Development-Roadmap.md) defines T2400–T2409, public API/regret gates, performance and platform evidence, dependency/version policy and release handoff. The [implementation plan](superpowers/plans/2026-10-11-icod-dcurses-2.4-widgets-editor-foundations.md) maps tasks to exact source/tests. T2400 established the alpha identity, T2401–T2408 implemented and froze the feature/package contract, and exact `2.4.0-rc.1` locally passed the release tests and package-only consumers. The `2.4.0` stable candidate with AssemblyVersion `2.0.0.0` then passed forced restore, complete Release build/test, package validation and isolated consumers. Exact-head [PR workflow 38113940513](https://github.com/uniblab/Icod.DCurses/actions/runs/38113940513) passed its package job and all six Linux/Windows/macOS x64/ARM64 runtime jobs. PR #37 subsequently merged and v2.4.0 was published; the roadmap and [T2409 gate](T2409-2.4.0-Stable-Source-Gate.md) remain historical qualification evidence.

| Tranche | Decision/qualification gate |
|---|---|
| T2400–T2402 | 1.29 dependency/preview identity, then clipboard contract and forwarding |
| T2403–T2404 | Scoped cursor-style contract and lifecycle qualification |
| T2405–T2406 | Measured viewport witness; conditional lower-layer API only after review |
| T2407 | Public-only editor interaction and fallback acceptance |
| T2408–T2409 | Adversarial/package/API freeze, RC/stable source and owner handoff |

## 2.5 objective — color-surface hardening

Version 2.5 qualifies DCurses as the complete retained color surface beneath Icod.Widgets. The existing model already gives every cell a `CursesStyle` with independent foreground, background and attributes; this track proves that model through writes, fills, background synthesis, editing, copying, text layout, pads, viewports, panels, damage, physical refresh, resize, invalidation and capability degradation. It corrects only demonstrated defects and adds no replacement palette abstraction.

The [versioned roadmap](Icod.DCurses-2.5.0-Development-Roadmap.md), [design](superpowers/specs/2026-10-12-icod-dcurses-2.5-color-surface-hardening-design.md), and [implementation plan](superpowers/plans/2026-10-12-icod-dcurses-2.5-color-surface-hardening.md) define T2500–T2507. Opaque panels are the contract for visually meaningful colored blank surfaces. Published `BlankCellsTransparent` behavior remains compatible and style-insensitive. Terminal continues to own normalization and encoding; unsupported colors degrade predictably to terminal defaults rather than being silently quantized.

| Tranche | Decision/qualification gate |
|---|---|
| T2500–T2501 | 2.4 baseline/preview identity, then complete-style logical mutation and wide-cell repair |
| T2502 | Pads, panels, colored blanks, compatibility-preserving transparency and conditional API-gap decision |
| T2503–T2504 | Style-only damage/refresh, capability degradation, resize, invalidation and lifecycle qualification |
| T2505 | Public-only color-surface sample and clean package consumer |
| T2506–T2507 | Performance/adversarial/API/package/documentation freeze, RC/stable source and owner handoff |

## Later development sequence

### Higher-level packages

The existing `Icod.Widgets` sibling package builds labels, buttons, text entry, lists, dialogs, multiline editing, tabs and interactive scrollbars over stable DCurses mechanisms. After 2.5 qualifies the lower color surface, Widgets may add host/subtree/widget theme inheritance and state/part style roles under its own roadmap. Editor-specific document storage/undo/search facilities and game-specific world/entity systems belong in separate higher layers rather than DCurses core.

### Richer sprite and physical raster coordination

Terminal already owns persistent raster placements, cropping, relative placement, z-order and animation. A later DCurses track may evaluate retained physical-placement coordination, sub-cell sprite movement or sprite animation if application evidence requires capabilities beyond the 2.3 Unicode-placeholder atlas. That work must define its scene and lifecycle model explicitly and preserve Terminal ownership. It is not silently included in `CursesRasterAtlas`.

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
- for any future release, merge, post-merge Release validation, tagging, and publication are separate maintainer actions.

For 2.5, published 2.4 API artifacts remain immutable historical evidence. The expected 2.5 public contract is unchanged unless a failing public-consumer witness, design amendment and explicit approval justify a narrowly additive API. `Version` and `PackageVersion` advance together at T2500; `AssemblyVersion` remains `2.0.0.0`.

---

## Current feature direction

Keep published 2.4 maintenance separate from the 2.5 stable-source PR until the maintainer merges and publishes it. Theme inheritance and widget-part styles remain in `Icod.Widgets`, while dedit remains the application. The direct production boundary remains `Icod.DCurses -> Icod.Terminal`; any newly discovered live-terminal normalization or protocol gap remains work for Terminal.

## Complete-frame fallback extension (2026-10-03)

The approved fallback work completed T2308 and feeds the T2309 release freeze through stable Terminal 1.28.0. DCurses adds explicit `RefreshRasterAsync`: one application-owned frame, exact geometry, same serialized transaction as text, conservative damage cleanup, and no persistent identity emulation. Terminal owns Kitty/Sixel selection and encoding. The sample prefers atlas, then complete frame, then text; `--raster` directly exercises the second path.

Automated integration, the additive API fingerprint, and package-only acceptance are implemented. Maintainer observations cover the documented ATLAS, FRAME, and TEXT paths, including movement, camera scrolling, blocked water, help restoration, resize cleanup, and independent Q/Escape exit. WezTerm remains a controlled-fallback observation unless current verification supplies a usable backend. Exact-head CI, merge, main validation, tag, and publication completed for 2.3.0; see the T2310 closure record.
