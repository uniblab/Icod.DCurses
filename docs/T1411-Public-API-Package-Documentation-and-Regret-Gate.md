# T1411 — Public API, Package, Documentation, Licensing, and Regret Gate

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1411  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Current runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** candidate closure audit complete; exact-head package/runtime qualification required before T1412

---

## Objective

T1411 is the final regret gate before release-candidate promotion. It reviews the complete 1.4 interaction surface as a stable 1.x contract rather than as a sequence of implementation tranches.

The gate asks whether the accepted API, ownership model, packed-artifact behavior, documentation, licensing, and dependency graph are suitable to carry unchanged into `1.4.0-rc.1` and then stable `1.4.0`.

No new interaction feature belongs in this tranche. Any API correction discovered here would return the affected surface to implementation/requalification rather than being hidden inside release promotion.

## Published 1.3 compatibility floor

The published `v1.3.0` contract remains:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Tagged baseline:

```text
v1.3.0 -> c10ca043a666b85225f2d3b8955a1ac2075b0d31
```

The current 1.4 interaction contract is:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

All three supported target frameworks are required to reproduce that one contract before release promotion.

## Additive exported-type review

Version 1.4 adds exactly eleven exported types over the 1.3 floor:

```text
CursesCommand
CursesFocusDirection
CursesInteractionHit
CursesInteractionRegion
CursesInteractionRegionOptions
CursesInteractionResult
CursesInteractionResultKind
CursesInteractionRouter
CursesKeyGesture
CursesPointerShape
CursesPointerShapeLease
```

No 1.3 exported type is removed.

The additions divide into four deliberate groups:

1. **region/routing mechanism** — `CursesInteractionRouter`, `CursesInteractionRegion`, `CursesInteractionRegionOptions`, `CursesInteractionHit`, `CursesInteractionResult`, and `CursesInteractionResultKind`;
2. **logical focus** — `CursesFocusDirection`;
3. **semantic commands/gestures** — `CursesCommand` and `CursesKeyGesture`;
4. **pointer preference/state bridge** — `CursesPointerShape` and `CursesPointerShapeLease`.

The surface is additive over the published 1.3 geometry and retained-panel contract. It reuses `CursesRectangle`, `CursesScreen`, `CursesPanel`, `CursesInputEvent`, `CursesKey`, modifiers, and input-event phases instead of introducing parallel geometry or protocol vocabularies.

## Naming and semantic regret review

The final names describe mechanism rather than application policy:

- `InteractionRouter` routes normalized input but does not own an event loop;
- `InteractionRegion` identifies a logical target but is not a widget, window, or renderer;
- `Command` is an identity, not a callback/delegate container;
- `KeyGesture` is semantic and terminal-family independent;
- `FocusDirection` describes deterministic forward/backward logical traversal;
- `PointerShape` is a DCurses semantic preference vocabulary, not a leaked Terminal protocol type;
- `PointerShapeLease` accurately communicates reversible physical-state lifetime.

No naming collision or misleading abstraction was found which justifies a stable-API break.

## Mutability and ownership regret review

The accepted mutability split is intentional:

- immutable value/result objects represent observations and identities;
- live interaction-region configuration may be changed explicitly by the single writer while the region is live;
- region disposal is one-way;
- router focus is logical application state and changes only through explicit focus/traversal operations or deterministic eligibility repair;
- mouse hit testing does not implicitly focus a region;
- command routing returns structured data and never invokes application callbacks;
- ordinary region geometry remains application-owned across resize;
- panel-associated regions observe the panel's current geometry, visibility, disposal, and z-order;
- pointer preferences are routing observations; physical pointer state remains Terminal-owned through an explicit lease.

The split avoids hidden lifetime ownership, callback reentrancy, and duplicated Terminal state.

No mutability or ownership correction was found which warrants changing the accepted public contract.

## Bounds and failure behavior

The interaction registries remain deliberately finite:

```text
MaximumRegions = 4096
MaximumRegionGestureBindings = 256
MaximumGlobalGestureBindings = 1024
MaximumGestureBindings = 16384
```

T1410 adversarial qualification proves capacity exhaustion, duplicate bindings, invalid/foreign focus attempts, disposed-region mutation, and churn fail without corrupting prior accepted state.

The common routing mechanism remains synchronous and callback-free. Ownership-audit tests freeze the absence of router/region public events, delegate parameters, asynchronous router methods, hidden Terminal I/O, and background dispatch.

No unbounded collection or hidden event-loop surface entered the release.

## Public dependency boundary

The project now declares:

```text
Icod.Terminal 1.13.0
Icod.TermInfo 1.12.0
```

The dependency update does not change the frozen DCurses 1.4 public API fingerprint.

Terminal remains authoritative for:

- the one live terminal input reader;
- byte/protocol decoding and query-response routing;
- rich-input protocol acquisition/restoration;
- terminal/window-manager focus reporting;
- physical pointer-shape protocol/state and lease lifetime;
- session lifecycle and output serialization.

DCurses does not expose `TerminalPointerShape`, `TerminalPointerShapeLease`, or other new Terminal pointer implementation types through its public interaction contract.

No new `Icod.TermInfo.Inspection` or terminal-database ownership enters DCurses.

