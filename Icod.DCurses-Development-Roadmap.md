# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published stable baseline:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Current development package:** `1.1.0-alpha.7`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1107 complete at implementation level; alpha.7 documentation-complete gate active; T1108 next

---

## Purpose

This file is the current roadmap index for `Icod.DCurses`.

The original long-form roadmap is preserved unchanged under:

- `docs/history/Icod.DCurses-Development-Roadmap-through-1.0.md`

Current development is organized by stable 1.x release documents rather than continuously rewriting the pre-1.0 plan.

---

## Current release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Published stable release |
| `1.1.0` | Semantic cell metadata and hyperlinks | Active — `1.1.0-alpha.7`; T1107 documentation gate / T1108 next |
| `1.2.0` | Panels, layers, visibility, and z-order composition | Approved future release |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus, interaction regions, key gestures, hit testing, and pointer semantics | Approved future release |
| later | Raster graphics over Terminal semantic raster routing | Deferred until the required Terminal public contract exists |

Authorities:

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.1.0-Development-Roadmap.md` — active detailed 1.1 plan.

---

## Layer ownership

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
- `Icod.Terminal` owns the live terminal conversation, protocol framing/routing, session-owned state, lifecycle, input decoding, and output serialization;
- `Icod.DCurses` owns higher-level terminal UI semantics, logical composition, cells, windows, pads, retained rendering, and application-facing interaction mechanics.

DCurses does not grow raw OSC/CSI/DCS/APC writers merely because Terminal supports those protocol families.

---

## Compatibility and version policy

Published 1.0 compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Provisional 1.1 contract introduced by T1103:

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

Active identity:

```text
Version         1.1.0-alpha.7
PackageVersion  1.1.0-alpha.7
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

---

## Qualified 1.1 checkpoints

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1102 | `60fa5e1a0b17e91f7c0e78ee397ff797645471d7` | #456 / `34400518088` | seven jobs green |
| T1103 | `d520adf79bf6a74bfbb09e1b2ecc4082a3cce960` | #474 / `34405314146` | seven jobs green |
| T1104 | `242deb76ede3e04a89a90591d8c27216f4efd980` | #485 / `34411626180` | seven jobs green |
| T1105 | `f35eee81a9660271ba8eb4a930738b1997207cb7` | #497 / `34413294079` | seven jobs green |
| T1106 | `59232c1eb9da1bd97f8c3ea950c757159f82a15f` | #507 / `34416436456` | seven jobs green |
| T1107 implementation/acceptance | `bc226acf20fc5a8ab88f81d0d2053663d0120288` | #516 / `34419328443` | seven jobs green |

The alpha.7 documentation/version synchronization requires its own exact-head seven-job gate before T1107 documentation closure.

---

## T1102 — sparse semantic representation

Semantic metadata uses a lazily allocated row-sparse reference plane rather than a permanent field inside every `CursesCell`.

Both tested inline metadata/token candidates impose an additional eight-byte slot per logical cell on the supported 64-bit matrix. At 2,048 × 256 cells that would add exactly 4 MiB even when semantics are unused.

The sparse representation retains only a top-level row-reference table until semantic rows are populated; individual row arrays are allocated on demand and released when empty.

---

## T1103 — public semantic contract

T1103 introduced exactly two public semantic types:

- `CursesHyperlink`;
- `CursesCellMetadata`.

Window/virtual-screen APIs provide metadata inspection/mutation and metadata-aware writes. Wide text elements carry one coherent metadata value, ordinary replacement removes overwritten semantics, and semantic-only changes participate in damage/change tracking.

---

## T1104 — retained physical hyperlink rendering

The retained renderer compares physical semantic metadata independently of glyph/style state. Adjacent equal links coalesce into semantic runs and are emitted through Terminal's typed bounded hyperlink operation. DCurses emits no raw OSC 8.

Physical invalidation clears semantic knowledge with cell knowledge. Synchronized-output composition is covered by a real Terminal-backed integration test.

---

## T1105 — structural semantic propagation

The internal transient `CursesLogicalCellState` pair moves `CursesCell` plus optional `CursesCellMetadata` through:

- insert/delete cells and lines;
- scroll up/down;
- destructive rectangle copy;
- transparent overlay;
- pads and independent viewports;
- preserved screen resize;
- wide-cell normalization/clipping repair.

Destructive copy transfers annotated blanks. Overlay blanks remain fully transparent and preserve destination metadata.

---

## T1106 — failure, cancellation, lifecycle, and recovery

A failed synchronized-output release remains retryable because DCurses retains the `TerminalSynchronizedOutputLease`; cleanup is retried before later synchronized refresh or rendition reset.

A non-cancellation failure from Terminal's bounded hyperlink operation is treated more conservatively because the internal synthetic hyperlink lease may be retained by Terminal but is not exposed to DCurses. The curses session therefore fails closed for further application text until disposal while Terminal control cleanup and final TerminalSession disposal remain authoritative.

Caller cancellation before hyperlink transmission is non-poisoning. Cancellation after a complete linked run invalidates retained physical state so a later fresh refresh repaints safely. Suspend/resume likewise invalidates retained semantic knowledge.

Permanent record:

- `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`

---

## T1107 — application, performance, allocation, and optimization acceptance

T1107 now provides application-shaped evidence across editor, pager/help, large-pad, and real Terminal-backed session workloads.

Editor coverage includes wide linked Unicode, style changes independent of hyperlink identity, edits before/inside linked content, and repeated semantic-only retargeting. Pager coverage includes many links, no-op settling, viewport movement, line insertion/deletion, and scrolling. The 2,048 × 256 pad covers sparse and dense semantics plus independent viewports.

The sparse reference-plane acceptance shape records 36 KiB of deterministic reference payload for ten populated rows at the reference pad, compared with the rejected 4 MiB unconditional inline-slot cost.

Equivalent semantic cells coalesce to bounded hyperlink runs; 256 equivalent linked cells generate one semantic transaction, while 32 intentionally distinct links remain 32 transactions.

Real Terminal-backed acceptance covers synchronized output on/off, concurrent rich input, live resize, suspend/resume, and deterministic protocol cleanup.

Permanent record:

- `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`

---

## Physical optimization boundary

Terminal-native line-shift, character-shift, erase, and scrolling shortcuts remain bypassed while desired or retained physical semantic metadata exists.

T1107 closes this decision for 1.1. Representative non-semantic shortcuts provide strict cost wins, but terminfo does not guarantee that emulator-side OSC 8 associations follow physical insert/delete/erase/scroll operations. Direct semantic rewriting therefore remains the portable correctness choice.

---

## Active 1.1 sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad propagation            complete
  -> T1106  lifecycle/failure/cancellation hardening        complete
  -> T1107  application/performance/allocation acceptance   implementation qualified; alpha.7 doc gate
  -> T1108  API/package/documentation/regret gate           next
  -> T1109  RC and stable 1.1.0 closure
```

---

## Current documents

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-Development-Roadmap.md`
- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`
- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`
- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`
- `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`
- `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`
- `docs/Public-API-Fingerprint-1.1.json`

Published 1.0 compatibility documents remain historical/stable authorities and are not rewritten for later development state.