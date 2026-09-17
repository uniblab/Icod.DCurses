# T1609 — Retained Mixed-Media Adversarial, Performance, and Allocation Gate

## Status

**Accepted.**

T1609 hardens the retained mixed-media implementation without adding public API. The tranche exercises DCurses-owned placeholder validation, sparse retained-raster storage, repeated editing/composition churn, allocation cost shape, malformed/default raster tokens, deterministic caller-driven retry, cancellation, and physical-state failure atomicity while leaving Terminal authoritative for its published live ownership registry limits.

## Capacity boundary

DCurses owns local placeholder dimension validation and retains the published `1..256` row/column contract. `CursesRasterCapacityTests` verifies invalid dimensions fail locally before disposed-state observation, valid boundary dimensions pass local validation, and default `CursesRasterCell` misuse is mutation-atomic for both virtual-screen and window write paths.

Terminal remains authoritative for its live per-session persistent-raster ceilings. T1609 does not add a second DCurses registry or duplicate Terminal's bounded ownership implementation.

## Sparse storage and allocation hardening

T1609 added allocation/churn coverage proving:

- large logical screens materialize retained-raster storage only for touched rows;
- repeated set/remove churn returns raster cell count, allocated row count, and the retained-raster plane itself to zero;
- ordinary text-only mutation does not materialize raster storage and retains the existing allocation shape;
- repeated replacement at one already-materialized raster coordinate remains bounded to the intended small immutable-reference cost rather than accumulating row storage or hidden caches.

The sparse replacement allocation gate initially exposed a real implementation issue. Before the fix, 10,000 retained-raster replacements allocated exactly `2,000,000` bytes on each tested TFM, or `200` bytes per mutation. Root-cause analysis found `CursesVirtualScreen.SetRasterCellRaw(...)` calling inherited `ValueType.Equals(object)` on `CursesRasterCell`, which forced boxing/value-type comparison before allocating the intended immutable `CursesRasterCellReference`.

The minimal GREEN change adds an internal typed `CursesRasterCell.Equals(CursesRasterCell)` identity comparison based on the private DCurses placeholder reference plus row/column. Existing internal call sites then bind to that non-boxing overload. The exported API remains unchanged.

## Adversarial retained-state churn

The T1609 test set repeatedly exercises:

- sparse row materialization and release;
- large sparse surfaces;
- screen resize preservation and discard;
- panel move, hide/show, composition, and cleanup;
- pad viewport panning and re-projection;
- copy/overlay propagation and cleanup; and
- exact retained-raster counts after repeated mutation cycles.

These tests use bounded deterministic state assertions rather than wall-clock timing gates.

## Failure atomicity and retry

Injected refresh failures cover both sides of raster commitment:

- raster output failure before physical raster state can be committed;
- cancellation during raster emission;
- failure after raster output succeeds but before refresh completion; and
- cancellation after raster output succeeds but before refresh completion.

In every case DCurses invalidates physical knowledge and requires an explicit caller retry. No hidden source-image replay, automatic re-upload, background worker, or silent backend switch is introduced. A successful explicit retry re-emits the raster exactly as required, and a subsequent unchanged refresh is silent.

## Test-harness correction

The first T1609 test-only head failed during compilation because two internal test counters declared `protected set` accessors on `internal` properties. C# correctly rejected that shape with `CS0273`. This was a test-harness defect rather than a production failure; the setters were corrected before the hardening tests were evaluated.

## Accepted qualification

The exact accepted T1609 head is:

`fd9d28352ff64b47c6e9dfd9e81ed425125f9b86`

Workflow **#989 / run `35145912917`** completed successfully across all seven jobs:

- Package candidate — success
- Runtime Windows x64 — success
- Runtime Windows ARM64 — success
- Runtime Linux x64 — success
- Runtime Linux ARM64 — success
- Runtime macOS x64 — success
- Runtime macOS ARM64 — success

Linux x64 built with:

```text
0 Warning(s)
0 Error(s)
```

and passed:

```text
net8.0   898 / 898
net9.0   898 / 898
net10.0  898 / 898
```

The package candidate also passed on the same exact source head.

## T1609 outcome

T1609 found one internal allocation-regression defect and corrected it without changing the 1.6 public surface or dependency graph. Retained mixed-media storage remains sparse and bounded, ordinary no-media workloads do not materialize raster storage, failure/cancellation behavior remains caller-driven and deterministic, and Terminal remains the sole live raster ownership/capacity authority.

The next tranche is **T1610 — public API, package, documentation, dependency, and licensing regret gate**.
