# Icod.DCurses 1.2.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.2.0`  
**Stable compatibility floor:** `1.1.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Stable-source package:** `1.2.0`  
**Assembly version:** `1.0.0.0`  
**Current declared dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** panels, independent retained layers, visibility, clipping, and deterministic z-order composition  
**Status:** T1201–T1209 complete; `1.2.0-rc.1` qualified; T1210 stable-source exact-head qualification active

---

## Release objective

`Icod.DCurses 1.2.0` adds first-class overlapping retained surfaces without changing ordinary `CursesWindow` shared-view semantics and without introducing widgets.

```text
CursesWindow
    -> rectangular view into one owning logical screen
    -> overlapping ordinary windows share logical cells

CursesPanel
    -> owns an independent retained surface
    -> belongs to one destination screen
    -> participates in deterministic z-order composition
    -> may be shown, hidden, moved, reordered, or permanently disposed
```

## Architecture and compatibility

Panel composition is terminal-independent logical work above the existing retained physical renderer. `Icod.Terminal` remains the live terminal/session/input/protocol owner; `Icod.TermInfo` remains the immutable capability authority.

Accepted 1.1 floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Accepted 1.2 contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly `CursesPanel` and `CursesPanelTransparency` are added over 1.1. `AssemblyVersion` remains `1.0.0.0`.

## Frozen design

- Ordinary windows remain shared views.
- Panels own independent retained surfaces edited through `CursesWindow`.
- One destination screen owns deterministic identity-based z-order.
- Hide/show preserves stack position.
- Composition never mutates panel content.
- Opaque is the default; `BlankCellsTransparent` intentionally reveals lower content.
- Wide-cell and semantic-metadata state compose as one logical footprint.
- Incremental recomposition is damage-bounded.
- Live refresh/lifecycle ownership remains with `CursesSession`/Terminal.
- `CursesPanel : IDisposable` provides deterministic one-way removal.
- Panel dimensions remain fixed in 1.2; general layout/resize belongs to 1.3.
- A no-panel session retains an allocation-free presence check and direct refresh path.

## Qualified sequence

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1201 | `b4582beebe9519d5b5feaf5a4f4ada54adc21672` | #564 / `34510470939` | seven jobs green |
| T1202 | `cfb4973992c78273e0edcdff17065e8b8a029af0` | #571 / `34511809356` | seven jobs green |
| T1203 | `85d907f7cf00fa4e8839ed6173255218a9efa17f` | #575 / `34513600585` | seven jobs green |
| T1204 | `09293c42dfa2c46484542601b7c0538babe07684` | #584 / `34515384714` | seven jobs green |
| T1205 | `f18d3a55877115f224f9aca8ccc7cde6cfafd845` | #588 / `34517869687` | seven jobs green |
| T1206 | `8a7ce13d25d3f911b9c17dbe6ce24af0012c2431` | #590 / `34518913849` | seven jobs green |
| T1207 | `c0b7fcd48c9b97eaa924299367ffd28779dfd1d0` | #593 / `34520535815` | seven jobs green |
| T1208 | `bb00707779cf3dc6c2222455a6f942469d036881` | #596 / `34522859308` | seven jobs green |
| T1209 API/lifetime | `866497c9d5015f3a149580d67eacefaf7e121aaf` | #600 / `34524054785` | seven jobs green |
| T1209 closure | `3728bf0e576b32747dd3a628ed5d3eca768ac67f` | #603 / `34525966166` | seven jobs green |
| T1210 RC | `8c5d329fa195685c0349068ce33a100aaf9eb0a3` | #604 / `34526810086` | seven jobs green |

T1209 closure includes the focused public panel sample, package-only runtime panel validation, current docs, and API baseline. Its Linux ARM64 leg reported 554/554 tests per TFM with zero build warnings/errors.

The sole `1.2.0-rc.1` release candidate retained the same API and implementation and passed the same full matrix. Its Linux ARM64 evidence leg again reported 554/554 tests per TFM with zero warnings/errors.

## T1210 stable-source gate

The source has been promoted from the qualified RC to `1.2.0` by changing only release/package identity and current status documentation. No behavioral source, test, sample, package-smoke, dependency, AssemblyVersion, or API-fingerprint change is permitted.

The stable-source exact head must pass package candidate validation plus Windows/Linux/macOS x64/ARM64 runtime validation with the fingerprint still exactly:

```text
4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

After that gate, repository-side 1.2 development is complete. The branch must not be moved merely to record its own tested SHA.

## Explicit non-goals

Version 1.2 does not add widgets, general layout arithmetic, panel resizing, focus routing, gesture routing, raster placement, animation, alpha blending, terminal pixel layout, terminal emulation, PTY/process hosting, or private Terminal protocol output.

## Current sequence

```text
T1201–T1209  implementation, acceptance, API/package/docs regret gates  complete
T1210 RC      1.2.0-rc.1 exact-head qualification                       complete
T1210 stable  1.2.0 exact-head qualification                            active
```

Merge, main Release validation, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
