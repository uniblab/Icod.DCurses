# Icod.DCurses 1.5.0 Development Roadmap — Advanced Interaction Control

**Project:** `Icod.DCurses`  
**Release:** `1.5.0`  
**Theme:** Advanced interaction control  
**Baseline:** published `v1.4.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Assembly version:** `1.0.0.0`  
**Production dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** T150-T158 complete; T159 `1.5.0-rc.1` exact-head qualification active

---

## 1. Purpose

Version 1.5.0 extends the published 1.4 deterministic interaction router with the mechanisms required for more demanding applications while preserving the library's non-widget character.

The release adds five coordinated capabilities:

1. bounded interaction scopes and explicit active-scope lifetime;
2. explicit singular pointer capture;
3. deterministic spatial logical-focus navigation;
4. deterministic clock-free pointer-gesture normalization;
5. scope-level command bindings between region-local and router-global bindings.

The release remains callback-free and terminal-I/O-free at the routing layer. It does not introduce buttons, text boxes, menus, modal-dialog widgets, a retained event tree, application navigation, timing-based click policy, or drag/drop policy.

## 2. Published baseline and accepted candidate

Published 1.4 interaction API fingerprint:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Frozen accepted 1.5 candidate fingerprint:

```text
last API-changing label: 1.5.0-alpha.5
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

The candidate is additive over 1.4. No published 1.4 public type/member is removed, renamed, or repurposed.

The seven new exported types are:

```text
CursesInteractionScope
CursesInteractionScopeOptions
CursesInteractionScopeLease
CursesPointerCaptureLease
CursesPointerTarget
CursesPointerGesture
CursesPointerGestureKind
```

T155 was the final API-changing tranche. T156-T159 preserve this exact fingerprint; RC promotion changes release identity/documentation only.

## 3. Dependency and layering stance

Family 1 is intentionally independent of lower-layer raster work.

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
- `Icod.Terminal` remains the single live terminal/input/protocol authority.
- No 1.5 public interaction type exposes Terminal or TermInfo implementation types.
- Terminal 1.14 raster-lifecycle observability is not required by Family 1.
- TermInfo 1.13/1.14 raster-evidence/backend-planning work is not required by Family 1.
- T158 re-evaluated the published lower layers and retained `Icod.Terminal 1.13.0` / `Icod.TermInfo 1.12.0`; no freshness-only dependency bump is warranted.

## 4. Interaction-scope contract

One `CursesInteractionRouter` owns one implicit root scope plus a bounded explicit scope registry.

```text
MaximumScopes      = 256 explicit live scopes
MaximumScopeDepth  = 32 explicit levels below root
```

An explicit scope has immutable parentage and belongs permanently to one router. A region belongs to one scope for its lifetime; omitted/null scope means the implicit root and preserves published 1.4 behavior.

Scope disposal is deliberately non-cascading. It is legal only when the scope has no live regions or child scopes and is not active or required by a live activation chain. Invalid disposal fails atomically.

Scope activation:

- returns `CursesInteractionScopeLease`;
- is descendant-only and strictly LIFO;
- makes the active scope a modal hit/focus/command eligibility boundary;
- saves logical focus for deterministic restoration;
- invokes no callback and performs no terminal operation.

## 5. Pointer-capture contract

Exactly one explicit pointer capture may exist per router.

`CapturePointer(...)` binds one owned eligible region to one concrete mouse button and returns `CursesPointerCaptureLease`.

Capture never:

- implies or changes logical focus;
- synthesizes a press;
- moves a window/panel;
- acquires Terminal mouse tracking;
- executes application behavior.

Capture ends on matching release, explicit disposal, target invalidation, modal exclusion, or router disposal. Stale lease disposal after automatic release is idempotent.

Captured routing uses immutable `CursesPointerTarget` rather than weakening the published `CursesInteractionHit` contract. It exposes:

```text
Region
LocalRow      signed int
LocalColumn   signed int
IsInside      bool
```

Captured targets may remain valid with `IsInside == false`, including negative or beyond-edge local coordinates.

## 6. Spatial logical-focus contract

The additive numeric contract is:

```text
Forward  = 0
Backward = 1
Up       = 2
Down     = 3
Left     = 4
Right    = 5
```

Forward/Backward published behavior remains unchanged.

Spatial candidates must be focus-eligible within the active scope subtree and have a non-empty effective visible rectangle after screen/panel clipping.

