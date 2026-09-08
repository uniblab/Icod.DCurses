# T305 — Wide-Cell Footprint Invariants

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Checkpoint:** `0.3.0-alpha.5`  
**Status:** Complete

## Purpose

T305 hardens the leader/continuation representation used by two-column terminal text so later editing, copying, and pad APIs can rely on valid screen-owned footprints.

## Ownership boundary

`CursesVirtualScreen` remains an exact low-level logical-cell store when constructed independently. This compatibility behavior is intentional: callers may place exact cells, including continuation cells, and tests/refresh machinery may construct deliberate low-level states.

A `CursesScreen`-owned virtual screen additionally repairs an **existing** two-column footprint when an ordinary non-continuation replacement is made through a window or other screen-owned operation. This prevents high-level operations from leaving the other half of a glyph behind across window boundaries.

The internal `CursesCellFootprint` validator/repair utility remains the explicit structural invariant authority.

## Invariants

For screen/window operations that manage terminal text:

- a two-column leader is followed by one continuation cell;
- replacing a leader repairs its continuation;
- replacing a continuation repairs its leader;
- clear/erase operations repair a footprint even when a subwindow boundary bisects the glyph;
- a preserved screen resize does not keep a leader whose continuation was clipped by the new right edge;
- complete footprints survive preserved resize unchanged;
- clipping never stores half of a newly written two-column text element;
- wrapping moves the complete two-column element to the next row;
- zero-width text attaches to the leader rather than the continuation.

## Compatibility correction

An early T305 implementation attempted to enforce these invariants directly in every public `CursesVirtualScreen.SetCell` call. That was too broad: it changed the established exact-cell storage contract and broke refresh/exact-cell consumers. The implementation was narrowed so standalone virtual screens preserve their previous semantics while `CursesScreen` structural/window operations receive the stronger footprint guarantees.

## Acceptance

The test suite covers:

- preserved resize through a wide glyph;
- preserved resize retaining a complete glyph;
- deliberate low-level invalid states and explicit repair;
- subwindow clear at both leader and continuation boundaries;
- overwrite at a subwindow boundary;
- repeated wide writes and screen resizes;
- a deterministic seeded sequence of writes, clears, scrolling, arbitrary subwindow clears, and resizes with invariant validation after every operation.

Windows, Linux, macOS, and package validation are green for the completed structural invariant implementation.
