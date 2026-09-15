# Icod.DCurses 1.5.0 Advanced Interaction Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved 1.5 advanced-interaction substrate: bounded interaction scopes, explicit pointer capture, deterministic spatial focus, clock-free pointer gestures, and scoped command resolution while preserving published 1.4 behavior.

**Architecture:** Extend the existing `CursesInteractionRouter` as the single interaction-ownership coordinator. New scope/capture/gesture state remains bounded and synchronous; the router continues to return immutable structured results and invokes no application callbacks or terminal I/O.

**Tech Stack:** C# 13; .NET `net8.0;net9.0;net10.0`; xUnit; existing GitHub Actions Staging matrix; compiler-derived public API fingerprinting; NuGet package-consumer tests.

**Spec:** `docs/superpowers/specs/2026-09-14-icod-dcurses-1.5-advanced-interaction-control-design.md`

## Global Constraints

- Published compatibility baseline: `Icod.DCurses 1.4.0`.
- Preserve `AssemblyVersion` as `1.0.0.0`.
- Preserve `CursesFocusDirection.Forward = 0` and `Backward = 1`.
- Existing region/global command behavior must remain unchanged when no explicit scope is used.
- Interaction routing remains callback-free, synchronous, bounded, and terminal-I/O-free.
- Starting production dependencies remain `Icod.Terminal 1.13.0` and `Icod.TermInfo 1.12.0` unless a later explicit dependency-regret gate changes them.
- Follow repository 1TBS formatting and existing validation conventions.
- Each tranche requires exact-head Staging qualification before acceptance.

---

## File structure

New production files should be split by responsibility rather than added wholesale to the already-large router file:

```text
src/CursesInteractionScope.cs
src/CursesInteractionScopeOptions.cs
src/CursesInteractionScopeLease.cs
src/CursesPointerCaptureLease.cs
src/CursesPointerTarget.cs
src/CursesPointerGesture.cs
src/CursesPointerGestureKind.cs
src/CursesInteractionRouter.Scopes.cs
src/CursesInteractionRouter.Capture.cs
src/CursesInteractionRouter.SpatialFocus.cs
src/CursesInteractionRouter.PointerGestures.cs
src/CursesInteractionRouter.Routing.cs
src/CursesInteractionRouter.cs
src/CursesInteractionRegion.cs
src/CursesInteractionRegionOptions.cs
src/CursesFocusDirection.cs
src/CursesInteractionResult.cs
```

New focused test files:

```text
tests/Icod.DCurses.Tests/src/CursesInteractionScopeTests.cs
tests/Icod.DCurses.Tests/src/CursesPointerCaptureTests.cs
tests/Icod.DCurses.Tests/src/CursesSpatialFocusTests.cs
tests/Icod.DCurses.Tests/src/CursesPointerGestureTests.cs
tests/Icod.DCurses.Tests/src/CursesScopedCommandTests.cs
tests/Icod.DCurses.Tests/src/CursesInteraction15CoherenceTests.cs
tests/Icod.DCurses.Tests/src/CursesInteraction15PerformanceTests.cs
```

### Task 1 / T150: Freeze 1.5 candidate contract and harden the old allocation measurement

