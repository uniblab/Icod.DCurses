# Icod.DCurses 0.7.0 — T708 Benchmark, API, Package, and Optimization-Regret Gate

**Release line:** `0.7.0`  
**Candidate target:** `0.7.0-rc.1`  
**Stable predecessor:** `0.6.0`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** T708 implementation complete; release-candidate validation active

## Objective

T708 reviews the complete 0.7 refresh/output optimization set before release-candidate promotion. No new optimization family or public API enters after this gate.

Correctness remains the first criterion. Byte/write reduction is accepted only where the retained physical-screen model can prove the same final screen and where every optimized path retains a deterministic ordinary-renderer fallback.

## Accepted optimization set

The 0.7 implementation retains:

- optional Terminal-owned synchronized-output framing (`T702`);
- cost-aware cursor motion (`T703`);
- cost-aware literal/`el`/`ed`/`clear` selection (`T704`);
- exact physical insert/delete-character shifts (`T705`);
- exact physical insert/delete-line and scrolling-region shifts (`T706`);
- differential retained-rendition transitions (`T707`).

No optimization was rejected at T708. Each remains conservative enough that the ordinary retained-screen renderer is still the correctness fallback.

## Deterministic workload results

The release gate uses synthetic advertised capabilities so protocol bytes can be compared exactly and repeatably. These numbers are maintainer fixtures, not universal terminal-performance claims.

### T701 reference workloads

The original reference costs remain:

| Workload | 0.7 result |
| --- | ---: |
| clean no-op refresh | 0 bytes / 0 writes / 1 flush |
| one ASCII cell with absolute cursor addressing | 10 bytes / 2 writes / 1 flush |
| one two-column Unicode cell | 12 bytes / 2 writes / 1 flush |
| T701 bold-cell reset-first reference | 19 bytes / 4 writes / 1 flush |

T707 intentionally improves the final row:

| Workload | T701 | T707 | Delta |
| --- | ---: | ---: | ---: |
| established-default → bold cell | 19 bytes / 4 writes | 13 bytes / 3 writes | -6 bytes (-31.6%); -1 write (-25%) |

### Full and large repaint

A `160 × 60` full repaint on a synthetic one-byte absolute-cursor terminal emits:

```text
9661 bytes / 121 writes / 1 flush
```

This workload exercises 9,600 logical cells and verifies that the optimization layers do not interfere with the established complete-repaint fallback.

### High-frequency small refreshes

After baseline synchronization, 1,000 alternating one-cell updates on an eight-column screen emit exactly:

```text
2000 bytes / 2000 writes / 1000 flushes
```

No wall-clock timing participates in correctness. This workload exists to exercise repeated retained-state transitions and ensure output cost remains bounded and deterministic.

### Editor-style character insertion

For a 32-column row and a two-column insertion at column 1:

| Path | Bytes | Writes |
| --- | ---: | ---: |
| optimized `ich` | 2 | 1 |
| ordinary rewrite fallback | 34 | 3 |

Both paths are asserted to produce identical logical/final retained-screen content.

### Pager-style line deletion

For a six-row, 32-column screen and one line deletion beginning at row 1:

| Path | Bytes | Writes |
| --- | ---: | ---: |
| optimized `dl` | 4 | 3 |
| ordinary rewrite fallback | 166 | 11 |

Both paths are asserted to produce identical final screen content.

## Failure and invalidation audit

The accepted optimization families have explicit recovery coverage:

- cursor selection never updates retained cursor knowledge before successful emission;
- erase state is recorded only after the erase sequence succeeds;
- character-shift failure invalidates retained physical/rendition/cursor knowledge and retries through ordinary rendering;
- line/scroll failure likewise invalidates retained state;
- temporary `csr` restoration is attempted with cancellation suppressed after an operation failure;
- simultaneous line operation and scroll-region restoration failures are both preserved;
- synchronized-output refresh framing preserves both refresh and lease-restoration failures when both occur;
- rendition output failure returns the engine to unknown/reset-first state through the existing refresh exception boundary.