Candidates are ranked deterministically and without floating point by:

1. requested half-plane eligibility;
2. perpendicular-axis overlap preference;
3. primary-axis edge distance;
4. perpendicular doubled-center distance;
5. `TraversalOrder`;
6. registration ordinal.

Spatial movement does not wrap. With no current focus or no candidate, it returns `null` rather than manufacturing focus.

## 7. Pointer-gesture normalization

The immutable 1.5 pointer gesture vocabulary is:

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

Classification is clock-free:

- same-target press/release with no intervening cell movement -> `Click`;
- first held-button cell movement -> `DragStart`;
- later movement -> `DragMove`;
- release after drag -> `DragEnd` without an additional click;
- wheel input maps one-to-one to wheel gesture kinds.

Excluded policy remains outside core:

- double/triple click;
- click-duration thresholds;
- sub-cell movement thresholds;
- inertial scrolling;
- drag/drop payloads/acceptance;
- hover dwell timing.

## 8. Scoped command resolution

Explicit scopes may own semantic key-gesture bindings.

```text
MaximumRegionGestureBindings  = 256 per region
MaximumScopeGestureBindings   = 256 per scope
MaximumGlobalGestureBindings  = 1024
MaximumGestureBindings        = 16384 total across all three layers
```

With a focused region, lookup is:

```text
region-local
-> region scope
-> parent scopes through active modal boundary
-> router-global
-> otherwise ordinary targeted result
```

With no focused region and an explicit active scope, only that active scope is considered before router-global lookup; inactive outer bindings do not leak through the modal boundary.

Commands remain `CursesCommand` identities only. There are no handlers, callbacks, enabled predicates, dependency injection, or automatic command execution.

## 9. Structured result evolution

`CursesInteractionResult` remains the sole structured routing output.

Published properties remain valid:

```text
Kind
Input
Region
Command
Hit
```

1.5 adds nullable immutable:

```text
PointerTarget
PointerGesture
```

`CursesInteractionResultKind` remains unchanged:

```text
Unrouted = 0
Targeted = 1
Command  = 2
```

The original normalized input object remains available in every result.

## 10. Lifecycle, mutation, and coherence rules

The accepted 1.5 implementation is governed by these invariants:

- active scope eligibility participates in hit testing, focus, capture, and scoped commands;
- focus save/restore/repair remains deterministic;
- region disposal releases its capture and local bindings;
- panel hide/disposal, bounds mutation, screen resize, and scope transitions cannot leave dangling pointer ownership;
- once pointer ownership is invalidated, later geometry/scope restoration cannot resurrect that old capture or press/drag state;
- stale capture/scope leases remain deterministic and idempotent where documented;
- no disposed object is returned as a later routing target;
- no automatic focus-on-click is introduced;
- routing remains synchronous and performs no terminal I/O;
- the existing single-writer interaction model remains the concurrency contract.

## 11. Performance and capacity qualification

T158 froze warmed repeated-sample regression tripwires using the existing 1.4 measurement methodology:

```text
warmup iterations       4096
allocation samples         8
normal iterations       10000
spatial iterations       1000
fixed sample noise       1024 bytes
```

Qualified ceilings:

```text
root-scope local command routing       <= 96 bytes / operation + fixed sample noise
nested-scope successful HitTest        <= 96 bytes / operation + fixed sample noise
spatial Right/Left focus pair          <= 1024 bytes total in minimum 1000-iteration sample
deep scoped-command lookup             <= 96 bytes / operation + fixed sample noise
captured out-of-bounds mouse routing   <= 256 bytes / operation + fixed sample noise
maximum-capacity combined churn        <= 60 seconds broad regression tripwire
```

Maximum-capacity churn covers all 256 explicit scope slots, the full 16,384 total binding budget with returned/reused capacity, and repeated capture/press/drag/release ownership.

These are regression tripwires, not latency or throughput SLAs.

## 12. Application/package acceptance

`Icod.DCurses.Interaction.Sample` demonstrates the complete 1.5 mechanism using public DCurses API only:

- modal + nested scope activation;
- Forward/Backward and spatial focus;
- scoped/global command identities;
- explicit pointer capture;
- `DragStart` / `DragMove` / `DragEnd` routing;
- signed captured pointer targets;
- application-owned popup movement policy;
- pointer-shape preferences;
- explicit resize/re-layout;
- terminal focus versus logical focus separation.

