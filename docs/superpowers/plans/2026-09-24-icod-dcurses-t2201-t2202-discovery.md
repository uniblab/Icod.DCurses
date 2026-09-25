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
- [ ] **Step 3: Implement only discovery.** Add an immutable `CursesCommandBinding` that rejects invalid/default gestures and null commands. Add internal `IEnumerable<KeyValuePair<CursesKeyGesture,CursesCommand>> GestureBindings` properties to region and scope. In router's new partial file, call `ThrowIfDisposed()` and `RepairFocusIfNeeded()`, traverse eligible owners in precedence order, sort each owner's bindings by the published semantic gesture fields, and keep the first occurrence of each gesture in a `HashSet<CursesKeyGesture>`. Copy the results to an array and return `Array.AsReadOnly(array)`; do not mutate `Route`.
- [ ] **Step 4: Advance identity and preserve historical API guards.** Set `Version` and `PackageVersion` to `2.2.0-alpha.1` together, keep assembly `2.0.0.0`, write package release notes for discovery only. Keep the 2.1 fingerprint file unchanged. Change `PublicTwoOneDevelopmentIdentityTests` to verify the historical 2.1 fingerprint rather than the current project version, and add `PublicTwoTwoDevelopmentIdentityTests` checking alpha version, dependency, assembly and package notes. Point the current API fingerprint test at a *new* `docs/Public-API-Fingerprint-2.2.json` and add a test asserting all 96 published 2.1 exported types remain. Push the implementation and read CI's measured current fingerprint hash/type/line counts; fill the 2.2 development JSON from the measured output rather than guessing hashes. The observed fingerprint mismatch is a gate, not a claimed green run.
- [ ] **Step 5: Observe GREEN on exact-head CI.** Push the 2.2 development fingerprint and any necessary source correction. Require all Staging PR runtime and package jobs to pass on all three TFMs; verify snapshot immutability by casting to `IList<CursesCommandBinding>` and asserting mutation throws `NotSupportedException`.

### Task 3: T2202 precedence, disposal and package qualification

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesInteractionDiscoveryTests.cs`
- Modify: `docs/Public-API-Fingerprint-2.2.json`, `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- Modify: `docs/2.2-Interaction-API-Design.md`, `Icod.DCurses-2.2.0-Development-Roadmap.md`
- Create: `docs/Public-API-Baseline-2.2.md`
- Create: `docs/T2202-Effective-Binding-Discovery-Gate.md`

**Interfaces:** Consumes `GetEffectiveGestureBindings()` from Task 2. Produces evidence of parity and package provenance; no additional production API.

- [ ] **Step 1: Add tests for modal and no-focus contexts.** Bind Enter to global, parent scope and child scope, focus a child-scope region and activate the child scope. Assert discovery reports the region/child/global winners exactly as `Route` does; deactivate and assert parent becomes visible. Clear focus and assert active scope's binding wins. Use `CursesInteractionScopeOptions { Parent = parent }` and dispose region, scope leases and scopes in LIFO order.
- [ ] **Step 2: Add invalidity tests before any fix.** Assert a disposed router rejects discovery; binding constructor rejects `default(CursesKeyGesture)` and `null!` command; an empty router produces an empty read-only snapshot. Add a Space-key representation and modifier/phase parity test against `Route`. Watch at least one new test fail when it exposes a real gap; adjust the implementation if necessary.
- [ ] **Step 3: Verify and record the gate.** Require all existing and new .NET 8/9/10 tests green on the exact PR head, fresh package-only consumer jobs green, and direct `Icod.Terminal 1.18.0` package metadata intact. Record the reviewed additive API delta in `docs/Public-API-Baseline-2.2.md`. Record run ID, exact executable commit, test counts and any honest limitation in `docs/T2202-Effective-Binding-Discovery-Gate.md`; do not label a merely queued job green.
- [ ] **Step 4: Mark T2202 accepted only after the full gate.** Update both roadmaps with the exact evidence, run `git diff --check`, commit, push, and proceed to a separate T2203 API amendment and sequence implementation plan. Keep the PR draft and unmerged.
