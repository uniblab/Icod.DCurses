# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Development checkpoint:** `1.1.0-alpha.6`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1106 complete; T1106 documentation-complete alpha.6 qualified at `59232c1e`; T1107 application/performance/allocation acceptance active

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

Version 1.1 must add semantic content without:

- turning `CursesStyle` into an untyped semantic bag;
- constructing raw OSC 8 in DCurses;
- exposing Terminal hyperlink leases publicly;
- weakening Unicode/wide-cell/editing/pad invariants;
- creating a second terminal serialization or lifecycle owner;
- imposing unconditional metadata cost on ordinary cells;
- hiding output/cleanup uncertainty behind optimistic retained state.

---

## 2. Compatibility and package identity

Published 1.0 compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Current provisional 1.1 contract:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

The only intentional new exported types remain:

- `CursesHyperlink`;
- `CursesCellMetadata`.

T1104–T1107 add no further public API.

Compatible 1.x policy:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0
```

Current identity:

```text
Version         1.1.0-alpha.6
PackageVersion  1.1.0-alpha.6
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

---

## 3. Terminal ownership boundary

DCurses owns retained semantic intent. Terminal owns:

- canonical OSC framing;
- live terminal state;
- session output ordering;
- synchronized-output ownership;
- hyperlink ownership and cleanup;
- terminal lifecycle participation.

DCurses does not emit raw OSC 8 or expose `TerminalHyperlinkLease` publicly.

Linked retained runs use Terminal's typed bounded hyperlink operation. This keeps normal hyperlink begin/text/end transactions inside Terminal while DCurses owns run selection, retained comparison, and damage policy.

---

## 4. T1102 — representation and memory gate

The accepted logical representation is a lazily allocated row-sparse metadata reference plane.

T1102 rejected:

- a permanent semantic reference field in every `CursesCell`;
- a surface-relative integer metadata token.

The portable measured incremental cost of both tested inline candidates is:

```text
+8 bytes per logical cell
```

At the established 2,048 × 256 reference pad, that is exactly 4 MiB even when semantic metadata is unused.

The sparse plane provides O(1) lookup, lazy row allocation/release, row snapshot/replace support, and leaves `CursesCell` context-free.

Qualified head:

```text
60fa5e1a0b17e91f7c0e78ee397ff797645471d7
```

Workflow #456 (`34400518088`) — seven jobs green.

---

## 5. T1103 — public logical semantic contract

T1103 introduced:

```text
CursesHyperlink
CursesCellMetadata
```

and metadata-aware operations on `CursesVirtualScreen` and `CursesWindow`.

Rules include:

- semantic metadata remains separate from `CursesStyle` and `CursesCell`;
- wide leader/continuation coordinates share one coherent semantic value;
- ordinary unannotated replacement clears overwritten semantics;
- semantic-only mutation participates in dirty/change tracking;
- `Fill()`/`Clear()` damage coordinates when semantics disappear even if visible cell values are unchanged;
- hyperlink target/identifier validation aligns with Terminal without leaking Terminal hyperlink types.

Qualified documentation-complete head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) — seven jobs green.

---

## 6. T1104 — retained physical hyperlink renderer

T1104 makes semantic metadata part of retained physical knowledge.

The renderer:

- detects semantic-only differences;
- subdivides changed style runs by metadata identity;
- coalesces adjacent equivalent links;
- emits linked text through Terminal's typed bounded hyperlink operation;
- emits only wide-cell leader text;
- invalidates physical semantic knowledge with cell knowledge;
- composes with Terminal synchronized output without another output lock.

A real Terminal-backed integration test proves Terminal-generated OSC 8 begin/text/end occurs inside the synchronized-output bracket without deadlock.

Qualified documentation-complete head:

```text
242deb76ede3e04a89a90591d8c27216f4efd980
```

Workflow #485 (`34411626180`) — seven jobs green.

---

## 7. T1105 — structural semantic propagation

