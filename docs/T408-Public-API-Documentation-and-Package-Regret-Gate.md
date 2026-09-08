# T408 — Public API, Documentation, and Package Regret Gate

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Current development version:** `0.4.0-alpha.6`  
**Stable baseline:** `0.3.0`  
**Status:** Regret review complete; release-candidate validation pending

## 1. Purpose

T408 reviews the complete 0.4 window editing/composition contract before release-candidate promotion. No new feature family enters this tranche.

## 2. Public delta accepted

The 0.4 release adds no public type or enum. The accepted public delta is limited to methods on `CursesWindow`:

```text
Reposition
GetCell
FillRectangle
ClearToBeginningOfLine
InsertCells
DeleteCells
InsertLines
DeleteLines
CopyRectangleTo
OverlayRectangleTo
DrawHorizontalLine
DrawVerticalLine
DrawBorder
TouchRegion
IsRegionTouched
```

The exact signatures and semantics are recorded in `docs/Public-API-Baseline-0.4.md` and guarded by `PublicWindowEditingApiContractTests`.

## 3. Naming review

The managed names are intentionally descriptive rather than native-curses transliterations.

`Reposition` remains distinct from the existing cursor-oriented `Move` operation.

`CopyRectangleTo` and `OverlayRectangleTo` make destination direction explicit and avoid ambiguous `Copy`/`Overlay` overloads.

`DrawBorder` is retained as the box-shaped perimeter helper. A duplicate `DrawBox` alias is not added merely for native-curses vocabulary symmetry.

## 4. Window-model review

Windows remain lightweight shared views projected into one logical screen.

Repositioning changes projection only; it does not move previously stored content. Nested origins remain immediate-parent-relative.

No window disposal/lifetime API is introduced in 0.4.

## 5. Editing and Unicode review

All insert/delete/copy/overlay operations remain terminal-cell transforms rather than UTF-16 transforms.

Snapshot-transform-commit semantics are retained where source/destination overlap could otherwise make behavior iteration-order dependent.

The 0.3 wide-cell contract remains authoritative. Partial glyphs at edit/copy boundaries are normalized or repaired rather than retained.

## 6. Drawing review

The geometric drawing methods use caller-supplied one-column cells.

This is intentional: 0.4 freezes geometry without committing to one permanent Unicode box-drawing set or alternate-character-set strategy. Capability-aware semantic line-drawing policy remains deferred to 0.6.

No new public `CursesBorderStyle`, glyph enum, or capability abstraction is introduced prematurely.

## 7. Damage review

`TouchRegion` and `IsRegionTouched` are accepted.

A public `UntouchRegion` method is rejected because dirty state participates in reconciliation with retained physical-screen knowledge. Arbitrary application clearing of that state can suppress required output.

The refresh engine already expands dirty spans across continuation cells when rendering, so a range touching one half of a wide glyph remains refresh-safe.

## 8. Compatibility and dependency review

The 0.4 release intentionally retains all existing 0.1/0.2/0.3 public contracts.

The package dependency graph remains:

```text
Icod.DCurses
    -> Icod.Terminal 1.0.0
    -> Icod.TermInfo 1.10.0
```

No direct dependency is added and the approved public Terminal/TermInfo type allow-list is unchanged.

## 9. Package-consumer review

The fresh package-only consumer now executes the 0.4 surface from the generated `.nupkg`, including:

- repositioning;
- cell inspection and rectangle fill;
- insert/delete cells and lines;
- clear-to-beginning-of-line;
- destructive copy and transparent overlay;
- horizontal/vertical lines and border drawing;
- region touch/query.

This is in addition to the established input and Unicode package checks.

## 10. Release-candidate gate

T408 may close and `0.4.0-rc.1` may be prepared when the complete alpha.6 contract passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation;
- fresh package-only consumers for all target frameworks;
- public dependency-boundary tests;
- the frozen 0.4 public API contract tests.

No additional feature family should enter after release-candidate promotion.
