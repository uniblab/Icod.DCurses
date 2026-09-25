# Icod.DCurses 2.2 Effective-Binding Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Work inline; do not dispatch subagents.

**Goal:** Freeze the T2201 discovery contract and implement opt-in immutable effective single-key binding snapshots as the first additive 2.2 feature.

**Architecture:** `CursesInteractionRouter` remains the sole focus and scope authority. Region/scope/global owner dictionaries expose internal read-only enumeration to one new router discovery method. A snapshot copies visible effective bindings in route precedence order; ordinary routing is untouched.

**Tech Stack:** C# 13; .NET 8, 9, 10; xUnit; Staging PR CI. No Python. Local environment has no `dotnet`, so use the PR CI for the observed RED and GREEN gates.

**Spec:** `docs/superpowers/specs/2026-09-24-icod-dcurses-2.2-interaction-conveniences-design.md`; concrete first increment in `docs/2.2-Interaction-API-Design.md`.

## Global Constraints

- Preserve published 2.1 APIs and `AssemblyVersion 2.0.0.0`.
- Production depends directly on `Icod.Terminal 1.18.0` only; never add direct TermInfo/protocol access.
- New snapshots are read-only, bounded by the published router ceiling of 16,384 bindings, independent of later mutations.
- Existing `Route(CursesInputEvent)` behavior and allocation path do not change when discovery is unused.
- PR checks run Staging; main push checks run Release. Never merge or publish as part of this plan.

## Review Focus

1. **Region and global share a shortcut:** the focused region's command appears once (Task 2 test `RegionWinsAndSnapshotIsDetached`).
2. **Modal scope hides ancestors:** only bindings at or below the active boundary are visible (Task 3 test `ModalScopeDiscoveryMatchesRoute`).
3. **No focus in a scope:** active-scope and global bindings still appear (Task 3 test `NoFocusUsesActiveScope`).
4. **Binding mutates after discovery:** an earlier read-only list retains its old command (Task 2 test `RegionWinsAndSnapshotIsDetached`).
5. **Router disposal and unused path:** discovery throws after disposal, while existing `Route` regression tests continue to pass (Task 3 test `DisposedRouterRejectsDiscovery` and full suite).

---

### Task 1: T2201 evidence and API freeze

**Files:**
- Create: `docs/T2201-Interaction-Baseline-and-API-Questions.md`
- Create: `docs/2.2-Interaction-API-Design.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`, `Icod.DCurses-2.2.0-Development-Roadmap.md`

**Interfaces:** Consumes the immutable `docs/Public-API-Fingerprint-2.1.json` and published `Route`. Produces the exact `GetEffectiveGestureBindings` and `CursesCommandBinding` contract consumed by Tasks 2–3.

- [x] **Step 1: Read the actual baseline and both sample loops.** Confirm 96 exported types, 751 canonical lines, region→scope→global precedence, the editor's eight-digit prompt and the roguelike's help/movement switch. Record the observations in T2201 evidence.
- [x] **Step 2: Write the additive discovery design.** Specify `CursesCommandBinding` and `GetEffectiveGestureBindings`, read-only detached snapshots, canonical gesture ordering within each owner, modal exclusion, limits, and deferred prompt/timing scope in `docs/2.2-Interaction-API-Design.md`.
- [x] **Step 3: Review the spec and roadmap for conflicts.** `git diff --check` passed; `rg -n 'T2202|T2203|2.2.0-alpha.1'` confirmed T2202 discovery, T2203 sequences and alpha identity only after RED.
- [x] **Step 4: Commit the foundation documents.** PR #34 head `6df116b` carried the two roadmaps, evidence, API design and plan; no production code or package identity changed.

### Task 2: T2202 first discovery RED→GREEN

**Files:**
- Create: `src/CursesCommandBinding.cs`
- Create: `src/CursesInteractionRouter.Discovery.cs`
- Modify: `src/CursesInteractionRegion.Bindings.cs`, `src/CursesInteractionScope.Bindings.cs`, `Icod.DCurses.csproj`
- Modify: `tests/Icod.DCurses.Tests/src/PublicTwoOneDevelopmentIdentityTests.cs`, `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/PublicTwoTwoDevelopmentIdentityTests.cs`, `docs/Public-API-Fingerprint-2.2.json`
- Create: `tests/Icod.DCurses.Tests/src/CursesInteractionDiscoveryTests.cs`

