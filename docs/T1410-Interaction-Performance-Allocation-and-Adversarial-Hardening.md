# T1410 — Interaction Performance, Allocation, and Adversarial Hardening

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1410  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** hardening test head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1410 freezes repeatable performance/allocation expectations and adversarial bounded-state behavior for the accepted 1.4 interaction-routing model. It does not add features or expand public API.

The work follows the evidence-first allocation methodology established by T1309: warm the exact path, take repeated current-thread allocation samples, and use the minimum sample where the purpose is to remove fixed runtime/JIT noise from a steady-state allocation assertion.

Implementation plan:

`docs/superpowers/plans/2026-09-13-icod-dcurses-t1410-interaction-hardening.md`

Planning head:

`5b7e8fe4654acf2ef99cfe4ff5e0079a3747197f`

## Representative measurement profile

The frozen common-path profile is:

```text
Screen:                    160 x 60
Ordinary regions:          256
Local gesture bindings:    64
Global gesture bindings:   64
Warmup iterations:         4,096
Allocation sample count:   8
Allocation sample length:  10,000 operations
Throughput loop length:    10,000 operations per path
Combined elapsed gate:     <= 15 seconds
```

The elapsed-time threshold is intentionally broad. It is a regression tripwire for accidental algorithmic/work amplification, **not** a hardware-independent latency guarantee or benchmark claim.

## Allocation contract

T1410 freezes these steady-state ceilings:

```text
HitTest miss:               minimum sample == 0 bytes
MoveFocus traversal:        minimum sample == 0 bytes
Successful HitTest:         <= 96 bytes / operation
Local command Route:        <= 96 bytes / operation
Global command Route:       <= 96 bytes / operation
Mouse-targeted Route:       <= 192 bytes / operation
```

The successful paths intentionally return immutable reference-type snapshots. Therefore their contract is not zero allocation. The successful-hit ceiling permits one expected hit snapshot plus alignment headroom; the command-route ceiling permits one expected routing-result snapshot plus alignment headroom; the mouse-route ceiling permits the expected hit plus routing-result snapshots plus alignment headroom. These limits reject accidental collection or panel-order snapshot allocation in normal routing paths.

The input objects used during route measurements are created outside the measured loops.

## Task 1 — common-path measurement qualification

Test source:

`tests/Icod.DCurses.Tests/src/CursesInteractionPerformanceHardeningTests.cs`

Exact head:

`829eaeb063d9bfbafdba78c9e154398a660b06f8`

Workflow #782 / `34762887943` passed all seven PR jobs.

The qualified tests prove:

- in-screen `HitTest` misses allocate zero bytes in the minimum steady-state sample;
- logical focus traversal allocates zero bytes in the minimum steady-state sample;
- successful hit snapshots stay within the 96-byte-per-operation ceiling;
- local and global semantic command routes stay within the 96-byte-per-operation ceiling;
- targeted mouse routing stays within the 192-byte-per-operation ceiling;
- the combined 256-region / 64-local-binding / 64-global-binding representative loop stays within the broad 15-second regression threshold.

No production correction and no threshold relaxation was required.

## Task 2 — bounds and adversarial churn qualification

Test source:

`tests/Icod.DCurses.Tests/src/CursesInteractionAdversarialHardeningTests.cs`

Exact head:

`f01f17333fed2fb3bb8e993355f91b957bae8010`

Workflow #783 / `34763127476` completed successfully across package candidate and all six runtime jobs.

### Region-capacity churn

The suite registers exactly:

```text
4,096 live regions
```

and proves:

- later registration remains the deterministic overlap winner;
- one additional registration fails before changing the winner;
- disposing every second region returns exactly 2,048 live-region slots;
- 2,048 replacement registrations consume those slots successfully;
- the final replacement becomes the deterministic overlap winner;
- one further registration again fails atomically;
- surviving original regions remain live.

### Total gesture-capacity churn

The suite fills the router to exactly:

```text
64 regions x 256 local bindings = 16,384 live gesture bindings
```

