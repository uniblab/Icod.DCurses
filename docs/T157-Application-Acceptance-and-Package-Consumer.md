# T157 — Application Acceptance and Package Consumer

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T157  
**Theme:** public application acceptance and packed-package consumption  
**Starting accepted checkpoint:** `9a448aee6e987fe71549525af66aa7078234c46f`  
**Starting qualification:** workflow #878 / `34905872767` — seven-job Staging matrix green  
**Final implementation checkpoint:** `596843f798999573162be7883051030db353b940`  
**Final implementation qualification:** workflow #884 / `34908524571` — seven-job Staging matrix green

---

## Purpose

T157 proves that the complete additive 1.5 interaction-control surface is usable through public DCurses APIs in two independent consumer shapes:

1. the repository interaction sample, which receives live normalized input through `CursesSession` and demonstrates application-owned policy;
2. the existing isolated package-only consumer, which restores from the freshly packed `.nupkg` and executes under net8.0, net9.0, and net10.0.

T157 changes no library production code and introduces no new public API. The accepted 1.5 candidate fingerprint therefore remains:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

## RED contract checkpoint

The sample/package contract tests were strengthened first at:

```text
388b2f2f120656e5130e6e00662b762508f1ad40
workflow #880 / 34907458972
```

The new assertions required the acceptance surfaces to expose and document:

- explicit interaction scopes and nested scope parentage;
- scope activation leases;
- singular explicit pointer capture;
- `CursesPointerTarget` routing state;
- `DragStart`, `DragMove`, and `DragEnd` gesture vocabulary;
- cardinal spatial focus directions;
- scoped command bindings and modal boundaries;
- mechanism/policy separation;
- package-only consumption of the additive surface.

The inspected runtime lane failed exactly three new contract tests on net8/net9/net10 while the other 794 tests passed. The first missing markers were `CursesInteractionScopeOptions` in the sample/package smoke and `nested scope` in the sample documentation. Build and package validation otherwise remained healthy, establishing a focused RED checkpoint rather than a production regression.

## Public 1.5 interaction sample

The existing `samples/Icod.DCurses.Interaction.Sample` was advanced rather than creating a competing second interaction sample.

Implementation checkpoint:

```text
9ef763e7a29e63785d2a0db4fd336a54cbe0db81
```

The sample now demonstrates:

- an explicit popup interaction scope;
- a child nested popup scope;
- LIFO scope activation/disposal and saved-focus restoration;
- sequential Tab/Shift+Tab focus plus cardinal Up/Down/Left/Right spatial focus;
- region-local, scope-local, nested-scope, and router-global command identities;
- outer-scope command exclusion while the nested modal boundary is active;
- explicit primary-button pointer capture for popup dragging;
- captured `CursesPointerTarget` local coordinates outside declared bounds;
- normalized `DragStart`, `DragMove`, and `DragEnd` results;
- application-owned retained-panel movement in response to gesture results;
- explicit pointer-shape preference leases;
- resize-driven application relayout and deterministic cleanup of live capture/scope ownership.

The sample continues to use one DCurses event loop and imports only `Icod.DCurses` plus framework namespaces. It does not directly reference `Icod.Terminal`, `Icod.TermInfo`, internal DCurses types, a widget framework, callback-driven command execution, automatic focus-on-click, or hidden layout ownership.

## Mechanism versus policy

The sample documentation was advanced at:

```text
4631e0c355a668d7d9ac969e223df79059f7afcd
```

It explicitly separates mechanism from policy:

```text
DCurses mechanism
    scope boundaries
    focus decisions
    capture targets
    gesture classification
    command identity

application policy
    whether a drag moves a retained panel
    what a resolved command does
    when a modal surface is opened or closed
```

This preserves the approved non-widget architecture. T157 does not introduce callback dispatch, a retained event tree, drag/drop payload policy, or application navigation.

## First package-consumer attempt exposed a real downstream boundary