No recovery path treats a partially emitted optimization as a successfully synchronized physical screen.

## Wide cells, semantic line cells, and window geometry

- character shifts reject ambiguous wide-cell/continuation and semantic-line regions;
- line shifts move only complete rows and therefore preserve complete wide-cell and semantic-line footprints;
- partial-width/nested window editing never implies terminal scrolling-region ownership;
- erase operations require the complete erased logical region to be default-styled blank.

The final logical screen remains the authority for all selection decisions.

## Public API regret review

The 0.7 public delta is frozen in `docs/Public-API-Baseline-0.7.md`.

The only accepted public addition is:

```csharp
CursesSessionOptions.UseSynchronizedOutput
```

It remains default `false`. One unnested synchronized refresh adds 16 protocol bytes for Terminal's begin/end frames, so automatic framing would be a measurable regression for sufficiently small refreshes.

The option is retained because it exposes a curses-shaped refresh transaction policy without exposing Terminal lease mechanics to ordinary DCurses callers.

All cost models, candidate resolvers, operation plans, retained physical state, and measurement helpers remain internal and are reflection-guarded by `PublicRefreshOptimizationApiContractTests`.

No public refresh-statistics/diagnostics type is added.

## Dependency boundary

No new `Icod.Terminal` or `Icod.TermInfo` type enters the public signature surface. Existing `PublicDependencyBoundaryTests` remain authoritative.

The dependency versions remain exactly:

```text
Icod.Terminal 1.0.0
Icod.TermInfo 1.10.0
```

## Package-only consumer

The fresh package consumer now executes the 0.7 public delta through a module-initializer smoke guard:

- default `CursesSessionOptions.UseSynchronizedOutput` must be `false`;
- an options instance initialized with `UseSynchronizedOutput = true` must retain the request.

This is compiled and executed from the packed package rather than the source-project reference.

## Analyzer/build policy

Staging and Release retain warning level 4 with warnings-as-errors. The PR matrix builds/tests `net8.0`, `net9.0`, and `net10.0` on Windows, Linux, and macOS, while canonical package validation exercises the fresh consumer.

No optimization correctness test depends on wall-clock timing.

## Allocation and elapsed-time review

The accepted 0.7 selectors are engine-owned and reused across refreshes. No public statistics object, per-refresh history, or persistent candidate collection is introduced.

Candidate expansion may allocate short-lived strings already required to emit the selected TermInfo operation. T708 found no reason to add a benchmark framework or allocation cache whose complexity would exceed the demonstrated output savings.

Large-screen and 1,000-refresh workloads are executed in the normal test matrix to catch pathological behavior. Elapsed time and allocation are observational; deterministic bytes, writes, retained-screen equivalence, and failure recovery are the release gates.

## Documentation/showcase decision

The README is updated for the 0.7 release candidate with:

- the optimization release theme;
- the opt-in synchronized-output option and fixed-cost tradeoff;
- the retained fallback/safety model;
- links to the 0.7 roadmap, tranche records, and public API baseline.

The existing interactive showcase continues to exercise repeated refresh, rich styles, semantic drawing, input, resize, and lifecycle invalidation. T708 deliberately does not force synchronized framing on the showcase because the accepted public policy is opt-in and default false. The fresh package consumer explicitly exercises the option instead.

## Gate decision

T708 may close and promote to `0.7.0-rc.1` when the exact pre-RC head passes:

- Windows Staging restore/build/test;
- Linux Staging restore/build/test;
- macOS Staging restore/build/test;
- canonical Staging package/fresh-consumer validation;
- the new deterministic optimization-regret workloads;
- public API and dependency-boundary reflection guards.

After a green rc.1 checkpoint, T709 is release closure only. No feature/API additions are permitted during T709.
