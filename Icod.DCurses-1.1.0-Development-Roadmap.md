# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Development checkpoint:** `1.1.0-alpha.4`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1103 complete; T1104 implemented and under exact-head validation; T1105 next

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

## 2. Stable compatibility floor

The published 1.0 compiled contract remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Version 1.1 is additive. No existing public type/member is removed, renamed, repurposed, or assigned a different enum value merely to accommodate semantic metadata.

The compatible 1.x assembly policy is frozen:

```text
NuGet/package version  advances through compatible 1.x releases
AssemblyVersion        remains 1.0.0.0
```

---

## 3. Terminal ownership boundary

The active dependency floor is `Icod.Terminal 1.6.0`.

DCurses may compose Terminal semantic hyperlink operations internally, but it must not:

- emit raw OSC 8 framing;
- introduce an independent OSC parser/writer;
- expose `TerminalHyperlinkLease` publicly;
- infer protocol support from terminal brand or `TERM` alone;
- create a second live terminal reader;
- bypass Terminal's output ordering and lifecycle ownership.

DCurses owns retained semantic intent. Terminal owns the wire protocol and live terminal state.

T1104 selects Terminal's bounded `TerminalSession.WriteHyperlinkAsync(...)` operation for each coalesced retained hyperlink run rather than holding a long-lived hyperlink lease across arbitrary refresh output. That leaves the Terminal hyperlink manager neutral between retained runs and keeps begin/text/end cleanup inside one Terminal-owned transaction.

---

## 4. T1102 semantic metadata representation

T1102 rejected both an unconditional metadata field in every `CursesCell` and a surface-relative metadata token.

The supported x64/ARM64 matrix does not promise one private `CursesCell` size. The portable measurement is the incremental slot cost:

```text
cell + metadata-reference wrapper overhead   +8 bytes per cell
cell + int-token wrapper overhead             +8 bytes per cell
```

For the established large-pad reference:

```text
2048 × 256 = 524,288 logical cells
```

one unconditional extra eight-byte slot costs exactly 4 MiB even when no semantic metadata is present.

The accepted representation is a lazily allocated row-sparse metadata reference plane:

```text
CursesVirtualScreen
    dense CursesCell[]
    optional semantic plane
        row 0 -> null
        row 1 -> CursesCellMetadata?[] only when needed
        row 2 -> null
        ...
```

Properties:

- no semantic-plane allocation for a surface with no metadata;
- per-row reference storage only for rows containing semantic values;
- empty rows and eventually the entire plane are released;
- O(1) coordinate lookup;
- detached row snapshot/replace mechanics fit the existing editing architecture;
- immutable metadata references may be shared across surfaces;
- `CursesCell` remains a standalone, context-free public value.

The internal foundation is `CursesSparseCellPlane<T>`.

---

## 5. T1103 public semantic contract

T1103 introduces exactly two public semantic types:

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

`CursesCell` itself remains unchanged. Semantic inspection is surface/window-aware because metadata is owned by the logical surface rather than embedded in detached cell values.

Two-column text elements carry one coherent metadata value across the leader/continuation footprint. Ordinary unannotated replacement removes overwritten semantic metadata even if the visible cell value is unchanged. Metadata-only mutation participates in dirty/change tracking. `Fill()` and `Clear()` also damage semantic coordinates when metadata disappears without a visible glyph change.

The provisional compiled 1.1 development contract is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

`docs/Public-API-Fingerprint-1.1.json` is a development baseline and may still change deliberately before T1108. The stable 0.9/1.0 fingerprint files remain untouched.

T1103 documentation-complete head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) passed all six runtime architectures plus package validation.

**Status:** complete.

---

## 6. Hyperlink value rules

`CursesHyperlink` aligns with Terminal's reviewed hyperlink contract:

- target is non-empty;
- target is absolute;
- string target is already URI-encoded caller data;
- invalid percent escapes are rejected;
- percent-escape hex digits canonicalize to uppercase;
- non-ASCII unescaped URI characters are rejected;
- target is bounded to Terminal's 2083-byte contract;
- null/empty identifier canonicalizes to no identifier;
- non-empty identifier is at most 128 bytes and contains only RFC 3986 unreserved ASCII characters;
- DCurses never dereferences, opens, downloads, activates, or follows the target.

