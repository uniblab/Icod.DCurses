# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published compatibility floor:** `1.4.0`  
**Current published package:** `1.4.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.5.0` — advanced interaction control  
**Status:** architecture approved; T150 planning/API-regret gate is next

---

## Current authorities

- `Icod.DCurses-1.5.0-Development-Roadmap.md`
- `docs/superpowers/specs/2026-09-14-icod-dcurses-1.5-advanced-interaction-control-design.md`
- `docs/superpowers/plans/2026-09-14-icod-dcurses-1.5-advanced-interaction-control.md`
- `docs/Public-API-Fingerprint-1.4.json`
- `Icod.DCurses-1.4.0-Development-Roadmap.md` — historical 1.4 development authority
- `docs/T1412-RC-and-Stable-1.4.0-Closure.md` — historical 1.4 release closure

The tagged `v1.4.0` source is the compatibility baseline for 1.5. Historical 1.0-1.4 tranche and release documents remain immutable evidence rather than being rewritten to simulate current development state.

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Historical stable baseline |
| `1.2.0` | Panels/layers/z-order composition | Historical stable baseline |
| `1.3.0` | Layout and resize primitives | Historical stable baseline |
| `1.4.0` | Interaction routing/focus/gestures/hit testing/pointer semantics | Current published stable release |
| `1.5.0` | Advanced interaction control: scopes, pointer capture, spatial focus, pointer gestures, scoped commands | Active development target |

The progression is intentionally cumulative:

```text
1.1  cells carry semantic meaning
1.2  retained surfaces overlap deterministically
1.3  surfaces have explicit immutable geometry and resize policy
1.4  normalized input targets logical application regions deterministically
1.5  interaction ownership can be scoped, captured, spatially navigated, and composed
```

## Published 1.4 API floor

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

The tagged compatibility baseline is `v1.4.0`, whose source was qualified before merge and whose merged `main` release build and tag release workflow were subsequently qualified.

Version 1.5 remains additive by default. Existing 1.4 region registration, hit testing, forward/backward focus traversal, gesture matching, global/local command routing, pointer-shape behavior, and structured routing results remain source- and binary-compatible unless an explicit T150 regret-gate finding proves a correction necessary and receives maintainer approval.

## 1.5 release objective

`Icod.DCurses 1.5.0` completes the next layer of the interaction substrate without becoming a widget framework.

The intended model is:

```text
CursesInputEvent
      |
      v
CursesInteractionRouter
      |
      +--> active interaction scope boundary
      |
      +--> pointer capture ownership
      |
      +--> deterministic mouse gesture normalization
      |
      +--> hit testing / pointer targeting
      |
      +--> sequential or spatial logical focus
      |
      +--> region -> scope -> global command resolution
```

The router remains mechanism, not an application event loop. It does not invoke application callbacks, own widgets, parse terminal protocols, perform terminal I/O, or silently manufacture policy such as focus-on-click, drag/drop semantics, double-click timing, command handlers, or navigation stacks.

## 1.5 architectural boundaries

The 1.5 track is governed by these rules:

- `Icod.Terminal` remains the single live terminal/input/protocol authority.
- `Icod.TermInfo` remains the immutable capability-data authority.
- 1.5 Family-1 work must not depend on unfinished raster-backend selection work in `Icod.TermInfo 1.14`.
- Published `Icod.Terminal 1.14` may be qualified as a compatible downstream dependency, but Family 1 does not require its raster lifecycle API.
- Interaction scopes are bounded routing/focus/command boundaries, not widgets or visual containers.
- Scope parentage is immutable; active scope lifetime is explicit and LIFO.
- Pointer capture is explicit, singular, bounded, and never implies logical focus.
- Captured pointer routing may produce signed region-local coordinates outside region bounds; ordinary hit testing remains unchanged.
- Spatial focus uses deterministic integer geometry over effective visible region rectangles.
- `Forward = 0` and `Backward = 1` remain frozen; spatial directions are additive enum values.
- Pointer gesture normalization is cell/event based and clock-free in 1.5; double-click/triple-click and timing thresholds remain deferred.
- Command resolution remains identity-only and callback-free.
- Scope command bindings compose between region-local and router-global bindings without introducing handlers, dependency injection, or enabled predicates.
- All registries, scope nesting, binding counts, capture state, and gesture bookkeeping remain bounded.
- Existing single-writer expectations remain unless a tranche explicitly proves an additive concurrency contract.

## 1.5 tranche sequence

```text
T150  architecture/API-regret gate, planning freeze, test-infrastructure housekeeping
T151  bounded interaction scopes and active-scope eligibility
T152  explicit pointer capture and capture lifetime
T153  deterministic spatial focus navigation
T154  deterministic pointer-gesture normalization
T155  scoped command bindings and precedence
T156  resize/panel/scope/capture/disposal coherence and adversarial hardening
T157  application acceptance sample and downstream/package consumer qualification
T158  performance/allocation/API/package/docs/dependency regret gate
T159  RC and stable-source 1.5.0 closure
```

Every implementation tranche must receive exact-head Staging qualification before being called complete. Final qualification retains package-only consumer validation, compiler-derived public API fingerprinting, Windows/Linux/macOS x64/ARM64 coverage, and `net8.0`/`net9.0`/`net10.0` validation.

## Deliberate 1.5 non-goals

Version 1.5 does not add:

- a widget framework or controls;
- retained capture/bubble event trees;
- application callback dispatch;
- automatic focus-on-click;
- drag/drop framework semantics;
- double-click/triple-click timing policy;
- generalized clock ownership for interaction;
- layout/grid/flex/constraint expansion;
- accessibility-tree ownership;
- raster scene/layout ownership;
- raster backend selection or ranking;
- animation;
- PTY/process hosting.

These remain candidates for later score-taking after Family 1 is complete and the lower-layer Terminal/TermInfo development picture has advanced.

## Dependency stance

The 1.5 Family-1 implementation does not require a production dependency bump. The starting package graph remains:

```text
Icod.Terminal 1.13.0
Icod.TermInfo  1.12.0
```

Published newer compatible dependencies may be exercised in dedicated qualification lanes or adopted later through an explicit dependency-maintenance/regret gate. No Family-1 public API may expose TermInfo/Terminal types merely to consume a newer package.

## Immediate next step

Execute T150 from the approved design and implementation plan. T150 must freeze the candidate 1.5 public interaction surface, record exact bounds and enum numerics, harden the known zero-allocation measurement test against unrelated thread-allocation noise, establish the first 1.5 API fingerprint, and qualify the exact planning/API head before T151 implementation begins.
