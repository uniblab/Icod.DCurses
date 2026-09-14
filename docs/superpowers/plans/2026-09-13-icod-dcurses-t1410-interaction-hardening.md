# T1410 Interaction Performance and Adversarial Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze repeatable performance/allocation expectations and adversarial bounds for the accepted 1.4 interaction-routing implementation without expanding its public API or ownership model.

**Architecture:** Add test-only hardening around the existing router/region/gesture implementation. Common synchronous routing paths are measured after warmup using the same current-thread allocation methodology accepted by T1309; adversarial tests exercise region/binding bounds, panel precedence churn, focus eligibility churn, and failure atomicity. A reflection/source audit proves routing remains callback-free, synchronous, bounded, and free of hidden Terminal I/O.

**Tech Stack:** C# 13; xUnit; .NET `net8.0;net9.0;net10.0`; `GC.GetAllocatedBytesForCurrentThread`; `Stopwatch`; existing internal test access to normalized `CursesInputEvent` factories.

**Spec:** `Icod.DCurses-1.4.0-Development-Roadmap.md` T1410, with measurement precedent from `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`.

## Global Constraints

- Work only on branch `1.4.0-interaction-routing` / PR #29.
- Preserve package identity `1.4.0-alpha.1` and `AssemblyVersion 1.0.0.0`.
- Preserve the public 1.4 fingerprint exactly: 62 exported types / 491 canonical contract lines / SHA-256 `8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147`.
- T1410 is expected to be tests/documentation only. Do not modify `src/` unless a new hardening test exposes a concrete defect.
- If a performance gate fails, do not raise the gate merely to make CI green. Inspect the failing path, distinguish fixed runtime noise from per-operation allocation/work, and correct production code only when the measured regression is structural.
- Use the T1309 repeated-sample allocation protocol: warm the exact path first, take eight measurements, and use the minimum measured allocation where the purpose is to isolate nondeterministic fixed runtime charges.
- Do not write benchmark data to stdout/stderr. Assertion messages may carry observed values when a gate fails.
- Use generous elapsed-time ceilings as regression tripwires, not claims of hardware-independent latency.
- Preserve 1TBS formatting and normal parameter-validation conventions.
- No new background task, event loop, callback/delegate registration, asynchronous router API, or Terminal I/O may be introduced.

## Frozen measurement profile

Representative topology:

```text
Screen:                    160 x 60
Ordinary regions:          256
Local gesture bindings:    64
Global gesture bindings:   64
Warmup iterations:         4,096
Allocation sample count:   8
Allocation sample length:  10,000 operations
Throughput loop length:    10,000 operations per path
Throughput combined gate:  <= 15 seconds
```

Allocation ceilings are derived from the current object model rather than arbitrary throughput targets:

```text
HitTest miss:              minimum sample == 0 bytes
MoveFocus traversal:       minimum sample == 0 bytes
Successful HitTest:        <= 96 bytes / operation
Local/global command Route <= 96 bytes / operation
Mouse-targeted Route:      <= 192 bytes / operation
```

The successful paths intentionally return immutable reference-type snapshots, so zero allocation is not the contract for those calls. The ceilings allow one expected result snapshot (or hit + route snapshot for mouse) plus alignment headroom while rejecting accidental collection/snapshot allocation in the hot path.

---

