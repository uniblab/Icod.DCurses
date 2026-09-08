# T407 — Damage Ranges and Editing Acceptance

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Checkpoint:** `0.4.0-alpha.6`  
**Status:** Complete

## Purpose

T407 completes the feature-side 0.4 editing surface with safe range-oriented damage marking and representative editor-like acceptance.

## Accepted public operations

```text
TouchRegion(...)
IsRegionTouched(...)
```

`TouchRegion(...)` marks every currently projected cell in a validated window-local rectangle dirty. Valid local cells temporarily clipped outside the owning screen are ignored until they project onto the screen again.

`IsRegionTouched(...)` returns `true` when at least one currently projected cell in the requested region is dirty.

Both operations preserve the window cursor.

## Public untouch rejected

A public `UntouchRegion(...)` API is deliberately **not** added.

Dirty state represents pending work required to reconcile the logical screen with retained physical-screen knowledge. Allowing an arbitrary window to clear that state could suppress a refresh which is still necessary for correctness, especially across overlapping windows and wide-cell spans.

Native-curses symmetry is not sufficient justification for weakening the retained-screen model.

## Wide-cell refresh behavior

A touched region may select only a continuation column. The refresh engine expands a dirty continuation backward to its leader and expands a rendered span through following continuation cells, so range-oriented damage marking remains structurally safe without artificially dirtying unrelated logical cells.

## Acceptance

T407 adds:

- precise range dirty-count tests;
- `IsRegionTouched` overlap/non-overlap tests;
- temporarily clipped-window behavior;
- continuation-only range coverage;
- an editor-like Unicode-heavy workload combining insertion, deletion, composition, drawing, and damage querying;
- fresh package-only execution of the complete 0.4 feature surface.

## Gate result

T407 passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package construction and fresh package-only consumers.

T408 is therefore an API/documentation/package regret gate only; no new feature family enters after this checkpoint.
