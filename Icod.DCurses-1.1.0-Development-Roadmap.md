# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Source checkpoint:** `1.1.0`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1108 complete and qualified; T1109 RC qualified and stable-source exact-head qualification active

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

Version 1.1 adds semantic content without turning `CursesStyle` into an untyped semantic bag, constructing raw OSC 8 in DCurses, exposing Terminal hyperlink leases publicly, weakening Unicode/wide-cell/editing/pad invariants, creating a second terminal serialization/lifecycle owner, imposing unconditional metadata cost on ordinary cells, or hiding output/cleanup uncertainty behind optimistic retained state.

---

## 2. Compatibility and package identity

Published 1.0 compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Accepted stable 1.1 contract after T1108:

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

The only intentional new exported types are:

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

Current source identity:

```text
Version         1.1.0
PackageVersion  1.1.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

---

## 3. Terminal ownership boundary

DCurses owns retained semantic intent. Terminal owns canonical OSC framing, live terminal state, session output ordering, synchronized-output ownership, hyperlink ownership/cleanup, and terminal lifecycle participation.

DCurses does not emit raw OSC 8 or expose `TerminalHyperlinkLease` publicly. Linked retained runs use Terminal's typed bounded hyperlink operation.

---

## 4. T1102 — representation and memory gate

The accepted logical representation is a lazily allocated row-sparse metadata reference plane. T1102 rejected both a permanent semantic reference field in every `CursesCell` and a surface-relative integer metadata token.

The portable measured incremental cost of both tested inline candidates is +8 bytes per logical cell. At the established 2,048 × 256 reference pad, that is exactly 4 MiB even when semantic metadata is unused.

The sparse plane provides O(1) lookup, lazy row allocation/release, row snapshot/replace support, and leaves `CursesCell` context-free.

Qualified head:

```text
60fa5e1a0b17e91f7c0e78ee397ff797645471d7
workflow #456 / 34400518088
```

Seven jobs green.

---

## 5. T1103 — public logical semantic contract

T1103 introduced `CursesHyperlink`, `CursesCellMetadata`, metadata inspection/mutation, and metadata-aware writes.

Rules include:

- semantic metadata remains separate from `CursesStyle` and `CursesCell`;
- wide leader/continuation coordinates share one coherent semantic value;
- ordinary unannotated replacement clears overwritten semantics;
- semantic-only mutation participates in dirty/change tracking;
- `Fill()`/`Clear()` damage coordinates when semantics disappear even if visible cell values are unchanged;
- hyperlink target/identifier validation aligns with Terminal without leaking Terminal hyperlink types.

The final T1108 spelling of the two-argument convenience is `WriteWithMetadata(string, CursesCellMetadata)`; explicit style plus metadata remains `Write(string, CursesStyle, CursesCellMetadata)`.

Qualified documentation-complete T1103 head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
workflow #474 / 34405314146
```

Seven jobs green.

---

## 6. T1104 — retained physical hyperlink renderer

T1104 detects semantic-only differences, subdivides changed style runs by metadata identity, coalesces adjacent equivalent links, emits linked text through Terminal's typed bounded hyperlink operation, emits only wide-cell leader text, invalidates physical semantic knowledge with cell knowledge, and composes with synchronized output without another output lock.

Qualified head:

```text
242deb76ede3e04a89a90591d8c27216f4efd980
workflow #485 / 34411626180
```

Seven jobs green.

---

## 7. T1105 — structural semantic propagation

The internal transient `CursesLogicalCellState` pair moves `CursesCell` plus optional `CursesCellMetadata` through insert/delete cells and lines, scroll up/down, destructive rectangle copy, transparent overlay, pads/viewports, preserved resize, and wide-cell repair.

Frozen composition rules include destructive copy transferring annotated blanks, overlay blanks remaining fully transparent, overlapping copy snapshotting cells/metadata before mutation, and clipped/orphaned wide footprints losing semantics with discarded content.

Qualified head:

```text
f35eee81a9660271ba8eb4a930738b1997207cb7
workflow #497 / 34413294079
```

Seven jobs green.

---

## 8. T1106 — lifecycle, failure, cancellation, and recovery hardening

A failed synchronized-output release remains retryable because DCurses retains the `TerminalSynchronizedOutputLease` and retries cleanup before later synchronized refresh or rendition reset.

A non-cancellation bounded hyperlink failure fails application text closed until disposal because Terminal may retain an internal synthetic hyperlink lease that DCurses cannot access. Caller cancellation before hyperlink transmission is non-poisoning; cancellation after one complete linked run invalidates retained physical state for a complete later repaint. Suspend/resume likewise invalidates retained semantic knowledge.

