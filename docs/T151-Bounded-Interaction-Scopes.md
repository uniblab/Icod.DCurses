# T151 — Bounded Interaction Scopes and Active-Scope Eligibility

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T151  
**Theme:** Bounded interaction scopes  
**Baseline:** accepted T150 head `eb34c8236a9e6c008b7ff486df177e8b52cba274`  
**T150 workflow:** #818 / `34878235207` — all seven jobs green  
**Status:** implementation complete; exact-head Staging qualification pending

---

## Purpose

T151 implements the first behavioral tranche of the approved 1.5 Advanced Interaction Control design: explicit bounded interaction scopes above the 1.4 flat region registry while preserving the implicit-root behavior of existing 1.4 consumers.

The scope layer remains mechanism-only. It introduces no widgets, callbacks, terminal I/O, automatic focus-on-click, retained event tree, or application navigation policy.

## Public surface

T151 adds exactly three exported types:

```text
CursesInteractionScope
CursesInteractionScopeLease
CursesInteractionScopeOptions
```

and additive members which allow a router to register/activate scopes and a region to carry one immutable optional explicit scope association.

The compiler-derived T151 public API candidate is:

```text
65 exported types
506 canonical declared contract lines
sha256 661e8d7adb76e23560fe5c70bedbd0d07e6a3d0b214a92111b0b1cdae3b27aaf
```

It is recorded as `1.5.0-alpha.2` in `docs/Public-API-Fingerprint-1.5.json`.

No published 1.4 type, member, enum numeric, or interaction bound is removed or renumbered.

## Ownership model

One router owns:

- one implicit root scope which is represented by `null` in the public region/scope-parent surface;
- at most `MaximumScopes = 256` live explicit scopes;
- explicit scope depth at most `MaximumScopeDepth = 32` below the implicit root.

Explicit scope parentage is immutable. A top-level explicit scope has `Parent == null`; a child retains its exact registered parent for its complete lifetime.

A region belongs to exactly one scope for its lifetime. `CursesInteractionRegionOptions.Scope == null` means the implicit root and therefore preserves the 1.4 registration/routing model.

Foreign or disposed scopes are rejected before registration mutates router state.

## Activation model

`ActivateScope(...)` returns a `CursesInteractionScopeLease`.

Activation is:

- explicit;
- descendant-only when another explicit scope is already active;
- strictly LIFO;
- synchronous and terminal-I/O-free.

An explicit active scope forms a modal eligibility boundary. Only regions associated with that scope or one of its descendants may participate in hit testing or focus eligibility.

Activating a sibling while another scope is active fails atomically with `ArgumentException`. Disposing a non-top activation lease fails atomically with `InvalidOperationException`; after the inner lease is released, the outer lease remains valid and may be disposed normally.

## Focus save and restoration

Before activation, the router repairs existing focus and captures the resulting region in the activation lease.

After activation, existing focus is repaired against the new active-scope boundary. If an out-of-scope focused region was previously active, ordinary deterministic traversal selects an eligible region inside the active subtree when one exists.

When the top lease is disposed, the saved focus is restored if that exact region is again live and eligible. Otherwise ordinary deterministic focus repair applies.

Activation with no current focus does not manufacture focus. This preserves the mechanism/policy boundary established in 1.4.

## Hit-testing and focus eligibility

The existing 1.4 hit precedence remains unchanged inside the eligible scope subtree:

```text
panel z-order
-> HitTestPriority
-> registration ordinal
```

The only T151 addition is an eligibility filter before candidate comparison.

Likewise, sequential focus ordering remains:

```text
TraversalOrder
-> registration ordinal
```

with the active-scope boundary applied as an eligibility constraint. Spatial focus behavior remains deferred to T153.

## Scope disposal

T151 implements the non-cascading rule frozen by T150.

An explicit scope cannot be disposed while it owns any live region, live child scope, or active lease. Such attempts fail atomically with `InvalidOperationException`.

After those ownership relationships end, disposal succeeds and is idempotent. Scope disposal never destroys child scopes or regions implicitly.

Router disposal remains authoritative for complete router teardown: regions are disposed first, active scope leases are marked released, and remaining explicit scopes are force-closed locally without invoking application callbacks.

## Bounds and failure atomicity

Tests cover:

- exactly 256 top-level scopes and rejection of the 257th;
- exactly 32 explicit nested levels and rejection of level 33;
- foreign parent rejection;
- foreign region-scope rejection;
- live-region scope-disposal rejection;
- live-child scope-disposal rejection;
- active-scope disposal rejection;
- out-of-order lease disposal rejection.

All rejection paths are validated before the corresponding registry mutation.

## TDD evidence

### RED

The T151 test suite was introduced before the public scope surface existed. Workflow #819 / `34878858492` failed compilation on the expected missing contract:

```text
CursesInteractionScope
CursesInteractionScopeOptions
CursesInteractionScopeLease
CursesInteractionRouter.RegisterScope(...)
CursesInteractionRouter.ActivateScope(...)
CursesInteractionRegionOptions.Scope
CursesInteractionRegion.Scope
```

### Behavioral GREEN / API RED

After the minimal scope implementation was added, workflow #827 / `34879281625` compiled successfully. On Linux x64, every non-fingerprint test passed on every TFM:

```text
746 passed / 0 behavioral failures on net8.0
746 passed / 0 behavioral failures on net9.0
746 passed / 0 behavioral failures on net10.0
```

The sole failure was the intentionally stale T150 API fingerprint. The compiler independently reported the exact T151 fingerprint recorded above. The same runtime job build completed with zero warnings and zero errors.

This cleanly separates scope behavior from reviewed public-surface promotion.

## Dependency boundary

T151 adds no production dependency and exposes no Terminal/TermInfo type.

The package dependency floor remains:

```text
Icod.Terminal 1.13.0
Icod.TermInfo  1.12.0
```

TermInfo 1.14 raster-backend planning and Terminal raster lifecycle functionality remain unrelated to this tranche.

## Acceptance gate

T151 is accepted only when an unchanged exact head containing:

- the scope public types;
- region-scope association;
- bounded registration/depth validation;
- active-scope LIFO leases;
- modal hit/focus eligibility;
- focus save/restore;
- non-cascading disposal;
- the promoted `1.5.0-alpha.2` fingerprint;
- this evidence record;

passes the complete seven-job Staging matrix:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```
