# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Accepted compatibility floor:** `1.1.0`  
**Active source package:** `1.2.0`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current declared runtime dependencies:** `Icod.Terminal 1.9.0`; `Icod.TermInfo 1.10.0`  
**Planning status:** 1.2 source complete; dependency refresh qualification pending NuGet indexing

---

## Release sequence

```text
1.1.0  semantic cell metadata + hyperlinks
1.2.0  panels/layers + z-order composition
1.3.0  layout + resize primitives
1.4.0  focus/interaction/key gestures/hit testing/pointer semantics
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes geometry manageable; 1.4 routes semantic input to logical regions.

## Ownership boundary

The current 1.2 source declares `Icod.Terminal 1.9.0` and `Icod.TermInfo 1.10.0`. DCurses consumes Terminal's live-session/input/lifecycle/semantic-output contracts rather than terminal-family protocol details and does not install private protocol writers or a second input owner.

## 1.1 compatibility floor

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

`CursesHyperlink` and `CursesCellMetadata` remain the two 1.1 additions. The detailed 1.1 roadmap and T1101–T1109 documents remain historical compatibility authorities.

## 1.2 retained panels

`CursesPanel` owns an independent retained surface edited through the established `CursesWindow` model. `CursesScreen` owns panel identity and deterministic bottom-to-top order.

The accepted contract provides show/hide, movement and relative ordering, opaque/default and blank-transparent composition, clipping, incremental damage-bounded recomposition, Unicode/wide-cell/metadata coherence, live `CursesSession.RefreshAsync()` integration, destination resize and suspend/resume recovery, deterministic `IDisposable` removal, and an allocation-free no-panel refresh presence check.

Accepted 1.2 contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1: `CursesPanel` and `CursesPanelTransparency`.

Late qualification:

- T1208 application/resource acceptance: `bb00707779cf3dc6c2222455a6f942469d036881`, workflow #596 / `34522859308`.
- T1209 API/lifetime: `866497c9d5015f3a149580d67eacefaf7e121aaf`, workflow #600 / `34524054785`.
- T1209 docs/sample/package: `3728bf0e576b32747dd3a628ed5d3eca768ac67f`, workflow #603 / `34525966166`.
- T1210 `1.2.0-rc.1`: `8c5d329fa195685c0349068ce33a100aaf9eb0a3`, workflow #604 / `34526810086`.
- T1210 stable-source `1.2.0`: `a065ef389b4e5224bd9defeb3f4de0e688e2e08a`, workflow #605 / `34527425232`.
- Final sample/test hardening: `93ef832042dac743008a065acb683d3029710f1c`, workflow #607 / `34528856997`.

All of those checkpoints passed the seven-job matrix. The final hardening evidence leg reported 560/560 tests per TFM with zero build warnings/errors.

The only post-qualification source change is the declared `Icod.Terminal` dependency update from `1.8.1` to `1.9.0` plus corresponding current-status documentation. A temporary restore failure is expected until NuGet indexes `Icod.Terminal 1.9.0`; once indexed, this dependency-refresh head must receive its own exact-head seven-job qualification.

Panel dimensions remain fixed in 1.2. General layout/resize belongs to 1.3. Widgets, focus routing, and raster placement remain non-goals for 1.2.

## 1.3 layout and resize primitives

Version 1.3 is planned to remove routine terminal-geometry arithmetic through deterministic rectangles/bounds, insets, splits, allocation rules, docking, clipping/empty-layout behavior, and resize recomputation. It is not intended to become CSS/flexbox/a general constraint solver.

## 1.4 focus and interaction mechanics

Version 1.4 is planned to add focusable regions, focus traversal, keyboard gestures/commands, mouse hit testing, interaction regions, pointer-shape requests, focus repair, resize-aware hit testing, and deterministic overlap precedence. Terminal remains the authoritative input/protocol owner.

## Cross-release rules

Compatible 1.x releases retain additive API by default, exact compiler-derived fingerprints, Terminal/TermInfo ownership boundaries, Unicode/wide-cell/metadata semantics, conservative lifecycle recovery, package-only consumer validation, Windows/Linux/macOS x64/ARM64 validation, and documentation/sample/package audits before stable promotion.

## Immediate next step

Wait only for NuGet indexing of `Icod.Terminal 1.9.0`, then require the current PR head to pass the seven-job matrix. No code fallback, version pin, or hard-coded compatibility check should be added merely to work around package-index propagation.
