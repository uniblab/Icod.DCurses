# Icod.DCurses 1.5.0 Advanced Interaction Control Design

**Date:** 2026-09-14  
**Status:** Approved design authority  
**Baseline:** published `Icod.DCurses 1.4.0`

## 1. Design objective

Version 1.5 completes the next mechanism layer above the 1.4 interaction router without turning `Icod.DCurses` into a widget framework.

The release adds five coordinated capabilities:

- bounded interaction scopes;
- explicit singular pointer capture;
- deterministic spatial logical focus;
- clock-free pointer gesture normalization;
- scope-level command bindings.

The release remains synchronous, deterministic, callback-free, bounded, and terminal-I/O-free at the interaction-routing layer.

## 2. Why extend the router rather than create a widget/event tree

Three approaches were considered.

### A. Extend `CursesInteractionRouter` with orthogonal mechanisms — selected

This preserves the existing 1.4 ownership center. Focus, hit testing, command routing, pointer targeting, scope boundaries, and capture all share one authoritative region registry and one eligibility model.

Advantages:

- preserves the flat application-owned region abstraction;
- minimizes duplicate state and cross-object synchronization;
- lets future widget packages build above stable mechanisms;
- keeps callbacks/application execution out of core;
- makes failure/lifecycle repair centralized and testable.

### B. Add a retained interaction/event tree — rejected for 1.5

A tree would enable capture/bubble routing and widget-style parent semantics, but it would prematurely freeze assumptions about widgets, event propagation, and application composition.

### C. Add separate navigator/capture/scope manager objects — rejected

Separate managers would reduce edits to the router but fragment authority. Focus eligibility, active scope, region disposal, panel visibility, and capture lifetime would then have to remain coherent across multiple owners.

## 3. Scope architecture

The router always owns an implicit root scope. Existing 1.4 regions belong to root by default.

Explicit `CursesInteractionScope` objects have:

- one immutable owner router;
- one immutable parent scope;
- bounded depth;
- bounded local command bindings;
- deterministic disposal rules.

Activating an explicit scope returns a `CursesInteractionScopeLease`.

Activation is stack-shaped and LIFO. A nested activation must be within the currently active subtree. The active scope restricts region eligibility for hit testing and focus. The router remembers focus across activation so that ending a lease can restore the previous focus when still eligible.

The scope model is deliberately not visual. A scope has no rectangle, rendering, layout, z-order, style, or child-widget collection.

## 4. Pointer capture architecture

`CursesPointerCaptureLease` represents explicit capture of one concrete mouse button to one region.

Only one capture is live per router.

Captured matching move/release events target the captured region even outside its bounds. Ordinary hit testing is not redefined; therefore captured targeting uses a new immutable `CursesPointerTarget` snapshot rather than abusing `CursesInteractionHit`.

`CursesPointerTarget` carries:

- target region;
- signed local row;
- signed local column;
- `IsInside`.

Capture does not alter logical focus and does not automatically request Terminal mouse tracking.

Capture is automatically released when its ownership is no longer valid, including matching release, region/panel/scope invalidation, or router disposal.

## 5. Spatial focus architecture

`CursesFocusDirection` preserves:

```text
Forward  = 0
Backward = 1
```

and adds:

```text
Up       = 2
Down     = 3
Left     = 4
Right    = 5
```

Sequential focus retains 1.4 semantics.

Spatial focus operates only from an existing focused region and only over focus-eligible regions in the active scope subtree.

Each region is projected to an effective visible screen rectangle after panel and screen clipping. Candidate ordering is deterministic and integer-only:

1. requested half-plane eligibility;
2. perpendicular overlap preferred;
3. smallest primary-axis edge distance;
4. smallest perpendicular center distance using doubled centers;
5. `TraversalOrder`;
6. registration ordinal.

There is no spatial wrap and no initial spatial focus synthesis.

## 6. Pointer gesture architecture

1.5 derives gestures from the normalized mouse stream already owned by DCurses.

The gesture vocabulary is:

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

No clock is introduced.

A click is a same-target press/release with no routed cell-coordinate movement in between.