### Task 1: Freeze common-path allocation and throughput gates

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesInteractionPerformanceHardeningTests.cs`

**Interfaces:**
- Consumes existing `CursesInteractionRouter.HitTest`, `MoveFocus`, `Route`, `RegisterRegion`, `BindGlobalGesture`, `CursesInteractionRegion.BindGesture`, and internal normalized input factories.
- Produces no library API; produces repeatable CI allocation/performance gates.

- [ ] **Step 1: Add representative topology helpers**

Create a GPL-headed test class. Use a helper that registers 256 enabled/focusable ordinary regions, each with `Bounds = new CursesRectangle(0, 0, 1, 1)`, monotonically increasing `TraversalOrder`, and identical hit priority. Later registration therefore remains the deterministic hit winner at `(0,0)` while `(59,159)` is a deterministic in-screen miss.

Bind 64 local numbered-function gestures on the final focused region and 64 global numbered-function gestures on the router. Use local commands named `local.00` through `local.63` and global commands named `global.00` through `global.63`.

Create all normalized input objects before allocation measurements:

```csharp
CursesInputEvent localInput = CursesInputEvent.FromKey(
    CursesKey.Function,
    functionKeyNumber: 63
);
CursesInputEvent globalInput = CursesInputEvent.FromKey(
    CursesKey.Function,
    modifiers: CursesKeyModifiers.Alt,
    functionKeyNumber: 63
);
CursesInputEvent mouseInput = CursesInputEvent.FromMouse(
    new CursesMouseEvent(
        CursesMouseAction.Move,
        CursesMouseButton.None,
        column: 0,
        row: 0
    )
);
```

Use `Alt+F0..F63` for global gestures so local unmodified function bindings cannot shadow them.

- [ ] **Step 2: Add zero-allocation miss/focus tests**

Warm each exact path for 4,096 calls. Then take eight 10,000-operation current-thread allocation samples and assert the minimum is exactly zero:

```csharp
Assert.Equal( 0, MeasureMinimumAllocatedBytes( () => {
    CursesInteractionHit? hit = router.HitTest( 59, 159 );
    if ( hit is not null ) {
        throw new InvalidOperationException( "Expected a miss." );
    }
} ) );
```

and separately:

```csharp
Assert.Equal( 0, MeasureMinimumAllocatedBytes( () => {
    _ = router.MoveFocus( CursesFocusDirection.Forward );
} ) );
```

The helper itself must allocate nothing inside the measured inner loop beyond what the supplied operation performs.

- [ ] **Step 3: Add successful snapshot allocation ceilings**

For each path, warm 4,096 iterations, then measure eight samples of 10,000 operations and use the minimum sample.

Assert:

```text
successful HitTest        <= 960,000 bytes
local command Route       <= 960,000 bytes
global command Route      <= 960,000 bytes
mouse-targeted Route      <= 1,920,000 bytes
```

Every measured result must also be semantically validated inside the loop so the JIT cannot discard meaningful work: expected winning region, expected command identity, and expected mouse hit/local coordinates.

- [ ] **Step 4: Add representative elapsed-time regression gate**

After warmup, time one combined sequence of:

```text
10,000 successful HitTest calls over 256 overlapping regions
10,000 forward MoveFocus calls over 256 focusable regions
10,000 local command Route calls over 64 local bindings
10,000 global command Route calls over 64 global bindings
```

Assert semantic checksums/counters and:

```csharp
Assert.True(
    elapsed <= TimeSpan.FromSeconds( 15 ),
    $"Representative interaction loop took {elapsed}."
);
```

This is a broad regression tripwire only; the closure document must not present it as a latency guarantee.

- [ ] **Step 5: Run focused tests**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~CursesInteractionPerformanceHardeningTests
```

Expected: PASS on all three target frameworks. If only an allocation ceiling fails, inspect the observed bytes from the assertion and the relevant implementation before changing either source or threshold.

- [ ] **Step 6: Commit Task 1**

```text
git add tests/Icod.DCurses.Tests/src/CursesInteractionPerformanceHardeningTests.cs
git commit -m "Harden interaction routing performance"
```

---

### Task 2: Bound, churn, topology, focus, and failure atomicity stress

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesInteractionAdversarialHardeningTests.cs`

**Interfaces:**
- Consumes frozen bounds `MaximumRegions`, `MaximumRegionGestureBindings`, `MaximumGlobalGestureBindings`, and `MaximumGestureBindings` plus existing region/panel/focus APIs.
- Produces no library API; freezes adversarial behavior at and around the bounded limits.

- [ ] **Step 1: Stress region capacity and slot recovery**

Register exactly `CursesInteractionRouter.MaximumRegions` overlapping regions. Assert the later registration wins hit precedence. Attempt one additional registration and assert `InvalidOperationException` before any observable precedence mutation.

Dispose every second registered region, register exactly the same number of replacements, and assert:

- no capacity failure occurs before the recovered slots are consumed;
- the last replacement deterministically wins the overlap hit;
- one additional registration again fails;
- all surviving pre-churn regions remain usable until explicitly disposed.

- [ ] **Step 2: Stress total gesture-binding capacity and recovery**

Register 64 ordinary regions. On each region bind exactly 256 character gestures, using the same 256 Unicode scalars per region so gesture identity remains valid while the total reaches exactly 16,384 bindings.

Assert a new global binding fails with `InvalidOperationException`. Dispose one fully-bound region, then bind 256 distinct global gestures successfully, proving disposal immediately returns capacity to the owning router. The next additional binding must fail again without altering existing command resolution.

- [ ] **Step 3: Stress overlapping panel topologies**

Create 16 overlapping visible panels and eight interaction regions per panel, all covering the same local point. Repeatedly:

- move a selected panel to top;
- vary same-panel `HitTestPriority`;
- hide/show one panel;
- move/resize a panel within the screen;
- call `HitTest` at the shared coordinate.

Run at least 256 topology mutations and assert every result matches current panel z-order first, then same-panel priority, then registration ordinal. Repeat the exact mutation sequence on a freshly constructed topology and assert the sequence of winning logical indices is identical.

- [ ] **Step 4: Stress focus eligibility and geometry churn**

Register 128 focusable regions with deterministic traversal order. Across at least 1,024 iterations, rotate these operations on the currently focused region:

```text
disable / re-enable
make non-focusable / focusable
move bounds off-screen / restore
explicitly clear and traverse forward/backward
```

Assert after each explicit focus boundary that the selected region is the deterministic next eligible region under `(TraversalOrder, RegistrationOrdinal)` and that equivalent replay produces the same focus-index sequence.

- [ ] **Step 5: Freeze invalid-operation atomicity**

Add focused cases proving:

- duplicate global gesture binding throws and the original global command still routes;
- duplicate local binding throws and the original local command still routes;
- invalid/foreign focus attempts leave the prior eligible focus unchanged;
- region-capacity overflow leaves the prior hit winner unchanged;
- operations on disposed router/region objects fail without reviving registrations or bindings.

Do not inspect private state when a public routing/focus observation can prove atomicity.

- [ ] **Step 6: Run focused adversarial tests**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~CursesInteractionAdversarialHardeningTests
```

