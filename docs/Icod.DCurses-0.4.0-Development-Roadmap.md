# Icod.DCurses 0.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.4.0`  
**Stable baseline:** `0.3.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Window editing and composition  
**Status:** Active

---

## 1. Release Objective

`Icod.DCurses 0.4.0` SHALL turn the existing window/view model into a mature editing and composition surface suitable for editors, pagers, dashboards, inspectors, file managers, and other general TUIs.

The `0.3.0` baseline already freezes the Unicode and terminal-column rules that this release depends on:

- normalized malformed UTF-16 handling before text-element segmentation;
- complete text-element width decisions;
- zero/one/two-column terminal-cell semantics;
- leader/continuation invariants for two-column content;
- explicit East Asian Ambiguous-width policy;
- column-safe measurement, truncation, and slicing helpers;
- repair of screen-owned wide-cell footprints across resize and window boundaries.

`0.4.0` builds editing operations on that contract. No editing primitive may split a text element or leave an invalid two-column footprint.

---

## 2. Architectural Rules

### 2.1 Windows remain shared views

A `CursesWindow` remains a rectangular view projected into its owning `CursesScreen`; it is not an independent compositing buffer. Overlapping windows therefore observe and edit the same logical cells.

### 2.2 Geometry is explicit and local

Window origins are relative to the immediate parent. Root-window origins are relative to the owning screen. Cursor coordinates remain local to the window.

The standard window remains anchored to the complete logical screen and cannot be independently repositioned or resized.

### 2.3 Editing is cell-structural, not UTF-16 structural

Insert/delete/copy/overlay operations work in terminal columns and logical cell footprints. Two-column leaders and continuations move as one structural unit. Operations SHALL repair any source or destination boundary that intersects a wide footprint.

### 2.4 Cursor effects are deliberate

Every editing operation SHALL document whether it preserves, moves, clamps, or otherwise changes the window cursor. Operations that are primarily region transforms SHOULD preserve the cursor unless native-curses precedent or usability strongly favors another behavior.

### 2.5 No terminal-output optimization in this release

Editing transforms modify the logical screen. Choosing terminal insert/delete-character, insert/delete-line, or scrolling escape sequences belongs to `0.7.0` refresh optimization. `0.4.0` SHALL not couple logical editing semantics to a particular terminal optimization.

---

## 3. Development Sequence

```text
T401  0.4 package/version and editing-contract foundation
  -> T402  window geometry, repositioning, and view semantics
  -> T403  cell inspection, region fill, and erase completion
  -> T404  character/cell and line insertion/deletion
  -> T405  region copy, overwrite, and transparent overlay
  -> T406  line drawing, borders, and boxes
  -> T407  dirty-range/touch-range operations and editing acceptance
  -> T408  public API/package regret gate
  -> T409  stable 0.4.0 closure
```

Meaningful contract checkpoints SHOULD advance the prerelease version.

---

# 4. T401 — Package and Editing-Contract Foundation

T401 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.4.0-alpha.1`;
- set `<AssemblyVersion>` to `0.4.0.0`;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- update repository status for the active 0.4 line;
- add initial geometry/editing contract tests before broad implementation.

**Gate T401:** repository build/test/package validation is green as `0.4.0-alpha.1` with no dependency-boundary change.

---

# 5. T402 — Window Geometry and Repositioning

T402 SHALL define geometry strongly enough for later region transforms.

Required work:

- add a descriptive repositioning API for non-standard windows;
- standard-window reposition attempts fail explicitly;
- repositioning validates the complete rectangle against the immediate parent or owning screen;
- existing `OriginRow` / `OriginColumn` remain parent-relative;
- root windows remain screen-relative;
- subwindow coordinates remain relative to the immediate parent;
- nested subwindow mapping remains correct after ancestor repositioning;
- cursor coordinates remain local and unchanged by repositioning;
- resize and reposition behavior is covered together;
- parent/child geometry behavior is documented rather than inferred from clipping accidents.

The initial policy is that window objects remain lightweight non-disposable views. `0.4.0` SHALL NOT introduce lifetime ownership or disposal merely to mimic native `delwin`; a window remains usable while its owning `CursesScreen` exists.

**Gate T402:** root and nested windows can be repositioned predictably without changing logical cursor coordinates or corrupting shared-screen mapping.

---

# 6. T403 — Cell Inspection, Region Fill, and Erase Completion

T403 SHALL expose the minimum window-local primitives needed by editing algorithms and applications.

Required work:

- window-local `GetCell(row, column)` inspection;
- an indexer MAY be considered only if it improves clarity without implying unrestricted raw mutation;
- public rectangular fill using a supplied `CursesCell`;
- clear-to-beginning-of-line;
- clearly defined fill behavior when a region intersects one half of an existing two-column footprint;
- continuation cells SHALL NOT be accepted as public fill/background values where doing so would create structurally invalid repeated storage;
- region validation uses checked window-local coordinates and positive dimensions;
- region operations preserve the cursor unless explicitly documented otherwise.

**Gate T403:** inspection/fill/erase operations are window-local, Unicode-safe, boundary-safe, and package-consumable.

---

# 7. T404 — Insert/Delete Cells and Lines

T404 SHALL add editing transforms that shift existing logical contents.

Candidate managed API:

```text
InsertCells(int count = 1)
DeleteCells(int count = 1)
InsertLines(int count = 1)
DeleteLines(int count = 1)
```

