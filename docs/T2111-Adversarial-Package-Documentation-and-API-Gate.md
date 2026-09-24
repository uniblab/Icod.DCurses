# T2111 Adversarial, Package, Documentation and API Gate

**Status:** accepted at executable head `f0b69470cace0bfe29cd3db311ac156a4a50deac`\
**Branch:** `2.1.0-roadmap`\
**PR workflow:** [36044565032](https://github.com/uniblab/Icod.DCurses/actions/runs/36044565032), 14/14 jobs successful

## Adversarial review

| Boundary | Permanent evidence |
| --- | --- |
| source positions, malformed UTF-16, controls, spans and text-element boundaries | `CursesTextSourceContractTests`, `CursesTextSourceUnicodeTests`, `CursesTextLayoutUnicodeTests` |
| row/fragment/cell capacity, invalid options, overflow and failure atomicity | `CursesTextLayoutCapacityTests`, `CursesTextLayoutTests` |
| caret/hit/selection geometry and indexed lookup | `CursesTextGeometryTests`, `CursesTextGeometryUnicodeTests`, `CursesTextGeometryPerformanceTests` |
| retained selected-line projection, clipping and prepared cell writes | `CursesTextPresentationTests`, `CursesBulkCellWriteTests`, `CursesBulkCellWritePerformanceTests` |
| large viewport boundaries and track caps/distribution | `CursesViewportBoundaryTests`, `CursesViewportPerformanceTests`, `CursesTrackLayoutTests`, `CursesTrackLayoutPerformanceTests` |
| disabled diagnostics, cancellation, partial output failure, retry and bounded allocation | `CursesRefreshDiagnosticsTests`, `CursesRefreshDiagnosticsPerformanceTests`, `CorePresentationTextWorkloadBaselineTests` |
| editor selection replacement/deletion at legal Unicode boundaries | `EditorSampleStateTests.SelectionReplacementAndDeletionRemainAtomicAtTextElementBoundaries` |

The immutable 2.0 fingerprint is unchanged. The development 2.1 fingerprint exports 96 types and 751 canonical contract lines, SHA-256 `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`. It adds 21 exported types and removes none; [the 2.1 development baseline](Public-API-Baseline-2.1.md) groups them by text geometry, viewport/track layout and diagnostics. The accepted API design remains the surface boundary; both application samples own their models and event loops. `AssemblyVersion` remains `2.0.0.0` and `Icod.Terminal 1.18.0` remains the sole direct production package dependency.

## Package and documentation review

The root README identifies 2.0 as the current published version and describes 2.1 as development, with the 2.0 install command unchanged. The changelog and package metadata describe the additive 2.1 surface and the two public-only samples; sample instructions document controls and limits. The package-validation jobs invoke the repository verifier to check package structure, version/assembly identity, direct dependency closure, license, README, XML documentation and portable symbols. They restore a fresh package-only consumer and run it on .NET 8, 9 and 10, including a Linux pseudo-terminal refresh.

## Verification and next step

The exact-head 14-job Staging/Release Windows/Linux/macOS x64/ARM64 runtime and Staging/Release package matrix passed. The representative Linux x64 Staging log reports 1,222 passing tests, zero failures/skips on each of .NET 8, 9 and 10, and zero build warnings. The Release package log reports zero build warnings and fresh package-only consumers on all three TFMs plus live Linux pseudo-terminal refresh. Package artifact ZIP SHA-256 digests: Staging `b8f7dacc4a69b9736cef8a833f76dd6cbef05c8063fcea85295f25c8b5bd262d` (artifact `10828410466`) and Release `9a175688bce8b5349ae100acaebd03fb1f4eb24384368634569efed701f3a9ae` (artifact `10827249158`); both report the exact executable head.

A live manual run of the interactive samples is not recorded in this environment. T2112 now qualifies RC and stable-source identities without merging PR #33.