Expected: PASS on all three target frameworks.

- [ ] **Step 7: Commit Task 2**

```text
git add tests/Icod.DCurses.Tests/src/CursesInteractionAdversarialHardeningTests.cs
git commit -m "Stress interaction routing bounds and churn"
```

---

### Task 3: Audit synchronous ownership and hidden-I/O boundaries

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesInteractionOwnershipAuditTests.cs`

**Interfaces:**
- Consumes reflection over public `CursesInteractionRouter` / `CursesInteractionRegion` plus repository source files already located by the test root helper pattern.
- Produces no library API; freezes the absence of callbacks, asynchronous router work, and direct Terminal I/O in routing source.

- [ ] **Step 1: Freeze callback-free synchronous public shape**

Use reflection to assert:

```text
CursesInteractionRouter public events == 0
CursesInteractionRegion public events == 0
```

For every public declared instance method on those two types:

- no parameter type derives from `Delegate`;
- return type is not `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`;
- no method returns `IAsyncEnumerable<T>`.

Exclude inherited `object` members by using `BindingFlags.DeclaredOnly`.

- [ ] **Step 2: Freeze no hidden Terminal-I/O source dependency in routing paths**

Read these exact repository files:

```text
src/CursesInteractionRouter.cs
src/CursesInteractionRouter.Routing.cs
src/CursesInteractionRegion.cs
src/CursesInteractionRegion.Bindings.cs
src/CursesInteractionHit.cs
src/CursesInteractionResult.cs
src/CursesKeyGesture.cs
```

Assert none contains:

```text
Icod.Terminal
TerminalSession
WriteAsync(
ReadAsync(
AcquirePointerShapeAsync(
Task.Run(
```

Pointer application remains intentionally outside these routing files on `CursesSession`/the DCurses lease wrapper.

- [ ] **Step 3: Freeze bounded-registry constants and observable enforcement**

Assert the four public constants remain exactly:

```text
MaximumRegions = 4096
MaximumRegionGestureBindings = 256
MaximumGlobalGestureBindings = 1024
MaximumGestureBindings = 16384
```

Pair these reflection assertions with one small behavioral enforcement check for each per-router/per-region limit so a future constant change cannot become documentation-only drift.

- [ ] **Step 4: Run focused audit tests**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~CursesInteractionOwnershipAuditTests
```

Expected: PASS on all three target frameworks.

- [ ] **Step 5: Commit Task 3**

```text
git add tests/Icod.DCurses.Tests/src/CursesInteractionOwnershipAuditTests.cs
git commit -m "Audit interaction routing ownership boundaries"
```

---

### Task 4: Full hardening qualification and T1410 closure

**Files:**
- Create: `docs/T1410-Interaction-Performance-Allocation-and-Adversarial-Hardening.md`
- Modify: PR #29 body after exact-head qualification

**Interfaces:**
- Consumes all T1410 test evidence and the unchanged 1.4 API fingerprint.
- Produces the T1410 closure authority; T1411 begins only after its exact documentation-complete head is green.

- [ ] **Step 1: Run full Staging qualification**

Run/require the normal PR workflow on the complete hardening-test head:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```

All must be green on the exact same SHA.

- [ ] **Step 2: Confirm public API fingerprint remains unchanged**

Require:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Any fingerprint movement is a tranche blocker unless separately reviewed as a necessary production correction.

- [ ] **Step 3: Write closure document**

Record:

- exact test-only implementation head and workflow;
- representative topology and iteration counts;
- frozen allocation ceilings and their object-model rationale;
- elapsed-time gate explicitly described as a regression tripwire, not a latency guarantee;
- region/binding max-capacity churn results;
- panel topology and focus churn determinism;
- invalid-operation atomicity evidence;
- callback/background-work/Terminal-I/O audit result;
- whether any production correction was required;
- unchanged dependency/public API surface.

- [ ] **Step 4: Qualify documentation-complete head**

After committing the closure document, require a fresh seven-job PR matrix on that exact documentation head. Do not combine T1411 work into this SHA.

- [ ] **Step 5: Update PR #29 ledger**

Mark T1410 complete with exact closure SHA/workflow and mark T1411 as next. This metadata-only update must not change the qualified branch SHA.