Documentation-complete head:

```text
59232c1eb9da1bd97f8c3ea950c757159f82a15f
workflow #507 / 34416436456
```

Seven jobs green.

Permanent record: `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`.

---

## 9. Physical optimization boundary

Terminal-native line-shift, character-shift, erase, and scrolling shortcuts remain disabled while desired or retained physical semantic metadata exists.

T1107 confirmed the non-semantic cost wins but found no portable terminfo guarantee that emulator-side OSC 8 associations survive those physical transforms. Direct rewriting remains the 1.1 portable correctness policy.

---

## 10. T1107 — application, performance, allocation, and optimization acceptance

Application-shaped editor, pager/help, large-pad, and real Terminal-backed session workloads are qualified.

At the 2,048 × 256 reference pad, ten populated semantic rows carry 36 KiB of deterministic reference payload versus the rejected 4 MiB unconditional inline-slot cost. Empty semantic rows allocate no per-row array and cleared rows are released.

Equivalent links coalesce:

```text
256 equivalent linked cells -> 1 bounded semantic hyperlink write
```

Distinct semantics remain distinct:

```text
32 distinct adjacent links -> 32 bounded semantic hyperlink writes
```

Real Terminal-backed acceptance covers synchronized output on/off, concurrent rich input, live resize, suspend/resume, and deterministic protocol cleanup.

Implementation/acceptance head:

```text
bc226acf20fc5a8ab88f81d0d2053663d0120288
workflow #516 / 34419328443
```

Documentation/version head:

```text
8bfdb38ac6964f8a6bd1654d29d931c89cedf5c0
workflow #517 / 34419902612
```

Both passed all seven jobs.

Permanent record: `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`.

---

## 11. T1108 — API/package/documentation/regret gate

T1108 regenerated the compiler-derived 1.1 API fingerprint across `net8.0`, `net9.0`, and `net10.0` and froze the accepted 45-type/337-line contract at:

```text
21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

It also:

- froze `WriteWithMetadata(...)` to preserve stable-source `default` calls;
- made `CursesCellMetadata.Hyperlink` nullable for future metadata extensibility while retaining the non-null constructor;
- retained the approved five-type Terminal/TermInfo public dependency allow-list;
- extended the fresh NuGet-only consumer through hyperlink construction, semantic writing, inspection, removal, and reassignment;
- added one retained hyperlink to the minimal sample;
- removed the superseded duplicate T1103 draft;
- recorded machine-readable and human-readable 1.1 API baselines.

Documentation-complete alpha.8 head:

```text
290508c69ed7e76179f168cc748edf38a4091b76
workflow #543 / 34422961869
```

Seven jobs green.

Permanent records:

- `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/Public-API-Fingerprint-1.1.json`
- `docs/Public-API-Baseline-1.1.md`

**Status:** complete and qualified.

---

## 12. T1109 — RC and stable closure

T1109 promotes the T1108-accepted contract unchanged through release-candidate and stable qualification.

The final `1.1.0-rc.1` head:

```text
b90e54c668ccb8a02142c434470a020775fd375f
workflow #552 / 34426070109
```

passed all seven jobs. No release blocker appeared.

Stable-source identity is therefore:

```text
Version         1.1.0
PackageVersion  1.1.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

The accepted fingerprint remains unchanged at `21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039`, 45 exported types, and 337 contract lines.

The atomic stable-source commit changes release/package/status documentation only. Its exact SHA/workflow are recorded in PR #25 after its seven-job qualification without moving the branch.

After that gate is green, T1109 is complete at source/qualification level. Merge, tag, GitHub Release creation, and NuGet publication remain explicit later actions.

Permanent record: `docs/T1109-RC-and-Stable-Closure.md`.

**Status:** stable-source exact-head qualification active.

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
  -> T1109  RC and stable 1.1.0 closure                     RC qualified; stable-source gate active
```

---

## 14. Explicit 1.1 non-goals

Version 1.1 does not add panels/layers/z-order, layout managers, focus/key binding/hit testing, Sixel or Kitty Graphics, generic raster APIs, public pixel geometry, widget controls, native ncurses ABI/source compatibility, generic raw OSC/CSI/DCS/APC writers, or application-level Terminal feature wrappers that add no curses meaning.

---

## 15. Definition of success

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content, preserved through the complete curses editing/composition/lifecycle model, emitted through Terminal's typed OSC 8 ownership, and recovered conservatively after output uncertainty—while ordinary unlinked workloads retain the memory, allocation, and refresh characteristics expected from the stable 1.0 core and the additive public API does not introduce avoidable source-compatibility or future-extensibility regret.
