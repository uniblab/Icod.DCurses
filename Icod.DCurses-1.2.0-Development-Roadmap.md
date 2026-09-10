# Icod.DCurses 1.2.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.2.0`  
**Stable compatibility floor:** `1.0.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Development package:** `1.2.0-alpha.1`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies at branch start:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** panels, independent retained layers, visibility, and deterministic z-order composition  
**Status:** T1201 starting

---

## 1. Release objective

`Icod.DCurses 1.2.0` adds first-class overlapping retained surfaces without changing the stable meaning of an ordinary `CursesWindow` and without introducing widgets.

The stable 1.0/1.1 window contract is deliberately preserved:

```text
CursesWindow
    -> rectangular view into one owning logical screen
    -> overlapping ordinary windows share logical cells
```

Panels add a different abstraction:

```text
CursesPanel
    -> owns an independent retained cell surface
    -> participates in one screen-owned z-order stack
    -> composites onto the screen according to position, visibility,
       clipping, and transparency policy
```

This distinction is central. Existing applications which use overlapping windows must not silently acquire retained-layer behavior after upgrading to 1.2.

The release must support application shapes such as modal help, command palettes, completion popups, context-menu surfaces, temporary status/error overlays, and movable dialogs. Hiding or moving a panel must reveal the correct retained content below it without requiring the application to reconstruct that content manually.

---

## 2. Architectural boundary

The ownership stack remains:

```text
application / future widget package
              |
              v
        Icod.DCurses
  panels / layers / composition
  cells / metadata / retained frame
              |
              v
        Icod.Terminal
  physical terminal conversation
  semantic protocol operations
              |
              v
        Icod.TermInfo
  immutable capability authority
