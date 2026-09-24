# T2106 Large-Content Viewport Geometry Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `5d8063bd6e3f0ef08e842de21d33fdbce469dd54`\
**PR workflow:** [35950838881](https://github.com/uniblab/Icod.DCurses/actions/runs/35950838881), 14/14 jobs successful

## Public increment

`CursesCellPosition` provides validated immutable cell coordinates. `CursesViewport` represents caller-owned content extent, viewport extent, and clamped origin. Its immutable operations resize, move, pan, page, ensure a point or rectangle is visible, expand a visible slice by clipped overscan, and translate positions in either direction. None of these operations owns content or opens a terminal session. The separate additive 2.1 fingerprint contains the new contracts; the frozen 2.0 fingerprint is unchanged.

## Red and green evidence

- The constructor/visible-slice test initially failed compilation because `CursesViewport` did not exist at `176e68f`; the constructor implementation was compiled at `1ced90a` and the additive fingerprint recorded separately.
- The resize/navigation tests failed compilation on missing methods at `85fc52f` in [workflow 35949050722](https://github.com/uniblab/Icod.DCurses/actions/runs/35949050722). The implementation used widened pan and page arithmetic. An observed parameter-name mismatch for `MoveTo` was corrected.
- The visibility tests failed compilation on missing `CursesCellPosition`, ensure-visible, overscan, and translation APIs at `c08be37` in [workflow 35949610791](https://github.com/uniblab/Icod.DCurses/actions/runs/35949610791). The first implementation run then exposed a mistaken test expectation for smallest rectangle movement and an exported-type ordering mismatch; both were corrected.
- The executable head `5d8063b` passed all 14 package and runtime jobs in workflow 35950838881 across .NET 8, 9, and 10.

## Qualification

| Requirement | Permanent evidence |
|---|---|
| clamped origins, immutable resize/move/start/end, empty and oversized extents | `CursesViewportTests` |
| signed deltas and page multiplication at integer limits | `CursesViewportTests`, `CursesViewportBoundaryTests` |
| smallest movement, oversized rectangle leading edge, zero viewport axis | `CursesViewportTests`, `CursesViewportBoundaryTests` |
| overscan clipping and two-way translation with default output on failure | `CursesViewportTests`, `CursesViewportBoundaryTests` |
| large caller-owned document and algorithmic world limited to visible cells plus overscan | `CursesViewportPerformanceTests` |
| no per-operation heap allocation after warmup | `CursesViewportPerformanceTests.ViewportOperationsAllocateNothingAfterWarmup` |

The tests use exact visible-slice counts and same-thread allocation measurement; they do not set a timing threshold. GitHub Actions supplied exact-head verification because `dotnet` was unavailable locally.

## Next tranche

T2107 adds stateless fixed and weighted track layout. This PR remains open for the remaining 2.1 tasks.
