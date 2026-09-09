# Icod.DCurses 1.1.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Stable compatibility floor:** `1.0.0`  
**Post-1.0 baseline commit:** `d3ff96ad57fd58a046135ca989ecccdda501d08f`  
**Development checkpoint:** `1.1.0-alpha.3`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** semantic cell metadata and retained hyperlinks  
**Status:** T1101–T1102 complete; T1103 code/API checkpoint green, documentation-complete alpha.3 gate active

---

## 1. Release objective

`Icod.DCurses 1.1.0` is the first additive release after the stable 1.0 contract. It adds non-visual semantic information to retained terminal content, beginning with hyperlinks.

The governing separation is:

```text
what a cell looks like       -> CursesStyle
what a cell means            -> semantic metadata
how terminal protocols emit
that meaning                 -> Icod.Terminal
```

Hyperlinks are the first semantic metadata kind because Terminal already provides reviewed, typed, session-owned OSC 8 operations with nesting, lifecycle replay, output serialization, and cleanup.

The release must add that semantic capability without:

- turning `CursesStyle` into an untyped bag of meaning;
- constructing raw OSC 8 inside DCurses;
- exposing Terminal hyperlink leases through the curses API;
- weakening Unicode/wide-cell/editing/pad invariants;
- creating a second terminal input or output-serialization domain;
- imposing an unjustified permanent memory cost on ordinary cells and large pads.

---

## 2. Stable compatibility floor

The frozen 1.0 compiled API remains the compatibility floor:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

Version 1.1 is additive by default. No existing type/member is removed, renamed, repurposed, or assigned a different enum value merely to accommodate metadata.

T1101 ratified the compatible 1.x assembly policy:

```text
NuGet/package version  advances through compatible 1.x releases
AssemblyVersion        remains 1.0.0.0
```

A breaking compatibility decision may revisit assembly identity explicitly; it must never drift incidentally.

---

## 3. Terminal ownership boundary

The active dependency floor is Terminal 1.6.0.

Terminal 1.6 strengthens its complete CSI/parser/query and internal pixel-geometry foundation without changing the public contract required by the 1.1 hyperlink design. Hyperlinks continue to rely on the already-stable typed OSC 8 operations:

```text
TerminalSession.AcquireHyperlinkAsync(...)
TerminalSession.WriteHyperlinkAsync(...)
```

DCurses may compose these operations internally, but it must not:

- emit raw OSC 8 framing;
- introduce an independent OSC parser/writer;
- expose `TerminalHyperlinkLease` publicly;
- infer protocol support from terminal brand or `TERM` alone;
- create a second live terminal reader;
- bypass Terminal's output ordering and lifecycle ownership.

DCurses owns retained semantic intent. Terminal owns the wire protocol and live terminal state.

---

## 4. Semantic metadata representation — T1102 accepted

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

The accepted representation is therefore a lazily allocated **row-sparse metadata reference plane**:

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

- no semantic plane allocation for an ordinary surface with no metadata;
- top-level row references allocated only after the first semantic value;
- per-row reference storage allocated only for rows containing metadata;
- empty rows and eventually the entire plane are released;
- O(1) coordinate lookup;
- detached row snapshot/replace mechanics fit the existing editing model;
- immutable metadata references may be shared across surfaces;
- `CursesCell` remains a standalone, context-free public value.

The internal foundation is `CursesSparseCellPlane<T>`.

---

## 5. Memory and scale contract

The existing large-pad reference remains:

```text
2048 × 256 = 524,288 logical cells
```

The representation gate freezes the cost shape rather than a public/private ABI size.

T1107 must preserve or explain behavior for:

- 160×60 ordinary full-screen repaint;
- 1,000 sparse ordinary updates;
- repeated no-op refresh;
- large pads with no metadata;
- large pads with sparse links;
- one large linked run versus many distinct links;
- mixed link/style/Unicode transitions.

---

## 6. T1103 public API contract

T1103 introduces the minimum DCurses-native semantic surface:

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

The release does **not** introduce:

- a generic `Dictionary<string, object>` metadata bag;
- arbitrary protocol payloads;
- browser/navigation behavior;
- Terminal protocol types in ordinary public signatures.

---

## 7. Hyperlink value rules

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

Successful refresh will mean that DCurses successfully asked Terminal to emit the semantic state; it will not imply that the terminal displayed or permitted activation of the link.

---

## 8. Retained physical semantic state — T1104

The physical renderer must distinguish at least:

```text
same cells / same style / same hyperlink
same cells / same style / different hyperlink
same cells / different style / same hyperlink
unknown physical hyperlink state
```

Adjacent equivalent links should be emitted as semantic runs, not one begin/end transaction per cell.

A conceptual transaction is:

```text
move cursor
set rendition
open hyperlink A
write linked run
change rendition while link A remains active
write more linked content
close or replace hyperlink A
write unlinked content
```

The implementation must use Terminal's typed hyperlink ownership and compose with DCurses' terminal-activity gate and optional synchronized-output lease.

No raw OSC 8 bytes belong in the refresh engine.

---

## 9. Editing and composition invariants

Semantic metadata must move with content wherever content moves.

Coverage must include:

- write/overwrite;
- clear/erase/fill;
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

For a wide text element, the leading cell and continuation footprint represent one semantic unit. Destruction or clipping of that element must remove/repair semantic metadata coherently.

T1103 already freezes the initial wide-footprint rule: assigning metadata through either valid coordinate applies one coherent metadata value across the leader/continuation footprint, and ordinary overwrite clears stale semantic state.

Copy/overlay blank/transparency behavior must still be explicitly specified in T1105 rather than inferred.

