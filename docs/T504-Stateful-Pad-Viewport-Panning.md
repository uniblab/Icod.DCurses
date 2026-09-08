# T504 — Stateful Pad Viewport Panning

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Checkpoint:** `0.5.0-alpha.2`  
**Stable baseline:** `0.4.0`  
**Status:** Implemented; validation pending

## 1. Purpose

T504 removes repeated application-private source-coordinate bookkeeping for common pad scrolling and panning workloads.

A pad does not own one global viewport position. Instead, each `CursesPadViewport` stores one independent projection from a pad into one destination `CursesWindow`.

This allows one pad to be presented in multiple destinations simultaneously at different source positions.

## 2. Creation

Viewports are created through:

```text
CursesPad.CreateViewport(
    destination,
    padRow,
    padColumn,
    rows,
    columns,
    destinationRow,
    destinationColumn
)
```

The source and destination rectangles are validated at construction.

The viewport height, width, and destination placement are fixed for this checkpoint. Source position is mutable through panning.

## 3. Public state

`CursesPadViewport` exposes:

```text
PadRow
PadColumn
Rows
Columns
DestinationRow
DestinationColumn
```

It deliberately does not expose or own terminal-session state.

## 4. Source positioning

Absolute source movement uses:

```text
SetSource(row, column)
```

Relative movement uses:

```text
PanBy(rowDelta, columnDelta)
```

Both reject movement that would place any part of the fixed viewport outside the pad. A failed move leaves the previous source state unchanged.

No clamping is implicit. Applications that want clamping can make that policy explicitly at a higher layer.

## 5. Presentation

`Present()` delegates to the pad's established `PresentTo(...)` operation.

Therefore:

- presentation remains a destructive logical copy;
- ordinary blank pad cells replace destination cells;
- wide-cell source/destination boundary rules remain those of T503 and the 0.4 copy engine;
- pad and destination cursor states remain preserved;
- no physical terminal refresh is implied.

The destination rectangle is revalidated every time `Present()` runs. If a destination window is resized so that the viewport no longer fits, presentation fails explicitly instead of silently clipping.

## 6. Multiple viewports

Each viewport owns only its own source coordinates. Panning one viewport does not alter any other viewport over the same pad.

This separation is important for T506 damage tracking because multiple viewports may acknowledge or present pad changes independently.

## 7. Deferred decisions

T504 does not yet add:

- mutable viewport dimensions;
- mutable destination placement;
- destination auto-clamping after resize;
- per-viewport incremental damage acknowledgement;
- a global pad viewport position.

Those concerns are evaluated in T506/T507.