The first movement while a concrete button owns an active press interaction becomes `DragStart`; later movements become `DragMove`; release becomes `DragEnd` and does not also emit `Click`.

Double-click, triple-click, time thresholds, inertia, drag/drop payloads, and hover dwell are excluded.

## 7. Scoped command architecture

1.4 resolves region-local bindings before router-global bindings.

1.5 inserts scope bindings between them while preserving the existing extremes:

```text
focused region local binding
    -> region scope
        -> parent scopes up to active scope boundary
            -> router global binding
```

Bindings above an active modal scope boundary do not participate. Router-global bindings remain global by explicit contract.

Scope bindings count against the existing router-wide `MaximumGestureBindings` ceiling.

Commands remain immutable identities. DCurses does not add handlers, delegates, automatic execution, dependency injection, enabled predicates, or command catalogs.

## 8. Result-model evolution

`CursesInteractionResult` remains the single public routing result.

Existing properties and 1.4 behavior remain valid. 1.5 may add nullable immutable pointer-target and pointer-gesture properties so captured/out-of-bounds pointer routing and derived gestures can be represented without changing `CursesInteractionHit` semantics.

T150 freezes exact type/member names, enum numerics, and bounds before implementation tranches proceed.

## 9. Bounds

The design adopts these candidate bounds for T150 freeze:

```text
MaximumRegions               4096   existing
MaximumScopes                 256   new explicit scopes
MaximumScopeDepth              32   explicit nested depth below root
MaximumRegionGestureBindings  256   existing
MaximumScopeGestureBindings   256   new per scope
MaximumGlobalGestureBindings 1024   existing
MaximumGestureBindings      16384   existing total across region/scope/global
Active pointer captures         1   fixed
```

All capacity failures must be deterministic and mutation-atomic.

## 10. Lifecycle and coherence

The router remains the authoritative coordinator for interaction ownership.

Every observable routing operation first repairs invalid focus/capture/scope-derived state as needed.

New mechanisms must remain coherent across:

- region enable/focusability/bounds mutation;
- panel hide/show/disposal;
- screen resize;
- region disposal;
- scope activation/deactivation/disposal;
- pointer release;
- router disposal.

No disposed or scope-ineligible region may escape as a later routing target.

## 11. Dependency boundaries

Family 1 requires no new lower-layer public contract.

Starting production dependencies remain:

```text
Icod.Terminal 1.13.0
Icod.TermInfo  1.12.0
```

Published Terminal 1.14 compatibility may be qualified separately, but no 1.5 interaction API depends on raster lifecycle observability.

Unfinished TermInfo 1.14 raster-backend planning is explicitly excluded.

## 12. Testing strategy

The implementation follows TDD per tranche.

Required test families include:

- exact 1.4 compatibility witnesses when no new feature is used;
- scope nesting, activation order, disposal, and focus restoration;
- capture routing inside/outside region bounds and every automatic-release cause;
- spatial-focus adversarial geometry ties, clipping, panel movement, and scope filtering;
- pointer press/click/drag/wheel streams and cancellation;
- command precedence under root and nested active scopes;
- combined resize/panel/region/scope/capture lifecycle churn;
- maximum-capacity and failure-atomicity tests;
- deterministic replay across TFMs/OSes;
- package-only consumer acceptance;
- allocation/performance regression measurements.

The known `NoPanelPresenceCheckIsAllocationFree` test is hardened in T150 so unrelated 24-byte runtime/test-harness noise on Windows ARM64 cannot masquerade as a production allocation regression.

## 13. Deliberate non-goals

The design does not include:

- widgets or controls;
- retained event-tree capture/bubble phases;
- automatic focus-on-click;
- callbacks or automatic command execution;
- drag/drop framework semantics;
- multi-click timing;
- layout expansion;
- accessibility-tree ownership;
- raster scene ownership or backend selection;
- animation;
- PTY/process hosting.

## 14. Success condition

The design is complete when 1.5 lets applications scope interaction, capture pointer ownership, navigate focus spatially, receive deterministic pointer gestures, and resolve scope-aware commands while preserving every 1.4 behavior for applications that do not opt into the new mechanisms.