Successful refresh proves semantic emission through Terminal, not that the terminal displayed or permits activation of the link.

---

## 7. T1104 retained physical hyperlink renderer

T1104 extends the retained physical model so it can distinguish:

```text
same cells / same style / same metadata
same cells / same style / different metadata
same cells / different style / same metadata
unknown physical semantic state
```

Implemented rules:

- physical state stores semantic metadata separately from cell values;
- `NeedsUpdate(...)` compares both cell and metadata state;
- semantic-only mutation therefore repaints identical glyph/style content;
- changed style runs are subdivided by metadata identity;
- adjacent equal hyperlink metadata is coalesced into one bounded linked payload;
- a two-column text element emits only the leader content while retaining metadata on both coordinates;
- linked payloads delegate to Terminal's `WriteHyperlinkAsync(...)` operation;
- unlinked payloads continue through ordinary application-text output;
- no raw OSC 8 framing exists in the DCurses refresh engine;
- physical invalidation invalidates metadata together with cell knowledge;
- optional synchronized output composes around bounded Terminal hyperlink writes without creating another output gate.

### Conservative optimization rule

Until T1105/T1107 prove semantic behavior for structural terminal operations, refresh does not select line-shift, character-shift, erase, or scrolling shortcuts while either the desired or retained physical screen contains semantic metadata.

This is intentionally conservative. Correct retained semantics take precedence over saving bytes.

### Focused coverage

T1104 tests cover:

- coalescing adjacent equal links;
- linked/unlinked/linked segmentation;
- semantic-only link replacement;
- hyperlink removal from unchanged glyphs;
- no-op retained refresh;
- two-column linked content;
- explicit physical invalidation;
- optimization suppression in the presence of semantics;
- real `TerminalSession` + `CursesSession` synchronized-output integration using Terminal-generated canonical OSC 8 frames.

### Validation status

The alpha.4 exact-head run on `c1925468e2b35264b42a73e98f192a73c5ef7619` executed successfully on Windows x64, Linux x64/ARM64, macOS x64/ARM64, and package validation. Windows ARM64 failed before restore inside `actions/setup-dotnet` with an internal CLR installation crash. That single infrastructure job is being rerun; no repository code executed in the failed attempt.

Permanent record:

- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`

---

## 8. T1105 editing and composition invariants

Semantic metadata must move with retained content wherever content moves.

Coverage must include:

- overwrite/clear/fill;
- insert/delete cells;
- insert/delete lines;
- scroll operations;
- resize;
- wide-cell clipping and repair;
- copy rectangle;
- overlay rectangle;
- windows/subwindows;
- pads;
- multiple independent pad viewports;
- damage/touch/invalidation.

The editing implementation currently snapshots `CursesCell[]` rows. T1105 will pair those snapshots with metadata rows so structural edits transform cell and semantic state together. The preferred internal discipline is one logical edit snapshot carrying both:

```text
CursesCell
CursesCellMetadata?
```

without changing the stable public `CursesCell` representation.

Copy/overlay behavior must be explicit:

- ordinary copy transfers source metadata with copied source content;
- copied source blanks transfer their semantic state if the source coordinate itself carries metadata;
- overlay transparency is governed by the existing blank/transparency rule, and transparent source coordinates do not overwrite destination metadata;
- wide-cell normalization removes metadata from discarded/invalid footprints and preserves it only for surviving coherent text elements.

Pads and viewport presentation must copy semantic state into the destination logical screen along with visible cells.

---

## 9. T1106 lifecycle, cancellation, and failure rules

The 1.0 hardening model remains authoritative.

Required failure coverage includes:

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

## 10. T1107 application/performance/allocation acceptance

Required application-shaped workloads:

### Editor-like content

- ordinary and linked text;
- selection/style changes independent of link meaning;
- edits before/inside linked spans;
- wide Unicode inside links;
- repeated semantic-only changes.

### Pager/help content

- many links;
- scrolling and line insertion/deletion;
- viewport movement;
- no-op refresh after settled presentation.

### Large pad

- 2,048 × 256 reference surface;
- no metadata baseline;
- sparse links;
- dense linked row;
- multiple independent viewports.

### Full-screen Terminal-backed session

- synchronized output on/off;
- rich input coexisting with refresh;
- resize and lifecycle transitions;
- deterministic disposal/restoration.

The release must preserve or explain ordinary non-semantic performance and allocation behavior.

---

## 11. Development sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      complete
  -> T1104  retained physical hyperlink renderer            implemented; exact-head gate
  -> T1105  editing/copy/overlay/pad/viewports propagation  next
  -> T1106  lifecycle/failure/cancellation/recovery
  -> T1107  application/performance/allocation acceptance
  -> T1108  public API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## 12. T1101 record

Permanent record:

- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`

