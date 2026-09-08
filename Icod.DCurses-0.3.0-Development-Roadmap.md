# Icod.DCurses 0.3.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Stable baseline:** `0.2.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Unicode and terminal-cell contract  
**Status:** Active

---

## 1. Release Objective

`Icod.DCurses 0.3.0` SHALL freeze the text-element and terminal-column semantics
that every later editing, copy, overlay, pad, and viewport API will depend on.

The `0.2.0` screen model already has useful foundations:

- Unicode text is written through `CursesWindow` rather than as raw UTF-16
  code units;
- text-element iteration is already used for string writes;
- the width policy is injectable through `ICursesTextWidthProvider`;
- wide cells use a leader plus continuation-cell representation;
- overwriting either half of a wide cell repairs the old footprint;
- clip mode does not write half of a two-column cell;
- malformed UTF-16 has replacement-character behavior.

The remaining problem is that the default Unicode width policy is deliberately
conservative and still makes important decisions from the first scalar value of
a text element. That is insufficient as the permanent basis for later editing
APIs.

`0.3.0` therefore establishes a single, testable contract for:

- malformed UTF-16 normalization;
- extended grapheme-cluster segmentation appropriate for terminal-cell text;
- zero-, one-, and two-column measurement;
- combining sequences;
- emoji presentation selectors;
- emoji ZWJ sequences;
- regional-indicator flags;
- keycap sequences;
- wide-cell continuation invariants;
- overwrite and clipping repair;
- East Asian Ambiguous-width policy;
- public column-oriented measurement and slicing helpers;
- versioned Unicode policy/data ownership.

This release is about terminal-cell correctness. It is not a general shaping or
layout engine.

---

## 2. Architectural Rules

### 2.1 One text-element pipeline

All ordinary string-writing and column-helper APIs SHALL share one normalization,
segmentation, and measurement pipeline.

A `CursesWindow` write, a public `MeasureColumns` helper, and a later pad/window
editing primitive SHALL NOT disagree about where a text element begins or how
many terminal columns it occupies.

### 2.2 Normalize before segmentation

Malformed UTF-16 SHALL be converted deterministically to Unicode replacement
characters before text-element segmentation.

This avoids allowing malformed surrogate boundaries to influence grapheme
segmentation differently from the content ultimately stored in a `CursesCell`.

### 2.3 Width is a property of the complete text element

The default provider SHALL inspect the complete text element rather than only
its first scalar value.

This is required for sequences whose presentation width depends on later
scalars, including:

- variation selectors;
- keycap combining marks;
- emoji ZWJ sequences;
- regional-indicator pairs;
- combining sequences.

### 2.4 Width domain remains bounded

`ICursesTextWidthProvider.GetWidth(...)` SHALL continue to return only:

```text
0   non-spacing element which may attach to preceding visible content
1   ordinary terminal column
2   wide terminal element
```

The screen model does not support terminal elements wider than two columns in
`0.3.0`.

### 2.5 Ambiguous width is explicit

East Asian Ambiguous characters SHALL have an explicit policy. The default
managed policy SHALL be **narrow (one column)** because that is the safer
cross-platform default outside a known CJK-wide environment.

A caller that explicitly needs wide ambiguous characters SHALL be able to
select that policy without replacing the complete Unicode algorithm.

### 2.6 No font shaping

`0.3.0` SHALL NOT implement:

- bidirectional paragraph layout;
- Arabic joining/shaping;
- Indic shaping;
- OpenType shaping;
- font-specific glyph measurement;
- terminal emulation.

Those concerns are outside a curses cell library and generally belong to the
terminal or renderer.

---

## 3. Development Sequence

```text
T301  0.3 package/version and Unicode contract foundation
  -> T302  normalized grapheme/text-element pipeline
  -> T303  complete default width policy and ambiguous-width mode
  -> T304  public column-oriented text helpers
  -> T305  window/cell invariant hardening
  -> T306  Unicode conformance and consumer acceptance
  -> T307  public API/documentation/package regret gate
  -> T308  stable 0.3.0 closure
```

Each tranche SHOULD receive an alpha or release-candidate checkpoint when it
changes a meaningful part of the contract.