**Files:**
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Create: `Icod.DCurses-1.5.0-Development-Roadmap.md`
- Create: `docs/Public-API-Fingerprint-1.5.json`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPanelApplicationAcceptanceTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesInteraction15ContractTests.cs`

**Interfaces:**
- Consumes: published 1.4 public surface.
- Produces: frozen candidate names/bounds/numerics and first 1.5 candidate fingerprint.

- [ ] **Step 1: Add RED contract tests for frozen old numerics and new direction numerics.**

```csharp
[Fact]
public void FocusDirectionNumericsPreserve14AndAppendSpatialDirections() {
	Assert.Equal( 0, (int)CursesFocusDirection.Forward );
	Assert.Equal( 1, (int)CursesFocusDirection.Backward );
	Assert.Equal( 2, (int)CursesFocusDirection.Up );
	Assert.Equal( 3, (int)CursesFocusDirection.Down );
	Assert.Equal( 4, (int)CursesFocusDirection.Left );
	Assert.Equal( 5, (int)CursesFocusDirection.Right );
}
```

Expected initially: compile failure because spatial enum values do not exist.

- [ ] **Step 2: Add RED contract tests for candidate bounds.**

```csharp
[Fact]
public void Interaction15BoundsAreFrozen() {
	Assert.Equal( 4096, CursesInteractionRouter.MaximumRegions );
	Assert.Equal( 256, CursesInteractionRouter.MaximumScopes );
	Assert.Equal( 32, CursesInteractionRouter.MaximumScopeDepth );
	Assert.Equal( 256, CursesInteractionRouter.MaximumScopeGestureBindings );
	Assert.Equal( 16384, CursesInteractionRouter.MaximumGestureBindings );
}
```

Expected initially: compile failure for the new constants.

- [ ] **Step 3: Harden the old no-panel allocation test without changing production code.**

```csharp
long minimumAllocated = long.MaxValue;
for ( int sample = 0; sample < 8; sample++ ) {
	long before = GC.GetAllocatedBytesForCurrentThread();
	for ( int index = 0; index < 10000; index++ ) {
		if ( screen.HasPanels ) {
			throw new InvalidOperationException(
				"An empty screen unexpectedly reported panels."
			);
		}
	}
	long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
	minimumAllocated = Math.Min( minimumAllocated, allocated );
}
Assert.Equal( 0, minimumAllocated );
```

- [ ] **Step 4: Add the enum values/constants only, with no feature behavior yet.**

```csharp
public enum CursesFocusDirection {
	Forward = 0,
	Backward = 1,
	Up = 2,
	Down = 3,
	Left = 4,
	Right = 5
}
```

and in `CursesInteractionRouter`:

```csharp
public const int MaximumScopes = 256;
public const int MaximumScopeDepth = 32;
public const int MaximumScopeGestureBindings = 256;
```

- [ ] **Step 5: Run targeted tests and then full Staging tests.**

```text
dotnet test Icod.DCurses.sln -c Staging --filter "FullyQualifiedName~CursesInteraction15ContractTests|FullyQualifiedName~CursesPanelApplicationAcceptanceTests"
dotnet test Icod.DCurses.sln -c Staging
```

Expected: zero failures.

- [ ] **Step 6: Generate/store the 1.5 candidate public API fingerprint and commit T150.**

Commit: `Freeze Icod.DCurses 1.5 interaction contract`

### Task 2 / T151: Bounded interaction scopes

**Files:**
- Create: `src/CursesInteractionScopeOptions.cs`
- Create: `src/CursesInteractionScope.cs`
- Create: `src/CursesInteractionScopeLease.cs`
- Create: `src/CursesInteractionRouter.Scopes.cs`
- Modify: `src/CursesInteractionRouter.cs`
- Modify: `src/CursesInteractionRegion.cs`
- Modify: `src/CursesInteractionRegionOptions.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesInteractionScopeTests.cs`

**Interfaces:**

```csharp
public sealed class CursesInteractionScopeOptions {
	public CursesInteractionScope? Parent { get; set; }
}

public sealed class CursesInteractionScope : IDisposable {
	public CursesInteractionScope? Parent { get; }
}

public sealed class CursesInteractionScopeLease : IDisposable { }

public CursesInteractionScope RegisterScope(
	CursesInteractionScopeOptions? options = null
);

public CursesInteractionScopeLease ActivateScope(
	CursesInteractionScope scope
);
```

`CursesInteractionRegionOptions` gains `CursesInteractionScope? Scope`; the region exposes the immutable association.

- [ ] Write RED root-compatibility, parent-ownership, depth, and maximum-count tests.
- [ ] Write RED LIFO activation and out-of-order disposal tests.
- [ ] Write RED hit/focus exclusion tests for active scopes.
- [ ] Write RED focus-save/restore tests.
- [ ] Implement immutable scope ownership and depth validation.
- [ ] Implement the active-scope lease stack and focus restoration snapshot.
- [ ] Integrate active-scope eligibility into hit/focus helpers.
- [ ] Run scope tests, all interaction tests, then full Staging tests.
- [ ] Commit: `Add bounded interaction scopes`.

### Task 3 / T152: Explicit pointer capture

**Files:**
- Create: `src/CursesPointerCaptureLease.cs`
- Create: `src/CursesPointerTarget.cs`
- Create: `src/CursesInteractionRouter.Capture.cs`
- Modify: `src/CursesInteractionRouter.Routing.cs`
- Modify: `src/CursesInteractionResult.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesPointerCaptureTests.cs`

**Interfaces:**

```csharp
public sealed class CursesPointerCaptureLease : IDisposable { }

public sealed class CursesPointerTarget {
	public CursesInteractionRegion Region { get; }
	public int LocalRow { get; }
	public int LocalColumn { get; }
	public bool IsInside { get; }
}

