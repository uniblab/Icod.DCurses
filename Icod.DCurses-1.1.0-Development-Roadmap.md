# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Development checkpoint:** `1.1.0-alpha.5`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1104 complete; T1105 implementation qualified and alpha.5 documentation-complete gate active; T1106 next

---

## 1. Release objective

`Icod.DCurses 1.1.0` is the first additive release after the stable 1.0 contract. It adds non-visual semantic information to retained terminal content, beginning with hyperlinks.

The governing separation is:

```text
what a cell looks like       -> CursesStyle
what a cell means            -> CursesCellMetadata
how terminal protocols emit
that meaning                 -> Icod.Terminal
```

The release must add semantic capability without:

- turning `CursesStyle` into an untyped semantic bag;
- constructing raw OSC 8 inside DCurses;
- exposing Terminal hyperlink leases publicly;
- weakening Unicode/wide-cell/editing/pad invariants;
- creating a second terminal serialization domain;
- imposing an unconditional per-cell metadata cost on ordinary screens and pads.

---

## 2. Compatibility and version policy

The published 1.0 compiled contract remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The current provisional 1.1 compiled contract is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

Version 1.1 is additive. No existing public type/member is removed, renamed, repurposed, or assigned a different enum value merely to accommodate semantic metadata.

The compatible 1.x assembly policy is frozen:

```text
NuGet/package version  advances through compatible 1.x releases
AssemblyVersion        remains 1.0.0.0
```

The active package identity is:

```text
Version         1.1.0-alpha.5
PackageVersion  1.1.0-alpha.5
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

---

## 3. Terminal ownership boundary

DCurses owns retained semantic intent. Terminal owns wire protocol, live terminal state, output ordering, lifecycle participation, and protocol cleanup.

DCurses must not:

- emit raw OSC 8 framing;
- introduce an independent OSC parser/writer;
- expose `TerminalHyperlinkLease` publicly;
- infer hyperlink support from terminal brand or `TERM` alone;
- create a second live terminal reader;
- bypass Terminal's output ordering/lifecycle ownership.

T1104 selects Terminal's bounded `TerminalSession.WriteHyperlinkAsync(...)` for each coalesced retained hyperlink run. A long-lived Terminal hyperlink lease is deliberately not held across arbitrary curses refresh output, so hyperlink state returns to neutral between linked runs.

---

## 4. T1102 semantic metadata representation

T1102 rejected both an unconditional metadata field in every `CursesCell` and a surface-relative metadata token.

The portable representation measurement is the incremental slot cost rather than one architecture-specific private `CursesCell` size:

```text
cell + metadata-reference wrapper overhead   +8 bytes per cell
cell + int-token wrapper overhead             +8 bytes per cell
```

At the established large-pad reference:

```text
2048 × 256 = 524,288 logical cells
```

an unconditional extra eight-byte slot would cost exactly 4 MiB even when semantic metadata is unused.

The accepted representation is a lazily allocated row-sparse metadata reference plane. It provides:

- no semantic-plane allocation for a surface with no metadata;
- per-row reference storage only for rows containing semantics;
- release of empty row storage and eventually the complete plane;
- O(1) coordinate lookup;
- row snapshot/replace mechanics;
- shareable immutable semantic values;
- unchanged standalone `CursesCell` values.

The internal storage foundation is `CursesSparseCellPlane<T>`.

---

## 5. T1103 public semantic contract

T1103 introduced exactly two public semantic types:

```text
CursesHyperlink
CursesCellMetadata
```

Logical APIs include:

```text
CursesVirtualScreen.GetMetadata(...)
CursesVirtualScreen.SetMetadata(...)

