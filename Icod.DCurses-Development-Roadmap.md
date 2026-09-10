# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published stable baseline:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Current source package:** `1.1.0`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.1.0` — semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1108 complete and qualified; T1109 RC qualified and stable-source exact-head qualification active

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
| `1.1.0` | Semantic cell metadata and hyperlinks | Stable-source candidate — final seven-job qualification |
| `1.2.0` | Panels, layers, visibility, and z-order composition | Approved future release |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus, interaction regions, key gestures, hit testing, and pointer semantics | Approved future release |
| later | Raster graphics over Terminal semantic raster routing | Deferred until the required Terminal public contract exists |

Authorities:

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.1.0-Development-Roadmap.md` — active detailed 1.1 plan;
- `docs/T1109-RC-and-Stable-Closure.md` — active final closure record.

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

Accepted 1.1 stable contract:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

T1101 ratified:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0 for compatible additive 1.x releases
```

Current source identity:

```text
Version         1.1.0
PackageVersion  1.1.0
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
| T1107 documentation/alpha.7 | `8bfdb38ac6964f8a6bd1654d29d931c89cedf5c0` | #517 / `34419902612` | seven jobs green |
| T1108 documentation/alpha.8 | `290508c69ed7e76179f168cc748edf38a4091b76` | #543 / `34422961869` | seven jobs green |
| T1109 rc.1 | `b90e54c668ccb8a02142c434470a020775fd375f` | #552 / `34426070109` | seven jobs green |

The final stable-source SHA is recorded in PR #25 after its exact-head workflow passes; the source branch is not moved merely to self-record that SHA.

---

## T1102 — sparse semantic representation

Semantic metadata uses a lazily allocated row-sparse reference plane rather than a permanent field inside every `CursesCell`.

Both tested inline metadata/token candidates impose an additional eight-byte slot per logical cell on the supported 64-bit validation matrix. At 2,048 × 256 cells that would add exactly 4 MiB even when semantics are unused.

The sparse representation retains only a top-level row-reference table until semantic rows are populated; individual row arrays are allocated on demand and released when empty.

---

## T1103 — public semantic contract

T1103 introduced exactly two public semantic types:

- `CursesHyperlink`;
- `CursesCellMetadata`.

Window/virtual-screen APIs provide metadata inspection/mutation and metadata-aware writes. Wide text elements carry one coherent metadata value, ordinary replacement removes overwritten semantics, and semantic-only changes participate in damage/change tracking.

T1108 froze the source-compatible two-argument convenience as `WriteWithMetadata(string, CursesCellMetadata)` so stable source such as `Write("text", default)` does not become ambiguous with `Write(string, CursesStyle)`.

---

## T1104 — retained physical hyperlink rendering

The retained renderer compares physical semantic metadata independently of glyph/style state. Adjacent equal links coalesce into semantic runs and are emitted through Terminal's typed bounded hyperlink operation. DCurses emits no raw OSC 8.

Physical invalidation clears semantic knowledge with cell knowledge. Synchronized-output composition is covered by a real Terminal-backed integration test.

---

## T1105 — structural semantic propagation

The internal transient `CursesLogicalCellState` pair moves `CursesCell` plus optional `CursesCellMetadata` through insert/delete cells and lines, scroll up/down, destructive rectangle copy, transparent overlay, pads and independent viewports, preserved screen resize, and wide-cell normalization/clipping repair.

Destructive copy transfers annotated blanks. Overlay blanks remain fully transparent and preserve destination metadata.

---

## T1106 — failure, cancellation, lifecycle, and recovery

A failed synchronized-output release remains retryable because DCurses retains the `TerminalSynchronizedOutputLease`; cleanup is retried before later synchronized refresh or rendition reset.

A non-cancellation failure from Terminal's bounded hyperlink operation is treated more conservatively because the internal synthetic hyperlink lease may be retained by Terminal but is not exposed to DCurses. The curses session therefore fails closed for further application text until disposal while Terminal control cleanup and final `TerminalSession` disposal remain authoritative.

Caller cancellation before hyperlink transmission is non-poisoning. Cancellation after a complete linked run invalidates retained physical state so a later fresh refresh repaints safely. Suspend/resume likewise invalidates retained semantic knowledge.

Permanent record: `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`.

---

## T1107 — application, performance, allocation, and optimization acceptance

T1107 provides application-shaped evidence across editor, pager/help, large-pad, and real Terminal-backed session workloads.

The sparse reference-plane acceptance shape records 36 KiB of deterministic reference payload for ten populated rows at the 2,048 × 256 reference pad, compared with the rejected 4 MiB unconditional inline-slot cost.

Equivalent semantic cells coalesce to bounded hyperlink runs; 256 equivalent linked cells generate one semantic transaction, while 32 intentionally distinct links remain 32 transactions.

Real Terminal-backed acceptance covers synchronized output on/off, concurrent rich input, live resize, suspend/resume, and deterministic protocol cleanup.

Permanent record: `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`.

---

## T1108 — public API, package, documentation, and regret gate

T1108 is complete and qualified. It regenerated the compiler-derived 1.1 API fingerprint across net8/net9/net10, froze the accepted 45-type/337-line contract at `21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039`, made `CursesCellMetadata.Hyperlink` nullable for forward-compatible future semantic kinds while retaining the non-null constructor, renamed the ambiguous two-argument semantic write to `WriteWithMetadata(...)`, extended the package-only consumer, added one retained-hyperlink use to the minimal sample, removed the superseded duplicate T1103 draft, and recorded machine-readable/human-readable 1.1 API baselines.

Permanent record: `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`.

---

## Physical optimization boundary

Terminal-native line-shift, character-shift, erase, and scrolling shortcuts remain bypassed while desired or retained physical semantic metadata exists.

T1107 closed this decision for 1.1. Representative non-semantic shortcuts provide strict cost wins, but terminfo does not guarantee that emulator-side OSC 8 associations follow physical insert/delete/erase/scroll operations. Direct semantic rewriting therefore remains the portable correctness choice.

---

## T1109 — RC and stable closure

The `1.1.0-rc.1` freeze `b90e54c668ccb8a02142c434470a020775fd375f` passed workflow #552 (`34426070109`) across all seven jobs with no release blocker.

Stable-source promotion preserves the accepted API and implementation unchanged and changes only release/package/status documentation to `1.1.0`.

The resulting exact stable-source SHA must pass the same seven-job matrix. After that qualification, PR #25 records the tested SHA/workflow without moving the branch. Merge, tag, GitHub Release creation, and NuGet publication remain explicit later actions.

Permanent record: `docs/T1109-RC-and-Stable-Closure.md`.

---

## Active 1.1 sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad propagation            complete
  -> T1106  lifecycle/failure/cancellation hardening        complete
  -> T1107  application/performance/allocation acceptance   complete
  -> T1108  API/package/documentation/regret gate           complete; alpha.8 qualified
  -> T1109  RC and stable 1.1.0 closure                     RC qualified; stable-source gate active
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
- `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1109-RC-and-Stable-Closure.md`
- `docs/Public-API-Fingerprint-1.1.json`
- `docs/Public-API-Baseline-1.1.md`

Published 1.0 compatibility documents remain historical/stable authorities and are not rewritten for later development state.
