# Icod.DCurses 1.5.0 Development Roadmap — Advanced Interaction Control

**Project:** `Icod.DCurses`  
**Release:** `1.5.0`  
**Theme:** Advanced interaction control  
**Baseline:** published `v1.4.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Assembly version:** `1.0.0.0`  
**Starting production dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** architecture approved; T150 next

---

## 1. Purpose

Version 1.5.0 extends the 1.4 deterministic interaction router with the mechanisms required for more demanding applications while preserving the library's non-widget character.

The release adds five coordinated capabilities:

1. bounded interaction scopes and explicit active-scope lifetime;
2. explicit singular pointer capture;
3. deterministic spatial logical-focus navigation;
4. deterministic clock-free pointer-gesture normalization;
5. scope-level command bindings between region-local and router-global bindings.

The release remains callback-free and terminal-I/O-free at the routing layer. It does not introduce buttons, text boxes, menus, modal-dialog widgets, a retained event tree, application navigation, or drag/drop policy.

## 2. Baseline and compatibility

Published 1.4 interaction API fingerprint:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

The following 1.4 behavior is frozen unless T150 records an approved correction:

- `CursesInteractionRouter` owns one logical screen and a bounded flat region registry;
- `MaximumRegions = 4096`;
- `MaximumRegionGestureBindings = 256`;
- `MaximumGlobalGestureBindings = 1024`;
- `MaximumGestureBindings = 16384`;
- ordinary mouse routing performs deterministic hit testing;
- hit precedence remains panel z-order, then `HitTestPriority`, then registration ordinal;
- logical focus remains separate from terminal/window-manager focus reports;
- `CursesFocusDirection.Forward = 0` and `Backward = 1` remain frozen;
- region-local gesture commands precede router-global gesture commands;
- command routing returns identities/results and invokes no application callbacks;
- pointer-shape ownership remains delegated to Terminal through the existing DCurses abstraction/lease;
- router disposal disposes owned regions and closes further mutation.

1.5 is additive by default. No existing public member is removed, renamed, or semantically repurposed.

## 3. Dependency and layering stance

Family 1 is intentionally independent of unfinished lower-layer graphics work.

```text
Icod.TermInfo
      ^
      |
Icod.Terminal
      ^
      |
Icod.DCurses 1.5 interaction control
      ^
      |
applications / future widget packages
```

Rules:

- DCurses routes already-normalized `CursesInputEvent` values only.
- DCurses does not inspect raw escape sequences or terminal families.
- No 1.5 public interaction type exposes `Icod.Terminal` or `Icod.TermInfo` types.
- Published Terminal 1.14 may be exercised for compatibility, but its raster lifecycle surface is not required by Family 1.
- TermInfo 1.14 raster-backend selection/planning is explicitly out of scope for 1.5 Family 1.

## 4. Interaction-scope model

### 4.1 Scope identity and ownership

One `CursesInteractionRouter` owns a bounded scope registry.

The intended public model is centered on:

```text
CursesInteractionScope
CursesInteractionScopeOptions
CursesInteractionScopeLease
```

The router has an implicit root scope which always exists and is never disposed by the caller. Explicit scopes have immutable parentage and are permanently owned by one router.

Candidate bounds to freeze in T150:

```text
MaximumScopes               = 256 explicit live scopes
MaximumScopeDepth           = 32 including an explicit scope chain below root
MaximumScopeGestureBindings = 256 per explicit scope
```

A region belongs to exactly one scope for its lifetime. Existing 1.4 registration without a scope means the root scope and therefore preserves 1.4 behavior.

### 4.2 Active-scope lifetime

The root scope is active when no explicit scope lease is held.

Activating a scope returns a disposable `CursesInteractionScopeLease`. Activation is LIFO and bounded by `MaximumScopeDepth`. Nested activation must identify a descendant of the currently active scope. Disposing only the top lease is legal; out-of-order disposal fails deterministically rather than silently corrupting scope state.

The active scope forms a modal routing/focus boundary:

- regions outside the active scope subtree do not participate in hit testing;
- regions outside the active scope subtree are not focus eligible;
- capture owned by an excluded region is released;
- focus is repaired into the active subtree;
- when a lease ends, the router restores the previously focused region when it is again eligible, otherwise ordinary deterministic focus repair applies.

Scope activation itself never invokes callbacks and never changes terminal focus state.

## 5. Pointer-capture model

### 5.1 Explicit capture

The intended public model includes a disposable `CursesPointerCaptureLease` obtained from the router for one owned eligible region and one concrete mouse button.

Exactly one capture may be active per router.

Capture does not:

- imply logical focus;
- synthesize a mouse press;
- move a panel/window;
- acquire Terminal mouse tracking;
- invoke application code.

### 5.2 Capture lifetime

Capture ends on the first applicable condition:

- matching button release routed through the router;
- explicit capture-lease disposal;
- captured region disposal;
- captured region becomes disabled;
- captured region becomes ineligible because its panel is disposed/hidden or geometry leaves the effective screen;
- active-scope change excludes the captured region;
- router disposal.

A stale lease may be disposed idempotently after automatic release.

### 5.3 Captured pointer targeting

Ordinary `CursesInteractionHit` remains an in-bounds hit snapshot and is not repurposed for capture.

The 1.5 candidate model adds an immutable pointer-target snapshot, conceptually:

```text
CursesPointerTarget
    Region
    LocalRow      signed int
    LocalColumn   signed int
    IsInside      bool
