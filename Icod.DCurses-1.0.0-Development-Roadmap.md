# Icod.DCurses 1.0.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Stable baseline:** `0.4.0`  
**Development destination:** `1.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** Approved development plan; `0.5.0` active

---

## 1. Purpose

This document defines the development sequence that carries `Icod.DCurses` to a stable `1.0.0` managed curses contract.

The implementation and its substrate libraries have advanced enough that the original sequence is no longer the best development order. Several capabilities once scheduled for later releases were already present in the early stable line, including retained logical and physical screen state, damage-driven refresh, windows/subwindows, wrapping/scrolling, indexed/RGB color, semantic mouse/focus/paste input, reversible rich-input leases, and Terminal-owned resize/lifecycle integration.

The current roadmap therefore orders the remaining contract gaps by dependency: Terminal semantic parity, Unicode/cell correctness, richer editing/composition, pads, presentation, refresh optimization, production hardening, and final public-contract freeze.

Completed milestones are retained below because later releases depend on their frozen contracts.

---

## 2. Architectural Boundary

The dependency and responsibility direction remains:

```text
Applications
    top / slabtop / watch / editors / pagers / TUIs
                         |
                    Icod.DCurses
       curses events / cells / windows / pads
      text-cell policy / refresh / presentation
                         |
                    Icod.Terminal
      endpoint / mode / input / lifecycle / leases
                         |
                    Icod.TermInfo
               capability authority
                         |
                 terminal / tty
