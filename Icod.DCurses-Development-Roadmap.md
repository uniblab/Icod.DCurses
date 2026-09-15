# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Stable compatibility floor:** `1.0.0`  
**Current published package:** `1.5.0`  
**Current published baseline for development:** `1.5.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.6.0` — retained mixed-media presentation  
**Status:** 1.5.0 published; 1.6 architecture/roadmap planning active

---

## Current authorities

Active 1.6 planning is governed by:

- `Icod.DCurses-1.6.0-Development-Roadmap.md`;
- `docs/superpowers/specs/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation-design.md` once committed and approved;
- T1601 architecture/API/representation evidence once implementation begins;
- the published `Icod.Terminal 1.15.0` persistent-raster/Unicode-placeholder contract;
- the published `Icod.TermInfo 1.14.0` advisory raster-backend planning contract.

The published 1.5 contract remains frozen by:

- `Icod.DCurses-1.5.0-Development-Roadmap.md`;
- `docs/T159-RC-and-Stable-1.5.0-Closure.md`;
- `docs/Public-API-Fingerprint-1.5.json`;
- tag/release `v1.5.0`.

Historical 1.0-1.5 roadmaps, tranche records, public-API baselines/fingerprints, plans, and release-closure documents remain historical authorities and are not rewritten to simulate later development state.

---

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Published historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Published |
| `1.2.0` | Retained panels/layers and deterministic z-order composition | Published |
| `1.3.0` | Geometry, layout, panel resize, and explicit resize recomputation | Published |
| `1.4.0` | Interaction regions, hit testing, focus, gestures, commands, pointer semantics | Published |
| `1.5.0` | Advanced interaction control: scopes, capture, spatial focus, pointer gestures, scoped commands | **Current published release** |
| `1.6.0` | Retained mixed-media presentation | **Active planning/development track** |

The post-1.0 progression is intentionally cumulative:

```text
1.1  retained cells can carry semantic meaning
1.2  independent retained surfaces overlap deterministically
1.3  surfaces have explicit geometry, layout primitives, and resize policy
1.4  normalized input can target logical application regions deterministically
1.5  interaction can be scoped, captured, spatially navigated, gesture-normalized, and scope-command aware
1.6  terminal-resident raster placeholder content participates in retained cell-grid composition and refresh
```

---

## Published 1.5 compatibility baseline

The current public 1.5 contract is:

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

---

## Active 1.6 objective — retained mixed-media presentation

The approved 1.6 direction integrates Terminal 1.15's opaque persistent-raster and Unicode-placeholder ownership with the presentation responsibilities DCurses already owns:

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

The following decisions are approved at roadmap level and must not be weakened accidentally during implementation:

- `Icod.Terminal` remains the sole live terminal/protocol/raster identity authority.
- DCurses does not parse or construct Kitty/Sixel/APC/DCS graphics commands.
- DCurses does not expose protocol-private image, placement, placeholder, or session-generation ids.
- Mixed-media retained state is distinct from `CursesCellMetadata`; semantic metadata remains terminal-independent while raster ownership is live/session-bound.
- The leading representation is a separate lazily allocated row-sparse retained-media plane; T1601 must measure alternatives and freeze the exact contract.
- Ordinary no-media screens and large pads must not pay a permanent per-cell media-reference cost without explicit measured justification.
- DCurses does not retain a hidden arbitrary source-image cache merely to recreate Terminal resources after lifecycle loss.
- Suspend/resume or Terminal state invalidation may preserve logical intent but must never emit stale raster identity or silently recreate ownership.
- No automatic Sixel fallback or hidden backend ranking enters 1.6.
- `Icod.TermInfo 1.14` planning may be demonstrated as optional caller policy, but production DCurses does not turn advisory backend planning into hidden routing policy.
- Existing event-loop, focus, command-execution, widget, and layout-policy ownership remain above DCurses.

---

## 1.6 tranche overview

```text
T1601  architecture / representation / API / dependency regret gate
T1602  session-owned raster resource and placeholder facade
T1603  sparse retained-media plane and core logical operations
T1604  editing / scrolling / copy / subwindow / pad / viewport propagation
T1605  panel composition / transparency / clipping / z-order / resize coherence
T1606  Terminal 1.15 placeholder refresh integration and physical/rendition state
T1607  lifecycle / suspend-resume / stale-released ownership / disposal hardening
T1608  application acceptance + package consumer + optional TermInfo planning sample
T1609  adversarial / capacity / performance / allocation / failure-atomicity hardening
T1610  public API / package / docs / dependency / licensing regret gate
T1611  RC and stable-source 1.6.0 closure
```

No new feature family enters after T1610.

---

## Stable architectural boundaries

Across 1.x, these boundaries remain intentional unless a future roadmap explicitly reopens them:

### TermInfo

`Icod.TermInfo` owns immutable terminal descriptions, capability evidence, expansion, and reusable/advisory inspection/planning. It does not own a live DCurses presentation or execute application graphics policy.

### Terminal

`Icod.Terminal` owns the live terminal conversation: endpoint state, native modes, input decoding, lifecycle, semantic protocols, active queries, reversible terminal state, persistent raster ownership, opaque raster identity, acknowledgement correlation, and serialized output.

### DCurses

`Icod.DCurses` owns retained terminal-cell presentation: screens, windows, pads, viewports, panels, cells, semantic metadata, Unicode-aware text, geometry/layout primitives, composition, clipping, scrolling, damage/refresh, and deterministic interaction mechanisms.

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

## Post-1.6 development options

The following are the strongest candidates after a stable mixed-media substrate exists. Their ordering is not yet frozen.

### Option A — `Icod.DCurses.Widgets`

A separate higher-level package could build controls over the stable DCurses mechanisms:

```text
panels + geometry/layout
interaction regions/scopes
focus + spatial focus
pointer capture + gestures
semantic commands
retained mixed-media presentation
```

Keeping widgets in a sibling package would preserve DCurses core as a mechanism/presentation library rather than an opinionated application framework.

### Option B — richer physical raster placement/scene coordination

If real applications require capabilities that Unicode-placeholder cells cannot express, a later track may evaluate higher-level coordination of Terminal physical placements, relative placement graphs, source cropping, and signed z-order.

Such a track must remain distinct from 1.6 and must justify its scene/lifecycle model rather than retrofitting one accidentally into the placeholder integration.

### Option C — higher-level layout/application framework facilities

Retained layout trees, flex/grid/constraint systems, event capture/bubble, timed multi-click, drag/drop payloads, automatic focus policy, and navigation frameworks remain possible future work but are lower priority than stabilizing the presentation and widget substrate first.

---

## Quality and release policy

Every development tranche should preserve the established process:

- tests written before or with the behavior they qualify;
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

---

## Immediate next step

Complete and review the 1.6 architecture/design authority, then begin T1601 as an architecture/representation/public-API candidate gate before writing the feature implementation.

T1601 must settle the sparse-media representation, public ownership facade, image-input boundary, cross-session behavior, panel transparency semantics, lifecycle vocabulary, capacity rules, and exact public API candidate before later 1.6 tranches proceed.
