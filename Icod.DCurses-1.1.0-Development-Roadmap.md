# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable source baseline:** merged `1.0.0`  
**Planning baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Current dependency floor:** `Icod.Terminal 1.5.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Planning status:** approved; implementation not yet started  
**Planning-PR version rule:** keep `Version 1.0.0`, `PackageVersion 1.0.0`, and `AssemblyVersion 1.0.0.0` unchanged

---

## 1. Release objective

`Icod.DCurses 1.1.0` introduces the first post-1.0 additive semantic-content feature: non-visual metadata attached to retained terminal content, beginning with hyperlinks.

The release must prove that semantic metadata is a first-class part of logical screen composition without conflating it with visual style, without duplicating Terminal protocol ownership, and without imposing an unjustified memory/allocation cost on ordinary screens and large pads.

The core invariant is:

```text
what a cell looks like       -> CursesStyle
what a cell means            -> semantic metadata
how that meaning reaches
physical terminal protocols  -> Icod.Terminal
```

Hyperlinks are the first semantic metadata kind because Terminal already provides a typed, session-owned OSC 8 implementation with scoped nesting and lifecycle behavior.

---

## 2. Stable 1.0 compatibility floor

The 1.0 compiled public contract remains the starting compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Version 1.1 is additive by default.

No existing public type/member is removed, renamed, repurposed, or assigned a different enum value merely to make semantic metadata easier to implement.

The existing public `CursesStyle` contract remains presentation-only:

```text
foreground
background
text attributes
```

A hyperlink URI is not a color/rendition attribute and must not be placed inside `CursesStyle`.

---

## 3. Terminal ownership boundary

Terminal's existing typed hyperlink API is the protocol authority:

```text
TerminalSession.AcquireHyperlinkAsync(...)
TerminalSession.WriteHyperlinkAsync(...)
```

DCurses may compose those operations internally, but it must not:

- construct raw OSC 8 sequences;
- introduce an independent hyperlink protocol parser/writer;
- expose `TerminalHyperlinkLease` as part of the public curses abstraction;
- infer hyperlink capability from terminal brand or `TERM` alone;
- create a second terminal output-ordering domain;
- create a second live input reader.

DCurses owns the retained semantic intent. Terminal owns the wire protocol, live-session state, nesting, suspend/resume replay, and final cleanup.

---

## 4. Semantic metadata representation problem

Before a public API is frozen, 1.1 must choose a representation that remains practical for large retained surfaces.

Candidate models include:

### A. Optional immutable metadata reference per cell

```text
CursesCell
    content
    width/continuation
    style
    line glyph
    optional metadata reference
```

Advantages:

- direct semantic equality;
- natural copy/edit/pad behavior;
- easy public inspection;
- one metadata object can be shared by every cell in a span.

Risk:

- one additional reference-sized field on every cell can materially increase large-pad memory even when almost no cells carry metadata.

### B. Surface-owned interned semantic token

Cells carry a compact token which resolves through a screen/pad-owned semantic table.

Advantages:

- smaller per-cell footprint than a managed reference in some layouts;
- strong sharing/interning.

Risks:

- cross-surface copying requires remapping;
- standalone `CursesCell` value semantics become less obvious;
- token lifetime/ownership can leak implementation details into the public contract.

### C. Sparse sidecar semantic spans

Semantic metadata is stored separately from the ordinary cell array.

Advantages:

- essentially no ordinary-cell memory tax;
- naturally sparse for hyperlink-heavy-but-not-every-cell workloads.

Risks:

- every edit/insert/delete/scroll/copy/overlay/resize operation must update span topology;
- random cell inspection becomes more expensive/complex;
- wide-cell repair and partial clipping require careful synchronization between cell and semantic structures.

T1102 must measure and choose among these (or a better equivalent) before the public semantic-cell API is accepted.

---

## 5. Candidate public API direction — not frozen

The desired conceptual surface is intentionally small.

Candidate concepts include:

```text
CursesHyperlink
CursesCellMetadata
CursesCell.Metadata
metadata-aware CursesWindow write operations
```

The exact names, struct/class choices, and overload shape are not frozen by this roadmap.

A likely semantic model is:

```text
CursesHyperlink
    Uri
    optional Identifier

CursesCellMetadata
    optional Hyperlink
