# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published stable baseline:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Current development package:** `1.1.0-alpha.5`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1104 complete; T1105 semantic propagation implemented at alpha.5 and under documentation-complete validation; T1106 next

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
| `1.1.0` | Semantic cell metadata and hyperlinks | Active — `1.1.0-alpha.5`; T1105 |
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
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`;
- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`.

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

## Compatibility and version policy

The published 1.0 public contract remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The provisional additive 1.1 contract introduced by T1103 remains:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

T1101 ratified:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0 for compatible additive 1.x releases
```

The active checkpoint is:

```text
Version         1.1.0-alpha.5
PackageVersion  1.1.0-alpha.5
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

---

## T1102 representation decision

Semantic metadata uses a lazily allocated row-sparse reference plane rather than a permanent field inside every `CursesCell`.

The portable measured cost that drove the decision is an additional eight-byte slot per logical cell for both tested inline metadata/token candidates. At the established 2,048 × 256 pad scale, that would add exactly 4 MiB even when semantics are unused.

The accepted model preserves standalone `CursesCell` value semantics, O(1) coordinate lookup, sparse row allocation, and deterministic row snapshot/replace operations.

---

## T1103 public semantic contract

T1103 introduced exactly two new public semantic types:

- `CursesHyperlink`;
- `CursesCellMetadata`.

Window/virtual-screen APIs provide metadata inspection/mutation and metadata-aware text/cell writes. Wide text elements use one coherent metadata value across their leader/continuation footprint, and ordinary replacement removes overwritten semantics.

T1103 documentation-complete head `d520adf79bf6a74bfbb09e1b2ecc4082a3cce960` passed workflow #474 (`34405314146`) across all seven jobs.

---

## T1104 retained physical hyperlink rendering

T1104 extends retained physical state with semantic metadata and delegates coalesced linked payloads to Terminal's bounded `WriteHyperlinkAsync(...)` operation.

It proves semantic-only repaint, no-op retained refresh, link removal, wide-cell linked output, invalidation, synchronized-output composition, and Terminal-owned canonical OSC 8 framing without raw OSC 8 in DCurses.

Documentation-complete alpha.4 head:

```text
242deb76ede3e04a89a90591d8c27216f4efd980
```

Workflow #485 (`34411626180`) passed the complete seven-job PR matrix.

**Status:** complete.

---

## T1105 structural semantic propagation

T1105 introduces the internal transient pair:

```text
CursesLogicalCellState
    CursesCell
    CursesCellMetadata?
```

The pair is used only while editing/composing retained content; it does not change the public `CursesCell` representation or provisional API fingerprint.

Semantic metadata now moves with surviving content through:

- insert/delete cells;
- insert/delete lines;
- upward/downward scrolling;
- destructive rectangle copy;
- transparent overlay;
- pad presentation;
- multiple independent pad viewports;
- preserved screen resize;
- wide-cell normalization/clipping repair.

Composition rules are explicit:

- `CopyRectangleTo` transfers source metadata, including metadata attached to a copied blank;
- `OverlayRectangleTo` treats source blanks as fully transparent and leaves destination metadata untouched;
- overlapping copy snapshots cells and metadata before mutation;
- invalid or clipped wide footprints lose semantic metadata together with discarded content.

The implementation-plus-independent-viewport checkpoint:

```text
2c5459984ac1d5a1616ed7fea09ea69429ca872b
```

passed workflow #492 (`34412687220`) across all seven jobs before alpha.5 documentation promotion.

The documentation-complete alpha.5 head must pass one further exact seven-job gate before T1105 is called complete.

---

## Optimization boundary

Terminal-native line-shift, character-shift, erase, and scrolling shortcuts remain conservatively bypassed while desired or retained physical semantic metadata exists.

T1105 proves **logical** semantic propagation. It does not assume that a terminal-native structural operation preserves OSC 8 association in a way equivalent to retained curses intent. T1107 may re-enable a subset only with explicit semantic equivalence and cost evidence.

---

## Active 1.1 sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad propagation            alpha.5 exact-head gate
  -> T1106  lifecycle/failure/cancellation hardening        next
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
- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`;
- `docs/Public-API-Fingerprint-1.1.json`.

Published 1.0 compatibility authority:

- `Icod.DCurses-1.0.0-Development-Roadmap.md`;
- `docs/Public-API-Fingerprint-1.0.json`;
- `docs/Public-API-Baseline-1.0.md`;
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`.

Historical pre-1.0 authorities remain historical and are not rewritten merely to reflect later dependencies or release state.