Decisions:

- first implementation checkpoint `1.1.0-alpha.1`;
- `AssemblyVersion 1.0.0.0` retained for compatible additive 1.x releases;
- Terminal dependency advanced to 1.6.0;
- TermInfo retained at 1.10.0;
- no raw OSC 8;
- metadata separate from `CursesStyle`;
- no public semantic types before the representation gate.

**Status:** complete.

---

## 13. T1102 record

Permanent record:

- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`

Qualified exact head:

```text
60fa5e1a0b17e91f7c0e78ee397ff797645471d7
```

Workflow #456 (`34400518088`) passed all six runtime architectures plus package validation.

**Status:** complete.

---

## 14. T1103 record

Permanent record:

- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`

Qualified exact head:

```text
d520adf79bf6a74bfbb09e1b2ecc4082a3cce960
```

Workflow #474 (`34405314146`) passed all six runtime architectures plus package validation.

**Status:** complete.

---

## 15. T1104 record

Permanent record:

- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`

T1104 introduces no additional public API beyond the T1103 provisional contract. Its work is retained physical semantics, Terminal composition, conservative optimization gating, and integration coverage.

**Status:** implementation complete; exact alpha.4 validation awaiting the infrastructure-only Windows ARM64 rerun.

---

## 16. T1105 — editing, copy/overlay, pads and viewports

### Objectives

- pair editing snapshots with semantic metadata;
- preserve semantics through insert/delete cells;
- preserve semantics through insert/delete lines and scroll;
- explicitly freeze copy/overlay metadata rules;
- preserve/repair wide-cell metadata coherently;
- carry metadata through pad viewport presentation;
- preserve metadata across screen resize where content survives;
- add focused and application-shaped tests;
- keep `CursesCell` unchanged.

### Exit gate

One exact `1.1.0-alpha.5` checkpoint must pass all runtime architectures plus package/fresh-consumer validation before T1106 begins.

---

## 17. T1106 — lifecycle/failure/cancellation hardening

Failure-inject semantic output transactions, preserve dual failures, prove later recovery, and retain authoritative Terminal restoration through suspend/resume and disposal.

---

## 18. T1107 — acceptance/performance/allocation

Run editor, pager/help, large-pad, no-op/high-frequency, synchronized-output, mixed-style, Unicode, and linked-run-coalescing workloads. Significant ordinary-workload regressions must be explained before release continuation.

---

## 19. T1108 — API/package/documentation/regret gate

- regenerate compiled API fingerprints for net8/net9/net10;
- review naming/nullability/equality/validation/future-extensibility regret;
- verify no accidental upstream Terminal/TermInfo type leakage;
- add fresh NuGet-only consumer coverage for the 1.1 semantic API;
- verify XML documentation;
- update README/samples/roadmaps/migration guidance/release notes;
- perform a documentation audit before RC promotion.

---

## 20. T1109 — RC and stable closure

1. promote the accepted contract to `1.1.0-rc.1`;
2. run Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer validation on one exact RC SHA;
3. correct release blockers only;
4. promote the unchanged contract to stable `1.1.0`;
5. perform the final documentation/status audit;
6. rerun the complete exact stable-source gate;
7. record the tested SHA in PR metadata without moving the branch;
8. leave merge/tag/publication as explicit later actions.

---

## 21. Explicit 1.1 non-goals

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

## 22. Definition of success

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content, preserved through the complete curses editing/composition/lifecycle model, and emitted through Terminal's typed OSC 8 ownership layer—while ordinary unlinked workloads retain the memory, allocation, and refresh characteristics expected from the stable 1.0 core.
