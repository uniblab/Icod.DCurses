# T1103 — Hyperlink Value and Public Logical Metadata Contract

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1103  
**Development checkpoint:** `1.1.0-alpha.3`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** logical/API checkpoint implemented and code-qualified; documentation-complete gate pending

## Purpose

T1103 is the first intentional public API expansion after the stable 1.0 contract. It defines the DCurses-native semantic values and logical screen/window operations required for retained hyperlinks while deliberately stopping short of physical OSC 8 emission.

The architectural split remains:

```text
visual rendition     -> CursesStyle
semantic meaning     -> CursesCellMetadata / CursesHyperlink
wire protocol/state  -> Icod.Terminal
```

`Icod.DCurses` stores application semantic intent. `Icod.Terminal` remains the typed OSC 8 protocol, ordering, lease, lifecycle, and terminal-state authority.

## Public API added by T1103

Two new exported types are introduced:

```text
CursesHyperlink
CursesCellMetadata
```

The logical surface/window API gains metadata inspection/mutation and metadata-aware write operations:

```text
CursesVirtualScreen.GetMetadata(...)
CursesVirtualScreen.SetMetadata(...)

CursesWindow.GetMetadata(...)
CursesWindow.SetMetadata(...)
CursesWindow.Write(string, CursesCellMetadata)
CursesWindow.Write(string, CursesStyle, CursesCellMetadata)
CursesWindow.WriteCell(CursesCell, CursesCellMetadata)
```

No Terminal hyperlink lease or other new Terminal/TermInfo type enters the public DCurses signature set.

## Hyperlink value contract

`CursesHyperlink` is an immutable DCurses-native value describing:

- one non-empty absolute hyperlink target;
- an optional OSC 8 identifier.

The target and identifier validation intentionally mirrors Terminal's reviewed hyperlink contract:

- the target must contain a valid absolute URI scheme;
- the target is already URI-encoded caller data;
- invalid percent escapes are rejected;
- percent-escape hex digits are canonicalized to uppercase;
- non-ASCII unescaped URI characters are rejected;
- the encoded target is bounded to Terminal's 2083-byte contract;
- null or empty identifiers canonicalize to no identifier;
- non-empty identifiers are bounded to 128 bytes and contain only RFC 3986 unreserved ASCII characters.

DCurses does not activate, dereference, download, resolve, or navigate to hyperlink targets.

## Semantic metadata value

`CursesCellMetadata` is an immutable reference value. In 1.1 its first semantic field is an optional `CursesHyperlink`.

This type is intentionally distinct from `CursesStyle`. A linked cell may change color, boldness, underline, or other rendition without changing hyperlink identity, and a hyperlink may remain unchanged while rendition changes.

The metadata type is also intentionally not embedded into `CursesCell`; T1102 selected a row-sparse sidecar representation to avoid imposing a permanent metadata reference on every ordinary retained cell.

## Logical storage behavior

`CursesVirtualScreen` owns an optional `CursesSparseCellPlane<CursesCellMetadata>`.

Properties of the implementation:

- no semantic plane exists before the first metadata value;
- only rows containing metadata allocate row storage;
- clearing the last value releases its row and eventually the complete plane;
- metadata-only changes mark the corresponding logical coordinate dirty;
- metadata-only changes participate in logical change revision tracking when enabled;
- ordinary cell replacement removes semantic metadata associated with the overwritten text-element footprint.

The stable `CursesCell` value remains context-free and independently copyable.

## Wide-cell invariant

A two-column text element is one semantic unit.

For a valid leader/continuation footprint:

- assigning metadata to the leader assigns the same metadata to the continuation coordinate;
- assigning metadata through the continuation updates the complete footprint;
- metadata-aware writes assign one coherent value across the footprint;
- ordinary replacement/repair clears metadata from the destroyed footprint.

Public inspection therefore reports the same semantic value from either coordinate of a valid wide element.

## Window semantics

Window metadata coordinates are zero-based and window-local, exactly like existing window cell operations.