---

# 4. T301 — Package and Unicode Contract Foundation

T301 starts the active release line.

Required work:

- set `<Version>` and `<PackageVersion>` to `0.3.0-alpha.1`;
- set `<AssemblyVersion>` to `0.3.0.0`;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- update active repository status to identify the Unicode/cell tranche;
- establish cluster-level regression tests before changing the default width
  provider;
- document the default East Asian Ambiguous-width direction as narrow.

**Gate T301:** the repository builds, tests, packs, and validates as
`0.3.0-alpha.1` with no dependency-boundary change.

---

# 5. T302 — Normalized Grapheme/Text-Element Pipeline

T302 SHALL centralize the path from .NET strings to terminal text elements.

Required behavior:

1. malformed UTF-16 is normalized to `U+FFFD` before segmentation;
2. text-element segmentation uses one implementation throughout DCurses;
3. ordinary `CursesWindow.Write(string)` consumes the normalized text elements;
4. supplementary scalars are never split as independent UTF-16 cells;
5. combining sequences remain one logical text element when the Unicode
   segmentation rules group them;
6. emoji ZWJ sequences, flags, keycaps, and variation-selector sequences remain
   intact as text elements when the runtime Unicode segmentation rules group
   them;
7. control-character handling remains explicit at the window-writing layer so
   carriage return, newline, and tab keep their existing curses semantics.

The implementation SHOULD leverage the runtime's Unicode grapheme segmentation
where it is suitable rather than maintaining a second private grapheme-break
algorithm without need.

**Gate T302:** segmentation/normalization tests cover ASCII, supplementary
scalars, combining marks, malformed surrogate input, regional-indicator flags,
keycaps, emoji presentation selectors, and representative ZWJ sequences.

---

# 6. T303 — Default Unicode Width Policy

T303 SHALL replace the old first-scalar-centric width decision with a
cluster-aware policy.

The default provider SHALL distinguish at least:

- zero-width combining/format-only elements;
- ordinary narrow text;
- East Asian wide/fullwidth text;
- emoji-presentation sequences;
- keycap sequences;
- regional-indicator flag sequences;
- emoji ZWJ sequences;
- text-presentation (`VS15`) versus emoji-presentation (`VS16`) requests where
  terminal-cell semantics are well defined;
- East Asian Ambiguous characters under the selected policy.

The provider SHALL expose or internally identify the Unicode policy/data version
used by the algorithm so a future table update is intentional and reviewable.

### 6.1 Ambiguous-width policy

Introduce a small semantic policy such as:

```text
CursesAmbiguousWidth.Narrow
CursesAmbiguousWidth.Wide
```

The default `UnicodeCursesTextWidthProvider.Instance` remains the narrow policy.
An explicitly wide provider SHALL be available without requiring applications to
implement `ICursesTextWidthProvider` themselves.

### 6.2 Data strategy

The implementation SHOULD prefer generated/versioned tables or an equivalent
reviewable Unicode-data mechanism over an ever-growing hand-maintained chain of
ad hoc ranges.

Generated data checked into the repository is acceptable. Runtime downloads are
not.

**Gate T303:** the default provider has deterministic tests for every listed
sequence family and for both ambiguous-width modes.

---

# 7. T304 — Public Column-Oriented Text Helpers

T304 SHALL expose the same text semantics to applications without requiring a
throwaway screen/window.

Candidate managed surface:

```text
CursesText.MeasureColumns(...)
CursesText.TruncateToColumns(...)
CursesText.SliceByColumns(...)
```

Exact naming is subject to API review, but the helpers SHALL:

- use the same normalization/segmentation/width provider contract as windows;
- never split a Unicode text element;
- never return half of a two-column element;
- define behavior when a requested slice boundary falls inside a wide element;
- define whether zero-width leading elements are retained, attached, or omitted;
- reject or explicitly handle terminal control characters rather than measuring
  them as printable columns;
- support a caller-supplied `ICursesTextWidthProvider` where appropriate.

Truncation and slicing SHALL be column-based, not UTF-16-index based.