The package-only consumer restores from the generated `.nupkg` and compiles/executes the public additive surface on net8.0, net9.0, and net10.0. Internal synthetic input factories remain internal.

## 13. Tranche program

```text
T150  architecture/API-regret gate, planning freeze, test housekeeping          complete
T151  bounded interaction scopes and active-scope eligibility                   complete
T152  explicit pointer capture and capture lifetime                             complete
T153  deterministic spatial focus navigation                                    complete
T154  deterministic pointer-gesture normalization                               complete
T155  scoped command bindings and precedence                                    complete
T156  resize/panel/scope/capture/disposal coherence and adversarial hardening   complete
T157  application acceptance sample and downstream/package consumer             complete
T158  performance/allocation/API/package/docs/dependency regret gate            complete
T159  RC and stable-source 1.5.0 closure                                        RC qualification active
```

Qualified implementation/API checkpoints:

```text
T150  eb34c8236a9e6c008b7ff486df177e8b52cba274  #818 / 34878235207
T151  ac0bfcbf38800a94533cc4ada4caf3b8feee4fb1  #829 / 34879561466
T152  b1e9e60df7ee6cfc3e2af10c4f3f819273c299fb  #839 / 34881203594
T153  6f440a9feda628f1948d11008402a0064bbd645b  #844 / 34882549455
T154  acae6104e7f0e4e431d7f5f836978be807008a40  #855 / 34892884559
T155  3e59e7613e214a5eaa84df13bbedba56abfefb6d  #864 / 34899684038
T156  e09324666bb6960d465f061d609a4fb53d8c5288  #876 / 34905503151
T157  596843f798999573162be7883051030db353b940  #884 / 34908524571
T158  9a8d0b2e2439bf4a936a5602d846a0c1bd20781f  #888 / 34914622882
```

T157 documentation head `9014bc8721ea8bde7248973d7d2d34e9ed2a8109` additionally passed #886 / `34908876499` across all seven jobs.

T158's first performance head `8fecfbd8f69555adfa469528105ba841f0d2b6e4` exposed a test-only disposal-order mistake: the spatial fixture attempted to dispose an explicit scope before its 256 router-owned regions. Corrected head `9a8d0b2e2439bf4a936a5602d846a0c1bd20781f` changed only fixture ownership and passed #888 / `34914622882` across all seven jobs; no production source, API, algorithm, or threshold changed. T158 documentation head `6336defe0fe0594301c1d20c0542ca1d8b8babd3` passed #892 / `34915072200`, and final evidence head `218f900aaf689029f8f5de26906f86724d029ebc` passed #893 / `34915390381`, all seven jobs.

## 14. Deliberate non-goals

Version 1.5 does not add:

- widgets or controls;
- buttons, text boxes, menus, dialogs, or application navigation;
- retained event trees or capture/bubble phases;
- automatic focus-on-click;
- callback dispatch;
- drag/drop payloads or targets;
- timing-based multi-click gestures;
- layout-system expansion;
- accessibility ownership;
- raster presentation/scene ownership;
- raster backend selection;
- animation;
- PTY/ConPTY hosting.

Future widget or mixed-media layers should be able to build on these mechanisms without bypassing Terminal ownership or reimplementing scopes, focus, capture, gesture routing, or command identity.

## 15. T158 regret decision and T159 closure

T158 found no public API, ownership, package, documentation, licensing, or dependency correction that warrants changing the accepted implementation before RC.

T159 has promoted the unchanged accepted implementation/API to:

```text
Version         1.5.0-rc.1
PackageVersion  1.5.0-rc.1
AssemblyVersion 1.0.0.0
```

The current gate is exact-head seven-job Staging qualification of that RC identity. Only after the RC head passes may T159 promote the same implementation/API to:

```text
Version         1.5.0
PackageVersion  1.5.0
AssemblyVersion 1.0.0.0
```

The final stable-source step may promote fingerprint metadata from its last API-changing alpha label to final `1.5.0` / stable while retaining the exact same 69/525/hash contract.

T159 may update release-facing README/release notes/final fingerprint metadata and closure records, but it must not silently change the accepted production interaction contract. Merge, post-merge Release validation, tagging, GitHub Release creation, and NuGet publication remain separate explicit maintainer actions.