Because windows are shared views over one owning logical screen:

- metadata written through a child/subwindow is immediately observable from overlapping parent/root windows;
- metadata is stored at the projected screen coordinate rather than on the window object;
- temporarily clipped valid window coordinates do not create detached semantic state.

Metadata-aware text writes reuse the existing Unicode normalization, grapheme segmentation, text-width, wrap/clip, tab, zero-width-combining, and wide-cell repair rules.

## Ordinary-write overwrite rule

Unannotated writes are explicit semantic replacement operations.

If ordinary text/cell output overwrites a coordinate carrying metadata, the overwritten semantic metadata is removed even when the resulting visible `CursesCell` value is equal to the previous value.

This rule prevents stale links from surviving a normal rewrite merely because the glyph/style is unchanged.

## Clear/fill damage correction

Qualification identified a semantic-only damage edge case: clearing/filling a surface could remove metadata while every visible `CursesCell` was already equal to the requested fill value.

The implementation now invalidates the affected logical surface whenever fill/clear removes semantic metadata. This is required so T1104 can later close or replace a physical hyperlink even when no glyph or rendition changed.

A focused regression test freezes that behavior.

## Public API fingerprint

The first compiled T1103 run intentionally used the stable 1.0 fingerprint so CI would report the exact additive delta rather than relying on a hand-authored baseline.

The compiler-derived provisional 1.1 development contract is:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

The only new exported types are:

```text
Icod.DCurses.CursesCellMetadata
Icod.DCurses.CursesHyperlink
```

`docs/Public-API-Fingerprint-1.1.json` records this provisional development baseline. It is allowed to evolve deliberately before T1108; it is not yet the stable 1.1 freeze.

Historical/stable fingerprint files remain unchanged:

- `docs/Public-API-Fingerprint-0.9.json` — historical pre-1.0 freeze;
- `docs/Public-API-Fingerprint-1.0.json` — stable 1.0 contract.

`PublicStableApiBaselineTests` continues independently to prove that the stable 1.0 contract exactly matches the 0.9 freeze.

## Qualification corrections

Two non-design defects were found while qualifying alpha.3:

1. `CursesWindow.Metadata.cs` initially omitted the `Icod.DCurses.Internal` namespace needed for the existing `CursesUnicodeText` pipeline. The missing import was added; no duplicate Unicode path was introduced.
2. `Fill(...)`/`Clear()` initially removed the semantic plane before recording semantic-only damage. The operation now invalidates when semantic metadata existed, including visually no-op fills.

## Code/API checkpoint

Exact head:

```text
b2c1e7c18c1de929e705f119794d7eb5f6d6073b
```

Workflow #469 (`34403872794`) passed on that exact SHA across:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

The documentation-complete alpha.3 head must receive the same full gate before T1103 is marked complete and T1104 begins.

## T1103 non-goals

T1103 does not yet implement:

- physical OSC 8 output;
- retained physical hyperlink knowledge;
- link-run coalescing;
- editing/copy/overlay/pad metadata propagation beyond the logical write/overwrite foundation;
- hyperlink lifecycle/failure recovery;
- browser/navigation behavior;
- raw OSC 8 framing.

Those belong to T1104–T1107.

## Exit criteria

T1103 is complete when one documentation-synchronized `1.1.0-alpha.3` head passes the normal seven-job PR matrix and proves:

1. the new logical semantic API is additive over the 1.0 compatibility floor;
2. `CursesHyperlink` validation remains Terminal-compatible without exposing Terminal protocol types;
3. semantic-only changes participate in damage tracking;
4. wide text footprints carry coherent metadata;
5. ordinary replacement clears overwritten semantics;
6. the provisional 45-type / 337-line 1.1 fingerprint is enforced;
7. package metadata and documentation agree on Terminal 1.6.0 / TermInfo 1.10.0;
8. no raw OSC 8 output has entered DCurses.

After that, T1104 may add retained physical hyperlink rendering through Terminal's typed hyperlink ownership API.
