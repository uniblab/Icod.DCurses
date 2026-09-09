# T706 — Physical Line-Shift and Scroll-Region Optimization

**Release:** `Icod.DCurses 0.7.0-alpha.6`  
**Roadmap tranche:** T706  
**Status:** Implemented; final alpha.6 matrix gate required  
**Public API change:** None

## Objective

T706 reduces physical refresh output for exact vertical row shifts without changing the logical window, screen, pad, cell, rendition, or cursor contracts already established by DCurses.

The logical editing APIs remain authoritative. `CursesWindow.InsertLines(...)` and `CursesWindow.DeleteLines(...)` still modify only the logical screen. T706 recognizes the resulting final full-screen diff and may reproduce it more cheaply through advertised TermInfo line or scroll operations.

No optimization is selected from knowledge that a particular window initiated an edit. The retained logical and physical screen images are the only authority used to prove a physical transformation.

## Candidate operations

The internal `CursesLineShiftResolver` considers advertised TermInfo capabilities for:

- parameterized insert-line (`il`) and one-line insert (`il1`);
- parameterized delete-line (`dl`) and one-line delete (`dl1`);
- parameterized forward scroll (`indn`) and one-line forward scroll (`ind`);
- parameterized reverse scroll (`rin`) and one-line reverse scroll (`ri`);
- change scrolling region (`csr`) when an exact interior full-width region requires temporary terminal state.

Parameterized and repeated one-line forms compete by concrete expanded terminal-byte cost. DCurses does not construct private escape strings and does not infer terminal behavior from terminal names, operating systems, or emulator brands.

## Exact transformation proof

A line-shift candidate is eligible only when all required physical cells are already known.

The resolver first identifies the first and last rows that differ between the desired logical image and the retained physical image. It then searches for a vertical region that is exactly representable as one insertion or deletion transformation.

For an insertion:

1. the newly inserted logical rows must be entirely default-styled blank cells;
2. every remaining desired row in the region must exactly equal the corresponding retained physical row shifted downward by the candidate count.

For a deletion:

1. the vacated rows at the bottom of the candidate region must be entirely default-styled blank cells;
2. every remaining desired row must exactly equal the corresponding retained physical row shifted upward by the candidate count.

Rows outside the candidate region remain unchanged by definition of the final-screen comparison.

This means a partial-width child-window edit cannot authorize a terminal line shift merely because it originated from `InsertLines(...)` or `DeleteLines(...)`. If the final screen is not an exact whole-row transformation, the ordinary retained-screen renderer remains the fallback.

## Wide cells and semantic line glyphs

T705 character shifting rejected wide and semantic-line cells because character insertion/deletion can split a terminal-column footprint.

T706 moves complete rows only. Therefore complete wide-cell leader/continuation footprints and semantic line-glyph cells may move with their containing rows when the full-row equality proof succeeds. No partial row or partial footprint is transformed physically.

## Rendition requirements

Line insertion, deletion, and scrolling can create blank rows whose physical rendition depends on terminal state. T706 therefore requires retained active rendition to be known and equal to `CursesStyle.Default` before selecting a physical line shift.

Inserted or vacated logical rows must also be default-styled blank rows.

If rendition knowledge is unknown or nondefault, T706 declines the optimization and uses the ordinary renderer.

## Direct line operations

When an exact affected region reaches the physical screen bottom, ordinary `il` / `dl` semantics can safely operate against the default full-screen scrolling region.

The resolver compares:

- parameterized `il` / `dl`;
- repeated `il1` / `dl1`;
- any safe competing full-region scroll candidate.

The operation's affected-line count is propagated through `TerminalCapabilityWriter` so TermInfo padding semantics receive the region height rather than an assumed single line.

## Full-screen scrolling

For an exact transformation covering the entire screen, forward and reverse scrolling may compete directly with insert/delete-line operations.

Forward scroll represents an upward logical shift with default blank rows entering at the bottom. Reverse scroll represents a downward logical shift with default blank rows entering at the top.

Parameterized `indn` / `rin` and repeated one-line `ind` / `ri` forms are compared by expanded terminal-byte cost.