**Gate T304:** helper results agree exactly with `CursesWindow` placement for the
same printable input and width provider.

---

# 8. T305 — Window and Cell Invariant Hardening

T305 SHALL make later editing APIs safe to build on the current cell model.

Required invariants:

- a continuation cell is never orphaned after a public window operation;
- every two-column leader owns exactly one following continuation cell;
- overwriting a leader repairs its continuation;
- overwriting a continuation repairs its leader;
- writing a wide element over an existing wide footprint repairs both old
  footprints correctly;
- clipping at the right edge never stores a partial element;
- wrapping moves a complete wide element to the next row;
- combining/zero-width content never attaches to a continuation cell as though
  it were a leader;
- resize preservation does not retain invalid continuation state at a clipped
  right boundary;
- copying/resizing internal storage in preparation for later releases maintains
  valid cell footprints.

Where useful, introduce internal invariant validation that can be exercised by
tests without becoming production overhead on every cell access.

**Gate T305:** randomized and boundary-focused tests cannot produce an orphaned
continuation or mismatched leader width through the supported public operations.

---

# 9. T306 — Unicode Conformance and Consumer Acceptance

T306 SHALL expand the test corpus beyond hand-selected examples.

Required coverage SHOULD include:

- representative Unicode grapheme-break cases relevant to terminal text;
- combining-mark stacks;
- supplementary-plane text;
- emoji presentation/text-presentation pairs;
- emoji modifier sequences;
- ZWJ families and professions;
- regional-indicator flags;
- keycap sequences;
- East Asian wide/fullwidth/ambiguous samples;
- malformed UTF-16 replacement behavior;
- edge clipping and wrap cases for every two-column family;
- deterministic behavior under `net8.0`, `net9.0`, and `net10.0`.

A package-only smoke consumer SHALL compile and execute the public column helper
surface from the generated `.nupkg`.

The interactive showcase SHOULD gain a Unicode diagnostics section useful for
visual checking, while automated correctness remains fixture-driven.

**Gate T306:** the conformance corpus and package-only consumer are green on the
supported framework/OS matrix.

---

# 10. T307 — Public API, Documentation, and Package Regret Gate

Before stable `0.3.0`:

- review every new Unicode/text public type and member;
- verify existing `0.1`/`0.2` public contracts remain intentionally compatible;
- freeze numeric values for any new public enums;
- document the Unicode data/policy version;
- document the default ambiguous-width choice;
- document malformed-input semantics;
- document column slicing/truncation boundary behavior;
- update README and samples;
- update package-only smoke validation;
- verify no new direct package dependency was introduced unless explicitly
  approved;
- rerun Staging validation on Windows, Linux, and macOS;
- validate the canonical package candidate and fresh consumers.

**Gate T307:** the complete Unicode/terminal-cell contract is intentional,
documented, package-consumable, and cross-platform green.

---

# 11. T308 — Stable 0.3.0 Closure

T308 is release closure only.

Required work:

- promote a green release candidate to `0.3.0`;
- retain assembly/version policy consistently;
- update package release notes;
- freeze a `0.3` public API baseline;
- merge the stable source commit to `main`;
- require the six-runner `main` Release matrix to pass;
- create `v0.3.0` only after the matching main commit is green;
- publish through the tag-controlled release workflow;
- verify NuGet.org, GitHub Packages, GitHub Release assets, symbols, and
  checksums.

**0.3.0 completion criterion:** every later DCurses editing or viewport API can
rely on one stable definition of Unicode text elements, terminal columns, wide
cell footprints, malformed text, and column-safe boundaries.

---

## 12. Explicit 0.3 Non-Goals

`0.3.0` does not include:

- general window insertion/deletion/copy/overlay APIs (`0.4.0`);
- pads or large virtual surfaces (`0.5.0`);
- new rendition families (`0.6.0`);
- synchronized-output/refresh-cost optimization (`0.7.0`);
- bidirectional layout;
- Arabic/Indic/OpenType shaping;
- font measurement;
- terminal emulation;
- PTY/process hosting.

The release should be narrow but foundational: define terminal text correctly
first, then build richer editing primitives on top of it.