The refreshed dependency graph must pass an exact-head package/runtime matrix before this gate is complete.

## Packed-artifact consumer audit

`tools/package-smoke/Program.cs` consumes the package artifact rather than repository project internals and now exercises the 1.4 interaction surface in addition to the 1.3 geometry/layout surface.

The interaction package probe covers:

- router construction and screen identity;
- ordinary region registration;
- focus and forward traversal;
- deterministic hit testing;
- region-local coordinates;
- pointer-shape preference observations;
- local gesture binding/unbinding;
- global gesture binding/routing;
- semantic command identities;
- routing API availability;
- `CursesPointerShape` vocabulary;
- compile-time `CursesSession.AcquirePointerShapeAsync(...)` / `CursesPointerShapeLease` surface.

This protects against a source-tree-only success where the packed NuGet artifact would omit or misdeclare the interaction contract.

## Application acceptance audit

`Icod.DCurses.Interaction.Sample` uses only public DCurses APIs for the 1.4 mechanism and exercises:

- one application-owned event loop;
- logical focus and forward/backward traversal;
- region-local versus router-global command precedence;
- retained panel overlap and panel-aware mouse precedence;
- mouse screen and region-local coordinates;
- explicit pointer-shape leases;
- explicit resize/re-layout;
- terminal focus reports without conflating them with logical focus.

The sample does not reference `Icod.Terminal` or `Icod.TermInfo` directly for its interaction behavior and does not install a second input reader.

## Documentation audit

The release-facing root `README.md` now:

- identifies `1.3.0` as the published stable floor;
- identifies 1.4 as the active release-qualification line;
- records the `Icod.Terminal 1.13.0` / `Icod.TermInfo 1.12.0` graph;
- explains DCurses versus Terminal interaction/pointer ownership;
- documents bounded regions, logical focus, semantic gestures/commands, panel-aware hit testing, structured routing results, local coordinates, and explicit pointer leases;
- distinguishes logical application focus from terminal/window-manager focus;
- states that mouse hits do not automatically mutate logical focus;
- states that routing owns no event loop and performs no hidden terminal I/O;
- describes package-only 1.4 consumer coverage.

`samples/README.md` now provides a sample-selection table and documents that visible pointer/focus/keyboard protocol behavior depends on terminal support; lack of a visible pointer-shape change is not evidence that hit testing/routing failed.

The active long-range and 1.4 roadmaps are synchronized to T1411 and the refreshed dependency graph rather than the obsolete T1401 planning state.

## Source/header and licensing audit

The 1.4 production additions retain the repository's LGPL-3.0-or-later source preamble. The interaction sample and test additions retain the repository's GPL test/sample preamble.

The PR source/header sweep covers the new interaction production files, Terminal pointer integration file, sample project/source, and interaction test files. The two files not surfaced in the first paginated header search (`CursesInteractionResultKind.cs` and `CursesInteractionOwnershipAuditTests.cs`) were inspected directly and also contain the expected preambles.

Package licensing remains:

```text
PackageLicenseExpression = LGPL-3.0-or-later
PackageRequireLicenseAcceptance = true
```

No license change is proposed by 1.4.

## Performance and adversarial evidence carried into the regret gate

T1410 qualified the representative hot path with 256 ordinary regions, 64 local bindings, 64 global bindings, repeated 10,000-operation loops, and a broad 15-second combined regression tripwire.

Frozen steady-state allocation ceilings remain:

```text
HitTest miss:               minimum sample == 0 bytes
MoveFocus traversal:        minimum sample == 0 bytes
Successful HitTest:         <= 96 bytes / operation
Local command Route:        <= 96 bytes / operation
Global command Route:       <= 96 bytes / operation
Mouse-targeted Route:       <= 192 bytes / operation
```

T1410 also qualified maximum-capacity region/binding churn, deterministic overlapping-panel replay, focus-eligibility churn, failure atomicity, and the synchronous/no-hidden-I/O ownership audit without a production or API correction.

## Regret decision

The completed static T1411 review found **no public API regret requiring a breaking or additive correction before RC**.

Specifically, no correction is warranted for:

- public type/member naming;
- mutable versus immutable responsibilities;
- logical-focus ownership;
- mouse-to-focus behavior;
- local/global command precedence;
- result-versus-callback routing semantics;
- panel precedence or coordinate spaces;
- bounds/capacity behavior;
- pointer preference versus physical lease ownership;
- dependency exposure;
- nullability/exception policy as frozen by T1401 and exercised by the tranche tests.

The accepted implementation/API should therefore move unchanged into T1412 **only after** the exact head containing this record passes the normal seven-job PR matrix with the refreshed dependencies.

## Exit gate

T1411 is complete when the exact candidate-closure head containing this document passes:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

That matrix must restore the published `Icod.Terminal 1.13.0` and `Icod.TermInfo 1.12.0` packages, reproduce the frozen 1.4 fingerprint across `net8.0`, `net9.0`, and `net10.0`, and pass package-only consumption.

After that exact-head qualification, T1412 may promote the **unchanged implementation/API** first to `1.4.0-rc.1` and then, after RC qualification, to stable-source `1.4.0`.
