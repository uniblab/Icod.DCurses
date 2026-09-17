# T1603 — Sparse Retained-Raster Logical Plane

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1603  
**Accepted source/package identity:** `1.6.0-alpha.2`  
**AssemblyVersion:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** accepted

## Objective

T1603 adds the separate lazy retained-raster logical plane frozen by T1601 while preserving ordinary `CursesCell` layout and terminal-independent `CursesCellMetadata` semantics.

The retained surface is now conceptually:

```text
Curses retained surface
    +-- dense CursesCell[] visual/text plane
    +-- optional row-sparse CursesCellMetadata plane
    +-- optional row-sparse retained-raster plane
```

No raster field was added to every `CursesCell`.

## TDD evidence

The test-only RED head was:

```text
0452d9fa2529aaa3008add7821d098906d2ac434
```

That head built successfully and failed only because the planned logical raster APIs and storage seams were absent. The representation-cost witnesses already passed.

The production GREEN implementation was:

```text
514483112e1ef148623e90b2335b5231e7e309b0
```

Its first matrix exposed one expected development-baseline failure only: the T1602 public API fingerprint still described the pre-T1603 surface. All T1603 behavior tests passed.

The reviewed alpha/fingerprint head is:

```text
5ca69df48057af1b4d05c6f39f3ccd1556949871
```

Workflow **#934 / 35015100313** passed all seven jobs:

```text
Package candidate       success
Runtime Windows x64     success
Runtime Windows ARM64   success
Runtime Linux x64       success
Runtime Linux ARM64     success
Runtime macOS x64       success
Runtime macOS ARM64     success
```

Linux x64 reported a zero-warning/zero-error Staging build and **827/827 tests passing on each of net8.0, net9.0, and net10.0**.

## Accepted behavior

T1603 establishes:

- lazy row-sparse retained-raster storage on `CursesVirtualScreen`;
- no raster storage allocation until raster content is retained;
- release of the sparse plane when its last retained raster reference is removed;
- `CursesVirtualScreen.GetRasterCell(...)` and `SetRasterCell(...)`;
- window-local `GetRasterCell(...)`, `SetRasterCell(...)`, and current-cursor `WriteRasterCell(...)`;
- raster mutations participating in existing dirty-cell and change-revision tracking;
- ordinary cell replacement clearing raster state at the replaced coordinate, including equal-value replacement;
- `Fill`/`Clear` removing retained raster state;
- direct raster removal leaving visual text/style and semantic metadata untouched;
- default/unassociated `CursesRasterCell` rejection before logical mutation;
- one-column cursor advance and established wrap policy for `WriteRasterCell`;
- `CursesLogicalCellState` expanded to carry an optional retained-raster reference so later editing/composition can move all retained axes together.

## Public API checkpoint

The T1603 development fingerprint is:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The exported type count is unchanged from T1602. T1603 adds the previously frozen logical raster members to existing `CursesVirtualScreen` and `CursesWindow` types.

## Representation result

The measured design remains the T1601-approved sparse side plane. Ordinary no-raster surfaces carry no raster-row allocation, while the established `2048 x 256` large-pad reference avoids the unconditional 4 MiB reference cost that an eight-byte raster field on every coordinate would impose.

## Deliberate exclusions

T1603 does not yet make structural editing/composition operations carry raster state. Specifically, raster propagation through insert/delete, scroll, rectangle copy/overlay, pads/viewports, and cross-session transfer validation belongs to T1604.

Panel composition belongs to T1605. Physical Terminal placeholder emission and physical-state tracking belong to T1606.

## Handoff to T1604

T1604 must use the existing `CursesLogicalCellState` snapshot/commit paths rather than creating raster-specific editing algorithms. It must prove raster propagation through editing, scrolling, copy/overlay, subwindows, pads, and viewports, and must reject a known foreign-session raster reference before partial mutation of a live session-owned destination.
