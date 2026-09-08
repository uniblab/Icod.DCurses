# Icod.DCurses 0.5 Public API Baseline

**Project:** `Icod.DCurses`  
**Release line:** `0.5.x`  
**Prepared during:** T508  
**Current candidate:** `0.5.0-rc.1`  
**Stable baseline:** `0.4.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Release-candidate public-contract baseline

## 1. Purpose

This document records the public contract intentionally added by `Icod.DCurses 0.5` over the stable `0.4.0` window editing/composition baseline.

The 0.1 screen/session contract, 0.2 semantic-input contract, 0.3 Unicode/terminal-cell contract, and 0.4 window-editing contract remain in force. The 0.5 release does not intentionally remove or rename an existing public member.

## 2. Public type result

`0.5` adds exactly two public sealed types:

```text
Icod.DCurses.CursesPad
Icod.DCurses.CursesPadViewport
```

No public enum is added.

There is intentionally no `CursesSubpad` type. Shared derived pad views use the established `CursesWindow.CreateSubwindow(...)` contract through `CursesPad.ContentWindow`.

## 3. `CursesPad`

### 3.1 Construction

```text
CursesPad(
    int columns,
    int rows,
    ICursesTextWidthProvider? textWidthProvider = null
)
```

Dimensions must be positive and remain subject to the existing checked logical-cell-count limit and actual runtime allocation capacity.

A pad owns an in-memory off-screen logical surface only. Construction does not acquire terminal state, perform terminal I/O, or create a second refresh engine.

### 3.2 Properties

```text
int Columns
int Rows
ICursesTextWidthProvider TextWidthProvider
CursesWindow ContentWindow
```

`ContentWindow` covers the complete pad and is the authoritative editing surface. It owns pad-local cursor state and exposes the existing Unicode, cell, editing, composition, drawing, and damage APIs.

### 3.3 Direct presentation

```text
void PresentTo(
    CursesWindow destination,
    int padRow,
    int padColumn,
    int rows,
    int columns,
    int destinationRow,
    int destinationColumn
)
```

Presentation is a destructive logical copy. Ordinary pad blanks replace destination cells. Both source and destination rectangles must fit their current logical surfaces. Both cursors are preserved. Wide-cell boundaries reuse the stable 0.4 copy/repair semantics.

No physical terminal refresh is implied.

### 3.4 Stateful viewport creation

```text
CursesPadViewport CreateViewport(
    CursesWindow destination,
    int padRow,
    int padColumn,
    int rows,
    int columns,
    int destinationRow,
    int destinationColumn
)
```

Each viewport owns independent projection state. A pad does not have one global scroll/pan position.

## 4. `CursesPadViewport`

`CursesPadViewport` has no public constructor. Instances are created by `CursesPad.CreateViewport(...)`.

### 4.1 Properties

```text
int PadRow
int PadColumn
int Rows
int Columns
int DestinationRow
int DestinationColumn
bool HasVisiblePadChanges
```

Viewport dimensions and destination-local placement are fixed after construction. The pad source origin can move.

`HasVisiblePadChanges` reports pad-source changes relative to this viewport's own last successful presentation. It is true when:

- the viewport has never presented;
- the source origin changed;
- at least one currently visible pad cell has a newer content/damage revision.

Off-screen pad mutations do not make the property true.

The property does **not** claim that the destination still matches the pad. Another logical window can overwrite the destination independently.

### 4.2 Source positioning

```text
void SetSource(
    int row,
    int column
)

void PanBy(
    int rowDelta,
    int columnDelta
)
```

Both operations require the complete fixed viewport rectangle to remain inside the pad. Invalid moves throw and leave the prior source position unchanged.

### 4.3 Presentation

```text
void Present()
```

`Present()` is authoritative and always performs the logical projection, even when `HasVisiblePadChanges` is false, so destination drift is repaired.

Changing the source position invalidates the full destination viewport. Otherwise, pad-local explicit touch/invalidation and cell changes propagate destination damage for changed visible row spans.

Presenting one viewport acknowledges only that viewport's pad-source revision snapshot. It never globally clears pad changes for another viewport.

## 5. Derived pad views

The official shared derived-view contract is:

```text
pad.ContentWindow.CreateSubwindow(...)
```

Derived views:

- share the pad backing storage;
- observe overlapping edits immediately;
- use immediate-parent-relative origins;
- keep independent cursor state;
- retain the 0.3 Unicode/wide-cell contract;
- remain lightweight/non-disposable.

## 6. Resize and geometry semantics

The pad does not resize in response to terminal or destination-screen resize.

A viewport retains its fixed dimensions and destination-local rectangle. Every `Present()` validates that rectangle against the destination window's current geometry.

If destination shrink makes the rectangle invalid, presentation fails explicitly. If later growth makes it valid again, the same viewport can present again.

Repositioning a non-standard destination window does not change viewport destination-local coordinates; the next presentation follows the window's new screen projection.

## 7. Change-tracking implementation boundary

Independent viewport observation requires per-cell logical content/damage revisions, but that mechanism is internal rather than public API.

Revision tracking is enabled only for pad backing surfaces. Ordinary `CursesScreen` and standalone `CursesVirtualScreen` instances do not allocate the pad-specific per-cell revision array, preserving the pre-0.5 logical-screen memory profile.

There is no public global pad `MarkClean`, `Acknowledge`, or equivalent operation.

## 8. Dependency boundary

The 0.5 release adds no package dependency and no newly approved Terminal/TermInfo type in a public signature.

The intentional upstream public-type allow-list remains:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

`PublicDependencyBoundaryTests` remains authoritative for this boundary.

## 9. Resource model

Pads are dense in-memory cell surfaces in 0.5. The release does not add sparse, disk-backed, virtualized, or memory-mapped storage.

The accepted resource limit is therefore the existing checked addressable cell-count limit plus actual process memory availability.

## 10. Deferred

The 0.5 baseline does not add:

- pad-owned terminal/session/physical-refresh state;
- a public `CursesSubpad` hierarchy;
- global pad clean/acknowledge semantics;
- resizable viewport geometry;
- automatic destination clipping after resize;
- sparse or disk-backed pad storage;
- capability-aware line-drawing presentation (`0.6.0`);
- terminal scrolling/insert/delete optimization (`0.7.0`).