Initial package-consumer checkpoint:

```text
45ea4791a1fe9c31e41913028db65ed09c1f36dd
workflow #883 / 34908182717
```

All six runtime lanes built and tested the repository source successfully. The package candidate, however, correctly failed when the isolated consumer attempted to construct synthetic input with:

```text
CursesInputEvent.FromText(...)
CursesInputEvent.FromMouse(...)
```

Those factories are intentionally internal; `CursesInputEvent` has no public synthetic-event constructor/factory. Repository unit tests can use those helpers through test visibility, but a real NuGet consumer cannot.

This was not corrected by widening the public API. Adding public synthetic-event factories merely for smoke-test convenience would create unnecessary API commitment. Real applications receive normalized input through `CursesSession`.

## Final package-only consumer

The correction at:

```text
596843f798999573162be7883051030db353b940
```

keeps the isolated consumer entirely on public APIs. It behaviorally exercises the parts of the 1.5 surface that do not require fabricated terminal input:

- scope and nested-scope registration;
- scope binding/unbinding;
- outer and nested activation leases;
- deterministic saved-focus restoration;
- cardinal spatial focus navigation;
- explicit pointer capture acquisition;
- automatic capture invalidation when a captured region becomes disabled;
- idempotent stale capture-lease disposal;
- re-acquisition after automatic release;
- 1.5 scope capacity constants and frozen focus-direction numerics.

It additionally compiles/reflects the public routing result vocabulary required by real live input:

- `CursesInteractionResult.PointerTarget`;
- `CursesInteractionResult.PointerGesture`;
- `CursesPointerTarget.Region`;
- `CursesPointerGesture.Target`;
- `CursesPointerGestureKind.DragStart`;
- `CursesPointerGestureKind.DragMove`;
- `CursesPointerGestureKind.DragEnd`;
- `CursesInteractionRouter.CapturePointer(...) -> CursesPointerCaptureLease`.

Live gesture-routing behavior remains exercised by the interaction sample and the ordinary library tests; the package consumer does not bypass the public input boundary merely to synthesize terminal events.

## Packed-package qualification

Workflow #884 / `34908524571` passed the complete seven-job Staging matrix on exact head `596843f798999573162be7883051030db353b940`:

```text
Package candidate       success
Runtime Windows x64     success
Runtime Windows ARM64   success
Runtime Linux x64       success
Runtime Linux ARM64     success
Runtime macOS x64       success
Runtime macOS ARM64     success
```

The package-candidate log independently proves the isolated consumer flow:

```text
Fresh package consumer restore                 success
Fresh package consumer: net8.0                 compiled and executed successfully
Fresh package consumer: net9.0                 compiled and executed successfully
Fresh package consumer: net10.0                compiled and executed successfully
```

Package structure, metadata, dependency closure, assembly identity, XML documentation, and portable symbols also passed validation.

## Version-metadata observation carried into T158

The current development branch still declares and packs `1.4.0`:

```text
<Version>1.4.0</Version>
<PackageVersion>1.4.0</PackageVersion>
```

Accordingly #884 produced `Icod.DCurses.1.4.0.nupkg` and `.snupkg`. T157 deliberately does not silently promote release/package versions. The checked-in 1.5 plan reserves release-facing metadata/regret review for T158 and RC/stable-source promotion for T159. This discrepancy is therefore recorded explicitly for the next gate rather than hidden.

## T157 conclusion

T157 establishes both application-shaped and downstream-package evidence for the 1.5 Family-1 interaction mechanisms without expanding the public API for test convenience.

The strongest finding is the package-consumer boundary itself: public consumers can compose scopes, focus, capture, gesture/result types, and command identities, but synthetic `CursesInputEvent` construction remains internal. The acceptance design now respects that boundary instead of weakening it.

T158 may proceed with performance/allocation measurement, maximum-capacity churn, exact API/package/document/dependency review, and release-facing regret analysis.
