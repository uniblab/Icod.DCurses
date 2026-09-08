# T501-T503 — Pad Foundation and Viewport Presentation

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Checkpoint:** `0.5.0-alpha.1`  
**Stable baseline:** `0.4.0`  
**Status:** Complete and cross-platform green

## 1. Foundation

`CursesPad` now owns a large off-screen logical backing surface without owning a terminal session, terminal mode, physical-screen state, input loop, or terminal output.

The pad deliberately reuses the established `CursesWindow` contract rather than duplicating Unicode, cell, editing, composition, drawing, or damage logic.

The initial public surface is:

```text
new CursesPad(columns, rows, textWidthProvider?)
Columns
Rows
TextWidthProvider
ContentWindow
PresentTo(...)
```

## 2. Content window

`ContentWindow` covers the complete pad backing surface and carries pad-local cursor state.

Existing `CursesWindow` APIs work unchanged on the pad, including:

- Unicode/grapheme-aware text writes;
- one- and two-column cells;
- insert/delete cells and lines;
- rectangle fill;
- copy/overlay;
- geometric drawing;
- touch/query damage operations;
- `CreateSubwindow(...)` for shared derived views.

No separate subpad type is introduced at this checkpoint.

## 3. Resource limits

Pad dimensions use the same checked logical-cell count validation as `CursesVirtualScreen`.

Dimensions must be positive and `columns * rows` must fit in the addressable `int` cell-count contract. Actual allocation remains subject to available runtime memory; 0.5 does not introduce sparse, memory-mapped, or disk-backed storage.

## 4. Viewport presentation

`PresentTo(...)` projects a rectangular pad source region into an ordinary destination `CursesWindow`.

Presentation is a destructive logical copy:

- ordinary pad blank cells replace destination cells;
- pad and destination cursors are preserved;
- source and destination rectangles are validated;
- source data is snapshotted by the existing 0.4 copy implementation;
- wide glyphs cut by a source boundary are normalized rather than copied partially;
- destination wide footprints are repaired when presentation intersects them;
- no physical terminal refresh is implied.

This deliberately reuses the stable `CursesWindow.CopyRectangleTo(...)` implementation rather than maintaining a second rectangle-copy algorithm.

## 5. Validation

The `0.5.0-alpha.1` checkpoint passed:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation/fresh consumer gate.

Tests cover large pad dimensions, Unicode/wide-cell editing, custom width providers, shared derived views, destructive blank presentation, wide source clipping, destination footprint repair, cursor preservation, and invalid source/destination rectangles.

## 6. Next step

T504 adds independent stateful viewport objects for repeated panning. The pad itself will not own one global viewport position because a single pad may be presented in multiple destinations simultaneously.