public CursesPointerCaptureLease CapturePointer(
	CursesInteractionRegion region,
	CursesMouseButton button
);
```

`CursesInteractionResult` gains `CursesPointerTarget? PointerTarget`.

- [ ] Write RED tests rejecting `None`, foreign/disposed/ineligible regions, and a second live capture.
- [ ] Write RED captured-move test with out-of-bounds signed local coordinates.
- [ ] Write RED automatic-release tests for matching release, region/panel/scope invalidation, and router disposal.
- [ ] Implement singular capture state with a generation/token so stale lease disposal is idempotent.
- [ ] Implement capture-aware target calculation before ordinary hit testing.
- [ ] Route matching release to the captured target before clearing capture.
- [ ] Wire existing eligibility/disposal pathways to capture repair.
- [ ] Verify uncaptured 1.4 mouse tests remain unchanged.
- [ ] Run targeted/full Staging tests and commit: `Add explicit pointer capture`.

### Task 4 / T153: Deterministic spatial focus

**Files:**
- Create: `src/CursesInteractionRouter.SpatialFocus.cs`
- Modify: `src/CursesInteractionRouter.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesSpatialFocusTests.cs`

**Interfaces:** existing `MoveFocus(CursesFocusDirection)` gains spatial behavior.

- [ ] Write RED cardinal-direction tests with obvious geometry.
- [ ] Write RED tests for no current focus and no spatial wrapping.
- [ ] Write RED tie-break tests for overlap, primary distance, perpendicular center distance, traversal order, and registration ordinal.
- [ ] Write RED clipping and active-scope tests.
- [ ] Implement `TryGetEffectiveScreenBounds` using long intermediates and clipped integer output.
- [ ] Implement integer lexicographic scoring with doubled centers.
- [ ] Dispatch sequential versus spatial directions in `MoveFocus`.
- [ ] Run targeted/full Staging tests and commit: `Add deterministic spatial focus navigation`.

### Task 5 / T154: Clock-free pointer gesture normalization

**Files:**
- Create: `src/CursesPointerGestureKind.cs`
- Create: `src/CursesPointerGesture.cs`
- Create: `src/CursesInteractionRouter.PointerGestures.cs`
- Modify: `src/CursesInteractionRouter.Routing.cs`
- Modify: `src/CursesInteractionResult.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesPointerGestureTests.cs`

**Interfaces:**

```csharp
public enum CursesPointerGestureKind {
	Press = 0,
	Release = 1,
	Move = 2,
	Click = 3,
	DragStart = 4,
	DragMove = 5,
	DragEnd = 6,
	WheelUp = 7,
	WheelDown = 8,
	WheelLeft = 9,
	WheelRight = 10
}

public sealed class CursesPointerGesture {
	public CursesPointerGestureKind Kind { get; }
	public CursesMouseButton Button { get; }
	public CursesKeyModifiers Modifiers { get; }
	public CursesPointerTarget? Target { get; }
}
```

`CursesInteractionResult` gains `CursesPointerGesture? PointerGesture`.

- [ ] Write RED one-to-one Press/Release/Move/wheel tests.
- [ ] Write RED click test for same-target press/release without movement.
- [ ] Write RED drag start/move/end tests proving drag release is not Click.
- [ ] Write RED cancellation tests for region/scope/capture invalidation during a press sequence.
- [ ] Implement bounded per-button press state for concrete buttons only.
- [ ] Feed capture-aware pointer targets into gesture classification.
- [ ] Ensure wheel events never mutate press/capture state.
- [ ] Run targeted/full Staging tests and commit: `Add deterministic pointer gesture routing`.

### Task 6 / T155: Scoped command bindings

**Files:**
- Modify: `src/CursesInteractionScope.cs`
- Create: `src/CursesInteractionScope.Bindings.cs`
- Modify: `src/CursesInteractionRouter.Routing.cs`
- Modify: `src/CursesInteractionRouter.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesScopedCommandTests.cs`

**Interfaces:**

```csharp
public void BindGesture(
	CursesKeyGesture gesture,
	CursesCommand command
);