```

Captured events route to the captured region even when `IsInside == false`. Local coordinates are computed relative to the region's effective screen origin and may be negative or greater than/equal to the declared region extent.

## 6. Spatial logical-focus model

### 6.1 Enum compatibility

The intended additive numeric contract is:

```text
Forward  = 0
Backward = 1
Up       = 2
Down     = 3
Left     = 4
Right    = 5
```

### 6.2 Candidate geometry

Spatial focus considers only focus-eligible regions in the active scope subtree. Geometry is based on each region's effective visible screen rectangle after screen and panel clipping.

A region whose effective visible rectangle is empty is ineligible.

### 6.3 Deterministic ranking

For a requested spatial direction, the candidate must lie in the requested half-plane relative to the currently focused effective rectangle.

Candidates are ordered lexicographically by:

1. perpendicular-axis overlap preference (`overlaps` before `does not overlap`);
2. primary-axis edge distance;
3. perpendicular center distance using doubled integer centers to avoid floating point;
4. existing `TraversalOrder`;
5. registration ordinal.

No Euclidean distance, floating-point arithmetic, randomization, culture-dependent comparison, or panel-brand policy is permitted.

Spatial movement has no wrapping. If no current region is focused or no candidate qualifies, `MoveFocus(Up/Down/Left/Right)` returns `null` without manufacturing initial focus. Existing Forward/Backward bootstrap and wrap semantics remain unchanged.

## 7. Pointer-gesture normalization

### 7.1 Purpose

1.5 derives deterministic application-facing pointer interaction phases from the already-normalized mouse stream without introducing a clock.

The intended gesture kinds are:

```text
Press
Release
Move
Click
DragStart
DragMove
DragEnd
WheelUp
WheelDown
WheelLeft
WheelRight
```

The candidate immutable snapshot is `CursesPointerGesture` carrying at minimum kind, button, modifiers, target, and original `CursesMouseEvent` identity through the enclosing interaction result/input.

### 7.2 Click semantics

A click is emitted when:

- a concrete button press targets one region;
- the corresponding release targets the same region through hit testing or capture;
- no cell movement for that button was routed between press and release.

No time window is used.

### 7.3 Drag semantics

The first routed cell movement while a pressed concrete button owns pointer interaction emits `DragStart`; subsequent movements emit `DragMove`; the corresponding release emits `DragEnd`.

A release after drag does not also emit `Click`.

Wheel reports map one-to-one to wheel gesture kinds and do not participate in capture lifetime.

### 7.4 Deliberate exclusions

1.5 does not define:

- double-click or triple-click;
- click-duration thresholds;
- movement thresholds beyond cell-coordinate change;
- inertial scrolling;
- drag/drop payloads or acceptance;
- hover dwell timing.

## 8. Scoped command resolution

Scopes may own semantic key-gesture bindings.

The total router binding ceiling remains `MaximumGestureBindings = 16384`; scope bindings participate in that same total rather than creating an unbounded parallel registry.

For keyboard input with a focused region, command resolution is:

```text
1. focused-region local binding
2. focused region's scope binding
3. successive parent-scope bindings up to and including the active scope boundary
4. router-global binding
5. otherwise Targeted to focused region
```

Bindings above the active scope boundary are not consulted. Router-global bindings remain global by explicit design.

For keyboard input with no focused region, scope bindings are resolved from the active scope upward only to itself, followed by router-global bindings; inactive outer scope bindings do not leak through a modal boundary.

Command binding still returns `CursesCommand` identity only. There are no handlers, delegates, callbacks, enabled predicates, dependency injection, or automatic command execution.

## 9. Structured routing result evolution

`CursesInteractionResult` remains the sole structured routing output.

Existing properties remain valid:

```text
Kind
Input
Region
Command
Hit
```

1.5 may add nullable immutable properties for pointer targeting and pointer gestures. Existing 1.4 results remain representable exactly; keyboard/paste behavior need not allocate pointer snapshots.

T150 must freeze any new result-kind numerics and prove that no existing enum numeric changes.

## 10. Lifecycle, resize, panel, and mutation coherence

All new mechanisms must remain coherent with existing mutable geometry and panel lifetime.

Required invariants:

- scope activation/deactivation performs deterministic focus repair;
- scope disposal is rejected while live regions/child scopes/leases would make ownership ambiguous, unless T150 freezes a safe cascading rule;
- region disposal automatically releases its capture and removes its local bindings as today;
- panel hide/disposal or region bounds change can invalidate focus/capture immediately through the existing eligibility pathways;
- screen resize lazily or explicitly repairs focus/capture before observable routing decisions;
- no disposed object may be returned as a routing target;
- no automatic focus-on-click is introduced;
- capture and scope lifetime are deterministic under repeated disposal;
- routing remains synchronous and performs no terminal I/O.

## 11. Performance and bounds

1.5 retains the 1.4 bounded philosophy.

The hardening gate must cover:

- `MaximumRegions` region routing with nested scopes;
- maximum scope count/depth;
- maximum local/scope/global binding counts and total count;
- capture churn and automatic-release churn;
- repeated spatial-focus movement over maximum practical region populations;
- repeated press/move/release gesture streams;
- resize/panel visibility churn during capture and nested scopes;
- deterministic replay of identical input/state transitions;
- registration ordinal exhaustion and binding-capacity failure atomicity;
- allocation ceilings for steady-state hit testing, spatial focus, command lookup, and mouse routing.

Exact performance ceilings are frozen only after measurement in T158; tests must distinguish true production allocations from unrelated runtime/JIT/test-framework thread-allocation noise.

## 12. Test-infrastructure housekeeping

T150 includes one targeted correction carried forward from the 1.4 release gate:

`CursesPanelApplicationAcceptanceTests.NoPanelPresenceCheckIsAllocationFree` measured an isolated 24-byte Windows ARM64/net10 thread allocation once even though the production path is exactly `HasPanels -> panelOrder.Count -> List<T>.Count`; the unchanged rerun passed.

The test must be hardened so it still proves the production property without treating unrelated runtime/JIT/test-harness allocation noise as product allocation. Production `HasPanels` behavior is not changed unless an independent failing product test proves a defect.

## 13. Tranche program

### T150 — Architecture/API-regret gate, planning freeze, and test housekeeping

Deliverables:

- publish this roadmap, approved design, and implementation plan;
- update the main roadmap to make 1.5 active;
- inventory the 1.4 public interaction API and enum numerics;
- freeze candidate names, bounds, ownership rules, and result evolution;
- add compiler-derived 1.5 candidate API fingerprinting without replacing historical 1.4 evidence;
- harden the old exact-zero allocation acceptance test;
- verify current dependencies and confirm no Family-1 production bump is required.

**Gate:** exact-head Staging matrix green; no unresolved API-regret finding.

### T151 — Bounded interaction scopes and active-scope eligibility

Deliverables:

- root-scope compatibility path;
- explicit scope registry, parentage, bounds, and disposal rules;
- LIFO active-scope lease stack;
- region-to-scope association at registration;
- scope-aware hit testing and focus eligibility;
- deterministic focus save/restore/repair.

**Gate:** existing 1.4 unscoped behavior remains byte/semantic compatible at the public routing level; nested scope tests cover boundaries and failure atomicity.

### T152 — Explicit pointer capture and capture lifetime

Deliverables:

- one active capture lease;
- button identity and eligibility validation;
- captured routing outside region bounds with signed local coordinates;
- automatic release on matching release, region/panel/scope/lifecycle loss, and router disposal;
- idempotent stale-lease disposal.

**Gate:** ordinary uncaptured hit testing is unchanged; capture never changes logical focus implicitly.

### T153 — Deterministic spatial focus navigation

Deliverables:

- freeze additive focus-direction numerics;
- effective visible region rectangle helper;
- Up/Down/Left/Right half-plane filtering and lexicographic ranking;
- active-scope restriction;
- adversarial tie and clipping tests.

**Gate:** integer-only deterministic results across repeated runs/TFMs/OSes; Forward/Backward behavior unchanged.

### T154 — Deterministic pointer-gesture normalization

Deliverables:

- immutable pointer gesture vocabulary/snapshot;
- per-button press ownership sufficient for click/drag classification;
- click, drag start/move/end, move, release, and wheel routing;
- capture-aware gesture targeting;
- cancellation/reset behavior when ownership becomes invalid.

**Gate:** no clock dependency and no double-click/drag-drop policy; raw input identity remains available in every structured result.

### T155 — Scoped command bindings and precedence

Deliverables:

- bounded scope gesture registry;
- total-binding ceiling integration;
- deterministic region -> scope chain -> global resolution;
- modal active-scope boundary enforcement;
- command identity remains callback-free.

**Gate:** 1.4 region/global precedence remains unchanged when all regions remain in root scope and no explicit scope is active.

### T156 — Coherence and adversarial hardening

Deliverables:

- combined scope/capture/focus/gesture/command state-machine tests;
- resize, panel hide/show/disposal, region mutation/disposal, nested lease disposal, router disposal;
- maximum-capacity and ordinal-boundary tests;
- deterministic replay and failure-atomicity coverage;
- concurrency audit preserving the documented single-writer model unless explicitly expanded.

**Gate:** no dangling capture/focus/scope target can survive invalidation into a later routing result.

### T157 — Application acceptance sample and downstream/package consumer

Deliverables:

- extend or add a focused interaction sample demonstrating nested scope activation, pointer capture, spatial focus, drag gesture results, and scoped commands;
- sample consumes public DCurses API only;
- fresh packed-package consumer exercises the complete additive surface on net8/net9/net10;
- documentation explains mechanism/policy separation.

**Gate:** sample and fresh consumer compile/run from the packed artifact; no internal or Terminal protocol types leak into public use.

### T158 — Performance/allocation/API/package/docs/dependency regret gate

Deliverables:

- steady-state allocation/performance measurements and justified ceilings;
- maximum-capacity churn;
- compiler-derived exact public API inventory/fingerprint;
- XML docs/package README/release-document review;
- LGPL/GPL header audit;
- dependency review against then-current stable Terminal/TermInfo releases;
- explicit public API regret review.

**Gate:** exact-head full Staging matrix green and no outstanding release-facing inconsistency.

### T159 — RC and stable-source 1.5.0 closure

Deliverables:

- promote accepted implementation to `1.5.0-rc.1` and qualify exact head;
- promote unchanged accepted implementation/API to stable-source `1.5.0`;
- update root README, release notes, roadmaps, package metadata, and final API evidence;
- qualify stable-source exact head through the complete matrix;
- leave merge, post-merge Release validation, tag, GitHub Release, and NuGet publication as explicit maintainer actions.

**Gate:** release-ready source with immutable qualification evidence.

## 14. Deliberate non-goals

1.5 does not add:

- widgets or controls;
- retained event trees or capture/bubble phases;
- automatic focus-on-click;
- callback dispatch;
- application navigation;
- drag/drop payloads or targets;
- timing-based multi-click gestures;
- layout-system expansion;
- accessibility ownership;
- raster presentation/scene ownership;
- raster backend selection;
- animation;
- PTY/ConPTY hosting.

## 15. Success criteria

1.5 is successful when an application can use only public DCurses APIs to:

1. register ordinary and scoped interaction regions;
2. enter and leave nested interaction scopes deterministically;
3. capture one pressed pointer to a region across out-of-bounds motion;
4. move focus sequentially or spatially with deterministic results;
5. receive clock-free click/drag/wheel gesture snapshots;
6. resolve commands through region, scope, and global precedence without callbacks;
7. survive resize, panel, region, scope, and disposal changes without dangling ownership;
8. do all of the above without DCurses parsing protocol bytes or performing hidden terminal I/O;
9. preserve all published 1.4 behavior for consumers that do not use the new features.
