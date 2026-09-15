# T158 — Advanced Interaction Regret and Qualification Gate

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T158  
**Published compatibility floor:** `1.4.0`  
**Starting accepted tranche:** T157  
**Starting documentation head:** `9014bc8721ea8bde7248973d7d2d34e9ed2a8109`  
**Starting documentation workflow:** #886 / `34908876499`  
**AssemblyVersion:** `1.0.0.0`  
**Production dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** static and performance regret review complete; documentation-complete exact-head qualification required before T159

---

## Objective

T158 is the final pre-RC regret gate for the complete 1.5 advanced-interaction surface. It does not add another interaction feature. It asks whether the accepted scope, capture, spatial-focus, pointer-gesture, and scoped-command mechanisms are suitable to carry unchanged into the T159 RC/stable-source promotion.

The gate covers:

- steady-state allocation and broad throughput regression tripwires;
- maximum-capacity churn;
- exact compiler-derived public API comparison against published 1.4;
- package-only downstream consumption and XML documentation;
- root/package README and sample documentation;
- LGPL/GPL source-header and package-license consistency;
- dependency review against the current published Terminal/TermInfo lines;
- explicit public API naming, ownership, mutability, bounds, and layering regret.

If this gate found a production/API defect, the affected surface would return to a RED -> correction -> exact-head requalification cycle rather than being hidden inside release promotion.

## Published 1.4 compatibility floor

The published `v1.4.0` contract remains:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Published merge source:

```text
v1.4.0 -> 48d591aa427096be78c173ad8ed85566d7f671bf
```

The accepted 1.5 candidate contract remains:

```text
release 1.5.0-alpha.5
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

T156 and T157 were behavior/acceptance-only, and T158 adds qualification tests/documentation only. The compiler-derived fingerprint therefore remains the T155-promoted alpha.5 fingerprint; changing its release label merely to reflect a non-API tranche would misstate the contract history.

## Additive public API review

Version 1.5 adds exactly seven exported types over 1.4:

```text
CursesInteractionScope
CursesInteractionScopeOptions
CursesInteractionScopeLease
CursesPointerCaptureLease
CursesPointerTarget
CursesPointerGesture
CursesPointerGestureKind
```

It also adds narrowly scoped members to the existing 1.4 interaction types:

- `CursesInteractionRegionOptions.Scope`;
- scope registration/activation and pointer capture on `CursesInteractionRouter`;
- `MaximumScopes`, `MaximumScopeDepth`, and `MaximumScopeGestureBindings`;
- `CursesFocusDirection.Up`, `Down`, `Left`, and `Right` while preserving `Forward = 0` and `Backward = 1`;
- nullable `CursesInteractionResult.PointerTarget` and `PointerGesture`;
- scope-level `BindGesture` / `UnbindGesture`.

No published 1.4 exported type or member is removed or renamed. `CursesInteractionResultKind` retains the published `Unrouted = 0`, `Targeted = 1`, `Command = 2` contract.

The delta remains **+7 exported types / +34 canonical declared contract lines**.

## Naming and abstraction regret review

The final names remain mechanism-oriented:

- `InteractionScope` is an eligibility/modal boundary, not a dialog/widget/view;
- `InteractionScopeLease` names reversible LIFO activation lifetime;
- `PointerCaptureLease` names explicit singular capture ownership without implying terminal mouse tracking ownership;
- `PointerTarget` is distinct from ordinary in-bounds `InteractionHit`, preserving signed out-of-bounds capture coordinates without weakening the old hit contract;
- `PointerGesture` is a deterministic normalized observation, not a callback/event dispatcher;
- spatial `FocusDirection` values extend the existing logical-focus vocabulary without introducing a second focus system;
- scope command bindings reuse `CursesKeyGesture` and `CursesCommand` instead of introducing handlers or execution policy.

No naming collision, hidden widget commitment, or misleading terminal-ownership implication was found that justifies changing the accepted surface before RC.

## Ownership and mutability regret review

The accepted ownership split remains coherent:

- one router owns its region/scope/capture/gesture routing state;
- the implicit root scope preserves ordinary 1.4 behavior;
- explicit scopes have immutable parentage and non-cascading disposal;
- scope activation is explicit, bounded, descendant-only, LIFO, and lease-owned;
- one explicit pointer capture may exist per router;
- capture never implies logical focus and never acquires Terminal protocol state;
- spatial focus mutates only DCurses logical focus;
- pointer gesture state is fixed-size per concrete mouse button and clock-free;
- commands remain identities/results and never invoke application callbacks;
- panel/screen/region mutations can invalidate pointer ownership, and invalidated ownership cannot resurrect after a later geometry/scope round trip;
- routing remains synchronous and performs no terminal I/O.

No ownership or mutability correction was found which warrants an API break or additive repair.

## Bounds and failure behavior

The complete interaction capacity model is intentionally finite:

```text
MaximumRegions                  4096
MaximumScopes                    256 explicit live scopes
MaximumScopeDepth                 32 explicit levels below root
MaximumRegionGestureBindings     256 per region
MaximumScopeGestureBindings      256 per explicit scope
MaximumGlobalGestureBindings    1024
MaximumGestureBindings         16384 total region + scope + global
ActivePointerCaptures              1
```

T156 already hardened capacity exhaustion, ordinal boundaries, invalidation, failure atomicity, stale lease disposal, deterministic replay, and nested ownership interactions. T158 adds a broad maximum-capacity churn gate which fills all 256 explicit scope slots, fills the total gesture-binding budget to exactly 16,384, releases/reuses scope-binding capacity, and repeats capture plus press/drag/release state transitions.

## Performance and allocation qualification

T158 extends the established 1.4 measurement discipline instead of creating a new benchmark methodology:

```text
warmup iterations       4096
allocation samples         8
normal iterations       10000
spatial iterations       1000
fixed sample noise       1024 bytes
```

The allocation gate takes the minimum of eight warmed same-thread samples so unrelated one-off runtime/JIT/test-harness thread allocations do not masquerade as product regressions.

The accepted regression ceilings are:

```text
root-scope local command routing       <= 96 bytes / operation + fixed sample noise
nested-scope successful HitTest        <= 96 bytes / operation + fixed sample noise
spatial Right/Left focus pair          <= 1024 bytes total in the minimum 1000-iteration sample
deep scoped-command lookup             <= 96 bytes / operation + fixed sample noise
captured out-of-bounds mouse routing   <= 256 bytes / operation + fixed sample noise
maximum-capacity combined churn        <= 60 seconds broad regression tripwire
```

The first four per-result ceilings preserve the published 1.4 snapshot/result cost model where the immutable result shape is unchanged. Captured mouse routing receives the broader 256-byte ceiling because it may materialize the immutable interaction result, signed `CursesPointerTarget`, and normalized `CursesPointerGesture` snapshots together.

These values are regression tripwires, not latency/throughput SLAs and not promises that every runtime will allocate exactly the ceiling.

### T158 fixture correction

The initial T158 performance-test head was:

```text
8fecfbd8f69555adfa469528105ba841f0d2b6e4
workflow #887 / 34914398546
```

Package validation succeeded and production/test compilation was clean. Linux x64 and ARM64 then failed the new spatial-focus test on all three TFMs during test teardown with:

```text
InvalidOperationException: An interaction scope with live regions cannot be disposed.
```

The failure was deterministic test-fixture ownership, not a production or allocation defect: the test declared the explicit scope with `using` while leaving its 256 regions router-owned, so C# disposed the scope before router teardown could dispose those regions. The existing scope ownership guard correctly rejected that order.

The minimal correction left the scope router-owned so router disposal tears down regions before scopes. No production source, API, algorithm, or allocation threshold changed.

Corrected implementation/measurement head:

```text
9a8d0b2e2439bf4a936a5602d846a0c1bd20781f
workflow #888 / 34914622882
```

Workflow #888 passed all seven required Staging jobs without rerun:

1. Package candidate;
2. Windows x64;
3. Windows ARM64;
4. Linux x64;
5. Linux ARM64;
6. macOS x64;
7. macOS ARM64.

This qualifies the T158 allocation ceilings and maximum-capacity churn across all supported OS/architecture lanes and all three target frameworks.

## Compiler-derived API qualification

`PublicApiFingerprintTests` derives the public assembly contract from reflection, including exported types, constructors, fields/constants, properties, events, methods, generic constraints, ref kinds, defaults, and nullability. The T158 matrix revalidated the existing checked-in 1.5 candidate fingerprint rather than relying on a hand-maintained member list.

The exact result remained:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

The historical 1.4 fingerprint remains unchanged beside the 1.5 candidate evidence.

## Packed-artifact and downstream-consumer audit

The package candidate continues to verify:

- one `Icod.DCurses.dll` for each of `net8.0`, `net9.0`, and `net10.0`;
- generated XML documentation for each target framework;
- package README, LGPL license, icon, repository metadata, and assembly identity;
- portable symbols in the symbol package rather than the primary package;
- no native/runtimes/repository-only payload leakage;
- dependency closure derived from project declarations.

T157 additionally upgraded the isolated package consumer to consume the complete additive 1.5 interaction surface from the generated `.nupkg` on net8/net9/net10. That exercise deliberately uses only public consumer APIs: scopes, spatial focus, command binding/precedence, explicit capture lifetime, pointer target/gesture/result contracts, and capacity behavior. Internal synthetic `CursesInputEvent` factories were not widened merely to simplify smoke testing.

T158 found no packed-artifact mismatch requiring source/API correction.

## Documentation audit

`samples/README.md` already describes the 1.5 interaction sample accurately, including:

- modal and nested scopes;
- forward/backward plus spatial logical focus;
- scope/global command precedence;
- explicit pointer capture;
- `DragStart` / `DragMove` / `DragEnd` normalization;
- signed captured pointer targets;
- application-owned panel movement policy;
- the mechanism/policy boundary;
- explicit resize ownership;
- no callback dispatcher, hidden event loop, automatic focus-on-click, or drag/drop policy.

The root/package `README.md` was stale at T158 entry: it still described 1.3 as current stable and 1.4 as awaiting merge/publication even though 1.4 is published. T158 corrects that release-facing inconsistency and adds the active 1.5 mechanism summary. The source `Version` / `PackageVersion` intentionally remain `1.4.0` until T159 performs the planned RC promotion; the README distinguishes published package identity from the current development target rather than pretending the branch is already publishable as 1.5.

The active roadmaps are also synchronized from “T158 next” to “T158 complete / T159 next” only after this gate’s evidence is recorded.

## Source/header and licensing audit

The seven new exported production types and the new/changed interaction production partials retain the repository LGPL-3.0-or-later source preamble. The new T158 test and existing 1.5 interaction tests/sample additions retain the repository GPL preamble used for tests/samples.

The inspected public 1.5 types and additive public members carry XML summaries, including scope identity/options/lifetime, pointer capture lifetime, pointer target fields, pointer gesture fields/kinds, scope limits, region scope association, result target/gesture properties, and spatial focus enum values.

Package licensing remains:

```text
PackageLicenseExpression = LGPL-3.0-or-later
PackageRequireLicenseAcceptance = true
```

No licensing change is proposed by 1.5.

## Dependency regret gate

Current published lower-layer state at T158 is:

```text
Icod.Terminal stable: 1.14.0
Icod.TermInfo stable: 1.13.0
Icod.TermInfo 1.14.0: draft PR / stable-promotion qualification in progress
```

The DCurses 1.5 production graph remains deliberately:

```text
Icod.Terminal 1.13.0
Icod.TermInfo  1.12.0
```

No freshness-only bump is warranted in T158:

- Terminal 1.14 adds persistent-raster lifecycle observability, which the 1.5 Family-1 interaction mechanism does not consume;
- TermInfo 1.13 adds persistent-raster runtime-evidence integration, which the interaction router does not consume;
- TermInfo 1.14 backend availability/selection/planning remains outside the 1.5 scope and was not yet published stable at the time of this audit;
- none of the new 1.5 public interaction types exposes Terminal or TermInfo implementation types;
- T157 package-only consumption and T158 package qualification both succeed on the existing declared graph.

The correct dependency decision is therefore **retain the existing production versions for 1.5 RC unless an independent compatibility/security requirement arises before T159**.

## Public API regret decision

The complete T158 review found **no public API regret requiring a correction before RC**.

No correction is warranted for:

- exported type/member names;
- scope identity, hierarchy, activation, or disposal semantics;
- pointer-capture ownership or lifetime;
- separation of `CursesInteractionHit` and `CursesPointerTarget`;
- spatial focus direction numerics/ranking contract;
- clock-free pointer gesture vocabulary;
- region -> scope -> global command precedence;
- nullable structured-result evolution;
- callback-free result semantics;
- registry/capacity bounds;
- failure atomicity;
- Terminal/TermInfo ownership boundary;
- dependency exposure;
- package layout or XML documentation;
- licensing/header policy.

The accepted implementation/API should therefore advance unchanged to T159 after the documentation-complete T158 head passes the normal seven-job Staging matrix.

## Version-state note

At T158 the project still declares and packs:

```text
Version         1.4.0
PackageVersion  1.4.0
AssemblyVersion 1.0.0.0
```

This is intentional under the checked-in plan, not a release-ready 1.5 identity. T159 owns the explicit promotion sequence:

```text
1.5.0-rc.1
    -> exact-head qualification
1.5.0 stable-source
    -> exact-head qualification
```

The assembly version remains `1.0.0.0`. No merge, tag, GitHub Release, or NuGet publication is authorized by T158/T159 source work.

## Exit gate

T158 is complete only when the exact documentation-complete head containing this record, the performance gate, corrected root/package README, and synchronized active roadmaps passes:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

After that exact-head qualification, T159 may promote the **unchanged accepted implementation/API** to `1.5.0-rc.1`, qualify it, then promote the same implementation/API to stable-source `1.5.0` for final pre-merge qualification.
