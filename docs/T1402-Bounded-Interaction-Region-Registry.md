# T1402 — Bounded Interaction-Region Registry

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1402  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Qualified T1401 head:** `d70b6a9892812a624c810ae115daaafd636d2192`  
**T1401 workflow:** #722 / `34712277620`  
**Implementation checkpoint:** `d705f4d8582278ea4df1b42f9d1a67559bc4d774`  
**Implementation workflow:** #730 / `34712804283`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation checkpoint qualified; final documentation head requires normal exact-head qualification

---

## Objective

T1402 implements the bounded application-owned interaction-region registry frozen by T1401 without pulling forward hit testing, logical focus, semantic gestures, command routing, or pointer-shape terminal I/O.

The tranche establishes the first production substrate for the 1.4 interaction-routing release:

```text
CursesScreen
    -> application-owned CursesInteractionRouter
        -> bounded CursesInteractionRegion registrations
            -> ordinary screen-relative geometry
            -> optional panel-relative geometry
            -> mutable enabled/focusable/order/priority metadata
            -> deterministic one-way disposal
```

## Test-first checkpoint

T1402 began with `CursesInteractionRegionRegistryTests` before production implementation.

The initial RED test head exposed an xUnit assertion-overload warning caused by expression-bodied `Assert.Throws` lambdas. Those test lambdas were corrected without adding production code.

The clean RED head was:

```text
aa76c4c72af55522ea54b1b36e5b127d4b70a026
```

Workflow #724 / `34712560648` then failed in build with zero warnings and only the expected missing-type errors for:

```text
CursesInteractionRouter
CursesInteractionRegionOptions
CursesInteractionRegion
```

This established the intended RED condition before implementation.

## Implemented public surface

T1402 adds exactly three exported types:

```text
CursesInteractionRouter
CursesInteractionRegionOptions
CursesInteractionRegion
```

### `CursesInteractionRegionOptions`

The registration options contain:

```text
Bounds
Panel
IsEnabled
IsFocusable
TraversalOrder
HitTestPriority
```

`IsEnabled` defaults to `true`; the remaining Boolean/integer metadata defaults follow ordinary CLR defaults.

`Bounds` may be empty or extend outside the current screen/panel. Effective clipping belongs to T1403 hit testing rather than T1402 registration.

### `CursesInteractionRouter`

The router:

- requires a non-null `CursesScreen`;
- exposes that screen through `Screen`;
- registers regions through `RegisterRegion(...)`;
- validates a panel association against the router's screen;
- rejects an already-disposed panel;
- enforces `MaximumRegions = 4096` before mutation;
- allocates a private monotonically increasing signed 64-bit registration ordinal beginning at zero;
- never reuses an ordinal within one router lifetime;
- rejects future registration if the ordinal domain is exhausted;
- releases the live-region count immediately when a region is disposed;
- owns no terminal session, input reader, output path, or layout policy;
- disposes all live regions when the router is disposed;
- makes router disposal idempotent.

The T1401 binding constants are also exposed now for future tranches:

```text
MaximumRegionGestureBindings = 256
MaximumGlobalGestureBindings = 1024
MaximumGestureBindings = 16384
```

T1402 does not yet create binding registries.

### `CursesInteractionRegion`

A registered region snapshots its initial options and exposes:

```text
Bounds
Panel
IsEnabled
IsFocusable
TraversalOrder
HitTestPriority
SetBounds(...)
Dispose()
```

The mutable properties and `SetBounds(...)` reject post-disposal mutation with `ObjectDisposedException`.

Disposal is idempotent and permanently removes the region from the owning router. Read-only observations remain readable after disposal so previously captured application state and future immutable route-result snapshots can remain diagnosable.

Panel disposal remains independent: a region does not own or dispose its associated panel.

## Registry bound acceptance

The T1402 tests allocate exactly `CursesInteractionRouter.MaximumRegions` live regions and prove:

1. all 4,096 registrations are accepted;
2. registration 4,097 fails with `InvalidOperationException` before mutation;
3. disposing one region releases one live slot;
4. one replacement region can then be registered successfully.

This is a live-count bound, not a lifetime-registration bound. Registration ordinals remain monotonic even when slots are reused.

## Version and compatibility

T1402 starts the 1.4 source/package line:

```text
Version         1.4.0-alpha.1
PackageVersion  1.4.0-alpha.1
AssemblyVersion 1.0.0.0
```

The dependency graph is unchanged:

```text
Icod.Terminal 1.11.1
Icod.TermInfo 1.11.0
```

The published 1.3 API remains an immutable compatibility floor and is retained in `docs/Public-API-Fingerprint-1.3.json`.

## Current 1.4 alpha API fingerprint

The compiler-derived T1402 surface is:

```text
54 exported types
432 canonical declared contract lines
sha256 64a202117a0de57cd5ad85a8b800fd411f0940bdd7c29561a3eaa8b3f3eabe22
```

Machine authority:

```text
docs/Public-API-Fingerprint-1.4.json
```

The three additions over published 1.3 are exactly:

```text
Icod.DCurses.CursesInteractionRegion
Icod.DCurses.CursesInteractionRegionOptions
Icod.DCurses.CursesInteractionRouter
```

The public Terminal/TermInfo dependency allow-list remains unchanged.

## GREEN qualification checkpoint

After implementation, the first GREEN matrix exposed only the intentional public-API fingerprint gate. The assembly built successfully and all ordinary tests passed; the gate reported the new 54-type / 432-line contract consistently on all target frameworks.

After advancing the **current-development** fingerprint to the 1.4 alpha authority while retaining the published 1.3 fingerprint unchanged, exact head:

```text
d705f4d8582278ea4df1b42f9d1a67559bc4d774
```

passed workflow:

```text
#730 / 34712804283
```

with all seven jobs green:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

Package validation also accepted the `1.4.0-alpha.1` package identity and unchanged sibling dependency graph.

## Deliberate exclusions

T1402 does not implement:

- hit testing;
- effective clipping;
- panel z-order routing;
- logical focus;
- focus repair;
- key gestures;
- command identity or binding registries;
- structured routing results;
- pointer-shape vocabulary or leases;
- terminal I/O;
- hidden input consumption;
- widgets or callbacks.

Those remain assigned to T1403 and later tranches.

## Completion rule

The implementation checkpoint is fully qualified. This documentation commit intentionally changes the exact head, so T1402 closes only when the resulting documentation-complete head passes the same seven-job pull-request qualification matrix.

The final exact T1402 head/workflow should be recorded externally in PR #29 after qualification rather than creating another self-referential source commit merely to record its own SHA.