T1105 introduced the internal transient state:

```text
CursesLogicalCellState
    CursesCell
    CursesCellMetadata?
```

This pair moves semantics with content while keeping `CursesCell` unchanged.

Covered transforms:

- insert/delete cells;
- insert/delete lines;
- scroll up/down;
- destructive rectangle copy;
- transparent overlay;
- pad presentation;
- independent pad viewports;
- preserved screen resize;
- wide-cell normalization/clipping repair.

Frozen composition rules:

- destructive copy transfers source metadata, including metadata on a copied blank;
- overlay blanks are fully transparent and leave destination metadata unchanged;
- overlapping copy snapshots cells and metadata before mutation;
- clipped/orphaned wide footprints lose semantics with discarded content.

Qualified documentation-complete head:

```text
f35eee81a9660271ba8eb4a930738b1997207cb7
```

Workflow #497 (`34413294079`) — seven jobs green.

---

## 8. T1106 — lifecycle, failure, cancellation, and recovery hardening

T1106 preserves the 1.0 rule that uncertain output invalidates retained physical knowledge, then extends it for Terminal-owned semantic state.

### 8.1 Synchronized-output restoration

A failed final `TerminalSynchronizedOutputLease.DisposeAsync()` is recoverable because DCurses owns that lease object and Terminal defines failed disposal as retryable.

DCurses therefore:

- retains the failed lease;
- invalidates retained physical state;
- retries the same cleanup before a later synchronized refresh;
- retries pending cleanup before rendition reset during suspend/disposal;
- does not begin a new refresh body while retry continues failing;
- preserves body + restoration dual failures in one aggregate.

### 8.2 Hyperlink uncertainty

Terminal's bounded `WriteHyperlinkAsync(...)` may retain an internal synthetic hyperlink lease after a non-cancellation failure. That synthetic lease is not returned to DCurses.

DCurses cannot reliably determine whether an arbitrary exception means:

- begin failed before commitment;
- begin committed and text failed;
- text completed and close failed;
- Terminal retains cleanup ownership.

The safe rule is therefore fail-closed: after the first non-cancellation bounded hyperlink failure, the Terminal-backed curses output refuses further linked or unlinked **application text** until the owning `CursesSession` is disposed.

Terminal control strings and flush remain available for cleanup/restoration. Terminal session disposal remains authoritative for final hyperlink cleanup.

This is deliberately conservative. A future Terminal public neutralization/recovery operation could permit a less restrictive policy.

### 8.3 Cancellation

Caller cancellation reported before hyperlink transmission does not latch semantic uncertainty.

If cancellation arrives after one complete linked run but before the complete curses refresh finishes:

- the successful linked Terminal transaction remains complete;
- refresh cancellation remains `OperationCanceledException`;
- retained physical state is invalidated;
- a later fresh-token refresh repaints the complete logical image.

### 8.4 Lifecycle replay

Suspend/resume invalidates retained physical semantic knowledge. Visible linked content is therefore repainted after re-entry.

Pending synchronized-output cleanup is retried before rendition reset so lifecycle restoration does not silently abandon a failed mode-2026 lease.

### 8.5 Failure preservation

The real Terminal-backed tests prove:

- hyperlink application text + hyperlink cleanup dual failures remain visible through DCurses as an aggregate;
- Terminal session disposal later retries retained OSC 8 cleanup;
- synchronized-output end failure is retried before a new presentation transaction;
- repeated cleanup failure blocks the new refresh body;
- successful retry is followed by full repaint;
- fail-closed hyperlink state blocks later application text;
- caller cancellation before semantic transmission is non-poisoning;
- post-semantic-run cancellation triggers complete later repaint.

Implementation/focused-test heads:

```text
3a4c33e26a256408058bb68a13d20e6bc2500949  workflow #501 / 34415613066
8aaeb5f5c188a1a42dd95f4097071e720e83f30e  workflow #502 / 34415923464
```

