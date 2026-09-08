# Icod.DCurses 0.5.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Stable source baseline:** `0.4.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Pads and large off-screen surfaces  
**Status:** Active

---

## 1. Release Objective

`Icod.DCurses 0.5.0` SHALL add large off-screen logical cell surfaces that reuse the stable Unicode, cell, editing, composition, drawing, and damage semantics established by `0.3.0` and `0.4.0`.

The primary use cases are editors, pagers, log viewers, inspectors, large tables, file managers, and dashboards whose logical content is larger than the current physical terminal.

A pad is not a second terminal session and does not own terminal modes, input, or physical refresh. It is an off-screen logical surface whose selected viewport can be projected into an ordinary `CursesWindow`.

---

## 2. Architectural Decisions

### 2.1 Reuse the existing window contract

A pad SHALL reuse `CursesWindow` for its content surface rather than duplicate text writing, Unicode width handling, cell insertion/deletion, copy/overlay, drawing, or damage APIs.

The initial managed model is:

```text
CursesPad
    owns a private large logical backing surface
    exposes one standard CursesWindow content view
    optionally exposes subpad/derived CursesWindow views
    presents selected rectangles into destination CursesWindow instances
```

This keeps the 0.3 Unicode and 0.4 editing contracts authoritative for pad contents.

### 2.2 Pads are off-screen only

A `CursesPad` SHALL NOT:

- own a terminal session;
- perform terminal I/O directly;
- acquire terminal presentation/input leases;
- maintain independent physical-screen knowledge;
- become a second refresh engine.

Viewport presentation modifies the destination logical screen. The existing session refresh path remains responsible for physical output.

### 2.3 Coordinates are explicit

Pad coordinates, destination-window coordinates, and viewport dimensions are all zero-based and explicit.

No API may infer viewport position from terminal cursor state.

### 2.4 Panning is logical composition

Changing pad source coordinates changes which logical pad cells are projected into the destination. Panning is therefore independent of terminal scrolling escape sequences; terminal-output optimization remains a `0.7.0` concern.

### 2.5 Wide cells remain atomic

Viewport boundaries SHALL obey the frozen 0.3 two-column leader/continuation contract. A viewport may not install half of a wide text element into a destination.

---

## 3. Development Sequence

```text
T501  0.5 package/version and pad-contract foundation
  -> T502  core pad storage and content-window reuse
  -> T503  rectangular viewport presentation
  -> T504  viewport state and efficient panning
  -> T505  subpads / derived pad views
  -> T506  pad damage and incremental presentation
  -> T507  resize, clipping, and large-surface acceptance
  -> T508  public API/documentation/package regret gate
  -> T509  stable 0.5.0 closure
```

Meaningful contract checkpoints SHOULD advance the prerelease version.

---

# 4. T501 — Package and Pad-Contract Foundation

