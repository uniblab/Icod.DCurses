# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Development checkpoint:** `1.1.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1108 complete and qualified; T1109 release-candidate validation active

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

Accepted 1.1 release-candidate contract after T1108:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

The only intentional new exported types remain:

- `CursesHyperlink`;
- `CursesCellMetadata`.

T1108 deliberately refined two provisional T1103 public contract lines before RC:

- `CursesCellMetadata.Hyperlink` is `CursesHyperlink?` for future additive semantic kinds while the current constructor still requires a real hyperlink;
- the two-argument convenience is `WriteWithMetadata(string, CursesCellMetadata)` rather than `Write(string, CursesCellMetadata)`, preserving stable 1.0 source such as `Write("text", default)` from overload ambiguity.

The explicit `Write(string, CursesStyle, CursesCellMetadata)` and `WriteCell(CursesCell, CursesCellMetadata)` operations remain unambiguous.

Compatible 1.x policy:

```text
Package version   advances normally through compatible 1.x releases
AssemblyVersion   remains 1.0.0.0
```

Current identity:

```text
Version         1.1.0-rc.1
PackageVersion  1.1.0-rc.1
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

The final pre-RC T1108 spelling of the two-argument convenience is `WriteWithMetadata(string, CursesCellMetadata)`; explicit style plus metadata remains `Write(string, CursesStyle, CursesCellMetadata)`.

Qualified documentation-complete T1103 head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) — seven jobs green.

T1103's alpha.3 fingerprint was provisional by design and was later refined at the designated T1108 regret gate.

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

T1107 closes the question for 1.1: representative non-semantic shortcuts have strict byte wins, but terminfo does not provide portable evidence that emulator-side OSC 8 hyperlink associations are preserved by physical insert/delete/erase/scroll operations. The safe 1.1 policy is therefore to keep the semantic-state gate and direct-rewrite semantic content.

A future shortcut may return only with portable semantic-equivalence evidence; terminal-name heuristics are not sufficient.

---

## 10. T1107 — application, performance, allocation, and optimization acceptance

T1107 exercises application-shaped semantic workloads rather than only unit-sized examples.

### Editor-like acceptance

Coverage proves:

- linked and unlinked content coexist;
- style changes are independent from link identity;
- edits before and inside linked spans preserve surviving semantic content;
- wide Unicode remains coherent inside links;
- repeated semantic-only retargeting damages and repaints otherwise unchanged text.

### Pager/help acceptance

Coverage proves:

- many independent links render correctly;
- settled unchanged linked content produces a no-op payload refresh;
- viewport movement preserves semantic projection;
- line insertion/deletion and scrolling move hyperlink identity with surviving logical content.

### Large-pad acceptance

The exact 2,048 × 256 reference surface covers:

- the established no-metadata baseline;
- sparse links;
- one dense linked row;
- independent viewports.

### Full Terminal-backed session

Real `TerminalSession` coverage proves:

- synchronized output enabled: Terminal-owned OSC 8 framing occurs inside mode-2026 ownership;
- synchronized output disabled: Terminal-owned OSC 8 framing remains correct without mode-2026 sequences;
- one rich-input wait coexists with semantic refresh;
- live resize repaints retained linked content at the new geometry;
- suspend/resume invalidates and replays semantic physical state;
- protocol cleanup remains deterministic.

### Performance and allocation evidence

The stable non-semantic deterministic cost fixtures remain unchanged.

At 2,048 × 256, the rejected unconditional eight-byte metadata slot costs 4 MiB. The accepted sparse plane has 16 KiB of top-level row references, and ten populated semantic rows add 20 KiB of row references, for 36 KiB of deterministic reference payload. Empty semantic rows allocate no per-row array and cleared rows are released.

Equivalent links coalesce:

```text
256 equivalent linked cells -> 1 bounded semantic hyperlink write
```

Distinct semantics remain distinct:

```text
32 distinct adjacent links -> 32 bounded semantic hyperlink writes
```

### Optimization decision

No structural terminal shortcut is restored for semantic state in 1.1. The performance value of the non-semantic shortcut path is already established; the missing requirement is portable semantic equivalence for terminal-maintained OSC 8 associations.

Qualified implementation/acceptance head:

```text
bc226acf20fc5a8ab88f81d0d2053663d0120288
```

Workflow #516 (`34419328443`) — seven jobs green.

Documentation/version-qualified alpha.7 head:

```text
8bfdb38ac6964f8a6bd1654d29d931c89cedf5c0
```

Workflow #517 (`34419902612`) — seven jobs green.

Permanent record:

- `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`

**Status:** complete and qualified.

---

## 11. T1108 — API/package/documentation/regret gate

T1108 is the final pre-RC public-contract review.

### 11.1 Compiler-derived public contract

The provisional T1103 fingerprint was intentionally left mutable until this gate.

A pre-fingerprint T1108 head built cleanly and executed the complete suite across all three target frameworks. On each of `net8.0`, `net9.0`, and `net10.0`, 485 of 486 tests passed; the only failure was the deliberately stale public fingerprint assertion.

The compiler-derived replacement was identical across all three targets:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

Pre-fingerprint evidence head:

```text
04ad8707956b7d45cb0eefba30d6af0b1833b5a3
```

Workflow #534 (`34422239633`) supplied that compiler-derived contract. Package construction passed; runtime jobs reached tests and failed only on the intentionally stale fingerprint.

### 11.2 Source-compatibility regret correction

Stable 1.0 already exposes:

```text
CursesWindow.Write(string, CursesStyle)
```

The provisional T1103 convenience:

```text
CursesWindow.Write(string, CursesCellMetadata)
```

would make previously valid stable source such as `window.Write("text", default)` ambiguous when recompiled against 1.1.

The accepted pre-RC convenience is therefore:

```text
CursesWindow.WriteWithMetadata(string, CursesCellMetadata)
```

The explicit `Write(string, CursesStyle, CursesCellMetadata)` operation remains available and unambiguous.

`PublicSemanticMetadataRegretTests` compiles both `Write("plain", default)` and `WriteWithMetadata(...)` to freeze this source-compatibility decision.

### 11.3 Metadata extensibility/nullability correction

The 1.1 constructor remains:

```text
CursesCellMetadata(CursesHyperlink hyperlink)
```

and requires a non-null hyperlink.

The public property is frozen as:

```text
CursesHyperlink? Hyperlink
```

so future additive metadata kinds do not require a later weakening of a published non-null return contract. Every `CursesCellMetadata` constructible through the 1.1 constructor still contains a hyperlink.

The record remains immutable with value equality semantics.

### 11.4 Dependency boundary

The approved public lower-layer allow-list remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

No Terminal hyperlink/protocol/output implementation type enters the public DCurses surface.

### 11.5 Package-only consumer

The fresh generated-package consumer now exercises:

- `CursesHyperlink` construction/canonicalization;
- `CursesCellMetadata` construction;
- `WriteWithMetadata(...)`;
- window and virtual-screen metadata inspection;
- metadata removal;
- metadata reassignment.

This remains a real NuGet-only consumer rather than a repository project-reference smoke test.

### 11.6 Sample and documentation audit

The minimal `Icod.DCurses.Sample` now demonstrates one retained hyperlink with `CursesHyperlink`, `CursesCellMetadata`, and `WriteWithMetadata(...)`. It emits no raw OSC 8; Terminal remains the framing owner.

The documentation audit found and removed the superseded `docs/T1103-Hyperlink-and-Public-Semantic-Metadata-Contract.md` implementation-staged draft. `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md` remains the single authoritative T1103 record and is synchronized with the final T1108 API decisions.

The root README, samples README, package release notes, current roadmap index, post-1.0 release-train roadmap, and this detailed 1.1 roadmap were synchronized to alpha.8 and the accepted pre-RC fingerprint before RC promotion.

### 11.7 XML/package gate

The package continues to generate XML documentation for `net8.0`, `net9.0`, and `net10.0`. The existing package verifier continues to require XML documentation, portable PDBs/symbol package, exact dependency groups, README/license/icon/repository metadata, and fresh package-only consumption.

Permanent records:

- `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/Public-API-Fingerprint-1.1.json`
- `docs/Public-API-Baseline-1.1.md`

Documentation-complete qualified alpha.8 head:

```text
290508c69ed7e76179f168cc748edf38a4091b76
```

Workflow #543 (`34422961869`) — seven jobs green.

**Status:** complete and qualified.

---

## 12. T1109 — RC and stable closure

T1109 promotes the T1108-accepted contract unchanged through release-candidate and stable qualification.

Release-candidate identity:

```text
Version         1.1.0-rc.1
PackageVersion  1.1.0-rc.1
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