CursesWindow.GetMetadata(...)
CursesWindow.SetMetadata(...)
CursesWindow.Write(string, CursesCellMetadata)
CursesWindow.Write(string, CursesStyle, CursesCellMetadata)
CursesWindow.WriteCell(CursesCell, CursesCellMetadata)
```

`CursesCell` remains unchanged. Semantic inspection is surface/window-aware because metadata is surface-owned.

Rules include:

- wide leader and continuation coordinates carry one coherent semantic value;
- ordinary unannotated replacement clears overwritten metadata;
- semantic-only mutation participates in dirty/change tracking;
- `Fill()`/`Clear()` invalidate semantic coordinates even if glyph/style values do not change;
- target/identifier validation aligns with Terminal while exposing no Terminal hyperlink type.

T1103 documentation-complete head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) passed all seven jobs.

**Status:** complete.

---

## 6. Hyperlink value rules

`CursesHyperlink` follows the reviewed Terminal-compatible contract:

- target is non-empty and absolute;
- string target is already URI-encoded caller data;
- invalid percent escapes are rejected;
- percent-escape hex digits canonicalize to uppercase;
- non-ASCII unescaped URI characters are rejected;
- target is bounded to Terminal's 2083-byte contract;
- null/empty identifier canonicalizes to no identifier;
- non-empty identifier is at most 128 bytes and uses only RFC 3986 unreserved ASCII characters;
- DCurses never dereferences, opens, downloads, activates, or follows a target.

Successful refresh proves emission through Terminal, not terminal recognition or activation.

---

## 7. T1104 retained physical hyperlink renderer

T1104 makes semantic metadata part of retained physical knowledge.

The renderer distinguishes:

```text
same cells / same style / same metadata
same cells / same style / different metadata
same cells / different style / same metadata
unknown physical semantic state
```

Implemented rules:

- physical state stores semantic metadata separately from cell values;
- `NeedsUpdate(...)` compares cells and metadata;
- semantic-only changes repaint identical visible cells;
- changed style runs are subdivided by metadata identity;
- adjacent equal hyperlinks are coalesced into one bounded linked payload;
- two-column elements emit only leader text;
- linked payloads use Terminal's `WriteHyperlinkAsync(...)`;
- unlinked payloads use ordinary application-text output;
- no raw OSC 8 framing exists in DCurses;
- invalidation clears semantic physical knowledge with cell knowledge;
- synchronized-output ownership composes with bounded Terminal hyperlink transactions.

A real Terminal-backed integration test proves canonical Terminal OSC 8 framing occurs inside the synchronized-output bracket without deadlock.

Documentation-complete alpha.4 head:

```text
242deb76ede3e04a89a90591d8c27216f4efd980
```

Workflow #485 (`34411626180`) passed all seven jobs.

**Status:** complete.

---

## 8. T1105 structural semantic propagation

T1105 makes every stable logical content-transform operation move semantic metadata with surviving content.

An internal transient value is introduced:

```text
CursesLogicalCellState
    CursesCell Cell
    CursesCellMetadata? Metadata
```

This pair is used for editing/composition snapshots only. It does not enter the public API and does not add a permanent field to `CursesCell`.

### Cell and line editing

`InsertCells`, `DeleteCells`, `InsertLines`, and `DeleteLines` snapshot/transform/commit paired cell+metadata state.

Rules:

- surviving content keeps its metadata;
- inserted/vacated background coordinates have no metadata;
- discarded content loses metadata with the discarded cell;
- a blank carrying metadata remains meaningful during destructive structural movement;
- normalization preserves metadata only for coherent wide-cell footprints.

### Scrolling

`ScrollUp` and `ScrollDown` move paired state in overlap-safe direction. Vacated rows are filled with background cells and null metadata.

### Rectangle copy

`CopyRectangleTo` transfers both source cell and source semantic metadata, including a source blank coordinate deliberately carrying metadata.

The complete source rectangle is snapshotted before destination mutation, so overlapping copies remain deterministic for semantics as well as cells.

### Overlay

The established transparency rule remains authoritative: a source blank is fully transparent.

Therefore a blank source coordinate does not replace or clear the destination cell or destination metadata, even if that source blank itself carries metadata.

### Wide-cell boundaries

Composition snapshots normalize wide-cell footprints at source rectangle boundaries. Orphaned continuations and leaders clipped from their continuation are replaced with boundary background and lose metadata. Whole surviving wide elements transfer one coherent semantic value across leader/continuation coordinates.

### Pads and viewports

`CursesPad.PresentTo(...)` already delegates to `ContentWindow.CopyRectangleTo(...)`, so the composition fix makes pad presentation semantic-aware without a second transfer engine.

Metadata-only pad mutation participates in existing cell revision tracking. `CursesPadViewport.HasVisiblePadChanges` therefore sees semantic-only changes.

Two independent viewports maintain independent observation state: presenting one does not acknowledge a change for the other.

### Preserved screen resize

`CursesScreen.Resize(..., preserveContents: true)` copies metadata with overlapping surviving cells. The existing `CursesCellFootprint.Repair(...)` pass removes both incomplete wide content and its metadata when the new geometry clips a wide element.

`preserveContents: false` continues to create a blank semantic-free replacement surface.

### Focused coverage

`CursesSemanticPropagationTests` covers:

- insert/delete cells;
- insert/delete lines;
- upward/downward scroll;
- wide edit normalization;
- destructive copy of semantic blanks;
- transparent overlay;
- overlapping copy;
- pad presentation;
- semantic-only viewport change detection;
- preserved resize;
- clipped-wide resize repair.

`CursesSemanticViewportIndependenceTests` separately covers independent viewport acknowledgement.

The implementation-plus-independent-viewport checkpoint:

```text
2c5459984ac1d5a1616ed7fea09ea69429ca872b
```

passed workflow #492 (`34412687220`) across all seven jobs before alpha.5 documentation promotion.

Permanent record:

- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`

**Status:** implementation qualified; documentation-complete alpha.5 exact-head gate active.

---

## 9. Conservative physical optimization boundary

Logical semantic propagation is now proven, but T1105 does not assume equivalent physical behavior for terminal-native editing controls.