Both passed all seven jobs.

Documentation-complete qualified head:

```text
59232c1eb9da1bd97f8c3ea950c757159f82a15f
```

Workflow #507 (`34416436456`) — seven jobs green.

Permanent record:

- `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`

**Status:** complete and qualified.

---

## 9. Physical optimization boundary

Terminal-native line-shift, character-shift, erase, and scrolling shortcuts remain disabled while desired or retained physical semantic metadata exists.

T1105 proves logical transformations. T1107 may restore a physical shortcut only when tests prove both semantic equivalence and a strict cost win.

---

## 10. T1107 — application, performance, allocation, and optimization acceptance

T1107 must exercise application-shaped semantic workloads rather than only unit-sized examples.

Required cases:

### Editor-like

- linked and unlinked content;
- style changes independent of link identity;
- edits before/inside linked spans;
- wide Unicode inside links;
- repeated semantic-only changes.

### Pager/help

- many links;
- scrolling and line insertion/deletion;
- viewport movement;
- settled no-op refresh.

### Large pad

- 2,048 × 256 reference surface;
- no-metadata baseline;
- sparse links;
- one dense linked row;
- independent viewports.

### Full Terminal-backed session

- synchronized output on/off;
- rich input coexistence;
- resize and lifecycle transitions;
- deterministic cleanup.

### Performance/allocation

- preserve or explain the stable non-semantic baseline;
- measure sparse semantic-plane allocation shape;
- prove adjacent equivalent links coalesce rather than generating one OSC transaction per cell;
- measure many-distinct-link behavior;
- decide whether any structural terminal shortcut can safely return for semantic content.

T1107 should not introduce public diagnostics solely to gather acceptance evidence.

Active application-shaped acceptance head:

```text
ac23d7e920b7f27679b1835b59cdbb3baaaf7708
```

This first T1107 acceptance layer adds editor-like wide linked mutation, pager-scale many-link/no-op settling, the exact 2,048 × 256 reference pad with sparse and dense semantic rows plus independent viewports, dense-link coalescing, and explicit many-distinct-link transaction behavior. It is additive test coverage only; no public API or production optimization is introduced by this checkpoint.

**Status:** active; seven-job qualification pending for the acceptance head and remaining Terminal-backed/performance evidence under review.

---

## 11. T1108 — API/package/documentation/regret gate

Before RC:

- regenerate public API fingerprint on net8/net9/net10;
- confirm the intended 45-type/337-line contract or document any deliberate delta;
- review names, nullability, equality, validation, and future extensibility;
- verify no accidental Terminal/TermInfo public type leakage;
- extend package-only consumer coverage to the 1.1 semantic API;
- verify XML documentation;
- audit README/samples/roadmaps/release notes;
- resolve duplicate/stale planning documents if any remain;
- perform a final public regret review before RC promotion.

---

## 12. T1109 — RC and stable closure

1. promote accepted contract to `1.1.0-rc.1`;
2. validate one exact RC SHA on all seven jobs;
3. correct release blockers only;
4. promote unchanged accepted contract to stable `1.1.0`;
5. perform final documentation/status audit;
6. validate one exact stable-source SHA;
7. record tested SHA in PR metadata without moving branch;
8. leave merge/tag/publication as explicit later actions.

---

## 13. Active sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad propagation            complete
  -> T1106  lifecycle/failure/cancellation/recovery         complete
  -> T1107  application/performance/allocation acceptance   active
  -> T1108  public API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## 14. Explicit 1.1 non-goals

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
- application-level Terminal feature wrappers that add no curses meaning.

---

## 15. Definition of success

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content, preserved through the complete curses editing/composition/lifecycle model, emitted through Terminal's typed OSC 8 ownership, and recovered conservatively after output uncertainty—while ordinary unlinked workloads retain the memory, allocation, and refresh characteristics expected from the stable 1.0 core.