```

Version 1.2 introduces no raw OSC/CSI/DCS/APC output, no new terminal input reader, no private terminal capability database, and no dependency on Terminal implementation details.

Panel composition is terminal-independent logical work. The existing retained renderer remains responsible for translating the final logical frame into physical terminal changes.

The branch depends on the package versions declared by `Icod.DCurses.csproj`, but 1.2 validation must not hard-code sibling-package versions in verifier policy. Restore/build/test establishes dependency compatibility; package verification establishes package integrity.

---

## 3. Stable compatibility constraints

The accepted 1.1 public contract is the starting API floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Version 1.2 is additive by default.

Required invariants:

1. `AssemblyVersion` remains `1.0.0.0`.
2. Existing `CursesWindow` overlapping/shared-cell semantics remain unchanged.
3. Existing standard-screen, pad, viewport, metadata, Unicode, wide-cell, line-glyph, damage, refresh, input, and lifecycle behavior remains source- and binary-compatible.
4. Public panel APIs must not expose `Icod.Terminal` routing/protocol types.
5. Panel implementation must not force an unconditional per-cell allocation or reference field into ordinary screens or windows.
6. Semantic metadata must compose with the same identity and overwrite rules as the cell content it annotates.
7. Wide-cell leader/continuation footprints must never be exposed in an invalid composed state.
8. Hidden or fully occluded panel changes must not force unnecessary physical terminal output.
9. Public API growth is frozen only after application-shaped acceptance and the 1.2 regret gate.

---

## 4. Core design decisions

### 4.1 Ordinary windows remain views

`CursesWindow` continues to represent a rectangular projection into one owning `CursesScreen`. It does not become a retained backing store merely to support panels.

### 4.2 Panels own independent retained surfaces

A panel has content independent of the screen beneath it. Its public content editing surface should use the existing `CursesWindow` API so text width, Unicode, drawing, editing, metadata, and scrolling semantics are reused rather than reimplemented.

The initial implementation therefore builds panel content over a private logical backing surface and exposes a normal content window over that surface.

### 4.3 One deterministic stack per destination screen

A panel belongs to exactly one destination `CursesScreen` at a time. Visible panels participate in one deterministic bottom-to-top order. Hidden panels retain their content and logical position but are excluded from composition until shown again.

Identity, not content equality, determines stack membership.

### 4.4 Composition does not mutate panel content

Composition reads panel logical state and writes the final destination image. Moving, hiding, raising, lowering, clipping, or occluding a panel must not rewrite its retained content.

### 4.5 Opaque is the default

Panels are opaque by default. A blank panel cell therefore normally covers the cell beneath it; this is required for dialogs and popup backgrounds.

A later T1204 public transparency policy will provide intentional transparent-cell behavior. The accepted candidate is a panel-level mode where ordinary blank cells are transparent. In that mode, a transparent blank carries no visual or semantic contribution to the composed frame. Styled blanks remain useful as opaque content by choosing opaque mode.

### 4.6 Composition stays above physical refresh

The panel compositor produces the desired logical screen. Existing DCurses refresh/diff/output machinery remains the only physical renderer. Panels do not call Terminal output APIs directly.

---

## 5. T1201 — foundation, representation, and order engine

### Goal

Establish the release contract and the smallest internal primitives needed for deterministic retained-layer work without freezing public panel APIs prematurely.

### Work

- advance branch package identity to `1.2.0-alpha.1` while retaining `AssemblyVersion 1.0.0.0`;
- record this dedicated 1.2 roadmap and implementation plan;
- correct active roadmap dependency/status text for the post-1.1 branch;
- introduce an internal deterministic panel-order engine;
- prove identity-based insertion, removal, top/bottom movement, relative movement, duplicate rejection, and stable bottom-to-top snapshots;
- keep T1201 internal so public naming remains available for regret review in T1202/T1203.

### Acceptance

The order engine must be deterministic, allocation-bounded by panel count rather than screen-cell count, and independent of Terminal/TermInfo.

---

## 6. T1202 — independent panel surface and public creation contract

### Goal

Introduce the first public panel object while preserving ordinary window semantics.

### Candidate public shape

The intended contract is equivalent to:

```csharp
public sealed class CursesPanel {
    public CursesWindow ContentWindow { get; }
    public int Row { get; }
    public int Column { get; }
    public int Rows { get; }
    public int Columns { get; }
    public bool IsVisible { get; }
}
```

and screen-owned creation equivalent to:

```csharp
public CursesPanel CreatePanel(
    int row,
    int column,
    int rows,
    int columns
);
```

Exact public spelling is frozen only after source-compatibility and API-regret review.

### Requirements

- panel content is independent of the destination screen and other panels;
- content editing uses the existing `CursesWindow` behavior;
- default content is ordinary blank cells with no semantic metadata;
- panel text-width policy matches the destination screen's `ICursesTextWidthProvider`;
- invalid dimensions and out-of-range initial placement fail before allocation;
- creating a panel must not change existing destination cells until composition/refresh policy says it should.

---

## 7. T1203 — visibility, movement, and z-order public operations

### Goal

Freeze deterministic stack manipulation.

Required semantics:

- show/hide;
- move to a new screen-cell origin;
- move to top/front;
- move to bottom/back;
- move immediately above another panel;
- move immediately below another panel;
- deterministic bottom-to-top enumeration or inspection adequate for tests and higher-level composition;
- reject cross-screen relative-order operations;
- idempotent no-op operations do not create spurious logical damage.

A hidden panel keeps its remembered stack relationship or a documented deterministic reinsertion rule; this must be frozen before public API closure. The preferred model is to retain full stack membership and make visibility an independent property, so hide/show does not unexpectedly change z-order.

---

## 8. T1204 — logical composition, clipping, and transparency

### Goal

Produce a correct final logical frame from the standard screen plus visible panels.

Composition order:

```text
base logical screen
    -> visible panel 0 (bottom)
    -> visible panel 1
    -> ...
    -> visible panel N (top)
    -> desired composed frame
