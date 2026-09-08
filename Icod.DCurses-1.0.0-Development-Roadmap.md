# Icod.DCurses 1.0.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Stable baseline:** `0.1.1`  
**Development destination:** `1.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** Approved development plan

---

## 1. Purpose

This document defines the post-`0.1.1` development sequence that carries
`Icod.DCurses` to a stable `1.0.0` managed curses contract.

The earlier roadmap was intentionally broad and staged feature families across
`0.2.0` through `0.9.0`. The implementation and its substrate libraries have
advanced enough that the old sequence is no longer the best development order.
Several capabilities once scheduled for later releases are already present in
the stable `0.1` line, including:

- retained logical and physical screen state;
- damage-driven refresh;
- root windows and subwindows;
- horizontal wrapping and vertical scrolling;
- indexed and RGB color;
- semantic mouse input;
- focus input;
- bracketed paste;
- reversible rich-input protocol leases;
- resize and lifecycle integration through `Icod.Terminal`.

The new roadmap therefore focuses on the remaining contract gaps and orders them
by dependency: Terminal semantic parity first, then Unicode/cell correctness,
then richer editing/composition, pads, presentation, refresh optimization,
production hardening, and finally public-contract freeze.

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

`Icod.DCurses` SHALL NOT regain responsibilities that belong to
`Icod.Terminal` or `Icod.TermInfo`.

In particular, DCurses SHALL NOT add:

- a second raw terminal input loop;
- a private terminal-mode implementation;
- a private terminal query/response router;
- a second terminal capability database;
- application-specific ProcPs policy;
- terminal emulation or PTY ownership.

DCurses MAY expose curses-shaped abstractions over stable Terminal mechanisms
when doing so creates useful TUI semantics and avoids forcing ordinary consumers
to drop down into the Terminal layer.

---

## 3. Release Train

| Release | Theme | Principal outcome |
|---|---|---|
| `0.2.0` | Terminal 1.0 input semantic parity | DCurses consumes the complete stable Terminal key/event/protocol contract without losing information or throwing on valid Terminal input |
| `0.3.0` | Unicode and terminal-cell contract | Grapheme-aware, column-safe text semantics are frozen before richer editing APIs are built |
| `0.4.0` | Window editing and composition | Mature geometry, editing, line drawing, copy/overlay, and region operations suitable for general TUIs |
| `0.5.0` | Pads and large surfaces | Large off-screen cell surfaces, viewports, subpads, and efficient panning |
| `0.6.0` | Rendition, drawing, and presentation | Complete managed presentation vocabulary with capability-aware degradation |
| `0.7.0` | Refresh and output optimization | Synchronized output, better terminal-operation selection, scrolling/edit optimizations, and measurable output efficiency |
| `0.8.0` | Production hardening | Explicit concurrency model, lifecycle/race recovery, stress testing, performance, and failure resilience |
| `0.9.0` | Contract freeze / release candidate | Public API regret review, compatibility fingerprint, documentation and package freeze |
| `1.0.0` | Stable release closure | Stable managed contract with no surprise feature family introduced during release closure |

The version numbers describe development checkpoints, not a requirement to
recreate historical native-curses versioning or function families one-for-one.

---

# 4. Version 0.2.0 — Terminal 1.0 Input Semantic Parity

## 4.1 Objective

`Icod.Terminal 1.0.0` has a broader stable semantic keyboard contract than the
current DCurses adapter. DCurses `0.2.0` SHALL consume that contract completely
through the curses event facade.

The release SHALL cover:

- the complete stable Terminal semantic key vocabulary;
- press, repeat, and release key phases;
- Shift, Control, Alt, Super, Hyper, Meta, CapsLock, and NumLock modifier state;
- shifted-layout character identity;
- base-layout character identity;
- associated text supplied by modern keyboard protocols;
- stable handling of Terminal's `Unrecognized` semantic key;
- curses-shaped keyboard reporting protocol acquisition;
- exhaustive conversion tests so a future Terminal vocabulary change cannot
  silently become an application-time exception.

The detailed implementation plan is maintained in
`Icod.DCurses-0.2.0-Development-Roadmap.md`.

---

# 5. Version 0.3.0 — Unicode and Terminal-Cell Contract

## 5.1 Why Unicode moves early

Unicode correctness is foundational to every later editing and viewport API.
The library must know what one drawable text element means before operations
such as insertion, deletion, clipping, copying, and pad viewporting can be
frozen.

The `0.3.0` contract SHALL establish:

- extended grapheme-cluster iteration appropriate for terminal cells;
- deterministic zero-, one-, and two-column measurement;
- continuation-cell invariants;
- combining-sequence behavior;
- variation-selector behavior;
- emoji ZWJ behavior;
- regional-indicator flag behavior;
- keycap-sequence behavior;
- safe overwrite of existing wide cells;
- safe clipping without emitting half of a wide element;
- deterministic malformed-Unicode behavior;
- an explicit East Asian Ambiguous-width policy;
- centrally owned/versioned Unicode width data or algorithm;
- column-oriented measurement and slicing helpers such as measurement,
  truncation, and safe column slicing.

The goal is terminal-cell correctness, not general-purpose script shaping.
Bidirectional layout, Arabic shaping, Indic shaping, and general font shaping
remain outside the core 1.0 contract.

---

# 6. Version 0.4.0 — Window Editing and Composition

The existing window model already provides root windows, subwindows, movement of
the logical cursor, resizing, wrapping, clear/erase, scrolling, touch, and
invalidation. `0.4.0` SHALL mature this into a general TUI editing surface.

Candidate required families:

- repositioning non-standard windows;
- rigorously defined parent/subwindow geometry and lifetime semantics;
- public window-local cell inspection;
- region fill operations;
- clear-to-beginning-of-line where useful;
- insert/delete character or cell ranges;
- insert/delete line ranges;
- horizontal and vertical line drawing;
- borders and boxes;
- copy-region operations;
- destructive overwrite operations;
- transparent overlay operations;
- range touch/untouch and dirty-query operations;
- predictable cursor behavior after editing operations;
- Unicode-safe behavior for every editing primitive.

The managed API SHALL use descriptive managed names rather than reproduce native
`w*` function naming mechanically.

---

# 7. Version 0.5.0 — Pads and Large Surfaces

`0.5.0` SHALL add cell surfaces larger than the physical terminal.

The release SHOULD introduce a managed pad abstraction with:

- arbitrary positive dimensions within defined resource limits;
- the same cell/style/text semantics as windows;
- pad-local cursor state;
- subpads or derived views where they are useful and unambiguous;
- rectangular viewport presentation into a screen/window;
- vertical and horizontal panning;
- clipping at both pad and destination boundaries;
- efficient refresh of only the visible viewport;
- damage propagation across viewport changes;
- explicit behavior when the physical terminal resizes.

Editors, pagers, log viewers, inspectors, tables, and file managers SHOULD be
possible without application-private viewport infrastructure.

---

# 8. Version 0.6.0 — Rendition, Drawing, and Presentation

The stable managed color model already supports terminal-default, indexed, and
RGB requests. `0.6.0` SHALL complete the presentation contract around it.

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
- a small read-only curses presentation-capabilities view if ordinary TUI
  decisions otherwise require direct TermInfo inspection.

A historical color-pair facade MAY be added for compatibility, but color pairs
SHALL NOT replace the semantic foreground/background model as the primary API.

---

# 9. Version 0.7.0 — Refresh and Output Optimization

The current refresh design already separates desired logical state from retained
physical-screen knowledge. `0.7.0` SHALL optimize that model without weakening
its correctness.

Required investigation includes:

- integrating Terminal synchronized-output framing around refresh batches when
  available;
- measuring emitted bytes as well as elapsed time and allocation volume;
- relative cursor motion versus absolute addressing where capability/cost data
  makes the choice worthwhile;
- insert/delete-character optimization;
- insert/delete-line optimization;
- scroll-region optimization;
- clear-to-end-of-screen and whole-screen erase optimization;
- minimizing unnecessary rendition resets;
- preserving correct state after partial writes or failed refreshes;
- large-screen and high-frequency refresh benchmarks.

Correctness SHALL remain more important than finding a globally minimal escape
sequence stream.

The primary managed batching boundary remains `RefreshAsync()` unless concrete
consumer evidence proves a separate native-style `noutrefresh`/`doupdate`
contract is beneficial.

---

# 10. Version 0.8.0 — Production Hardening

`0.8.0` SHALL turn the feature-complete pre-1.0 library into a production-grade
runtime component.

The release SHALL explicitly freeze the concurrency model. The default design
preference is:

- logical screen/window/pad mutation is single-writer unless documented
  otherwise;
- session input/output/refresh ownership is internally serialized where needed;
- the library does not add pervasive locks to every cell mutation merely to
  claim transparent thread safety.

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
- decide finally which approved Terminal/TermInfo types remain in the public
  1.x contract;
- establish a machine-readable public API fingerprint/baseline;
- validate nullable annotations;
- validate XML documentation;
- validate package metadata and package-only consumers;
- expand conceptual documentation and migration guidance;
- run editor-like, pager-like, Unicode-heavy, rich-input, lifecycle, and
  high-frequency refresh acceptance workloads.

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
- terminal restoration SHALL be demonstrated for normal exit, exceptions,
  cancellation, resize, and supported suspend/resume;
- no known refresh correctness defect SHALL remain;
- package version, assembly version, and compatibility policy SHALL be frozen;
- README, samples, conceptual docs, and XML docs SHALL describe the supported
  stable contract;
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

Focused compatibility or widget packages MAY be developed after the stable core
contract exists.

---

## 14. Cross-Cutting Engineering Rules

Throughout the 1.0 train:

1. `<Version>` and `<PackageVersion>` SHALL remain synchronized with the active
   development package.
2. `Debug` remains the local-development configuration, pull requests use
   `Staging`, and `main`/tags use `Release`.
3. `net8.0`, `net9.0`, and `net10.0` remain first-class targets unless a
   concrete support/security constraint requires reconsideration.
4. Public/protected/internal methods SHALL validate applicable parameters at
   entry.
5. Braces SHALL be used for every `if`/`else` body.
6. Terminal capability behavior SHALL remain TermInfo-driven when a capability
   models the operation.
7. Tests SHALL remain non-interactive unless explicitly categorized as manual
   acceptance.
8. Tests SHALL not write unsolicited standard output/error.
9. Public API additions SHALL be deliberate contract decisions.
10. The dependency-boundary allow-list SHALL prevent accidental new
    Terminal/TermInfo public leakage.
11. Every release SHALL include package-only validation from the generated
    artifact.
12. Historical milestone documents SHALL remain historical rather than being
    rewritten to describe current versions.

---

## 15. Immediate Sequence

```text
0.1.1 stable baseline
  -> 0.2.0 Terminal 1.0 input semantic parity
  -> 0.3.0 Unicode / terminal-cell contract
  -> 0.4.0 window editing and composition
  -> 0.5.0 pads and large surfaces
  -> 0.6.0 rendition / drawing / presentation
  -> 0.7.0 refresh and output optimization
  -> 0.8.0 production hardening
  -> 0.9.0 contract freeze / RC
  -> 1.0.0 stable closure
```

The active development release is `0.2.0`.