**Interfaces:** Consumes existing `CursesKeyGesture`, `CursesCommand`, `CursesInteractionRouter.FocusedRegion`, `ActiveScope`, `RepairFocusIfNeeded` and binding dictionaries. Produces `CursesCommandBinding(CursesKeyGesture gesture, CursesCommand command)` with `Gesture`/`Command`, plus `IReadOnlyList<CursesCommandBinding> CursesInteractionRouter.GetEffectiveGestureBindings()`.

- [x] **Step 1: Write an executable first RED test.** In `CursesInteractionDiscoveryTests`, include GPL test header and `using Xunit;`:

```csharp
[Fact]
public void RegionWinsAndSnapshotIsDetached() {
    CursesScreen screen = new( 20, 10 );
    using CursesInteractionRouter router = new( screen );
    using CursesInteractionRegion region = router.RegisterRegion(
        new CursesInteractionRegionOptions( new CursesRectangle( 0, 0, 2, 2 ) ) {
            IsFocusable = true
        }
    );
    CursesKeyGesture key = CursesKeyGesture.ForKey( CursesKey.Enter );
    CursesCommand local = new( "local" );
    router.BindGlobalGesture( key, new CursesCommand( "global" ) );
    region.BindGesture( key, local );
    Assert.True( router.Focus( region ) );
    IReadOnlyList<CursesCommandBinding> before = router.GetEffectiveGestureBindings();
    Assert.Single( before );
    Assert.Equal( local, before[0].Command );
    Assert.Equal( local, router.Route( CursesInputEvent.FromKey( CursesKey.Enter ) ).Command );
    Assert.True( region.UnbindGesture( key ) );
    Assert.Equal( local, before[0].Command );
    Assert.Equal( "global", router.GetEffectiveGestureBindings()[0].Command.Name );
}
```

- [x] **Step 2: Observe the missing-API RED on PR CI.** Head `d853248` failed Linux x64 job `107883318336` with `CS0246` and `CS1061` on .NET 8/9/10 in workflow 36074699970; see T2201 evidence.
- [x] **Step 3: Implement only discovery.** Added the immutable value, internal owner enumeration, router snapshot and stable semantic-gesture sort. `Route` is unchanged. The extra ordering test failed before the sort on head `be34d3e` and passed after it on head `1d6097d`.
- [x] **Step 4: Advance identity and preserve historical API guards.** Version/package moved together; published 2.1 fingerprint remains unchanged; current tests use a new 2.2 development fingerprint. CI measured hash `06b8e57805004a8a7edc3ce787af86ca7e3d96c7ea7beecbd1ab541e744e1f2c`, 97 types and 756 lines from compiled head `79f9422`.
- [x] **Step 5: Observe GREEN on exact-head CI.** Head `1d6097d` passed all seven Staging jobs in workflow 36076001958; representative Windows/Linux/macOS logs show 1,233 tests green per TFM. The snapshot immutability assertion passed.

### Task 3: T2202 precedence, disposal and package qualification

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesInteractionDiscoveryTests.cs`
- Modify: `docs/Public-API-Fingerprint-2.2.json`, `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- Modify: `docs/2.2-Interaction-API-Design.md`, `Icod.DCurses-2.2.0-Development-Roadmap.md`
- Create: `docs/Public-API-Baseline-2.2.md`
- Create: `docs/T2202-Effective-Binding-Discovery-Gate.md`

**Interfaces:** Consumes `GetEffectiveGestureBindings()` from Task 2. Produces evidence of parity and package provenance; no additional production API.

- [x] **Step 1: Test modal and no-focus contexts.** `ModalScopeDiscoveryAgreesWithCommandRouting` exercises child-scope routing, hides a parent binding, keeps global bindings and clears focus to expose active scope/global discovery.
- [x] **Step 2: Test invalidity and ordering.** The tests cover disposed router, invalid binding arguments, detached read-only snapshots and canonical order. Ruling: a separate Space-key/modifier parity test would repeat existing `Route` matching tests because discovery never transforms a gesture; no additional normalization code entered this path. An empty router yields an empty read-only array by construction; the snapshot invariants were verified through observable nonempty bindings and mutation rejection.
- [x] **Step 3: Verify and record the gate.** Exact executable head `1d6097d`, workflow 36076001958, 7/7 jobs; package-only consumers for .NET 8/9/10 and live pseudo-terminal refresh passed. See `docs/T2202-Effective-Binding-Discovery-Gate.md` and `docs/Public-API-Baseline-2.2.md`.
- [x] **Step 4: Mark T2202 accepted only after the full gate.** Updated both roadmaps with exact evidence; `git diff --check` passed. Commit and push this documentation checkpoint, then continue with the separate T2203 design. Keep PR #34 draft and unmerged.
