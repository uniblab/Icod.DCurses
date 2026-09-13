# T1408 — Resize, Panel, Disposal, and Lifecycle Coherence

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1408  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** coherence acceptance head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1408 proves that the interaction mechanisms implemented by T1402–T1407 remain deterministic while logical-screen geometry, retained panels, session dimensions, lifecycle coordination, and Terminal-owned pointer state change.

This is intentionally an **acceptance/hardening tranche**. The T1401 architecture specified that interaction geometry remains application-owned and is resolved against current screen/panel state rather than maintained by a hidden subscription or layout graph. T1408 therefore begins by testing the already-composed behavior rather than presuming a new production mechanism is required.

The acceptance suite passed without any production correction. T1408 adds tests and this evidence record only; it does not change the public API fingerprint.

## Acceptance checkpoint

The T1408 coherence suite was added at:

`44ea46a735303f853ec3c7f44e855f630a9ae4f9`

Workflow #764 / `34732410658` passed all seven PR jobs on that exact head:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

Linux x64 qualification recorded:

```text
Build succeeded
0 warnings
0 errors
716 / 716 tests passed on net8.0
716 / 716 tests passed on net9.0
716 / 716 tests passed on net10.0
```

The six T1408 tests all passed on their first execution against the already-qualified T1407 production implementation. No source correction was necessary.

## Screen shrink/regrow coherence

T1408 proves that ordinary interaction-region geometry remains application-owned across logical-screen resize.

A region near the old lower-right edge is focused and owns a local command binding. Shrinking the screen until the region is fully clipped:

- does not mutate the region's declared `Bounds`;
- makes the region ineligible through current-screen clipping;
- repairs focus deterministically to the next eligible region when focus is observed;
- leaves the clipped region and its gesture binding registered.

Growing the screen again:

- preserves the same declared region rectangle;
- does not silently restore the former focus;
- makes the region eligible again under current geometry;
- allows explicit refocus;
- preserves the original local command binding.

This confirms that screen resize changes **effective geometry**, not application-owned declared geometry or registration identity.

## Panel churn coherence

T1408 exercises a panel-associated region while its retained panel is moved, resized, hidden, and shown.

The router follows current panel state without re-registration:

- old screen coordinates stop hitting after panel movement;
- new screen coordinates resolve against the moved panel;
- panel resize clips the effective interaction rectangle;
- hiding the panel removes it from hit/focus eligibility;
- logical focus repairs when the hidden panel made the focused region ineligible;
- showing the panel makes the still-registered region eligible again;
- explicit refocus restores local-command routing;
- the region's pointer-shape preference remains intact throughout.

No interaction geometry cache or panel-change subscription is required.

## Panel disposal coherence

Panel disposal does not dispose its interaction region.

T1408 proves that after the associated panel is disposed:

- the region no longer hits;
- the region is no longer focus eligible;
- current logical focus repairs away from it;
- `Focus(region)` returns `false` rather than reviving an invalid panel association;
- immutable/observational region state remains readable;
- the region's pointer preference remains diagnosable;
- its local gesture binding remains application-owned and can still be explicitly unbound.

The region itself remains live until the application disposes it, exactly as frozen by T1401.

## Live-dimension synchronization

T1408 uses a mutable Terminal control provider to exercise `CursesSession.SynchronizeDimensions()` against an already-created application router.

When Terminal dimensions shrink and later regrow:

- the materialized `CursesScreen` follows the current Terminal size;
- ordinary interaction region declarations remain unchanged;
- the router immediately resolves eligibility against the current `CursesScreen`;
- losing focus due to clipping does not erase registrations or command bindings;
- regrow does not automatically restore a previously lost logical focus;
- explicit refocus after regrow restores command routing without re-registration.

This preserves the 1.3/1.4 division of responsibility: DCurses synchronizes the logical screen, while applications remain responsible for any desired relayout of their own interaction geometry.

## Lifecycle-participant coherence

Terminal remains the lifecycle owner. DCurses contributes only its registered higher-layer lifecycle participant for curses-specific rendition/activity coordination.

T1408 repeatedly calls the DCurses participant's suspend preparation and resume callback while an application-owned router has focused regions, bindings, and pointer preferences.

Across repeated cycles:

- router registrations remain intact;
- logical focus remains intact when geometry/eligibility did not change;
- command routing remains deterministic;
- pointer preference metadata remains intact;
- no router lifecycle owner or background subscription is introduced.

The Terminal 1.11.1 lifecycle ordering was reviewed as part of this tranche. Terminal prepares higher-layer participants before core pointer-state suspension and resumes core state before completing higher-layer resume. Its `TerminalPointerShapeManager` is itself a core lifecycle participant which resets physical pointer state for suspension and re-enters the newest active owner after resume. DCurses therefore must not duplicate that state machine.

## Session disposal and pointer ownership

T1408 also proves closure behavior with a live DCurses pointer-shape lease.

The test:

1. opens a DCurses session and application-owned router;
2. registers/focuses a region and binds a command;
3. acquires `CursesPointerShape.Pointer` through the DCurses wrapper;
4. confirms Terminal emits the OSC 22 pointer request;
5. disposes the `CursesSession` while the lease remains live;
6. confirms Terminal closes pointer ownership with its terminal-policy reset;
7. disposes the wrapper lease repeatedly after session closure;
8. confirms the independent application-owned router still routes deterministically over its retained `CursesScreen` object.

This confirms that Terminal remains authoritative for physical pointer cleanup and that DCurses does not make router lifetime dependent on Terminal pointer-lease lifetime.

## No production delta

T1408 discovered no coherence defect requiring source modification.

That absence of a production delta is intentional evidence, not a skipped implementation step. The following already-qualified mechanisms compose successfully:

- T1402 region ownership and disposal;
- T1403 current-state hit testing and panel precedence;
- T1404 lazy deterministic focus repair;
- T1405 semantic gestures;
- T1406 binding/route preservation;
- T1407 Terminal-owned pointer leases;
- 1.3 logical-screen and panel resize behavior;
- existing Terminal lifecycle participation.

Adding another watcher, geometry cache, layout owner, pointer manager, or lifecycle owner would duplicate responsibilities and weaken the frozen architecture.

## Public API fingerprint

T1408 adds no production member or exported type. The current 1.4 alpha public API therefore remains:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

`docs/Public-API-Fingerprint-1.4.json` is unchanged from the T1407-qualified value.

## Scope audit

T1408 deliberately does **not** add:

- automatic region relayout on screen resize;
- automatic restoration of a previously clipped logical focus;
- panel-change subscriptions;
- hidden router background work;
- a second Terminal lifecycle owner;
- a second pointer-state manager;
- automatic pointer application during routing;
- new Terminal/TermInfo public dependencies;
- public API changes.

## Exit gate

T1408 is complete when this documentation-complete head passes the full PR package/runtime matrix. After that exact-head qualification, T1409 may begin the public-API-only interaction acceptance sample.
