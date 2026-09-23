# T2009 Behavioral Parity and Performance Gate

**Status:** accepted on exact executable head `eab9f4ab4c2da293cde12ed2e71f937bc76be134`; T2010 may begin.

## Frozen baseline comparison

The [T2001 1.6 behavioral baseline](T2001-DCurses-1.6-Behavioral-Baseline.md) names the existing full/sparse/no-op refresh, cursor, rendition, ACS/Unicode, erase/shift/scroll, hyperlink, raster, lifecycle, panels/pads, interaction, large-surface and allocation witnesses. The complete unfiltered suite ran on the 2.0 candidate; those witnesses were retained, including literal output assertions:

| 1.6 baseline workload | 2.0 executable gate |
| --- | --- |
| 160x60 full paint: 9,661 bytes, 121 writes, one flush | The same literal counts in `CursesOptimizationRegretTests.LargeFullRepaintHasDeterministicOutputCost`. |
| 1,000 one-cell updates: 2,000 bytes, 2,000 writes, 1,000 flushes | The same literal counts in `ThousandSmallUpdatesRemainDeterministicAndBounded`. |
| 32-column insert: 2 bytes / 1 write versus 34 bytes / 3 writes | The same optimized/fallback counts in `EditorCharacterShiftUsesExactCheaperTerminalPlan`. |
| Six-row delete: 4 bytes / 3 writes versus 166 bytes / 11 writes | The same optimized/fallback counts in `PagerLineShiftUsesExactCheaperTerminalPlan`. |
| 2,048x256 pad, 256 pans; sparse geometry/raster/panel allocation ceilings | The unchanged `CursesScaleHardeningTests`, `CursesLayoutApplicationAcceptanceTests`, `CursesPanelApplicationAcceptanceTests`, `CursesRasterAllocationHardeningTests` and semantic representation tests pass. |

The T2001 text described a 1.6 no-op flush on each call. The Terminal transaction cutover intentionally made clean no-op refreshes emit and flush nothing; T2005 accepted this behavior and `RepeatedNoOpRefreshDoesNotGrowTerminalWriteCount` now asserts the one initial flush only after 512 unchanged refreshes. Physical certainty and caller-driven invalidation are tested separately.

## 2.0 transaction workload bounds

The five 160x60 application shapes (roguelike, editor, pixel-art/HUD, tile-map, sprite/HUD) each exercise styled text, Unicode, semantic content, raster placeholders, a complete repaint, a sparse edit and clean no-op refreshes. T2009 measures emitted bytes as well as writes and flushes for the actual transaction path: a full frame is between 1 byte and 1 MiB, the sparse frame emits fewer bytes than its complete frame, and unchanged frames emit zero bytes/writes/flushes. The same output fixture measures at most 4 MiB of thread-local allocation for one sparse transaction after a warm full frame, and at most 2 MiB across 512 unchanged refreshes; tests require the measurement to stay on one thread. These are deliberate coarse ceilings across the six CI hosts, not timing benchmarks or invented exact 1.6 equivalents.

Terminal's 65,536-item and 64 MiB application-payload limits remain enforced before output; overflow retains damage for explicit retry. The frozen 1.6 retained representation limits (including the 36 KiB sparse-pad reference workload and bounded raster-row/metadata churn) still pass. Clean refreshes prepare no output transaction; the tests find no unbounded output or allocation growth in the accepted workloads.

## Exact-head evidence

[PR workflow 35823884700](https://github.com/uniblab/Icod.DCurses/actions/runs/35823884700) passed the package candidate with isolated NuGet consumer and live pseudo-terminal acceptance, and all six runtime jobs (Windows/Linux/macOS x64/ARM64). Each runtime job passed **1,055 tests per framework** for net8.0, net9.0 and net10.0 with zero failures and zero skips. The package build and verifier reported zero warnings and errors. No historical test was removed or disabled for this gate.

This gate authorizes the T2010 API/package/documentation freeze. It does not authorize a merge, tag or publication.