The accepted public fingerprint remains:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

T1109 rules:

1. no new feature work or opportunistic API changes enter RC;
2. only demonstrated release blockers may alter implementation before stable promotion;
3. one exact documentation-synchronized RC SHA must pass all seven jobs;
4. stable promotion changes only version/package identity, release notes, and current release-status documentation unless a blocker requires reopening the gate;
5. one exact stable-source SHA must then pass all seven jobs;
6. the tested stable-source SHA is recorded in PR metadata without subsequently moving the branch;
7. merge, tag, GitHub Release creation, and NuGet publication remain explicit later actions.

Permanent record:

- `docs/T1109-RC-and-Stable-Closure.md`

**Status:** active; exact `1.1.0-rc.1` qualification pending.

---

## 13. Active sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            complete
  -> T1105  editing/copy/overlay/pad propagation            complete
  -> T1106  lifecycle/failure/cancellation/recovery         complete
  -> T1107  application/performance/allocation acceptance   complete
  -> T1108  public API/package/documentation/regret gate    complete; alpha.8 qualified
  -> T1109  RC and stable 1.1.0 closure                     active; rc.1 validation
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

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content, preserved through the complete curses editing/composition/lifecycle model, emitted through Terminal's typed OSC 8 ownership, and recovered conservatively after output uncertainty—while ordinary unlinked workloads retain the memory, allocation, and refresh characteristics expected from the stable 1.0 core and the additive public API does not introduce avoidable source-compatibility or future-extensibility regret.
