# T2110 Editor Application Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `035f78603740fbc22a41fbf1325f5c1f58938bc9`\
**PR workflow:** [36043351407](https://github.com/uniblab/Icod.DCurses/actions/runs/36043351407), 14/14 jobs successful

## Application boundary

`Icod.DCurses.Editor.Sample` is a public-API executable for .NET 8/9/10. Its ten-million-record synthetic document generates untouched text on demand and retains only edited records. Each record reserves four visual rows and at most 100 UTF-16 units; edits that would exceed the visible wrapped bound are rejected. This is an application-owned fixed-record policy, not a reusable library document buffer. `CursesTextLayout` provides legal source positions, hit tests, vertical motion and selection rectangles; `CursesViewport` maps the current slice to terminal coordinates; retained `PresentTextLayout` projects only visible records. A newline is an in-record hard break, not an insertion into the document row index. Status, prompt and document tracks recompute after resize. One event loop owns input and lifecycle handling.

## Red and green evidence

- Project-contract test alone at `39466d7236aa8c271b1a5cc96fd02c395b913d66` specified the missing executable and public-only boundary. Its workflow was cancelled when the implementation superseded it; it is not counted as an observed red run.
- Initial executable head `2fbf62a2d425650956353d0dafea875341b932f2` compiled and passed tests on 13 completed jobs, then was superseded during a caret-rendering correction. Its workflow was cancelled and is not the acceptance matrix.
- `035f78603740fbc22a41fbf1325f5c1f58938bc9` styles the complete source text element under the caret, and draws an end-of-record marker only on an empty cell. Its exact-head workflow passed all 14 Staging/Release runtime and package jobs.

## Permanent qualification

| Requirement | Evidence |
| --- | --- |
| solution, .NET 8/9/10, configurations and public-only production source | `EditorSampleProjectContractTests` |
| ten-million-record extent, sparse edits and bounded visible record count | `EditorSampleStateTests.SyntheticDocumentKeepsOnlyEditedRecordsAndVisibleLayouts` |
| grapheme, wide, combining, tab and hard-break navigation/deletion; malformed text rejection | `EditorSampleStateTests.UnicodeEditsUseTextElementBoundariesAndRejectMalformedInput` |
| selection geometry, wrap/no-wrap, horizontal scroll, resize and large page clamp | `EditorSampleStateTests.SelectionWrapHorizontalScrollAndResizePreserveLegalPositions` |
| disjoint document, status and prompt tracks | `EditorSampleStateTests.TracksRemainDisjointAtSupportedSizes` |

The README documents fixed-record and selection limitations, controls and runnable command. This environment has no `dotnet` installation or interactive terminal, so no live manual terminal run is claimed. Package validation and runtime tests ran on the CI hosts; no public library API, assembly identity or direct dependency changed during T2110.

## Next tranche

T2111 reviews adversarial and package coverage, freezes the development API baseline, and closes release documentation. The application selection-edit polish is part of that follow-up. PR #33 remains open and unmerged.