Exact names are subject to API review.

Cell insertion/deletion SHALL operate on the current row beginning at the cursor column. Line insertion/deletion SHALL operate beginning at the cursor row and remain confined to the window rectangle.

Required semantics:

- positive counts only;
- counts larger than the remaining region collapse to fill/erase behavior;
- inserted space uses the window background cell;
- shifted-out content is discarded;
- two-column footprints move atomically or are repaired at edit boundaries;
- no half-wide glyph survives an insertion/deletion boundary;
- overlapping/shared views observe the transformed underlying cells;
- cursor position is preserved;
- dirty state covers every logically changed destination cell.

**Gate T404:** boundary-focused and seeded tests cannot create invalid cell footprints through cell/line insertion or deletion.

---

# 8. T405 — Copy, Overwrite, and Transparent Overlay

T405 SHALL add rectangular composition primitives.

The implementation SHALL distinguish:

- **copy/overwrite**: every source cell in the selected region participates;
- **transparent overlay**: source blank cells do not replace destination cells.

Required semantics:

- source and destination may be the same window or overlapping windows;
- overlapping copies behave as though the source region were snapshotted before destination writes;
- coordinates are window-local;
- source and destination rectangles must fit their respective windows;
- wide-cell boundaries are normalized so a copy never installs half a glyph;
- copying a continuation without its leader does not create an orphan;
- transparent overlay treats ordinary blank cells as transparent; continuation cells remain structural rather than independently transparent;
- cursor states are preserved in both source and destination windows;
- changed destination ranges are marked dirty.

**Gate T405:** same-window overlap, nested-window overlap, cross-window copies, wide glyphs, and transparent blanks are deterministic and invariant-safe.

---

# 9. T406 — Line Drawing, Borders, and Boxes

T406 SHALL provide a managed drawing vocabulary useful for layout without forcing callers to manually place every cell.

Required work:

- horizontal line drawing;
- vertical line drawing;
- border/box helper;
- corners and edge semantics;
- clipping/validation inside the target window;
- explicit style selection using `CurrentStyle` or a supplied style;
- cursor-preserving behavior for region-style drawing methods.

`0.4.0` MAY initially use Unicode box-drawing characters as semantic defaults. Capability-aware ACS/alternate-character-set degradation belongs to `0.6.0`, so the 0.4 drawing API SHOULD be designed so the glyph policy can evolve without renaming the operations.

**Gate T406:** common single-line boxes and horizontal/vertical separators render correctly through shared windows and preserve cell invariants.

---

# 10. T407 — Dirty Range and Editing Acceptance

T407 SHALL round out editing-oriented damage control and acceptance.

Candidate operations:

```text
TouchRegion(...)
UntouchRegion(...)
IsRegionTouched(...)
```

Exact names are subject to API review and to whether exposing "untouch" is safe with the retained physical-screen model.

Required investigation:

- range-oriented touch without forcing whole-window invalidation;
- whether public untouch can incorrectly suppress required refresh work;
- dirty-query semantics that do not expose internal implementation details unnecessarily;
- editor-like and table-like editing workloads;
- nested/overlapping-window operations;
- Unicode-heavy editing;
- package-only consumer usage of the accepted 0.4 surface.

If public untouch cannot be made correctness-preserving, it SHALL be rejected rather than added for native-curses symmetry.

**Gate T407:** accepted damage-range APIs and representative editing workloads are cross-platform green.

---

# 11. T408 — Public API, Documentation, and Package Regret Gate

Before stable `0.4.0`:

- review every new public window-editing member;
- verify the `0.1`/`0.2`/`0.3` public contracts remain intentionally compatible;
- document geometry/reposition semantics;
- document cursor effects for every editing primitive;
- document wide-cell behavior at every region boundary;
- document copy/overlay overlap behavior;
- document drawing semantics and the deferred `0.6` capability-aware glyph policy;
- machine-guard the accepted 0.4 public surface;
- update README and showcase/sample usage;
- update package-only smoke validation;
- verify no new package dependency or Terminal/TermInfo public leakage;
- run the complete Staging and canonical package gates.

**Gate T408:** the window editing/composition contract is intentional, documented, machine-guarded, and package-consumable.

---

# 12. T409 — Stable 0.4.0 Closure

T409 is release closure only.

Required work:

- promote a green release candidate to `0.4.0`;
- retain `AssemblyVersion 0.4.0.0`;
- update package release notes and repository status;
- freeze `docs/Public-API-Baseline-0.4.md`;
- run one definitive stable-source PR matrix/package gate;
- merge only a green stable source head;
- require the exact merged `main` commit to pass the Release matrix;
- create `v0.4.0` only after that main commit is green;
- verify NuGet.org, GitHub Packages, symbols, checksums, and GitHub Release assets.

---

## 13. Explicit 0.4 Non-Goals

`0.4.0` does not include:

- pads or large off-screen surfaces (`0.5.0`);
- terminal capability-aware line-drawing degradation (`0.6.0`);
- new rendition/attribute families (`0.6.0`);
- terminal insert/delete escape-sequence optimization (`0.7.0`);
- synchronized-output optimization (`0.7.0`);
- general widget/toolkit abstractions;
- native `ncurses` function-name compatibility as the primary API;
- bidi or complex-script shaping.

The release objective is a clean logical editing/composition model that later pads and refresh optimization can reuse unchanged.