## Temporary scrolling regions

An interior full-width vertical shift may be realized only if the terminal advertises `csr` and the complete transaction is cheaper than fallback rendering.

The transaction is:

1. set the exact candidate scrolling region;
2. position the cursor at the required operation boundary;
3. emit the selected line or scroll operation;
4. restore the full-screen scrolling region;
5. continue with the ordinary final requested cursor placement.

Changing `csr` is treated as stateful terminal configuration, not as a harmless output string.

### Restoration guarantee

Once a temporary scrolling-region transaction has begun, DCurses attempts to restore the full-screen scrolling region even if the operation fails or cancellation is observed after region setup.

Restoration uses `CancellationToken.None` so cancellation of the refresh does not intentionally strand the terminal in a narrowed scrolling region.

If both the line-shift operation and scrolling-region restoration fail, both exceptions are preserved in an `AggregateException`.

After temporary `csr` setup or restoration, retained cursor-position knowledge is conservatively treated as unknown because scrolling-region changes may have terminal-specific cursor side effects. T703 then performs safe final cursor positioning from unknown state.

## Retained physical-screen state

The retained physical model is updated only after the selected physical transformation has been emitted successfully and, for temporary-region operations, after full scrolling-region restoration has also succeeded.

Every row in the transformed region is then recorded as exactly equal to the desired logical row. Rows outside the region remain unchanged.

If any operation or restoration failure escapes the transaction, the existing refresh exception path invalidates retained physical, rendition, and cursor knowledge before the exception reaches the caller.

A subsequent refresh therefore falls back to a safe repaint rather than trusting a partially applied vertical transformation.

## Cost gate

T706 requires a strict deterministic byte win.

The conservative fallback lower bound counts only changed nonblank application-text bytes in the affected rows. It deliberately excludes cursor movement, rendition transitions, blank output, erase sequences, and write-call overhead. This makes the threshold harder—not easier—for a line operation to beat.

Candidate cost includes all protocol bytes that DCurses knows it must emit for the optimization:

- temporary `csr` setup, when required;
- cursor movement to the line/scroll operation origin;
- the selected line or scroll capability;
- full-region `csr` restoration, when required;
- final requested cursor repositioning.

Application-text byte cost uses the owning `TerminalSession.ApplicationEncoding`. TermInfo protocol cost uses `CursesOutputCostModel.GetTerminalStringByteCount(...)` with the appropriate affected-line count.

Equal cost retains the ordinary renderer.

## Test coverage

Focused resolver tests cover:

- direct insert-line and delete-line selection;
- parameterized versus repeated one-line cost choice;
- temporary interior scrolling-region selection;
- forward scroll competing with delete-line;
- complete wide-cell row preservation;
- styled vacated-row rejection;
- unknown physical-state rejection;
- equal-cost fallback.

Refresh integration tests exercise:

- the real public `CursesWindow.InsertLines(...)` path;
- the real public `CursesWindow.DeleteLines(...)` path;
- exact no-write second-refresh equivalence;
- affected-line propagation;
- bounded full-width child-window transformations;
- partial-width child-window fallback;
- full-screen scroll selection;
- restoration after operation failure;
- simultaneous operation/restoration failure preservation.

The initial implementation checkpoint exposed two missing loop-closing braces in the new resolver. Those syntax defects were corrected without changing the intended algorithm. The corrected implementation head passed Windows, Linux, macOS, and package validation before alpha.6 promotion.

## Non-goals and deliberate exclusions

T706 does not:

- add a public scrolling-region API;
- expose terminal line/scroll operations to applications;
- infer scrolling regions from window ownership or geometry alone;
- perform partial-column terminal scrolling;
- assume support based on `TERM`, OS, or emulator identity;
- relax the retained-screen correctness model;
- attempt a globally optimal escape-stream search.

## Result

T706 adds a conservative physical line-shift layer above the frozen logical editing contract. Exact pager/editor-style vertical shifts can now collapse to advertised line or scroll capabilities, including safely restored temporary scrolling regions, while every unproven or non-beneficial case remains on the established renderer.
