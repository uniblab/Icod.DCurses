# T507 — Resize, Clipping, and Large-Surface Acceptance

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Checkpoint:** `0.5.0-alpha.4`  
**Stable baseline:** `0.4.0`  
**Status:** Acceptance implementation complete; validation pending

## 1. Purpose

T507 hardens the pad and viewport contract under geometry changes, boundary clipping, large in-memory surfaces, repeated panning, and representative editor-like mutations.

No new public feature family is introduced by this tranche.

## 2. Destination resize semantics

Pads remain independent of terminal and destination dimensions.

A destination screen resize does not mutate pad dimensions or pad contents.

`CursesPadViewport` retains its fixed logical viewport and destination rectangle. Every `Present()` revalidates that rectangle against the destination window's current geometry.

Therefore:

- destination growth is transparent when the fixed viewport still fits;
- destination shrink which makes the rectangle invalid causes presentation to fail explicitly;
- regrowing the destination makes the same viewport usable again when its rectangle fits;
- no silent clipping changes the viewport contract.

## 3. Destination repositioning

Viewport destination coordinates remain window-local.

If a non-standard destination window is repositioned, the next presentation follows that window's new projection into the owning screen without changing the viewport's stored destination row/column.

## 4. Source boundary coverage

Acceptance tests exercise the viewport at all four legal pad-corner positions.

Wide text elements intersecting the left or right source boundary are normalized according to the stable 0.3/0.4 copy contract rather than installing an orphan continuation or half of a two-column leader.

Destination writes which intersect an existing wide footprint repair the out-of-rectangle half before storing the projected pad cells.

## 5. Large in-memory surfaces

The acceptance corpus includes both:

- very wide pads (`20,000` columns);
- very tall pads (`20,000` rows).

Far-edge cells are edited and presented through ordinary pad/window APIs. The resource model remains the existing checked in-memory cell-count limit plus actual runtime allocation capacity.

Sparse, virtualized, memory-mapped, and disk-backed pads remain explicit 0.5 non-goals.

## 6. Repeated panning and editing

The acceptance workload combines:

- shared derived pad views;
- Unicode wide content;
- cell insertion/deletion;
- line insertion/deletion;
- repeated deterministic viewport repositioning;
- presentation at many source locations;
- destination wide-footprint validation after every presentation.

This is intended to approximate editor/table/log-view behavior without introducing application-specific widgets.

## 7. Package-only acceptance

The fresh package-only consumer now creates and uses:

- `CursesPad`;
- a derived `ContentWindow.CreateSubwindow(...)` view;
- direct `PresentTo(...)` projection;
- `CursesPadViewport`;
- `HasVisiblePadChanges`;
- explicit pad touch/damage observation;
- panning and repeated presentation.

This keeps the 0.5 package gate independent of repository project references.

## 8. Gate

T507 is complete when the exact alpha.4 checkpoint passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation;
- fresh package-only consumers for all target frameworks;
- all large-pad and geometry-churn acceptance tests.

A green T507 gate moves the release into T508 public API/documentation/package regret review. No additional pad feature family should enter after that point without reopening the acceptance design.