```

with immutable reusable values.

The public design should allow applications to:

- create hyperlink semantics without referencing Terminal types;
- write a text span with one hyperlink meaning;
- inspect semantic metadata from logical content where appropriate;
- reuse one immutable metadata value across many cells/spans;
- copy/overlay/edit content without manually reconstructing hyperlink state.

The release should avoid a broad generic dictionary-of-arbitrary-metadata API unless a concrete stable use case requires one. 1.1 should establish an extensible semantic model without turning cells into untyped property bags.

---

## 6. URI and identifier contract

DCurses hyperlink semantics should align with the reviewed Terminal contract rather than inventing a second incompatible grammar.

The design gate should preserve these principles:

- hyperlink target is non-empty;
- target is absolute;
- caller supplies the already URI-encoded target when using the string-oriented API;
- optional identifier is bounded and compatible with Terminal's allowed OSC 8 identifier grammar;
- DCurses never dereferences, opens, activates, downloads, or otherwise follows the target;
- successful output means the semantic protocol was emitted through Terminal, not that a terminal displayed or allowed activation of the link.

If a `System.Uri` overload is considered, it must not silently canonicalize or re-encode a target differently from the string contract. A string-first API may therefore remain preferable.

---

## 7. Retained physical semantic state

Hyperlink semantics must participate in retained refresh planning.

The physical renderer needs enough retained knowledge to distinguish:

```text
same cells / same style / same hyperlink
same cells / same style / different hyperlink
same cells / different style / same hyperlink
unknown hyperlink physical state
```

The renderer should group adjacent equivalent hyperlink runs rather than emitting a begin/end pair per cell.

A conceptual output transaction may look like:

```text
move cursor
set rendition
open hyperlink A
write several adjacent cells
change rendition while hyperlink A remains active
write more linked cells
close/replace hyperlink A
write unlinked cells
```

The actual sequencing must compose with Terminal's session-owned hyperlink manager and DCurses' existing terminal-activity and synchronized-output boundaries.

No raw OSC 8 bytes belong in the refresh engine.

---

## 8. Editing and composition invariants

Semantic metadata must move with logical content wherever content moves.

Version 1.1 must cover at least:

- ordinary text write/overwrite;
- clear/erase/fill;
- insert/delete cells;
- insert/delete lines;
- scroll operations;
- resize and clipped-wide-cell repair;
- copy rectangle;
- overlay rectangle;
- windows and nested subwindows;
- pads;
- multiple independent pad viewports;
- damage/touch/invalidation behavior.

For wide text:

- the leading cell and continuation footprint represent one semantic text element;
- an edit that destroys/replaces the wide element must remove its semantic metadata coherently;
- clipping must never leave a hyperlink semantic fragment attached to an invalid continuation footprint.

Blank/transparency behavior must be explicitly defined for copy/overlay just as content/style behavior already is.

---

## 9. Failure, lifecycle, and cancellation rules

Hyperlink integration must preserve the 0.8/1.0 hardening model.

The release must test:

- failure while opening hyperlink state;
- failure after hyperlink begin but before content completion;
- failure while closing/restoring hyperlink state;
- simultaneous refresh failure and hyperlink restoration failure;
- cancellation before semantic output begins;
- cancellation after a physical semantic transition is committed;
- `Invalidate()` while a hyperlink was believed active;
- suspend/resume while logical hyperlink content remains visible;
- disposal while physical hyperlink state may be active/unknown;
- later safe refresh after uncertain physical semantic state.

Terminal remains authoritative for session-owned hyperlink cleanup. DCurses remains responsible for invalidating/recovering its own retained physical knowledge.

A failed semantic transition must never leave the renderer trusting an unproven hyperlink state.

---

## 10. Performance and allocation contract

Hyperlink support must not make ordinary non-hyperlinked workloads materially worse without evidence and an explicit tradeoff decision.

At minimum, T1102/T1107 should measure:

- default blank/ordinary cell size implications of the selected representation;
- 160×60 ordinary full-screen repaint with no metadata;
- 1,000 sparse ordinary updates with no metadata;
- repeated no-op refresh with no metadata;
- a large approximately 0.5M-cell pad with no metadata;
- a large pad with sparse hyperlink spans;
- a full row of one hyperlink versus many distinct hyperlinks;
- mixed style transitions inside one hyperlink span;
- repeated hyperlink/unlinked transitions.

Correctness gates should remain deterministic. Allocation and elapsed-time results may be observational unless a stable deterministic threshold can be justified.

At least one gate should prove adjacent linked text is coalesced into semantic runs rather than one Terminal hyperlink transaction per cell.

---

## 11. Application-shaped acceptance

Representative acceptance workloads should include:

### Editor-like content

A file/document view containing:

- ordinary text;
- file/URL hyperlinks;
- mixed linked/unlinked spans;
- selections/styles independent of hyperlink meaning;
- insertion/deletion before and inside linked spans;
- wide Unicode inside linked text.

### Pager/help content

A help page with multiple links, scrolling, line deletion/insertion, and viewport changes.

### Large pad

Sparse hyperlinks on a large logical document presented through independently panned viewports.

### Full-screen interactive session

At least one real Terminal-backed acceptance should combine:

- linked text;
- rich input;
- synchronized output enabled and disabled;
- resize;
- suspend/resume where supported;
- repeated refresh;
- deterministic disposal/restoration.

The acceptance must prove DCurses adds semantic-retained behavior rather than merely calling `TerminalSession.WriteHyperlinkAsync(...)` in a sample.

---

## 12. Development sequence

```text
T1101  contract/reference/version-policy freeze
  -> T1102  semantic metadata representation + memory gate
  -> T1103  hyperlink value/public write/read contract
  -> T1104  retained physical hyperlink renderer + Terminal composition
  -> T1105  editing/copy/overlay/pad/viewports semantic propagation
  -> T1106  lifecycle/failure/cancellation/recovery hardening
  -> T1107  application-shaped performance/allocation acceptance
  -> T1108  public API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## 13. T1101 — contract, reference, and release-policy freeze

