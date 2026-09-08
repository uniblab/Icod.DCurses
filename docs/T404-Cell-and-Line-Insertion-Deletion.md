# T404 — Cell and Line Insertion/Deletion

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Checkpoint:** `0.4.0-alpha.3`  
**Status:** Complete and cross-platform green

## 1. Public surface

T404 adds:

```text
CursesWindow.InsertCells(int count = 1)
CursesWindow.DeleteCells(int count = 1)
CursesWindow.InsertLines(int count = 1)
CursesWindow.DeleteLines(int count = 1)
```

All counts must be positive. Counts larger than the remaining editable row/line span collapse to background fill behavior rather than overflowing the window.

## 2. Cell editing

Cell insertion/deletion operates on the current cursor row beginning at the cursor column.

- insertion shifts the remaining row content right;
- deletion shifts following row content left;
- shifted-out content is discarded;
- vacated cells use `BackgroundCell`;
- the cursor is preserved;
- a complete two-column footprint moves atomically when both halves remain representable;
- if an edit boundary cuts through a two-column glyph, the partial footprint is discarded rather than producing an orphaned leader/continuation.

## 3. Line editing

Line insertion/deletion operates beginning at the current cursor row and is confined to the complete width/height of the current window.

- inserted/vacated rows use `BackgroundCell`;
- content never shifts outside the window rectangle;
- overlapping/shared windows observe the resulting underlying-screen transformation;
- cursor row/column are preserved;
- valid two-column footprints survive row movement intact.

## 4. Snapshot-transform-commit model

T404 intentionally does not mutate the live row one cell at a time while deciding the transformation.

The operation:

1. snapshots the affected logical window contents;
2. computes the shifted result in memory;
3. normalizes leader/continuation pairs at edit and window boundaries;
4. structurally clears the destination through a one-column background value;
5. commits the final normalized cell image.

This gives deterministic overlap/boundary behavior and prevents transient setter repairs from altering the source data still being moved.

## 5. Validation result

Tests cover:

- ordinary ASCII insertion/deletion;
- custom background cells;
- oversized counts;
- complete wide-glyph movement;
- insertion through a continuation boundary;
- deletion through a wide leader;
- line insertion/deletion in non-standard/repositioned windows;
- cursor preservation;
- wide-cell footprint validation after line movement;
- rejection of nonpositive counts.

The initial T404 run exposed one incorrect test fixture: it populated a shared window before repositioning and incorrectly expected repositioning to move that content. The fixture was corrected to honor the already-frozen T402 view semantics; no production-code correction was required.

The corrected T404 head passed Windows, Linux, macOS Staging build/tests and canonical package/fresh-consumer validation before promotion to `0.4.0-alpha.3`.
