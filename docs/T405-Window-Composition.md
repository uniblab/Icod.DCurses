# T405 — Window Composition

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Checkpoint:** `0.4.0-alpha.4`  
**Status:** Complete

## Purpose

T405 adds deterministic rectangle composition between logical windows without introducing an independent layer/compositor model.

## Public operations

```text
CopyRectangleTo(...)
OverlayRectangleTo(...)
```

Both operations use window-local coordinates for source and destination rectangles and preserve both window cursors.

## Semantics

`CopyRectangleTo(...)` is destructive: source blank cells are copied and therefore replace destination content.

`OverlayRectangleTo(...)` treats ordinary blank source cells as transparent and leaves the corresponding destination cells unchanged.

Both operations snapshot the complete source rectangle before the first destination write. Same-window overlapping copies therefore behave deterministically and do not depend on iteration direction.

## Unicode and wide-cell behavior

The `0.3.0` terminal-cell contract remains authoritative.

A two-column source glyph is copied only when both leader and continuation are present inside the source rectangle. A rectangle cutting either half of a wide footprint substitutes a blank source cell rather than emitting an orphaned half.

Destination writes continue to use screen-owned footprint repair, so replacing either half of an existing wide glyph also repairs the other half.

## Gate result

The T405 implementation and tests pass:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package/fresh-consumer validation.

T406 may therefore add geometric line and border drawing on top of this composition foundation.
