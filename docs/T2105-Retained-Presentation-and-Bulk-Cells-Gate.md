# T2105 Retained Presentation and Bulk Cells Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `b26b5a12333319f60a6cfbf9f0954629e892088c`\
**PR workflow:** [35947857085](https://github.com/uniblab/Icod.DCurses/actions/runs/35947857085), 14/14 jobs successful

## Public increment

`CursesWindow.PresentTextLayout` projects selected immutable visual lines into retained cells without moving the cursor or invoking refresh. The two `WriteCells` overloads accept one prepared row or a rectangular block with a source stride. The public 2.0 fingerprint remains immutable; the additive 2.1 fingerprint includes these three methods. The sole direct production package reference remains `Icod.Terminal 1.18.0`, and the assembly version remains `2.0.0.0`.

## Red and green evidence

- The bulk contract tests at commit `0e8f58c` failed with `NotImplementedException` for all six initial cases in [workflow 35942791318](https://github.com/uniblab/Icod.DCurses/actions/runs/35942791318). That run was later cancelled by the next head; its completed Linux ARM64 job preserved the observed failures.
- The public 2.1 fingerprint initially failed when the overloads were declared. Its separate development baseline was updated from the exact runtime report; the frozen 2.0 baseline was not edited.
- The unchanged-layout projection test at commit `35653e8` failed with three dirty cells instead of zero in [workflow 35943805677](https://github.com/uniblab/Icod.DCurses/actions/runs/35943805677). Rows are now composed before retained mutation so identical final cells avoid transient damage.
- The bulk implementation and corrected fixture passed all 14 jobs in [workflow 35943381835](https://github.com/uniblab/Icod.DCurses/actions/runs/35943381835). Projection composition passed all 14 in [workflow 35944152100](https://github.com/uniblab/Icod.DCurses/actions/runs/35944152100). Expanded clipping/raster/atomicity tests passed all 14 in [workflow 35946923644](https://github.com/uniblab/Icod.DCurses/actions/runs/35946923644). The cost/equivalence head passed all 14 in [workflow 35947366909](https://github.com/uniblab/Icod.DCurses/actions/runs/35947366909); the final arithmetic-validation head passed all 14 in workflow 35947857085.

## Contract qualification

| Requirement | Permanent evidence |
|---|---|
| selected visual lines, destination clipping, wide glyphs, cursor stability | `CursesTextPresentationTests` |
| metadata and raster replacement, repeated presentation without new damage | `CursesTextPresentationTests` |
| provider failure before mutation on a later selected line | `CursesTextPresentationTests.WidthProviderFailureOnLaterLineDoesNotPartiallyPresent` |
| validated in-bounds row and rectangular stride; source remains caller-owned | `CursesBulkCellWriteTests` |
| invalid input preserves earlier cells and raster state; matching wide footprints, boundary repair, bounds/stride/source overflow | `CursesBulkCellWriteTests` |
| one bulk call versus 1,920 scalar writes; identical cells and damage for 80x24 and nine-cell updates | `CursesBulkCellWritePerformanceTests` |
| broad portable allocation gate | minimum of eight same-thread samples, 16 frames per sample; bulk allocation at most 64 KiB and at most half the measured scalar allocation |

The cost assertion uses allocation and deterministic call/damage counts. Elapsed time is not a pass criterion. GitHub Actions supplied the full exact-head verification because `dotnet` was unavailable in the local workspace.

## Next tranche

T2106 introduces pure viewport geometry. It must keep application content caller-owned and memory proportional to the visible slice. This PR remains open; this gate does not authorize a merge, tag, or publication.