T501 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.5.0-alpha.1`;
- set `<AssemblyVersion>` to `0.5.0.0`;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- define pad ownership and resource-limit semantics;
- establish tests that prove pad storage is independent of terminal/screen dimensions.

**Gate T501:** repository build/test/package validation is green with the 0.5 foundation and no dependency-boundary change.

---

# 5. T502 — Core Pad Storage and Content Window

T502 SHALL introduce the core managed pad abstraction.

Candidate API:

```text
new CursesPad(columns, rows, textWidthProvider?)
Columns
Rows
ContentWindow
CreateSubpad(...)
```

Exact names remain subject to API review.

Required semantics:

- arbitrary positive dimensions supported up to the existing checked logical-cell count limit and available runtime memory;
- pad dimensions are independent of terminal/screen dimensions;
- the pad uses the supplied text-width provider or the standard Unicode provider;
- `ContentWindow` covers the complete pad backing surface;
- content-window cursor state is pad-local;
- all established `CursesWindow` write/edit/copy/drawing operations work unchanged on pad content;
- pad construction does not perform terminal I/O;
- pad contents are initially blank and logically dirty for pad-internal tracking.

A pad MAY internally reuse a private `CursesScreen` implementation detail if that avoids duplicating the window/cell machinery. That backing screen is not part of the public pad contract.

**Gate T502:** applications can create and edit a pad larger than the destination screen using the existing window API and Unicode rules.

---

# 6. T503 — Rectangular Viewport Presentation

T503 SHALL project one rectangular pad viewport into an ordinary destination `CursesWindow`.

Candidate API:

```text
PresentTo(
    destination,
    padRow,
    padColumn,
    rows,
    columns,
    destinationRow,
    destinationColumn
)
```

Required semantics:

- source rectangle is pad-local;
- destination rectangle is window-local;
- source and destination rectangles are validated explicitly;
- destination cursor is preserved;
- pad content cursor is preserved;
- source is snapshotted before destination mutation where overlap can matter;
- wide cells cut by the pad viewport boundary are normalized rather than copied partially;
- destination wide footprints intersected by presentation are repaired;
- ordinary blank pad cells are copied destructively by default;
- no physical terminal refresh is implied.

This operation should reuse the stable 0.4 rectangle-copy semantics rather than introduce a separate cell-copy algorithm unless pad-specific behavior requires one.

**Gate T503:** a large pad can present arbitrary valid viewports into root/nested destination windows without invalid cell footprints.

---

# 7. T504 — Viewport State and Efficient Panning

Repeated manual source-coordinate bookkeeping should not be required for common scrolling/panning applications.

T504 SHALL investigate and, if worthwhile, introduce a small viewport object or equivalent stateful abstraction that records:

- pad source row/column;
- viewport rows/columns;
- destination row/column;
- destination window identity;
- validated pan operations or absolute repositioning.

Candidate operations include descriptive managed names such as:

```text
SetSource(...)
PanBy(...)
Present()
```

The API SHALL NOT imply physical terminal scrolling. Panning only changes logical projection.

The design SHOULD avoid making the pad itself remember one global viewport, because one pad may legitimately be presented in multiple destinations at once.

**Gate T504:** repeated horizontal/vertical panning can be expressed without application-private coordinate plumbing and without coupling one pad to only one viewport.

---

# 8. T505 — Subpads and Derived Views

T505 SHALL add shared pad-local subviews only where their ownership semantics remain unambiguous.

Preferred model:

- subpads are views into the same backing pad storage;
- edits through a subpad are immediately visible through the parent pad and sibling overlapping views;
- subpad origins are relative to the immediate containing pad/view;
- subpad cursor state is local to the view;
- subpads do not copy cells merely to create a view;
- lifetime remains lightweight/non-disposable unless a concrete resource requires ownership.

If ordinary `CursesWindow.CreateSubwindow(...)` over `ContentWindow` already provides the exact desired semantics, `0.5.0` SHOULD reuse it rather than invent a duplicate `CursesSubpad` type.

**Gate T505:** shared pad subviews work predictably with edits, Unicode wide cells, and viewport presentation.

---

# 9. T506 — Pad Damage and Incremental Presentation

T506 SHALL define damage semantics useful for large surfaces without conflating them with physical-screen dirty state.

Required investigation:

- whether the existing backing-surface dirty bits are sufficient for pad-local change tracking;
- how a viewport determines that visible pad content changed since its previous presentation;
- how panning itself invalidates newly exposed destination cells even when pad contents are unchanged;
- whether unchanged visible cells can be skipped safely at logical composition time;
- whether damage state belongs to the pad, the viewport, or both;
- how multiple simultaneous viewports over one pad observe changes independently.

A single global "mark pad clean" operation SHALL NOT be exposed if it would make one viewport hide changes from another.

**Gate T506:** multiple viewports can detect/present relevant changes without unsafe global acknowledgement semantics.

---

# 10. T507 — Resize, Clipping, and Large-Surface Acceptance

T507 SHALL harden pads against realistic terminal/application geometry changes.

Required coverage:

- destination screen growth and shrink;
- destination window resize/reposition before subsequent presentation;
- viewport source panning near every pad boundary;
- viewport destination placement near every window boundary;
- wide glyphs on all source/destination edges;
- very tall and very wide pads;
- editor-like insert/delete workloads in large pads;
- table/log-view workloads;
- repeated panning without cell-footprint corruption;
- allocation/resource-limit failure behavior;
- package-only consumption of the accepted pad surface.

The pad itself SHALL NOT resize merely because the physical terminal resizes. Destination viewport geometry is revalidated or adjusted by the viewport/presentation contract.

**Gate T507:** large-pad workloads remain cross-platform green and terminal resize does not mutate pad content unexpectedly.

---

# 11. T508 — Public API, Documentation, and Package Regret Gate

Before stable `0.5.0`:

- review every new pad/viewport public type and member;
- freeze ownership and lifetime semantics;
- freeze coordinate and dimension semantics;
- freeze destructive-vs-transparent presentation behavior;
- freeze wide-cell viewport-boundary behavior;
- freeze multi-viewport damage semantics;
- machine-guard the accepted public surface;
- document editors/pagers/log viewers as representative usage;
- update README and appropriate showcase/sample material;
- extend fresh package-only validation;
- verify no new direct dependency or Terminal/TermInfo public leakage;
- run the complete Staging and package gates.

**Gate T508:** the pad/viewport contract is intentional, documented, machine-guarded, and package-consumable.

---

# 12. T509 — Stable 0.5.0 Closure

T509 is release closure only.

Required work:

- promote a green release candidate to `0.5.0`;
- retain `AssemblyVersion 0.5.0.0`;
- update package release notes and repository status;
- freeze `docs/Public-API-Baseline-0.5.md`;
- run one definitive stable-source PR matrix/package gate;
- merge only a green stable source head;
- require the exact merged `main` commit to pass the Release matrix;
- create `v0.5.0` only after that main commit is green;
- verify NuGet.org, GitHub Packages, symbols, checksums, and GitHub Release assets.

---

## 13. Explicit 0.5 Non-Goals

`0.5.0` does not include:

- capability-aware semantic line-glyph selection (`0.6.0`);
- new color/rendition families (`0.6.0`);
- terminal insert/delete/scroll optimization (`0.7.0`);
- synchronized-output optimization (`0.7.0`);
- virtualized/sparse backing stores for content vastly larger than addressable in-memory cell arrays;
- memory-mapped or disk-backed pads;
- widgets or higher-level layout controls;
- terminal emulation;
- independent pad terminal sessions.

The release objective is a clean, reusable in-memory large-surface and viewport model that later presentation and refresh optimization can consume unchanged.
