# Icod.DCurses 0.5.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Stable baseline:** `0.4.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Pads and large off-screen surfaces  
**Current development version:** `0.5.0-alpha.4`  
**Status:** T501-T507 complete; T508 public API/package regret gate active

---

## 1. Release Objective

`Icod.DCurses 0.5.0` adds logical cell surfaces larger than the physical terminal and a managed viewport/panning contract suitable for editors, pagers, log viewers, inspectors, tables, and file managers.

The release builds directly on the stable contracts already frozen by:

- `0.3.0`: Unicode text elements, terminal-column width, and wide-cell invariants;
- `0.4.0`: window geometry, cell/line editing, copy/overlay, drawing, and damage ranges.

A pad does not create a second terminal model. It is an off-screen logical cell surface whose visible regions can be projected into ordinary screen windows.

---

## 2. Architectural Rules

### 2.1 Pads are off-screen only

`CursesPad` SHALL NOT own terminal modes, terminal input, terminal output, a physical-screen model, or a second refresh engine.

### 2.2 Reuse the frozen window contract

Pad content is edited through ordinary `CursesWindow` semantics. The release SHALL NOT duplicate text segmentation, cell writing, insert/delete, copy/overlay, drawing, or wide-cell repair merely because the destination is off-screen.

### 2.3 Viewports are logical projections

Presenting a pad viewport copies logical cells into an ordinary destination window. Physical terminal synchronization remains the responsibility of the existing session refresh boundary.

### 2.4 Multiple projections are independent

One pad may be presented through multiple independently positioned viewports. The pad does not own one global scroll position, and one viewport must not acknowledge content changes on behalf of another.

### 2.5 Correctness before terminal-output optimization

Panning changes logical projection state. Terminal scrolling regions, insert/delete-line optimizations, synchronized-output framing, and output-cost selection remain `0.7.0` work.

---

## 3. Development Sequence

```text
T501  0.5 package/version and pad-contract foundation             complete
  -> T502  core pad storage and content-window reuse              complete
  -> T503  rectangular viewport presentation                     complete
  -> T504  viewport state and efficient panning                   complete
  -> T505  subpads / derived pad views                            complete
  -> T506  pad damage and incremental presentation                complete
  -> T507  resize, clipping, and large-surface acceptance         complete
  -> T508  public API/documentation/package regret gate           active
  -> T509  stable 0.5.0 closure
```

Meaningful contract checkpoints advance the prerelease version.

---

# 4. T501 — Package and Pad-Contract Foundation

Completed as `0.5.0-alpha.1`.

The release established:

- `<AssemblyVersion>0.5.0.0</AssemblyVersion>`;
- unchanged `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0` dependencies;
- this detailed roadmap;
- the rule that pads are off-screen logical surfaces rather than terminal/session owners.

---

# 5. T502 — Core Pad Storage and Content-Window Reuse

Completed as part of `0.5.0-alpha.1`.

`CursesPad` owns a private logical backing surface and exposes `ContentWindow` for editing. The existing dense in-memory `CursesVirtualScreen` resource limit remains authoritative: dimensions must be positive, the checked cell count must fit in `int`, and actual allocation must fit available process memory.

Pad content therefore automatically reuses:

- Unicode/wide-cell semantics;
- styles and colors;
- cursor/wrap behavior;
- insert/delete cells and lines;
- region fill/erase;
- copy/overlay;
- geometric drawing;
- touch/damage operations.

---

# 6. T503 — Rectangular Viewport Presentation

Completed as part of `0.5.0-alpha.1`.

`CursesPad.PresentTo(...)` projects a validated source rectangle into an ordinary destination `CursesWindow` by reusing the stable 0.4 destructive rectangle-copy engine.

The contract is:

- source/destination coordinates are local to their respective logical surfaces;
- both rectangles must fit completely;
- ordinary source blanks replace destination cells;
- source and destination cursor positions are preserved;
- wide glyphs clipped at the source boundary are normalized;
- destination wide footprints are repaired before replacement;
- no terminal refresh is implied.

---

# 7. T504 — Viewport State and Panning

Completed as `0.5.0-alpha.2`.

`CursesPadViewport` binds one pad source rectangle to one destination window and owns independent source position state.

Accepted operations:

```text
SetSource(row, column)
PanBy(rowDelta, columnDelta)
Present()
```

Viewport dimensions and destination-local placement are fixed at construction in 0.5. Invalid absolute/relative source moves throw without changing prior state. Multiple viewports over one pad pan independently.

---

# 8. T505 — Derived Pad Views

Completed as `0.5.0-alpha.3` without adding a separate subpad type.

The official derived-view contract is:

```text
pad.ContentWindow.CreateSubwindow(...)
```

This already provides shared storage, immediate-parent-relative origins, independent cursors, overlap visibility, Unicode/wide-cell behavior, and lightweight lifetime. A separate `CursesSubpad` hierarchy was rejected as redundant.

---

# 9. T506 — Pad Damage and Independent Viewport Observation

Completed as `0.5.0-alpha.3`.

`CursesPadViewport.HasVisiblePadChanges` reports pad-source changes relative to that viewport's own last successful presentation.

The backing pad surface maintains internal content/damage revisions for:

- value changes;
- wide-footprint repairs;
- explicit touch operations;
- logical invalidation.

Each viewport snapshots its own visible revisions. Presenting viewport A never acknowledges changes for viewport B.

`Present()` remains authoritative even when `HasVisiblePadChanges` is false, because destination cells may have been changed independently by another window.

A global public pad-clean/acknowledge operation is intentionally rejected.

---

# 10. T507 — Resize, Clipping, and Large-Surface Acceptance

Completed as `0.5.0-alpha.4` and green on Windows, Linux, macOS, and the canonical package/fresh-consumer gate.

Acceptance covers:

- destination screen shrink/growth;
- destination window repositioning;
- all pad-corner viewport locations;
- wide glyphs on source and destination boundaries;
- very wide and very tall pads (`20,000` columns/rows);
- editor-like insert/delete mutations through derived views;
- repeated deterministic panning and presentation;
- wide-cell footprint validation after every stress presentation;
- fresh-package use of the complete accepted pad surface.

Pads remain independent of terminal resize. Each fixed viewport revalidates current destination geometry at presentation time; invalid geometry fails explicitly rather than silently clipping.

---

# 11. T508 — Public API, Documentation, and Package Regret Gate

T508 is active and feature-frozen.

The accepted 0.5 public delta is limited to two sealed types:

```text
CursesPad
CursesPadViewport
```

The exact surface is recorded in `docs/Public-API-Baseline-0.5.md` and guarded by `PublicPadApiContractTests`.

The regret pass has also corrected one internal cost issue before RC: per-cell pad revision tracking is now opt-in/lazy and is enabled only on pad backing surfaces. Ordinary `CursesScreen` / standalone `CursesVirtualScreen` instances do not allocate pad revision arrays.

T508 SHALL close only after the exact alpha.4 feature contract plus this internal correction passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation;
- fresh package-only consumers;
- dependency-boundary tests;
- frozen pad public-API tests;
- opt-in change-tracking tests;
- large-surface acceptance.

No new pad feature family enters after a green T508 gate.

---

# 12. T509 — Stable 0.5.0 Closure

T509 is release closure only.

Required sequence:

1. promote the green T508 contract to `0.5.0-rc.1`;
2. run one definitive RC Staging/package gate;
3. if green, promote the unchanged contract to stable `0.5.0`;
4. retain `AssemblyVersion 0.5.0.0`;
5. update package release notes and repository status;
6. run one definitive stable-source PR matrix/package gate;
7. merge only a green stable source head;
8. require the exact merged `main` commit to pass the Release matrix;
9. create `v0.5.0` only after that main commit is green;
10. verify package, symbols, checksums, and GitHub Release publication.

---

## 13. Explicit 0.5 Non-Goals

`0.5.0` does not include:

- terminal/session ownership by pads;
- a second physical refresh engine;
- sparse or disk-backed pad storage;
- a separate `CursesSubpad` hierarchy;
- global pad clean/acknowledge semantics;
- resizable viewport geometry after construction;
- silent automatic clipping after destination resize;
- capability-aware line-drawing degradation (`0.6.0`);
- terminal scrolling/edit optimization (`0.7.0`);
- widgets or higher-level layout controls.

The release objective is a stable off-screen surface and viewport contract that later presentation and refresh optimization can reuse unchanged.