public bool UnbindGesture(
	CursesKeyGesture gesture
);
```

- [ ] Write RED capacity/duplicate/invalid-gesture tests.
- [ ] Write RED precedence test: region > nearest scope > parent scope to active boundary > global.
- [ ] Write RED modal-boundary test proving outer inactive-scope binding does not leak inward.
- [ ] Write RED root-only compatibility test reproducing 1.4 local/global behavior.
- [ ] Implement bounded per-scope dictionaries and integrate them with `MaximumGestureBindings`.
- [ ] Implement scope-chain lookup with no callbacks or predicates.
- [ ] Run targeted/full Staging tests and commit: `Add scoped command bindings`.

### Task 7 / T156: Combined coherence and adversarial hardening

**Files:**
- Test: `tests/Icod.DCurses.Tests/src/CursesInteraction15CoherenceTests.cs`
- Modify production files only for defects proven RED.

- [ ] Add RED scenario: captured region becomes disabled during drag; later input cannot target it.
- [ ] Add RED scenario: nested scope active while focused panel hides; focus repairs only inside active subtree.
- [ ] Add RED scenario: region disposal during press sequence cancels pending click/drag ownership.
- [ ] Add maximum scope count/depth and total-binding mutation-atomicity tests.
- [ ] Add registration-ordinal near-exhaustion tests covering spatial focus and hit/capture precedence.
- [ ] Add deterministic replay test over identical state/event sequences.
- [ ] Fix only proven root causes.
- [ ] Run all tests across Staging matrix and commit: `Harden advanced interaction lifecycle coherence`.

### Task 8 / T157: Acceptance sample and packed consumer

**Files:**
- Modify: `samples/Icod.DCurses.Interaction.Sample/*`
- Modify: `samples/README.md`
- Modify: `tools/package-smoke/Program.cs`
- Test: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`

- [ ] Extend the sample with root region, modal scope, draggable region, and scoped/global commands.
- [ ] Demonstrate explicit capture and `DragStart/DragMove/DragEnd`.
- [ ] Demonstrate sequential and cardinal focus movement.
- [ ] Demonstrate nested scope activation and focus restoration.
- [ ] Extend package-smoke to compile/run every new public member on net8/net9/net10.
- [ ] Add sample-contract tests rejecting internal/Terminal protocol dependencies.
- [ ] Run sample/package qualification and commit: `Add 1.5 advanced interaction acceptance coverage`.

### Task 9 / T158: Performance, allocation, API, package, docs, and dependency regret gate

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesInteraction15PerformanceTests.cs`
- Create: `docs/T158-Advanced-Interaction-Regret-and-Qualification-Gate.md`
- Update: `docs/Public-API-Fingerprint-1.5.json`

- [ ] Measure steady-state root-scope routing against 1.4 behavior.
- [ ] Measure nested-scope hit testing, spatial focus, captured mouse routing, and scoped command lookup.
- [ ] Use warmed repeated allocation sampling rather than trusting one thread-allocation sample.
- [ ] Run maximum-capacity churn for scopes/bindings/capture/gesture state.
- [ ] Generate exact compiler-derived 1.5 API inventory and compare against 1.4.
- [ ] Audit XML docs, package README, sample docs, LGPL/GPL headers, and dependencies.
- [ ] Evaluate then-current stable Terminal/TermInfo without bumping merely for freshness.
- [ ] Run exact-head complete Staging matrix and commit: `Close Icod.DCurses 1.5 interaction regret gate`.

### Task 10 / T159: RC and stable-source closure

**Files:**
- Modify: `Icod.DCurses.csproj`
- Modify: `README.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `Icod.DCurses-1.5.0-Development-Roadmap.md`
- Create: `docs/T159-RC-and-Stable-1.5.0-Closure.md`

- [ ] Promote accepted implementation to `1.5.0-rc.1` and update release notes without behavior changes.
- [ ] Run full exact-head RC Staging matrix and inspect every job.
- [ ] Resolve any failure by systematic debugging rather than blind rerun acceptance.
- [ ] Promote unchanged accepted implementation/API to `Version=1.5.0` and `PackageVersion=1.5.0`; retain `AssemblyVersion=1.0.0.0`.
- [ ] Update README/roadmaps/closure evidence to stable-source status.
- [ ] Run final exact-head seven-job PR matrix.
- [ ] Present the PR for explicit maintainer merge approval; do not merge, tag, create a GitHub Release, or publish NuGet in this task.

## Self-review

- Spec coverage: Family-1 mechanisms map to T151-T155, combined lifecycle hardening to T156, package/sample and regret gates to T157-T159.
- Placeholder scan: exact performance ceilings are intentionally measured/frozen in T158 rather than guessed during planning; no implementation behavior is left undefined.
- Type consistency: scope, capture, pointer target, pointer gesture, focus directions, and result extensions use consistent names across the plan and design authority.
- Scope discipline: widgets, callbacks, drag/drop payloads, multi-click timing, layout expansion, accessibility, and raster work remain excluded.
