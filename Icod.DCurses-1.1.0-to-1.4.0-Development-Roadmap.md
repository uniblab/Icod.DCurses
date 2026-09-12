# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Published compatibility floor:** `1.2.0`  
**Current main source package:** `1.3.0`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current declared runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Planning status:** 1.3 complete, merged, and Release-qualified; 1.4 remains the approved next development release

---

## Release sequence

```text
1.1.0  semantic cell metadata + hyperlinks                  complete/published history
1.2.0  panels/layers + z-order composition                  complete/published
1.3.0  layout + resize primitives                           complete/merged; publication pending
1.4.0  focus/interaction/key gestures/hit testing/pointer   approved future release
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes geometry manageable; 1.4 routes semantic input to logical regions.

## Ownership boundary

DCurses consumes Terminal's live-session/input/lifecycle/semantic-output contracts rather than terminal-family protocol details and does not install private protocol writers or a second input owner.

The 1.3 layout work preserves the same ownership principle internally: geometry values are immutable, `CursesLayout` is pure/stateless, and applications explicitly recompute/apply layout rather than delegating control to a background layout owner.

## Published 1.2 floor

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

`CursesPanel` and `CursesPanelTransparency` are the two 1.2 exported additions. The published contract includes retained panel content, deterministic z-order, show/hide, movement/ordering, opaque or blank-transparent composition, clipping, incremental recomposition, semantic-metadata/wide-cell coherence, live refresh/lifecycle integration, and deterministic one-way disposal.

Panel dimensions are fixed in the published 1.2 package; resize/layout belongs to 1.3.

## Release 1.3 — layout and resize primitives

Frozen stable-source contract:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Four exported types are added over 1.2:

```text
CursesRectangle
CursesInsets
CursesDockEdge
CursesLayout
```

Additive members on existing types provide screen/window/panel bounds, atomic rectangle application, and retained panel resizing.

The accepted 1.3 design provides:

- immutable terminal-cell rectangles and insets;
- deterministic containment/intersection/inset semantics;
- fixed top/bottom/left/right allocation;
- proportional row/column allocation;
- Top/Right/Bottom/Left docking;
- clipping and explicit empty geometry;
- `CursesScreen.Bounds`;
- parent-relative nested `CursesWindow.Bounds` plus atomic `SetBounds`;
- screen-relative `CursesPanel.Bounds`, retained `Resize`, and atomic `SetBounds`;
- surviving upper-left content/metadata preservation on panel resize;
- width-two footprint repair at shrink boundaries;
- explicit lifecycle-driven recomputation from `session.Screen.Bounds`;
- a dedicated interactive layout sample;
- Unicode/metadata/repeated-resize hardening;
- application/performance/allocation acceptance with no retained layout state;
- fresh NuGet-only package consumption of the full 1.3 surface.

T1310 qualified exact head `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7` in workflow #681 / `34694609178`, all seven jobs green. The API regret audit found no naming, mutability, ambiguity, ownership, or 1.4-reuse correction requiring an API break.

The unchanged implementation/API was promoted to `1.3.0-rc.1`; exact RC head `2ab949a64f63759ad9cf4e93e45a368c50ab6e49` passed workflow #689 / `34694881296` across all seven jobs. The declared dependencies were then advanced to `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0` without changing the frozen public API. Final stable-source head `b90b444556291434bddb9f56d031f09ac1aabfbf` passed workflow #719 / `34708925529` across all seven jobs. PR #27 was merged as `18255d59136922b9e246f4113dc6fdb6a9ea24a3`, and main Release workflow #17 / `34709142076` passed.

## Planned 1.4 focus and interaction mechanics

Version 1.4 is planned to add focusable regions, focus traversal, keyboard gestures/commands, mouse hit testing, interaction regions, pointer-shape requests, focus repair, resize-aware hit testing, and deterministic overlap precedence. Terminal remains the authoritative input/protocol owner.

The key 1.3-to-1.4 bridge is `CursesRectangle`: 1.4 can reuse the same immutable terminal-cell coordinate substrate for focus and hit-test regions without inventing a second geometry model or changing 1.3 layout ownership.

Version 1.4 is not being pulled backward into 1.3. No focus router, gesture registry, hit-test tree, pointer policy, widget framework, raster placement, animation system, or general constraint solver belongs in the 1.3 release.

## Cross-release rules

Compatible 1.x releases retain additive API by default, exact compiler-derived fingerprints, Terminal/TermInfo ownership boundaries, Unicode/wide-cell/metadata semantics, conservative lifecycle recovery, package-only consumer validation, Windows/Linux/macOS x64/ARM64 validation, and documentation/sample/package audits before stable promotion.

Historical release documents remain historical authorities and are not rewritten simply to reflect newer package/dependency state.

## Immediate next step

Repository-side 1.3 development is complete. Public `v1.3.0` tagging/GitHub Release/NuGet publication remain explicit release actions. The next approved development track is 1.4.0.
