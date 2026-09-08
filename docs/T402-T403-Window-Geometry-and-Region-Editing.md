# T402–T403 — Window Geometry and Region Editing

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Checkpoint:** `0.4.0-alpha.2`  
**Status:** Complete and cross-platform green

## 1. T402 — Geometry and repositioning

`CursesWindow.Reposition(row, column)` repositions a non-standard window relative to its immediate parent or owning screen.

The accepted geometry contract is:

- the standard window remains anchored to the complete `CursesScreen`;
- root-window origins are screen-relative;
- subwindow origins are relative to the immediate parent;
- nested descendants automatically follow ancestor repositioning because windows remain shared views rather than independent buffers;
- local cursor coordinates are preserved by repositioning;
- repositioning validates the complete current rectangle before changing the origin;
- a failed reposition leaves the previous origin unchanged;
- resize and reposition both validate against the immediate containing surface.

No native `delwin`-style lifetime/ownership model is introduced. Window objects remain lightweight managed views for the lifetime of the owning screen.

## 2. T403 — Inspection, fill, and erase completion

T403 adds:

```text
CursesWindow.GetCell(row, column)
CursesWindow.FillRectangle(row, column, rows, columns, cell)
CursesWindow.ClearToBeginningOfLine()
```

`GetCell` uses window-local coordinates. For a valid local coordinate temporarily clipped from the owning screen by changed ancestor/screen geometry, it returns the window background cell rather than exposing an invalid physical-screen coordinate.

`FillRectangle`:

- requires a positive rectangle fully contained in the window;
- accepts only a one-column non-continuation `CursesCell`;
- preserves the cursor;
- uses shared-screen replacement semantics so replacing either half of an existing two-column glyph repairs the other half, including when that other half lies outside the filling window;
- marks logically changed cells dirty through the normal virtual-screen path.

`ClearToBeginningOfLine` fills from column zero through the current cursor column, inclusive, using `BackgroundCell` and preserves the cursor.

## 3. Validation

The `0.4.0-alpha.2` foundation has machine coverage for:

- root and nested repositioning;
- parent-relative subwindow geometry;
- cursor preservation;
- invalid geometry rejection;
- local cell inspection after repositioning;
- rectangular fills and invalid rectangles;
- rejection of continuation/two-column fill cells;
- wide-cell repair across a window boundary;
- clear-to-beginning-of-line semantics.

The T403 source checkpoint passed Windows, Linux, macOS Staging build/tests and canonical package/fresh-consumer validation before promotion to `0.4.0-alpha.2`.

## 4. Next tranche

T404 adds logical cell and line insertion/deletion. Those transforms operate on snapshots of the affected window region so overlap and wide-cell boundaries can be normalized before committing the destination image.