```

Requirements:

- screen-edge clipping;
- panel-to-panel occlusion;
- opaque blank cells cover lower content;
- optional blank-cell transparency is explicit and panel-scoped;
- transparent cells contribute neither cell content nor semantic metadata;
- nonblank panel content replaces lower cell and metadata state together;
- panel content remains unchanged by composition;
- the compositor must not depend on terminal family or protocol support.

---

## 9. T1205 — damage, invalidation, and incremental recomposition

### Goal

Avoid repainting the entire screen for routine panel operations while preserving correctness.

Damage sources include:

- panel content changes;
- semantic-only content changes;
- show/hide;
- reposition;
- z-order changes;
- transparency-policy changes;
- destination-screen resize;
- lower-layer changes that become visible through transparent or moved panels.

The implementation should use rectangular/row-range invalidation where practical and must never acknowledge panel-local changes on behalf of another consumer before those changes have been incorporated into the destination composition.

Acceptance includes hidden-panel updates that cause no destination repaint until the panel becomes visible.

---

## 10. T1206 — Unicode, wide cells, line glyphs, and semantic metadata

### Goal

Qualify composition across the existing logical-content contracts.

Required cases:

- width-1 text;
- width-2 leader/continuation footprints;
- clipping of a wide cell at panel or screen edges;
- panel overlap through a wide footprint;
- semantic line glyphs;
- styled blanks;
- hyperlink metadata and semantic-only changes;
- transparent blank behavior with metadata absent;
- repair after a higher panel exposes only part of a previously occluded wide footprint.

The composed screen must always satisfy `CursesCellFootprint` invariants.

---

## 11. T1207 — resize, lifecycle, and session refresh integration

### Goal

Integrate panels with live `CursesSession` behavior without creating another terminal owner.

Requirements:

- destination screen resize clips/reveals panels deterministically;
- panel content survives destination resize;
- panel coordinates remain logical screen-cell coordinates;
- refresh observes the composed desired frame;
- synchronized output continues to be owned by the existing refresh path;
- suspend/resume invalidates physical knowledge without losing retained panel content;
- session disposal remains Terminal-authoritative for physical cleanup;
- no panel-specific input reader or lifecycle task is introduced.

---

## 12. T1208 — application, performance, and allocation acceptance

Application-shaped acceptance must include:

1. modal help overlay over a changing base screen;
2. command palette which is repeatedly shown/hidden without content reconstruction;
3. completion popup moving above editor content;
4. context menu with intentional blank background cells;
5. transient status/error overlay;
6. movable dialog crossing screen edges after resize;
7. multiple overlapping panels with repeated z-order changes;
8. semantic metadata beneath, within, and above panels;
9. large base screen with sparse small panels;
10. hidden panels receiving updates without physical churn.

Measure allocations and recomposition work against panel count and damaged area. Avoid any design that imposes panel-related per-cell storage on applications that never create a panel.

---

## 13. T1209 — public API, package, documentation, and regret gate

Before RC:

- generate the compiler-derived public API fingerprint for all three TFMs;
- compare against the accepted 1.1 45-type/337-line floor;
- audit overload ambiguity and `default` source compatibility;
- audit nullable annotations;
- audit panel lifetime/ownership semantics;
- verify no public Terminal protocol/routing types escape;
- update fresh NuGet-only consumer coverage;
- add one focused panel sample;
- update README and current roadmaps;
- verify package dependency declarations without hard-coded sibling-version policy;
- freeze the final public names only after these checks.

---

## 14. T1210 — RC and stable 1.2.0 closure

Promote through one release candidate only after T1201–T1209 are green.

Stable closure requires:

- clean restore/build/test/package validation;
- Windows/Linux/macOS x64/ARM64 runtime matrix;
- fresh package consumer on `net8.0`, `net9.0`, and `net10.0`;
- exact public API baseline recorded;
- sample/docs/package metadata synchronized;
- no known lifecycle, composition, Unicode, metadata, or damage blocker;
- stable source qualified at an exact immutable head before merge/tag/publication.

Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.

---

## 15. Explicit non-goals

Version 1.2 does not add:

- buttons, text boxes, menus, dialogs, tables, or other widgets;
- focus traversal or gesture routing (planned for 1.4);
- general layout arithmetic (planned for 1.3);
- raster/image placement;
- animation;
- compositor effects, alpha blending, shadows, or translucency;
- terminal pixel-coordinate layout;
- direct Terminal protocol output;
- terminal emulation;
- PTY/process hosting.

---

## 16. Immediate implementation sequence

```text
T1201  roadmap + 1.2 alpha identity + deterministic internal order engine
  -> T1202  independent retained panel surface + public creation
  -> T1203  visibility/movement/z-order API
  -> T1204  opaque/transparent logical composition
  -> T1205  damage + incremental recomposition
  -> T1206  Unicode/wide/metadata composition hardening
  -> T1207  resize/lifecycle/session integration
  -> T1208  application/performance/allocation acceptance
  -> T1209  public API/package/docs/regret gate
  -> T1210  RC/stable closure
```

The first implementation task is deliberately internal: prove the deterministic order model before exposing a public panel contract.