### Objectives

- confirm the stable 1.0 API fingerprint and semantic compatibility floor;
- review Terminal's current hyperlink ownership contract and package version dependency;
- freeze the rule that DCurses never emits OSC 8 directly;
- decide the 1.1 package/assembly versioning policy;
- define semantic-metadata invariants before adding public types;
- establish benchmark/memory baselines for current `CursesCell`, screens, and pads;
- create the first 1.1 permanent design record.

### Version checkpoint

This planning PR does **not** change package/version metadata.

When implementation begins, the recommended first checkpoint is:

```text
Version         1.1.0-alpha.1
PackageVersion  1.1.0-alpha.1
AssemblyVersion 1.0.0.0   (recommended; must be explicitly ratified)
```

### Exit criteria

No public metadata type is accepted until the representation/memory questions for T1102 are explicit and testable.

---

## 14. T1102 — semantic metadata representation and memory gate

### Objectives

- prototype the viable metadata storage models;
- measure ordinary-cell and large-pad consequences;
- choose one representation;
- define immutable/default equality semantics;
- prove no per-cell heap allocation occurs for ordinary cells;
- prove metadata sharing/interning behavior if applicable;
- define wide-cell leader/continuation metadata invariants;
- define how standalone `CursesCell` inspection represents semantics.

### Exit criteria

The selected design must have a clear explanation for:

- memory cost;
- equality/hash behavior;
- copy across surfaces;
- editing/scroll behavior;
- public inspectability;
- future semantic extensibility.

---

## 15. T1103 — hyperlink value and public content contract

### Objectives

- add the minimal public semantic metadata/hyperlink types;
- validate URI/identifier inputs consistently with Terminal's reviewed contract;
- add metadata-aware text-writing operations;
- expose logical metadata inspection without leaking Terminal types;
- add XML documentation and focused unit tests;
- capture the intentional public API delta but do not yet declare stable release freeze.

### Non-goals

- generic raw OSC 8 writer;
- public Terminal hyperlink lease;
- browser/URI activation;
- terminal-brand support inference;
- arbitrary metadata dictionaries.

---

## 16. T1104 — retained physical hyperlink rendering

### Objectives

- add retained physical hyperlink knowledge;
- coalesce adjacent equivalent hyperlink spans;
- compose hyperlink transitions with cursor/rendition output;
- use Terminal's typed hyperlink ownership rather than raw output;
- audit deadlock/order interactions among DCurses terminal activity, Terminal semantic output serialization, synchronized-output leases, and hyperlink leases;
- invalidate physical semantic state after uncertain output;
- add deterministic emitted-operation tests.

