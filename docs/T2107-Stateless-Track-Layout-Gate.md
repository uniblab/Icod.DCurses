# T2107 Stateless Track Layout Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `8c53b3e30a3780884c26ca609f6a1a255c56cf6d`\
**PR workflow:** [35954736073](https://github.com/uniblab/Icod.DCurses/actions/runs/35954736073), 14/14 jobs successful

## Public increment

`CursesTrack.Fixed` and `CursesTrack.Weighted` describe immutable preferred sizes, weights, minimums, and optional maximums. `CursesLayout.ArrangeRows` and `ArrangeColumns` return independent rectangles for up to 4,096 tracks, with explicit gaps and six surplus distributions. The operations are pure geometry: they do not create windows or retain application content. The additive 2.1 API fingerprint includes the new types and methods; the 2.0 baseline remains unchanged.

## Red and green evidence

- The initial arrangement tests at `c4750e0` failed compilation because the track contracts did not exist. Basic fixed/weighted placement and the separate fingerprint then passed 14/14 jobs at `4124565` in [workflow 35952134518](https://github.com/uniblab/Icod.DCurses/actions/runs/35952134518).
- Capped redistribution, minimum feasibility, six surplus placements, count limits, and extreme input tests at `e33b91e` exposed the incomplete initial behavior in [workflow 35954043212](https://github.com/uniblab/Icod.DCurses/actions/runs/35954043212).
- The cap and distribution implementation at `eb0fb88` passed 14/14 jobs in [workflow 35954347863](https://github.com/uniblab/Icod.DCurses/actions/runs/35954347863). The subsequent application and allocation tests passed all 14 jobs at executable head `8c53b3e` in workflow 35954736073.

## Contract qualification

| Requirement | Permanent evidence |
|---|---|
| fixed ideals, weighted shares, low-index remainder, explicit gaps | `CursesTrackLayoutTests` |
| minimum reservation, fixed preference compression in track order, maximum saturation, redistribution | `CursesTrackLayoutTests` |
| start, center, end, between, around, evenly, including one-track and odd surplus | `CursesTrackLayoutTests` |
| invalid factories, infeasible minimums, gap arithmetic, 4,096/4,097 tracks, integer-limit geometry | `CursesTrackLayoutTests` |
| transposed row/column arithmetic and editor/roguelike resize recomputation | `CursesTrackLayoutApplicationTests` |
| 4,096-track result array plus bounded scratch independent of content extent | `CursesTrackLayoutPerformanceTests` |
| legacy split/dock allocation parity | `CursesTrackLayoutPerformanceTests.ExistingDockAndSplitOperationsRemainAllocationFree` |

The 4,096-track allocation gate measures the minimum of eight same-thread samples, requiring at least the result array's 64 KiB and at most 128 KiB including scratch. A small track set stays below 4 KiB even with an `int.MaxValue` coordinate extent. The layout does not iterate per free cell; capped redistribution scans at most the track count per saturation round. Elapsed time is informational, not a pass criterion. GitHub Actions supplied exact-head verification because `dotnet` was unavailable locally.

## Next tranche

T2108 introduces opt-in bounded refresh diagnostics and evidence-backed performance qualification. PR #33 stays open for the remaining 2.1 tasks.
