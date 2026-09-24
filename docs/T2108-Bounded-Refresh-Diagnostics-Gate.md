# T2108 Bounded Refresh Diagnostics Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `878fcdd8630f97556ee2791509d805ed4a35ece7`\
**PR workflow:** [36035524413](https://github.com/uniblab/Icod.DCurses/actions/runs/36035524413), 14/14 jobs successful

## Public increment

`CursesSessionOptions.EnableRefreshDiagnostics` opts a session into one atomically published immutable snapshot per refresh attempt. The snapshot carries a saturating sequence, outcome, logical and prepared-work counts, full-repaint and invalidation flags, and semantic operation categories. Disabled sessions retain a null snapshot. No Terminal or TermInfo object, serialized output or private plan enters the public snapshot. The public 2.1 development fingerprint includes the exact accepted declarations; the 2.0 assembly identity remains unchanged.

## Implementation and failure evidence

- The disabled default and public declarations passed 14/14 jobs at `700521ba` in [workflow 35955896130](https://github.com/uniblab/Icod.DCurses/actions/runs/35955896130).
- Initial collection at `e646155` exposed an existing reflection boundary test for `CursesPreparedRefresh` in [workflow 36033192107](https://github.com/uniblab/Icod.DCurses/actions/runs/36033192107). The recording helpers moved into the internal accumulator at `4b284f2`.
- The output-cancellation fixture at `d9aea1d` cancelled a token but returned success from the write. [Workflow 36033885651](https://github.com/uniblab/Icod.DCurses/actions/runs/36033885651) exposed the mistaken expectation; `7f6256c` makes the fixture throw after recording output.
- The initial allocation comparison at `7f6256c` used a blocking test call rejected by xUnit1031 in [workflow 36034412087](https://github.com/uniblab/Icod.DCurses/actions/runs/36034412087). The awaited same-thread measurement at `63d9468` passed 14/14 jobs in [workflow 36034782897](https://github.com/uniblab/Icod.DCurses/actions/runs/36034782897).

## Qualification

| Requirement | Permanent evidence |
|---|---|
| enabled success, no-op, sequence and newest snapshot | `CursesRefreshDiagnosticsTests.EnabledRefreshPublishesOneBoundedSnapshotForEachAttempt` |
| cancellation before output and after a write | `CursesRefreshDiagnosticsTests` cancellation cases |
| failure before a write, failure after a write, invalidation and retry | `CursesRefreshDiagnosticsTests` failure and repaint cases |
| bounded scalar-only public state | `CursesRefreshDiagnosticsTests.SnapshotSurfaceContainsOnlyBoundedScalarData` |
| disabled versus enabled allocation on warmed no-op refreshes | `CursesRefreshDiagnosticsPerformanceTests` |
| editor Unicode slice and mapping complexity | `CursesTextLayoutPerformanceTests`, `CursesTextGeometryPerformanceTests` |
| large-content viewport memory bounded to the visible slice | `CursesViewportPerformanceTests` |
| prepared full-frame and sparse-block cost and damage | `CursesBulkCellWritePerformanceTests` |
| fixed/weighted track recomputation at 160×48 and large extents | `CursesTrackLayoutApplicationTests`, `CursesTrackLayoutPerformanceTests` |
| composed viewport, full frame and nine-cell local refresh | `CorePresentationTextPerformanceQualificationTests` |

The allocation comparison measures minimum same-thread bytes after warmup, bounding opt-in observation overhead to 1 KiB per no-op refresh. Existing tests qualify Unicode editor, viewport, track and prepared-cell workloads independently; the integrated qualification ensures a large content extent does not expand the visible frame and a local update prepares less work than a full frame. Elapsed timings are informational, not pass criteria. CI supplies exact-head .NET 8/9/10 verification on Windows, Linux and macOS because `dotnet` is unavailable locally.

## Next tranche

T2109 adds a roguelike application sample against these public APIs. PR #33 remains open for T2109–T2112; merge, tags, releases and package publication are maintainer actions.
