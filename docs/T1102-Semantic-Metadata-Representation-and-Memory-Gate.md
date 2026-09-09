# T1102 — Semantic Metadata Representation and Memory Gate

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1102  
**Development checkpoint:** `1.1.0-alpha.2`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** row-sparse reference-plane design selected; validation in progress

## Purpose

T1102 selects the storage model for post-1.0 semantic cell metadata before any public hyperlink/metadata type is frozen.

The design must balance three competing requirements:

1. semantic meaning must move with logical content through editing, copy/overlay, pads, and viewports;
2. ordinary non-semantic screens and large pads must not pay a large permanent per-cell memory tax;
3. the public `CursesCell` value contract must remain coherent and independent of one owning surface.

## Existing storage and editing observations

`CursesVirtualScreen` stores ordinary content in one dense `CursesCell[]`. The editing and composition layers snapshot, clone, normalize, and commit `CursesCell` rows and rectangles directly.

This makes an inline metadata field mechanically attractive because semantic values would move automatically with cell copies. It also makes its cost permanent: every logical cell in every screen and pad would carry the field even when the application never uses semantic metadata.

A fully interval-based sparse span structure avoids that cost but creates a different problem: every insertion, deletion, scroll, rectangle transfer, clipping operation, and wide-cell repair must also maintain interval topology. That is disproportionate complexity for the first semantic metadata kind.

## Measured representation baseline

The supported runtime matrix is 64-bit x64/ARM64. `CursesSemanticMetadataRepresentationBaselineTests` freezes the current representation evidence on that matrix:

```text
CursesCell                                   72 bytes
CursesCell + one metadata object reference  80 bytes
CursesCell + one int token wrapper           80 bytes
```

For the established large-pad reference:

```text
2048 × 256 = 524,288 cells
```

one additional eight-byte field costs:

```text
524,288 × 8 = 4,194,304 bytes = 4 MiB
```

That is paid even when the pad contains zero hyperlinks.

The exact numbers are implementation measurements rather than public ABI promises, but they make the relative cost visible and repeatable on every supported architecture.

## Why an inline metadata reference is rejected

An inline reference is simple but imposes the full four-megabyte increase on the reference large pad even when semantic metadata is unused.

It also changes the private physical layout of the public `CursesCell` struct. Although the stable public-signature fingerprint would not expose that private field directly, avoiding unnecessary layout churn in a stable public value type is preferable.

The permanent ordinary-cell cost is not justified when semantic metadata is expected to be sparse in common editor, help, browser, and diagnostic screens.

## Why a surface-relative token is rejected

A compact token looks attractive because a carefully reordered `CursesCell` implementation might be able to use existing alignment/padding more efficiently than the simple wrapper measurement.

The semantic problem is more important than the byte count:

- a token is meaningful only relative to an owning metadata table;
- a standalone public `CursesCell` would no longer fully describe its semantic value;
- copying a cell between screens or pads would require token remapping;
- equality and hashing of `CursesCell` would become context-sensitive or incomplete;
- public callers can already construct and pass `CursesCell` values independently of a screen.

A surface-relative identifier therefore conflicts with the existing standalone value semantics and is rejected for 1.1.

## Selected model: row-sparse metadata reference plane

T1102 selects a lazily allocated per-surface reference plane organized by row.

Conceptually:

```text
CursesVirtualScreen
    dense CursesCell[]
    optional semantic rows
        row 0 -> null
        row 1 -> metadata-reference array only if needed
        row 2 -> null
        ...
```

Properties of the model:

- an ordinary surface with no metadata allocates no semantic row storage;
- the top-level row-reference array is allocated only after the first semantic value;
- a metadata row is allocated only when that row gains its first semantic value;
- a row allocation is released when its last semantic value is removed;
- the complete plane is released when its last semantic value is removed;
- coordinate lookup remains O(1);
- row snapshots are straightforward for existing insertion/deletion algorithms;
- no surface-relative token appears in `CursesCell` or public API;
- immutable metadata objects may be shared by any number of cells and surfaces.

The internal generic prototype is `CursesSparseCellPlane<T>`.

## Sparse and dense reference cost

Ignoring small array-object headers and metadata objects themselves, a 2,048 × 256 reference plane has these representative payloads on a 64-bit runtime:

```text
Top-level row references                 16 KiB
10 populated semantic rows               20 KiB
Top-level + 10 populated rows             36 KiB
All 2,048 rows populated                   4 MiB
```

The design therefore makes sparse semantic use cheap while allowing dense semantic content to pay roughly the same reference payload that an inline field would have imposed unconditionally.

This is the desired cost shape: applications pay for the feature when they use it.

## Editing/composition strategy

The sidecar must not lead to two independent editing engines.

T1103/T1105 should introduce an internal paired logical snapshot concept equivalent to:

```text
cell value
semantic metadata reference
```

Editing and composition algorithms should snapshot and move the pair together. The existing wide-cell normalization remains the content authority; semantic metadata for a destroyed or repaired footprint is cleared or moved in the same commit operation.

Row-level `SnapshotRow(...)` and `ReplaceRow(...)` support in `CursesSparseCellPlane<T>` exists specifically so cell insertion/deletion and line editing can move semantic rows with the same deterministic snapshot/commit model already used by `CursesWindow`.

Rectangle copy/overlay will likewise snapshot semantic coordinates alongside cell coordinates rather than maintaining interval arithmetic.

## Public model implications

T1102 does **not** freeze the T1103 public names, but it establishes these constraints:

- `CursesCell` remains the existing visual/text cell value and does not gain a surface-relative token;
- semantic metadata is an independent immutable reference value;
- public inspection must be surface/window-aware rather than pretending a detached `CursesCell` alone carries all semantic state;
- metadata-aware write/cell operations must update cell and semantic planes atomically from the logical model's perspective;
- cross-surface copy must copy semantic values directly, with no remapping table.

Likely T1103 concepts remain equivalent to `CursesHyperlink`, `CursesCellMetadata`, and window/screen metadata-aware write/inspection operations, but exact names and overloads remain subject to the public regret review.

## Wide-cell invariant

A two-column text element is one semantic unit.

The selected implementation must ensure:

- the leader and continuation footprint cannot retain conflicting metadata;
- writing a wide element applies one semantic value coherently across its footprint;
- destroying either half repairs content and semantic state together;
- clipping or resize cannot strand metadata on an invalid continuation cell.

The exact storage choice for continuation coordinates may be either a repeated metadata reference or a leader-only representation, provided public inspection and edit behavior are deterministic. T1103/T1105 must freeze that detail with tests.

## Validation

T1102 adds:

- exact supported-runtime representation measurements;
- large-pad overhead calculations;
- row-sparse storage prototype tests;
- lazy row allocation/release tests;
- detached row snapshot/replace tests;
- coordinate validation tests.

The tranche does not yet add public metadata API or physical OSC 8 rendering.

## Exit criteria

T1102 is complete when one exact `1.1.0-alpha.2` head passes the normal PR matrix and proves:

1. `CursesCell` remains 72 bytes on the supported 64-bit runtime matrix;
2. the reference/token comparison remains visible and deterministic;
3. row-sparse storage allocates only rows containing semantic values;
4. the storage releases empty rows and the entire plane when cleared;
5. no public DCurses API delta has entered before T1103;
6. Terminal 1.6.0 / TermInfo 1.10.0 remain the exact package dependencies.

After that, T1103 may define the minimal public immutable hyperlink/metadata contract on top of the selected plane.