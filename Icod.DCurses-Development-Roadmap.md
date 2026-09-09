# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Merged stable-source baseline:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Latest published release at 1.1 start:** `0.9.0`  
**Current development package:** `1.1.0-alpha.3`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and hyperlinks  
**Status:** T1101–T1102 complete; T1103 logical/API implementation green and documentation-complete validation active

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
| `1.0.0` | Stable core contract | Merged; publication remains separate |
| `1.1.0` | Semantic cell metadata and hyperlinks | Active — `1.1.0-alpha.3`; T1103 |
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
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`.

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

`Icod.DCurses` must not grow raw OSC/CSI/DCS/APC protocol writers merely because Terminal supports those protocol families. DCurses consumes Terminal semantic operations only where the higher-level curses model adds meaning.

Terminal 1.6.0 strengthens the CSI/parser/query foundation and internal pixel-geometry substrate without changing the public contract used by DCurses 1.1. Hyperlink protocol ownership continues through Terminal's existing typed OSC 8 operations.

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

The stable 1.0 public contract remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

T1101 ratified this additive 1.x identity policy:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0 for compatible additive 1.x releases
```

The active checkpoint is:

```text
Version         1.1.0-alpha.3
PackageVersion  1.1.0-alpha.3
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

A future breaking compatibility decision may revisit assembly identity explicitly; it must not change incidentally.

---

## T1102 representation decision

T1102 rejects a permanent metadata reference inside every `CursesCell` and rejects a surface-relative token.

The portable 64-bit measurement frozen by the gate is the **incremental cost**, not one architecture-specific private `CursesCell` size:

```text
cell + metadata-reference wrapper overhead   +8 bytes per cell
cell + int-token wrapper overhead             +8 bytes per cell
```

At the established 2,048 × 256 large-pad scale, one unconditional eight-byte slot adds exactly 4 MiB even when metadata is unused.

T1102 therefore selects a lazily allocated row-sparse reference plane:

- no semantic plane allocation for an ordinary surface with no metadata;
- top-level row references allocated only after the first semantic value;
- per-row reference storage allocated only for rows that contain semantic values;
- row storage released when its last semantic value is removed;
- O(1) coordinate lookup;
- deterministic row snapshot/replace support for editing algorithms;
- `CursesCell` remains a standalone context-free public value.

The internal foundation is `CursesSparseCellPlane<T>`.

---

## T1103 logical semantic contract

T1103 introduces the first intentional additive public API after 1.0:

```text
CursesHyperlink
CursesCellMetadata
```

along with metadata-aware logical write/inspection/mutation operations on `CursesWindow` and `CursesVirtualScreen`.

The provisional compiled 1.1 development contract is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

`docs/Public-API-Fingerprint-1.1.json` is a development baseline and may still change deliberately before T1108. The stable 0.9/1.0 fingerprint files remain untouched historical/stable authorities.

T1103 remains logical only: physical OSC 8 rendering begins in T1104.

---

## Active 1.1 sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      implementation green; docs gate active
  -> T1104  retained physical hyperlink renderer            next
  -> T1105  editing/copy/overlay/pad propagation
  -> T1106  lifecycle/failure/cancellation hardening
  -> T1107  application/performance/allocation acceptance
  -> T1108  API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

The code/API checkpoint `b2c1e7c18c1de929e705f119794d7eb5f6d6073b` passed workflow #469 (`34403872794`) on all six runtime architectures plus package validation. One documentation-synchronized alpha.3 head must pass the same gate before T1103 is complete.

---

## Historical and current roadmap documents

Current authorities:

- `Icod.DCurses-Development-Roadmap.md` — this current index;
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.1.0-Development-Roadmap.md` — active detailed 1.1 implementation plan;
- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`;
- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`;
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`;
- `docs/Public-API-Fingerprint-1.1.json` — provisional 1.1 development fingerprint.

Stable 1.0 closure authority:

- `Icod.DCurses-1.0.0-Development-Roadmap.md`;
- `docs/Public-API-Fingerprint-1.0.json`;
- `docs/Public-API-Baseline-1.0.md`;
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`.

Historical pre-1.0 authorities remain in their existing release-specific roadmap and tranche files. The superseded original root long-form roadmap is additionally preserved verbatim under `docs/history/` for discoverability.