```

`Icod.DCurses` SHALL NOT regain responsibilities that belong to `Icod.Terminal` or `Icod.TermInfo`.

In particular, DCurses SHALL NOT add:

- a second raw terminal input loop;
- a private terminal-mode implementation;
- a private terminal query/response router;
- a second terminal capability database;
- application-specific ProcPs policy;
- terminal emulation or PTY ownership.

DCurses MAY expose curses-shaped abstractions over stable Terminal mechanisms when doing so creates useful TUI semantics and avoids forcing ordinary consumers to drop down into the Terminal layer.

---

## 3. Release Train

| Release | Theme | Principal outcome |
|---|---|---|
| `0.2.0` | Terminal 1.0 input semantic parity | DCurses consumes the complete stable Terminal key/event/protocol contract without losing information or throwing on valid Terminal input |
| `0.3.0` | Unicode and terminal-cell contract | Grapheme-aware, column-safe text semantics are frozen before richer editing APIs are built |
| `0.4.0` | Window editing and composition | Mature geometry, editing, line drawing, copy/overlay, and region operations suitable for general TUIs |
| `0.5.0` | Pads and large surfaces | Large off-screen cell surfaces, viewports, derived views, independent viewport damage observation, and efficient panning |
| `0.6.0` | Rendition, drawing, and presentation | Complete managed presentation vocabulary with capability-aware degradation |
| `0.7.0` | Refresh and output optimization | Synchronized output, better terminal-operation selection, scrolling/edit optimizations, and measurable output efficiency |
| `0.8.0` | Production hardening | Explicit concurrency model, lifecycle/race recovery, stress testing, performance, and failure resilience |
| `0.9.0` | Contract freeze / release candidate | Public API regret review, compatibility fingerprint, documentation and package freeze |
| `1.0.0` | Stable release closure | Stable managed contract with no surprise feature family introduced during release closure |

The version numbers describe development checkpoints, not a requirement to recreate historical native-curses versioning or function families one-for-one.

---

# 4. Version 0.2.0 — Terminal 1.0 Input Semantic Parity

`0.2.0` established complete stable Terminal semantic keyboard/input parity through the curses facade, including the expanded key vocabulary, press/repeat/release phases, modern modifiers, shifted/base-layout characters, associated text, `Unrecognized`, and curses-shaped keyboard-reporting protocol acquisition.

The detailed implementation plan is maintained in `Icod.DCurses-0.2.0-Development-Roadmap.md`.

---

# 5. Version 0.3.0 — Unicode and Terminal-Cell Contract

`0.3.0` froze the terminal-cell foundation required by later editing and viewport APIs:

- normalized malformed UTF-16 before segmentation;
- complete text-element width decisions;
- deterministic zero-, one-, and two-column semantics;
- continuation-cell invariants;
- combining, variation-selector, emoji-ZWJ, flag, and keycap behavior;
- safe wide-cell overwrite and clipping;
- explicit East Asian Ambiguous-width policy;
- Unicode 17.0.0 width/emoji data;
- public column measurement, truncation, and slicing helpers.

The goal remains terminal-cell correctness, not general-purpose script shaping. Bidirectional layout, Arabic shaping, Indic shaping, and general font shaping remain outside the core 1.0 contract.

---

# 6. Version 0.4.0 — Window Editing and Composition

`0.4.0` matured the window/view model into a general TUI editing surface with:

- non-standard window repositioning and explicit nested geometry;
- public window-local cell inspection;
- region fill and erase completion;
- insert/delete cells and lines;
- deterministic destructive copy and transparent overlay;
- horizontal/vertical geometric drawing and borders;
- range-oriented touch/query semantics;
- Unicode-safe wide-cell repair across every editing boundary.

The managed API uses descriptive managed names rather than reproducing native `w*` function naming mechanically.

---

# 7. Version 0.5.0 — Pads and Large Surfaces

`0.5.0` is the active development release. Its detailed tranche plan is maintained in `Icod.DCurses-0.5.0-Development-Roadmap.md`.

The release introduces large in-memory off-screen cell surfaces which reuse the 0.3/0.4 contracts rather than duplicating them. The accepted design includes:

- `CursesPad` as an off-screen logical surface;
- ordinary `CursesWindow` editing through `ContentWindow`;
- direct rectangular pad-to-window presentation;
- independent `CursesPadViewport` projection state;
- vertical and horizontal panning;
- ordinary `CreateSubwindow(...)` as the shared derived-pad-view model;
- independent per-viewport visible pad change observation;
- explicit touch/invalidation propagation without a global pad-clean acknowledgement;
- strict destination-geometry revalidation after resize;
- wide-cell-safe source/destination boundary handling;
- large-surface and repeated-panning acceptance.

Pads do not own terminal sessions, terminal modes, terminal input, physical-screen state, or a second refresh engine.

---

# 8. Version 0.6.0 — Rendition, Drawing, and Presentation

The stable managed color model already supports terminal-default, indexed, and RGB requests. `0.6.0` SHALL complete the presentation contract around it.

Candidate scope:

- capability-aware indexed-color range handling;
- direct RGB/truecolor degradation policy;
- default foreground/background restoration;
- bold/intensity interaction;
- dim;
- italic where available;
- underline;
- reverse;
- standout;
- blink where available;
- conceal/invisible where available;
- strikeout where available;
- semantic line-drawing vocabulary;
- corners, tees, crossings, horizontal and vertical line primitives;
- Unicode or terminal alternate-character-set fallback policy;
- cursor presentation semantics beyond simple visibility where justified;
- a small read-only curses presentation-capabilities view if ordinary TUI decisions otherwise require direct TermInfo inspection.

A historical color-pair facade MAY be added for compatibility, but color pairs SHALL NOT replace the semantic foreground/background model as the primary API.

---

# 9. Version 0.7.0 — Refresh and Output Optimization

The current refresh design already separates desired logical state from retained physical-screen knowledge. `0.7.0` SHALL optimize that model without weakening its correctness.

Required investigation includes:

- integrating Terminal synchronized-output framing around refresh batches when available;
- measuring emitted bytes as well as elapsed time and allocation volume;
- relative cursor motion versus absolute addressing where capability/cost data makes the choice worthwhile;
- insert/delete-character optimization;
- insert/delete-line optimization;
- scroll-region optimization;
- clear-to-end-of-screen and whole-screen erase optimization;
- minimizing unnecessary rendition resets;
- preserving correct state after partial writes or failed refreshes;
- large-screen and high-frequency refresh benchmarks.

Correctness SHALL remain more important than finding a globally minimal escape sequence stream.

The primary managed batching boundary remains `RefreshAsync()` unless concrete consumer evidence proves a separate native-style `noutrefresh`/`doupdate` contract is beneficial.

---

# 10. Version 0.8.0 — Production Hardening

`0.8.0` SHALL turn the feature-complete pre-1.0 library into a production-grade runtime component.

The release SHALL explicitly freeze the concurrency model. The default design preference is:

- logical screen/window/pad mutation is single-writer unless documented otherwise;
- session input/output/refresh ownership is internally serialized where needed;
- the library does not add pervasive locks to every cell mutation merely to claim transparent thread safety.

Hardening coverage SHALL include:

- resize storms;
- input concurrent with refresh;
- cancellation during reads and refresh;
- disposal during pending waits;
- repeated session entry/exit;
- rich-input leases during suspend/resume;
- partial writes and output failure;
- disconnect and end-of-input;
- lifecycle registration failure;
- large pads and large screens;
- high-frequency refresh;
- allocation pressure;
- deterministic restoration after exceptions;
- x64/ARM64 validation on the supported Windows/Linux/macOS matrix.

---

# 11. Version 0.9.0 — Contract Freeze and Release Candidate

No major feature family SHOULD enter after this release begins.

The `0.9.0` tranche SHALL:

- review every public type and member;
- remove accidental public surface;
- freeze type/member naming;
- freeze enum numeric values;
- freeze coordinate/dimension semantics;
- freeze session and ownership semantics;
- freeze window and pad lifetime semantics;
- freeze cell/text/Unicode semantics;
- freeze input event semantics;
- freeze exception and cancellation behavior;
- decide finally which approved Terminal/TermInfo types remain in the public 1.x contract;
- establish a machine-readable public API fingerprint/baseline;
- validate nullable annotations;
- validate XML documentation;
- validate package metadata and package-only consumers;
- expand conceptual documentation and migration guidance;
- run editor-like, pager-like, Unicode-heavy, rich-input, lifecycle, and high-frequency refresh acceptance workloads.

---

# 12. Version 1.0.0 — Stable Closure

`1.0.0` SHALL be release closure, not another feature tranche.

Before publication:

- the complete `0.9` public API baseline SHALL be intentionally accepted;
- all supported target frameworks SHALL build/test/package cleanly;
- the full Windows/Linux/macOS runtime matrix SHALL pass;
- package-only consumers SHALL pass for representative TUI workloads;
- `top`, `slabtop`, and `watch` SHALL remain valid downstream consumers;
- Unicode/cell behavior SHALL be documented and tested;
- terminal restoration SHALL be demonstrated for normal exit, exceptions, cancellation, resize, and supported suspend/resume;
- no known refresh correctness defect SHALL remain;
- package version, assembly version, and compatibility policy SHALL be frozen;
- README, samples, conceptual docs, and XML docs SHALL describe the supported stable contract;
- NuGet.org and GitHub Packages publication SHALL use the validated tag artifact.

---

## 13. Explicit 1.0 Non-Goals

The following are not required for `Icod.DCurses 1.0.0`:

- native `ncurses` ABI compatibility;
- exhaustive source-level C curses compatibility;
- `printw`/`scanw`-style formatted-I/O compatibility;
- forms;
- menus;
- panels;
- widget/toolkit frameworks;
- declarative UI;
- terminal emulation;
- pseudo-terminal creation or management;
- SSH transport;
- graphics protocols;
- arbitrary child ANSI interpretation;
- bidirectional or complex-script text shaping;
- wrappers for every feature exposed by `Icod.Terminal`.

Focused compatibility or widget packages MAY be developed after the stable core contract exists.

---

## 14. Cross-Cutting Engineering Rules

Throughout the 1.0 train:

1. `<Version>` and `<PackageVersion>` SHALL remain synchronized with the active development package.
2. `Debug` remains the local-development configuration, pull requests use `Staging`, and `main`/tags use `Release`.
3. `net8.0`, `net9.0`, and `net10.0` remain first-class targets unless a concrete support/security constraint requires reconsideration.
4. Public/protected/internal methods SHALL validate applicable parameters at entry.
5. Braces SHALL be used for every `if`/`else` body.
6. Terminal capability behavior SHALL remain TermInfo-driven when a capability models the operation.
7. Tests SHALL remain non-interactive unless explicitly categorized as manual acceptance.
8. Tests SHALL not write unsolicited standard output/error.
9. Public API additions SHALL be deliberate contract decisions.
10. The dependency-boundary allow-list SHALL prevent accidental new Terminal/TermInfo public leakage.
11. Every release SHALL include package-only validation from the generated artifact.
12. Historical milestone documents SHALL remain historical rather than being rewritten to describe current versions.

---

## 15. Immediate Sequence

```text
0.2.0 Terminal 1.0 input semantic parity        complete
  -> 0.3.0 Unicode / terminal-cell contract     complete
  -> 0.4.0 window editing and composition       complete
  -> 0.5.0 pads and large surfaces              active
  -> 0.6.0 rendition / drawing / presentation
  -> 0.7.0 refresh and output optimization
  -> 0.8.0 production hardening
  -> 0.9.0 contract freeze / RC
  -> 1.0.0 stable closure
```