Line-shift, character-shift, erase, and terminal scrolling shortcuts remain disabled whenever desired or retained physical semantic metadata exists.

T1107 may re-enable a subset only if tests prove the operation reproduces both visible cells and semantic state exactly and remains a strict cost win.

---

## 10. T1106 lifecycle, cancellation, and failure rules

The 1.0 hardening model remains authoritative.

Required coverage includes:

- hyperlink begin failure;
- content failure after begin;
- hyperlink close/restoration failure;
- primary refresh plus synchronized-output restoration failure;
- cancellation before semantic output commit;
- cancellation after Terminal has committed semantic framing;
- `Invalidate()` after uncertain semantic output;
- suspend/resume with visible linked content;
- disposal after a failed semantic transaction;
- later safe repaint after uncertainty.

Terminal owns protocol cleanup. DCurses owns conservative invalidation of retained physical knowledge.

---

## 11. T1107 application/performance/allocation acceptance

Required workloads include:

- editor-like linked/unlinked Unicode content and edits;
- pager/help scrolling and viewport movement;
- the 2,048 × 256 large-pad reference with no metadata, sparse metadata, and a dense linked row;
- multiple independent viewports;
- synchronized output on/off;
- rich input concurrent with serialized refresh;
- resize/lifecycle transitions;
- no-op/high-frequency refresh;
- linked-run coalescing;
- ordinary non-semantic performance/allocation regression checks.

---

## 12. Active development sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad/viewports propagation  alpha.5 exact-head gate
  -> T1106  lifecycle/failure/cancellation/recovery         next
  -> T1107  application/performance/allocation acceptance
  -> T1108  public API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## 13. T1101 record

Permanent record:

- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`

**Status:** complete.

---

## 14. T1102 record

Permanent record:

- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`

Qualified exact head:

```text
60fa5e1a0b17e91f7c0e78ee397ff797645471d7
```

Workflow #456 (`34400518088`) passed all seven jobs.

**Status:** complete.

---

## 15. T1103 record

Permanent record:

- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`

Qualified exact head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) passed all seven jobs.

**Status:** complete.

---

## 16. T1104 record

Permanent record:

- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`

Qualified documentation-complete head:

```text
242deb76ede3e04a89a90591d8c27216f4efd980
```

Workflow #485 (`34411626180`) passed all seven jobs.

**Status:** complete.

---

## 17. T1105 record

Permanent record:

- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`

Implementation-plus-independent-viewport head:

```text
2c5459984ac1d5a1616ed7fea09ea69429ca872b
```

Workflow #492 (`34412687220`) passed all seven jobs before package/documentation promotion.

### Exit gate

One exact documentation-complete `1.1.0-alpha.5` head must pass Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer validation before T1106 begins.

---

## 18. T1106 — lifecycle/failure/cancellation hardening

Failure-inject bounded semantic output, preserve meaningful dual failures, prove later repaint/recovery, and retain authoritative Terminal cleanup through synchronized output, suspend/resume, and disposal.

---

## 19. T1107 — acceptance/performance/allocation

Run editor, pager/help, large-pad, no-op/high-frequency, synchronized-output, mixed-style, Unicode, and linked-run-coalescing workloads. Significant ordinary-workload regressions must be explained before release continuation.

---

## 20. T1108 — API/package/documentation/regret gate

- regenerate compiled API fingerprints for net8/net9/net10;
- review naming/nullability/equality/validation/future-extensibility regret;
- verify no accidental upstream Terminal/TermInfo type leakage;
- add fresh NuGet-only consumer coverage for the 1.1 semantic API;
- verify XML documentation;
- update README/samples/roadmaps/migration guidance/release notes;
- perform a documentation audit before RC promotion.

---

## 21. T1109 — RC and stable closure

1. promote the accepted contract to `1.1.0-rc.1`;
2. run Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer validation on one exact RC SHA;
3. correct release blockers only;
4. promote the unchanged contract to stable `1.1.0`;
5. perform the final documentation/status audit;
6. rerun the complete exact stable-source gate;
7. record the tested SHA in PR metadata without moving the branch;
8. leave merge/tag/publication as explicit later actions.

---

## 22. Explicit 1.1 non-goals

Version 1.1 does not require:

- panels/layers/z-order — planned for 1.2;
- layout managers — planned for 1.3;
- focus/key binding/hit testing — planned for 1.4;
- Sixel or Kitty Graphics;
- generic raster APIs;
- public pixel-geometry API;
- widget controls;
- native `ncurses` ABI/source compatibility;
- generic raw OSC/CSI/DCS/APC writers;
- notification or shell-integration wrappers that add no curses-level meaning.

---

## 23. Definition of success

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content, preserved through the complete curses editing/composition/lifecycle model, and emitted through Terminal's typed OSC 8 ownership layer—while ordinary unlinked workloads retain the memory, allocation, and refresh characteristics expected from the stable 1.0 core.
