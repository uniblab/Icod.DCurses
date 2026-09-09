# T1103 — Hyperlink and Public Semantic Metadata Contract

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1103  
**Development checkpoint:** `1.1.0-alpha.3`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Predecessor:** T1102 row-sparse semantic metadata representation  
**Status:** implementation staged; exact-head validation in progress

## Purpose

T1103 introduces the first intentional public API delta after the stable 1.0 contract.

The release establishes a small curses-level semantic-content vocabulary without exposing Terminal protocol types and without moving hyperlink state into `CursesStyle` or `CursesCell`.

The layer split remains:

```text
CursesStyle            visual rendition
CursesCell             retained text/cell value
CursesCellMetadata     retained semantic meaning
CursesHyperlink        hyperlink semantic value
Icod.Terminal          OSC 8 protocol/output/lifecycle ownership
```

T1103 is logical-only. Physical OSC 8 rendering begins in T1104.

## Public values

### `CursesHyperlink`

`CursesHyperlink` is an immutable DCurses-native semantic value containing:

```text
Uri
Identifier?
```

The public string contract intentionally follows the reviewed Terminal OSC 8 contract:

- the target is non-empty;
- the target is absolute;
- the target is already URI-encoded printable ASCII;
- malformed percent escapes are rejected;
- percent escapes are canonicalized to uppercase hexadecimal;
- the target is bounded to 2,083 ASCII bytes;
- the optional identifier is bounded to 128 ASCII bytes;
- the identifier permits only RFC 3986 unreserved ASCII characters;
- null and empty identifiers canonicalize to no identifier.

DCurses does not dereference, activate, fetch, navigate to, or otherwise interpret the target as an application action.

Terminal remains authoritative at physical output time and revalidates the semantic value before OSC 8 emission.

### `CursesCellMetadata`

`CursesCellMetadata` is an immutable reference value separate from `CursesStyle`.

Version 1.1 exposes one metadata kind:

```text
Hyperlink
```

The separate metadata container gives later 1.x releases room to introduce additional reviewed semantic kinds without turning `CursesStyle` into a mixed presentation/meaning object and without adding an untyped property dictionary.

## Surface ownership

Metadata is stored by `CursesVirtualScreen` through the row-sparse reference plane selected by T1102.

It is therefore surface-owned rather than embedded in a detached `CursesCell` value.

Public inspection/mutation is available through both logical surfaces and windows:

```text
CursesVirtualScreen.GetMetadata(row, column)
CursesVirtualScreen.SetMetadata(row, column, metadata)

CursesWindow.GetMetadata(row, column)
CursesWindow.SetMetadata(row, column, metadata)
```

Window coordinates remain zero-based and local. Subwindows map through the same parent-relative projection rules as ordinary cells.

A valid window coordinate temporarily clipped outside the owning screen returns no metadata and ignores metadata mutation, matching the existing clipped-window storage model.

## Metadata-aware writes

T1103 adds the minimal write surface:

```text
CursesWindow.Write(string text, CursesCellMetadata metadata)
CursesWindow.Write(string text, CursesStyle style, CursesCellMetadata metadata)
CursesWindow.WriteCell(CursesCell cell, CursesCellMetadata metadata)
```

The text pipeline retains the existing DCurses behavior for:

- grapheme/text-element segmentation;
- malformed UTF-16 normalization through the existing Unicode pipeline;
- configured terminal-column width policy;
- one- and two-column content;
- wrapping and clipping;
- carriage return, line feed, and tab handling;
- zero-width text appended to the preceding logical element.

Tabs written through a metadata-aware string write associate the same metadata with the generated spaces.

## Ordinary writes clear overwritten semantics

An unannotated ordinary cell replacement removes semantic metadata from the logical text-element footprint it replaces.

This is intentional. Once metadata exists, writing the same visible glyph/style again without metadata is a semantic change even when the visible `CursesCell` value compares equal.

The metadata removal therefore participates in dirty/change tracking so T1104 can later observe and physically close or replace a hyperlink even when glyph/style content did not change.

Callers that want the replacement content to remain linked use the metadata-aware write overload.

## Wide-cell invariant

A valid two-column text element is one semantic unit.

T1103 freezes these rules:

- a metadata-aware two-column write associates the same immutable metadata value with leader and continuation coordinates;
- setting metadata through either coordinate of a valid two-column footprint updates both coordinates;
- ordinary replacement of either half removes metadata from the old footprint coherently;
- no caller may create conflicting hyperlink values on the leader and continuation of a valid wide element through the public metadata API.

The row-sparse plane may physically repeat the same immutable metadata reference in both coordinates. This is deliberate and keeps O(1) coordinate inspection while preserving one semantic value.

## Damage/change tracking

Metadata-only changes are logical changes.

`CursesVirtualScreen.SetMetadata(...)` records per-cell change revision and dirty state when the semantic value changes, including:

- adding metadata;
- replacing metadata;
- removing metadata.

Setting an equal metadata value is a no-op.

This is required because T1104's retained physical renderer must detect:

```text
same glyph/style, different hyperlink
same glyph/style, hyperlink removed
same glyph/style, hyperlink added
```

without forcing applications to mutate visible text merely to trigger output.

## Intentional limits of T1103

T1103 does not yet qualify metadata propagation through every existing content-transforming operation.

Those transformations remain the dedicated T1105 work:

- insert/delete cells;
- insert/delete lines;
- scroll operations;
- copy/overlay rectangles;
- pad/viewports;
- resize preservation;
- wide-footprint repair at transform boundaries.

Until T1105 is complete, the public semantic value/write/inspection contract is considered an alpha development surface rather than the stable 1.1 release contract.

T1103 also does not add:

- physical hyperlink output;
- raw OSC 8 framing;
- `TerminalHyperlinkLease` exposure;
- hyperlink capability inference from terminal brand or `TERM`;
- URI activation;
- arbitrary metadata dictionaries;
- generic OSC/CSI/DCS/APC writing.

## Public API fingerprint policy

The stable 0.9/1.0 fingerprint files remain historical and unchanged.

Because T1103 intentionally adds public API, the first alpha.3 validation run is allowed to report the compiler-derived mismatch against the stable 1.0 baseline. That output is used to create the provisional 1.1 development fingerprint.

T1108 remains the final public API/package/documentation regret gate. The provisional alpha fingerprint is not itself a stable-release promise.

## Validation

Focused T1103 tests cover:

- URI percent-escape canonicalization;
- relative/non-ASCII/malformed hyperlink rejection;
- identifier grammar;
- metadata visibility through shared subwindows;
- coherent two-column metadata footprints;
- setting metadata through a continuation coordinate;
- ordinary rewrite removing metadata even when visible cell content is unchanged;
- semantic-only dirty tracking;
- metadata removal and sparse-plane release.

The exact alpha.3 head must pass the full Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer matrix after the provisional 1.1 fingerprint is established.

## Exit criteria

T1103 is complete when one exact `1.1.0-alpha.3` head proves:

1. the new public value and window/surface APIs compile with complete XML documentation;
2. no Terminal/TermInfo implementation type is added to the public signature surface;
3. logical hyperlink metadata is independent of `CursesStyle` and `CursesCell` storage;
4. ordinary replacement removes stale semantics;
5. metadata-only changes participate in damage tracking;
6. wide text carries one coherent semantic value;
7. the intentional public delta is captured by a provisional 1.1 fingerprint;
8. the complete PR matrix is green.

T1104 may then add retained physical hyperlink state and compose it through Terminal's typed OSC 8 ownership API.