---

## 10. Lifecycle, cancellation, and failure rules

The 1.0 hardening model remains authoritative.

Required failure coverage includes:

- hyperlink begin failure;
- content failure after begin;
- hyperlink close/restoration failure;
- primary refresh plus restoration failure;
- cancellation before semantic output commit;
- cancellation after a semantic transition is committed;
- `Invalidate()` while hyperlink state was believed active;
- suspend/resume with visible linked content;
- disposal with active or uncertain physical hyperlink state;
- later safe repaint after uncertainty.

Terminal owns final protocol cleanup. DCurses owns conservative invalidation of retained physical knowledge.

---

## 11. Application-shaped acceptance

### Editor-like content

Exercise ordinary text, multiple links, selections/styles independent of link meaning, edits before/inside linked spans, and wide Unicode within linked content.

### Pager/help content

Exercise multiple links, scrolling, insertion/deletion, and viewport changes.

### Large pad

Exercise sparse links in a large logical document through independently panned viewports.

### Full-screen Terminal-backed session

Combine linked text with rich input, synchronized output on/off, resize, suspend/resume where supported, repeated refresh, and deterministic disposal/restoration.

The acceptance must prove retained semantic integration, not merely call `TerminalSession.WriteHyperlinkAsync(...)` from a sample.

---

## 12. Development sequence

```text
T1101  contract/reference/version-policy freeze             complete
  -> T1102  semantic metadata representation + memory gate  complete
  -> T1103  hyperlink value/public write/read contract      code/API green; docs gate active
  -> T1104  retained physical hyperlink renderer            next
  -> T1105  editing/copy/overlay/pad/viewports propagation
  -> T1106  lifecycle/failure/cancellation/recovery
  -> T1107  application/performance/allocation acceptance
  -> T1108  public API/package/documentation/regret gate
  -> T1109  RC and stable 1.1.0 closure
```

---

## 13. T1101 — contract, reference, and version-policy freeze

### Decisions

- first implementation checkpoint is `1.1.0-alpha.1`;
- `AssemblyVersion` remains `1.0.0.0` for compatible additive 1.x releases;
- direct Terminal dependency advances to `1.6.0`;
- TermInfo remains `1.10.0`;
- stable 1.0 public API remains the compatibility floor;
- no new public semantic metadata type enters T1101;
- DCurses never emits raw OSC 8;
- metadata remains separate from `CursesStyle`;
- representation selection is deferred to measured T1102 evidence.

Permanent record:

- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`

**Status:** complete.

---

## 14. T1102 — representation and memory gate

### Accepted decisions

- reject unconditional per-cell metadata reference;
- reject surface-relative metadata token;
- select row-sparse reference-plane storage;
- freeze the portable +8-byte incremental candidate-slot cost instead of one architecture-specific `CursesCell` size;
- preserve context-free standalone `CursesCell` semantics.

Permanent record:

- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`

Qualified exact head:

```text
60fa5e1a0b17e91f7c0e78ee397ff797645471d7
```

Workflow #456 (`34400518088`) passed all six runtime architectures plus package validation.

**Status:** complete.

---

## 15. T1103 — public hyperlink/content contract

### Implemented

- immutable `CursesHyperlink`;
- immutable `CursesCellMetadata`;
- Terminal-compatible URI/identifier validation without Terminal public types;
- sparse logical metadata storage on `CursesVirtualScreen`;
- metadata-aware window text/cell writes;
- window and virtual-screen metadata inspection/mutation;
- semantic-only damage tracking;
- coherent two-column metadata footprints;
- ordinary overwrite clears stale metadata;
- clear/fill semantic removal invalidates even when visible cells are unchanged.

The first compiled public delta was intentionally observed against the stable 1.0 fingerprint. The provisional 1.1 contract is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

Development fingerprint:

- `docs/Public-API-Fingerprint-1.1.json`

Permanent tranche record:

- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`

Code/API-qualified exact head:

```text
b2c1e7c18c1de929e705f119794d7eb5f6d6073b
```

Workflow #469 (`34403872794`) passed all six runtime architectures plus package validation.

### Exit gate

One documentation-synchronized `1.1.0-alpha.3` head must pass the same seven-job matrix. After that T1103 is complete and T1104 may begin.

---

## 16. T1104 — retained hyperlink renderer

### Objectives

- add retained physical semantic knowledge;
- update on semantic-only changes even when glyph/style is unchanged;
- coalesce adjacent equivalent hyperlink runs;
- compose with rendition/cursor/synchronized-output behavior;
- use Terminal's typed ownership only;
- audit lock/ordering interactions;
- invalidate after uncertain semantic output.

---

## 17. T1105 — editing, copy/overlay, pads and viewports

Prove semantic metadata is preserved or removed correctly through every content-transforming operation in the stable core, including wide-cell repair and multiple pad viewports.

---

## 18. T1106 — lifecycle/failure/cancellation hardening

Failure-inject begin/content/end transitions, preserve dual failures, prove later recovery, and retain authoritative Terminal restoration through suspend/resume and disposal.

---

## 19. T1107 — acceptance/performance/allocation

Run editor, pager/help, large-pad, no-op/high-frequency, synchronized-output, mixed-style, Unicode, and linked-run-coalescing workloads. Significant ordinary-workload regressions must be explained before release continuation.

---

## 20. T1108 — API/package/documentation/regret gate

- regenerate compiled API fingerprints for net8/net9/net10;
- review naming/nullability/equality/validation/future-extensibility regret;
- verify no accidental upstream Terminal/TermInfo type leakage;
- add fresh NuGet-only consumer coverage;
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
