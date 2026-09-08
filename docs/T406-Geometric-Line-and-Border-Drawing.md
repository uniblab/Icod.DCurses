# T406 — Geometric Line and Border Drawing

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Checkpoint:** `0.4.0-alpha.5`  
**Status:** Complete

## Purpose

T406 adds reusable layout drawing mechanics without prematurely freezing the capability-aware line-glyph policy planned for `0.6.0`.

## Public operations

```text
DrawHorizontalLine(...)
DrawVerticalLine(...)
DrawBorder(...)
```

All operations use caller-supplied one-column non-continuation `CursesCell` values and preserve the window cursor.

## Border contract

`DrawBorder(...)` draws a box-shaped border around the complete window using separate horizontal, vertical, and four corner cells. The window must be at least two rows by two columns so corner precedence is unambiguous.

## Presentation boundary

`0.4.0` owns the geometry only. It does not define a permanent Unicode/ACS semantic line-drawing vocabulary and does not inspect terminal capabilities to choose alternate glyphs.

Capability-aware semantic line drawing, alternate-character-set degradation, and presentation fallback remain part of the approved `0.6.0` tranche.

## Unicode and cell safety

Drawing uses the same screen-owned cell replacement path as other window edits. If a line or border replaces either half of an existing two-column glyph, the other half is repaired before the new cell is stored.

Continuation cells are rejected as drawing cells.

## Gate result

T406 passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package/fresh-consumer validation.