### Exit criteria

A no-change linked refresh emits no redundant hyperlink transition, while a semantic-only hyperlink change updates the terminal even if visible glyph/style content is unchanged.

---

## 17. T1105 — editing, composition, pads, and viewports

### Objectives

Prove semantic metadata survives or is removed correctly through every content-transforming operation used by the stable core.

Required tests cover:

- overwrite;
- insert/delete cells;
- insert/delete lines;
- scrolling;
- clear/erase;
- copy/overlay;
- resize;
- wide-cell clipping/repair;
- windows/subwindows;
- pads and multiple viewports.

### Exit criteria

No edit may leave stale hyperlink semantics behind after the associated logical content is gone.

---

## 18. T1106 — lifecycle, failure, cancellation, and recovery

### Objectives

- failure-inject hyperlink begin/content/end transitions;
- preserve independently meaningful primary/restoration failures;
- prove later safe repaint after semantic-state uncertainty;
- prove suspend/resume composition with Terminal-owned hyperlink state;
- prove disposal remains authoritative;
- prove caller cancellation does not leave ghost logical/physical hyperlink ownership;
- keep retained physical state conservative after any ambiguous failure.

### Exit criteria

The established 1.0 restoration/hardening guarantees remain intact with hyperlink semantics active.

---

## 19. T1107 — acceptance, performance, and allocation pressure

### Objectives

- run editor-like linked-content acceptance;
- run pager/help scrolling acceptance;
- run large-pad sparse-link acceptance;
- run high-frequency/no-op/sparse ordinary refresh regressions;
- measure ordinary-cell/large-pad memory impact;
- prove hyperlink run coalescing;
- exercise mixed hyperlink/style/Unicode/wide-cell content;
- exercise synchronized-output composition.

### Exit criteria

No significant ordinary-workload regression remains unexplained, and linked workloads demonstrate semantic-run output rather than per-cell protocol churn.

---

## 20. T1108 — public API, package, documentation, and regret gate

### Objectives

- regenerate the compiled public API fingerprint for `net8.0`, `net9.0`, and `net10.0`;
- review every intentional public addition for naming, nullability, equality, validation, and future extensibility regret;
- verify no new Terminal/TermInfo type leaked into the public signature boundary without explicit approval;
- add fresh NuGet-only consumer coverage for the new hyperlink/metadata API;
- verify XML documentation for every new public member;
- update README, samples, current roadmap index, compatibility/migration guidance, and package release notes;
- perform a documentation audit before RC promotion.

### Exit criteria

The public delta is intentional, minimal, documented, package-consumable, and stable enough for the 1.1 compatibility commitment.

---

## 21. T1109 — release candidate and stable 1.1.0 closure

### Candidate sequence

1. promote the accepted contract to `1.1.0-rc.1`;
2. run Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer validation on one exact RC SHA;
3. correct only release-blocking findings;
4. promote the unchanged accepted contract to stable `1.1.0`;
5. perform the final documentation/status audit;
6. run the same exact stable-source gate;
7. record the final tested SHA in PR metadata without moving the branch;
8. leave merge/tag/publication as explicit later actions.

### Stable release gate

Stable 1.1 must pass:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- exact package/symbol/XML/dependency/fresh-consumer validation.

---

## 22. Explicit 1.1 non-goals

Version 1.1 does not require:

- panels/layers/z-order — planned for 1.2;
- layout managers — planned for 1.3;
- focus/key-binding/hit-test infrastructure — planned for 1.4;
- Sixel;
- Kitty Graphics;
- generic raster APIs;
- terminal pixel-geometry public API unless required by an accepted 1.1 feature (not expected);
- widget controls;
- native `ncurses` ABI/source compatibility;
- generic raw OSC/CSI/DCS/APC writers;
- notification/shell-integration wrappers which add no curses-level meaning.

---

## 23. Definition of success

`Icod.DCurses 1.1.0` succeeds when semantic meaning can be attached to retained screen content and preserved through the full curses composition/editing/lifecycle model, with OSC 8 handled entirely by Terminal's typed ownership layer, while ordinary unlinked workloads retain the performance and memory characteristics expected from the stable 1.0 core.
