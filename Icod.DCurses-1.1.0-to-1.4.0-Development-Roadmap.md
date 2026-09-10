# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Accepted compatibility floor:** `1.1.0`  
**Active source package:** `1.2.0-rc.1`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current declared runtime dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Planning status:** approved release train; 1.2 T1201–T1209 qualified; T1210 RC qualification active

---

## 1. Purpose

The 1.0 release established a stable curses-style terminal UI substrate. The next four minor releases grow that substrate upward into semantic content, composition, layout, and interaction without taking raw terminal-protocol ownership away from `Icod.Terminal` and without prematurely turning the core into a widget toolkit.

```text
1.1.0  semantic cell metadata + hyperlinks
1.2.0  panels/layers + z-order composition
1.3.0  layout + resize primitives
1.4.0  focus/interaction/key gestures/hit testing/pointer semantics
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes their geometry manageable; 1.4 routes semantic input to logical regions.

## 2. Terminal relationship

The current 1.2 branch declares `Icod.Terminal 1.8.1` and `Icod.TermInfo 1.10.0`.

DCurses depends on Terminal's semantic session/input/lifecycle/output contracts rather than terminal-family protocol details. Package validation therefore does not hard-code sibling dependency versions.

```text
DCurses semantic UI concepts
            |
            v
Terminal live terminal semantics
            |
            v
TermInfo immutable capability descriptions
```

DCurses does not introduce private OSC/CSI/DCS/APC emitters, terminal-family capability probes, or a second live input/lifecycle owner.

## 3. Release 1.1.0 — semantic cell metadata and hyperlinks

The accepted 1.1 contract is:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

The two 1.1 exported types are `CursesHyperlink` and `CursesCellMetadata`. Metadata uses a lazily allocated row-sparse plane, follows surviving content through editing/composition/pads/resize, remains coherent across wide-cell footprints, and is rendered through Terminal-owned bounded hyperlink operations.

The detailed 1.1 roadmap and T1101–T1109 records remain historical compatibility authorities.

## 4. Release 1.2.0 — panels, layers, visibility, and z-order

`CursesPanel` owns an independent retained surface edited through the existing `CursesWindow` model. A `CursesScreen` owns panel identity and deterministic bottom-to-top order.

The accepted 1.2 candidate provides:

- screen-owned panel creation and independent retained content;
- show/hide with remembered z-order;
- movement and relative/top/bottom ordering;
- opaque composition by default and explicit `BlankCellsTransparent` composition;
- clipping without mutating panel content;
- damage-bounded incremental recomposition;
- Unicode width-two, line-glyph, style, and semantic-metadata coherence;
- live `CursesSession.RefreshAsync()` integration;
- retained content across destination resize and suspend/resume;
- deterministic one-way `IDisposable` removal for transient panels;
- a no-panel fast path without panel snapshot allocation.

Accepted candidate fingerprint:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1: `CursesPanel` and `CursesPanelTransparency`.

T1208 application/resource acceptance qualified at `bb00707779cf3dc6c2222455a6f942469d036881` in workflow #596 / `34522859308`.

T1209's lifetime/API head `866497c9d5015f3a149580d67eacefaf7e121aaf` passed workflow #600 / `34524054785`. The documentation/sample/package-complete head `3728bf0e576b32747dd3a628ed5d3eca768ac67f` passed workflow #603 / `34525966166`, including package-only panel consumption, the new focused panel sample, and 554/554 tests per supported TFM on Linux ARM64.

The current `1.2.0-rc.1` promotion carries the same implementation/API into T1210 exact-head qualification.

No `Button`, `TextBox`, `Menu`, `Dialog`, widget tree, general panel resizing, layout solver, or focus dispatch belongs in 1.2. Panel size remains fixed; general layout/resize primitives belong to 1.3.

## 5. Release 1.3.0 — layout and resize primitives

Version 1.3 is planned to remove routine terminal-geometry arithmetic from applications while keeping layout deterministic and curses-oriented. Candidate concepts include rectangles/bounds, insets, splits, fixed/remainder allocation, proportional allocation, minimum/maximum sizes, docking, clipping/empty-layout behavior, and deterministic recomputation after resize.

No CSS, browser-style flexbox, general constraint solver, animation system, or declarative widget tree is required in the core.

## 6. Release 1.4.0 — focus and interaction mechanics

Version 1.4 is planned to add focusable logical regions, focus ownership/traversal, keyboard gestures/commands, mouse hit testing, interaction regions, pointer-shape requests, focus repair, resize-aware hit testing, and deterministic overlap precedence.

DCurses owns geometry, focus, target selection, and semantic interaction policy. Terminal remains the authoritative input stream and physical pointer/protocol owner. Version 1.4 provides interaction substrate, not widgets.

## 7. Cross-release rules

Each compatible 1.x release must preserve additive API by default, compiler-derived public fingerprints, Terminal/TermInfo ownership boundaries, Unicode/wide-cell/metadata semantics, conservative lifecycle recovery, package-only consumer validation, Windows/Linux/macOS x64/ARM64 validation, and a documentation/sample/package audit before stable promotion.

## 8. Immediate next step

Qualify the exact `1.2.0-rc.1` head across the full seven-job PR matrix. If green, promote the same implementation/API to stable-source `1.2.0` and qualify that exact head. Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