and proves:

- an additional global binding fails at total capacity;
- disposing one fully-bound region immediately returns 256 total-binding slots;
- exactly 256 global bindings can then be added;
- the final global binding routes correctly;
- one further binding again fails without replacing or corrupting the accepted route.

### Overlapping panel topology

The suite constructs:

```text
16 overlapping panels
8 overlapping interaction regions per panel
256 topology mutations
320 recorded hit observations per replay
```

The mutation sequence exercises:

- current panel z-order changes;
- same-panel hit priority changes;
- panel hide/show;
- panel move/resize;
- repeated hit resolution at the shared coordinate.

A second fresh construction replays the same mutation sequence and produces the same winner-identity sequence. Cross-panel z-order remains authoritative before same-panel priority and registration order.

### Focus eligibility churn

The suite registers 128 focusable regions and runs 1,024 deterministic mutations across:

- enabled state;
- focusability;
- on-screen/off-screen geometry;
- explicit clear plus forward/backward traversal.

A second fresh replay produces the same focused-region index sequence, while each repair point follows the frozen `(TraversalOrder, RegistrationOrdinal)` semantics.

### Failure atomicity

The suite also proves:

- duplicate global bindings fail while the original command continues routing;
- duplicate local bindings fail while the original local command continues routing;
- foreign-region focus attempts fail while prior focus remains unchanged;
- ineligible focus attempts do not displace valid focus;
- disposed-region mutations fail without reviving state.

No production correction was required.

## Task 3 — synchronous ownership and hidden-I/O audit

Test source:

`tests/Icod.DCurses.Tests/src/CursesInteractionOwnershipAuditTests.cs`

Exact complete hardening-test head:

`0d5509f684586fe6bd4ce11613afee3f8ede7450`

Workflow #784 / `34763319143` passed all seven PR jobs.

The audit freezes these architectural properties:

- `CursesInteractionRouter` exposes no public events;
- `CursesInteractionRegion` exposes no public events;
- neither type's declared public instance methods accept delegate parameters;
- neither type exposes `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, or `IAsyncEnumerable<T>` return types;
- routing source files contain no direct `Icod.Terminal` / `TerminalSession` dependency;
- routing source files contain no `WriteAsync(`, `ReadAsync(`, `AcquirePointerShapeAsync(`, or `Task.Run(` path;
- pointer-shape application therefore remains outside the router in the explicit `CursesSession`/lease boundary.

The four frozen bounds remain exactly:

```text
MaximumRegions = 4096
MaximumRegionGestureBindings = 256
MaximumGlobalGestureBindings = 1024
MaximumGestureBindings = 16384
```

The ownership audit additionally exercises the per-region and global binding limits behaviorally so those constants cannot drift into documentation-only values.

## Full hardening-test qualification

The exact complete test head is:

`0d5509f684586fe6bd4ce11613afee3f8ede7450`

Workflow #784 / `34763319143` passed:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

On Linux x64 the exact Staging build reported:

```text
0 warnings
0 errors
```

and the complete test suite reported on each target framework:

```text
735 passed / 0 failed / 0 skipped
```

for `net8.0`, `net9.0`, and `net10.0`.

## Public API and dependency result

T1410 changed tests and documentation only. It introduced no production source correction and no public API change.

The accepted 1.4 alpha fingerprint remains exactly:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Package identity remains `1.4.0-alpha.1`; `AssemblyVersion` remains `1.0.0.0`; runtime dependencies remain `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`.

## Scope audit

T1410 introduced no:

- production API;
- unbounded interaction collection;
- callback registration surface;
- asynchronous/background router work;
- hidden Terminal I/O;
- automatic focus/layout policy;
- widget/control abstraction;
- performance promise stronger than the measured regression tripwires.

## Exit gate

T1410 is complete when this documentation-complete head passes the full seven-job PR matrix without moving the accepted public API fingerprint. Only after that exact-head qualification may T1411 begin the final API/package/documentation/licensing regret gate.
