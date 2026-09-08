# Icod.DCurses 0.4 Public API Baseline

**Project:** `Icod.DCurses`  
**Release line:** `0.4.x`  
**Prepared during:** T408  
**Current development version:** `0.4.0-alpha.6`  
**Stable baseline:** `0.3.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Pre-release public-contract baseline

## 1. Purpose

This document records the public contract intentionally added by `Icod.DCurses 0.4` over the stable `0.3.0` baseline.

The 0.1 screen/session contract, 0.2 semantic-input contract, and 0.3 Unicode/terminal-cell contract remain in force. The 0.4 release does not intentionally remove or rename an existing public member.

## 2. Public type result

`0.4` adds **no new public type and no new public enum**.

The complete public delta is expressed through methods on the existing `CursesWindow` type.

## 3. Geometry

```text
void Reposition(
    int row,
    int column
)
```

The standard window remains anchored and rejects repositioning. Root-window coordinates are screen-relative; subwindow coordinates remain relative to the immediate parent. Repositioning preserves cursor coordinates and does not move existing screen contents because windows remain shared views.

## 4. Inspection and region editing

```text
CursesCell GetCell(
    int row,
    int column
)

void FillRectangle(
    int row,
    int column,
    int rows,
    int columns,
    CursesCell cell
)

void ClearToBeginningOfLine()
```

`GetCell` uses window-local coordinates. A valid local coordinate currently clipped outside the screen returns the window background cell.

`FillRectangle` accepts only one-column non-continuation cells and preserves the cursor.

## 5. Insertion and deletion

```text
void InsertCells( int count = 1 )
void DeleteCells( int count = 1 )
void InsertLines( int count = 1 )
void DeleteLines( int count = 1 )
```

Counts must be positive. Counts beyond the remaining row/region collapse to background fill semantics. All four methods preserve the cursor and normalize two-column footprints at edit boundaries.

## 6. Composition

```text
void CopyRectangleTo(
    CursesWindow destination,
    int sourceRow,
    int sourceColumn,
    int rows,
    int columns,
    int destinationRow,
    int destinationColumn
)

void OverlayRectangleTo(
    CursesWindow destination,
    int sourceRow,
    int sourceColumn,
    int rows,
    int columns,
    int destinationRow,
    int destinationColumn
)
```

Both operations snapshot the complete source rectangle before destination mutation, making same-window and overlapping copies deterministic.

Copy is destructive and includes ordinary source blanks. Overlay treats ordinary source blank cells as transparent. Wide-cell leaders/continuations remain structural and are normalized at source and destination boundaries.

Source and destination cursors are preserved.

## 7. Geometric drawing

```text
void DrawHorizontalLine(
    int row,
    int column,
    int length,
    CursesCell cell
)

void DrawVerticalLine(
    int row,
    int column,
    int length,
    CursesCell cell
)

void DrawBorder(
    CursesCell horizontal,
    CursesCell vertical,
    CursesCell topLeft,
    CursesCell topRight,
    CursesCell bottomLeft,
    CursesCell bottomRight
)
```

Drawing cells must be one-column non-continuation cells. `DrawBorder` requires a window of at least two rows by two columns.

The 0.4 contract freezes drawing geometry, not a permanent semantic glyph vocabulary. Capability-aware Unicode/ACS selection remains deferred to the 0.6 presentation tranche.

## 8. Damage ranges

```text
void TouchRegion(
    int row,
    int column,
    int rows,
    int columns
)

bool IsRegionTouched(
    int row,
    int column,
    int rows,
    int columns
)
```

`TouchRegion` marks currently projected cells dirty. `IsRegionTouched` returns true when at least one currently projected cell in the region is dirty.

A public `UntouchRegion` operation is intentionally rejected because arbitrary dirty-state clearing could suppress refresh work still required to reconcile retained physical-screen knowledge.

## 9. Shared-view and cursor contract

Windows remain lightweight shared views over one logical screen rather than disposable independent buffers or compositing layers.

All new 0.4 region/edit/drawing operations preserve the logical window cursor. `Reposition` also preserves the local cursor.

## 10. Unicode and wide-cell contract

The stable 0.3 contract remains authoritative:

- malformed UTF-16 normalization precedes segmentation;
- text elements occupy zero, one, or two terminal columns;
- no 0.4 editing/composition operation intentionally retains half of a two-column glyph;
- boundary cuts are repaired or normalized to safe blank cells;
- drawing and fills use the screen-owned footprint-repair path.

## 11. Dependency boundary

The 0.4 release adds no package dependency and no newly approved Terminal/TermInfo type in a public signature.

The intentional upstream public-type allow-list remains:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

`PublicDependencyBoundaryTests` remains authoritative for this boundary.

## 12. Deferred

The 0.4 baseline does not add:

- pads or large off-screen surfaces (`0.5.0`);
- capability-aware line-drawing degradation (`0.6.0`);
- new rendition families (`0.6.0`);
- terminal insert/delete/scroll escape optimization (`0.7.0`);
- public dirty-state clearing;
- disposable/lifetime-owned windows;
- widgets or higher-level layout controls.
