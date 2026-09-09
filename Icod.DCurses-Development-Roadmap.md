# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published stable baseline:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Current development package:** `1.1.0-alpha.4`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1103 complete; T1104 retained hyperlink renderer implemented and under exact-head validation; T1105 next

---

## Purpose

This file is the current roadmap index for `Icod.DCurses`.

The original long-form roadmap served the project from its initial 0.1 work through the 1.0 stable-contract program. It is preserved unchanged under:

- `docs/history/Icod.DCurses-Development-Roadmap-through-1.0.md`

Current development is organized by stable 1.x release documents rather than by continuously rewriting the original pre-1.0 plan.

---

## Current release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Published stable release |
| `1.1.0` | Semantic cell metadata and hyperlinks | Active — `1.1.0-alpha.4`; T1104 |
| `1.2.0` | Panels, layers, visibility, and z-order composition | Approved future release |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus, interaction regions, key gestures, hit testing, and pointer semantics | Approved future release |
| later | Raster graphics over Terminal semantic raster routing | Deferred until the required Terminal public contract exists |

The approved 1.1–1.4 release train is documented in:

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`

The detailed active 1.1 plan is:

- `Icod.DCurses-1.1.0-Development-Roadmap.md`

Current implementation records are:

- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`;
- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`;
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`;
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`.

---

## Layer ownership remains unchanged

```text
Applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
     semantic cells / windows / pads / composition
       retained refresh / interaction / layout
                         |
                    Icod.Terminal
   live session / input / lifecycle / semantic protocols
     capability evidence / protocol routing / output
                         |
                    Icod.TermInfo
             immutable capability authority
                         |
                  terminal / tty
```

The stable boundary remains:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal conversation, raw protocol framing/routing, active evidence, session-owned state, input decoding, and output serialization;
- `Icod.DCurses` owns higher-level terminal UI semantics, logical composition, cells, windows, pads, retained rendering, and application-facing interaction mechanics.

`Icod.DCurses` must not grow raw OSC/CSI/DCS/APC protocol writers merely because Terminal supports those protocol families. DCurses consumes Terminal semantic operations where the higher-level curses model adds meaning.

Terminal 1.6.0 remains the active dependency floor. Hyperlink rendering uses Terminal's typed OSC 8 operation; DCurses does not construct OSC 8 frames.

---

## Post-1.0 development principles

1. Stable 1.x changes are additive unless an explicit compatibility decision requires otherwise.
2. Every public API addition receives a machine-readable compiled API fingerprint and human-readable regret review before stable release.
3. Semantic metadata is distinct from visual rendition. `CursesStyle` remains presentation-only.
4. Terminal protocol ownership remains in `Icod.Terminal`; DCurses does not construct private OSC/CSI/DCS/APC sequences.
5. Higher-level features must preserve Unicode/wide-cell, editing, copy/overlay, pad, lifecycle, and failure-recovery invariants.
6. Large-surface memory and allocation behavior remain release concerns; convenient APIs must not casually impose large permanent per-cell costs.
7. `Icod.DCurses` does not become a one-for-one wrapper over every `TerminalSession` semantic method.
8. The core library remains a terminal UI substrate rather than a widget toolkit.
9. A future native-curses compatibility facade should be a separate package layered over explicit `CursesSession` ownership.
10. A future widget library should be a separate package so controls can evolve independently of the stable core.
11. Raster graphics should wait for Terminal's reviewed common raster/public routing contract rather than introducing Sixel- or Kitty-specific DCurses core APIs.
12. Historical release and tranche documents remain historical and are not rewritten merely to reflect newer dependencies or release state.

---

## Compatibility and version policy

The published 1.0 public contract remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The provisional additive 1.1 contract introduced by T1103 is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

T1101 ratified this additive 1.x identity policy:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0 for compatible additive 1.x releases
```

The active checkpoint is:

```text
Version         1.1.0-alpha.4
PackageVersion  1.1.0-alpha.4
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

A future breaking compatibility decision may revisit assembly identity explicitly; it must not change incidentally.

---

## Semantic metadata representation

T1102 rejected a permanent metadata reference inside every `CursesCell` and rejected a surface-relative token. The portable measurement that matters is the incremental cost: both a metadata-reference wrapper and the tested token wrapper add one eight-byte slot per logical cell on the supported 64-bit matrix.

At the established 2,048 × 256 large-pad scale, one unconditional eight-byte slot adds exactly 4 MiB even when metadata is unused.

The accepted row-sparse reference plane therefore provides:

- no semantic plane allocation for an ordinary surface with no metadata;
- per-row reference storage only for rows that contain semantic values;
- row storage release when its last semantic value disappears;
- O(1) coordinate lookup;
- deterministic row snapshot/replace support for editing algorithms;
- unchanged, standalone `CursesCell` value semantics.

The internal foundation is `CursesSparseCellPlane<T>`.

---

## T1103 public semantic contract

T1103 introduced exactly two new public semantic types:

- `CursesHyperlink`;
- `CursesCellMetadata`.

`CursesVirtualScreen` and `CursesWindow` expose coordinate metadata inspection/mutation, and `CursesWindow` exposes metadata-aware text/cell writes. Two-column text elements carry one coherent metadata value across leader and continuation coordinates. Ordinary replacement removes overwritten semantic metadata even when the visible cell value is unchanged.

The provisional current compiled API is guarded by `docs/Public-API-Fingerprint-1.1.json`. Historical 0.9/1.0 baselines remain unchanged.

T1103's documentation-complete head `d520adf79bf6a74bfbb09e1b2ecc4082a3cce960` passed workflow #474 (`34405314146`) on all six runtime architectures plus package validation.

---

## T1104 retained physical hyperlink rendering

T1104 extends retained physical knowledge to include semantic metadata. A semantic-only difference is therefore a refresh difference even when glyph and style are identical.

The renderer:

- splits changed style runs further by semantic metadata identity;
- coalesces adjacent cells with equal hyperlink metadata into one linked payload;
- delegates each linked payload to Terminal's bounded `WriteHyperlinkAsync(...)` operation;
- remains hyperlink-neutral between bounded linked runs;
- invalidates physical metadata together with physical cell knowledge after uncertain output;
- supports two-column linked elements without emitting the continuation as text;
- composes with optional synchronized output through Terminal's existing ownership model.

Until T1105/T1107 prove semantic behavior for terminal-native structural operations, line-shift, character-shift, erase, and scroll shortcuts are conservatively bypassed whenever desired or retained physical semantic metadata exists.

The current alpha.4 exact-head gate is rerunning only Windows ARM64 after an infrastructure-only `actions/setup-dotnet` CLR crash. All repository-executing jobs on the same head were green.

---

## Active 1.1 sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            implemented; exact-head gate
  -> T1105  editing/copy/overlay/pad propagation            next
  -> T1106  lifecycle/failure/cancellation hardening
  -> T1107  application/performance/allocation acceptance
  -> T1108  API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## Historical and current roadmap documents

Current authorities:

- `Icod.DCurses-Development-Roadmap.md` — this current index;
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.1.0-Development-Roadmap.md` — active detailed 1.1 implementation plan;
- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`;
- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`;
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`;
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`;
- `docs/Public-API-Fingerprint-1.1.json`.

Published 1.0 compatibility authority:

- `Icod.DCurses-1.0.0-Development-Roadmap.md`;
- `docs/Public-API-Fingerprint-1.0.json`;
- `docs/Public-API-Baseline-1.0.md`;
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`.

Historical pre-1.0 authorities remain in their existing release-specific roadmap and tranche files. The superseded original root long-form roadmap is additionally preserved verbatim under `docs/history/` for discoverability.
