# T303 — Unicode Width Data and East Asian Ambiguous Policy

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Current development version:** `0.3.0-alpha.2`  
**Tranche:** T303 — cluster-aware width policy and ambiguous-width mode  
**Unicode data baseline:** `17.0.0`  
**Status:** Implementation in progress — generator and policy contract frozen

---

## 1. Purpose

T303 replaces the permanent dependence on hand-maintained scalar ranges with a
reproducible, versioned terminal-width data contract.

The authoritative East Asian width source for the `0.3.x` line is the Unicode
17.0.0 `EastAsianWidth.txt` data file. Generated range data is checked into the
repository so ordinary builds remain deterministic and offline.

## 2. Unicode version policy

The `0.3.x` width contract is pinned to:

```text
Unicode 17.0.0
```

A later Unicode data update is an intentional library change. It SHALL update:

- the generator's expected version;
- generated width tables;
- conformance fixtures;
- public documentation;
- package release notes where behavior changes.

DCurses SHALL NOT silently switch width semantics merely because the host .NET
runtime updates its Unicode tables.

Runtime Unicode text-element segmentation remains the T302 segmentation seam;
T303 independently versions the terminal-column classification data that
DCurses owns.

## 3. Generated East Asian Width data

`tools/unicode-width-generator` consumes the official UCD file and emits merged
ranges for:

```text
East_Asian_Width = A
East_Asian_Width = W
East_Asian_Width = F
```

The generated source SHALL live under:

```text
src/Internal/Generated/UnicodeEastAsianWidthData.Generated.cs
```

Normal builds SHALL consume only that checked-in generated source. They SHALL
NOT fetch Unicode.org or any other network resource.

## 4. Ambiguous-width policy

East Asian Ambiguous characters require application/environment policy rather
than an unconditional universal width.

DCurses `0.3` SHALL expose:

```text
CursesAmbiguousWidthPolicy
    Narrow = 0
    Wide   = 1
```

The default remains **Narrow**. This preserves the behavior expected by the
majority of contemporary UTF-8 terminal environments and avoids changing the
existing `UnicodeCursesTextWidthProvider.Instance` contract unexpectedly.

The provider contract SHALL be:

```text
UnicodeCursesTextWidthProvider.Instance
    default narrow-Ambiguous provider

UnicodeCursesTextWidthProvider.WideAmbiguousInstance
    explicit wide-Ambiguous provider

UnicodeCursesTextWidthProvider.AmbiguousWidthPolicy
    policy used by this provider

UnicodeCursesTextWidthProvider.UnicodeDataVersion
    "17.0.0"
```

No locale sniffing or environment-variable guessing is performed by the width
provider itself. Applications that require wide-Ambiguous semantics select the
explicit provider when constructing a `CursesScreen` or session policy.

## 5. Cluster width decision order

For one already-normalized T302 text element, the default Unicode provider SHALL
resolve terminal width in this order:

1. a cluster containing only zero-width marks/format controls is zero columns;
2. explicit text presentation (`VS15`, U+FE0E) uses text/base width semantics;
3. recognized emoji presentation, keycap, flag, and emoji-ZWJ clusters use two
   columns;
4. a base scalar classified `W` or `F` by Unicode 17 East Asian Width uses two
   columns;
5. a base scalar classified `A` uses one or two columns according to
   `CursesAmbiguousWidthPolicy`;
6. other printable clusters use one column.

Combining marks and format scalars inside a nonzero-width grapheme do not add
columns independently.

## 6. Compatibility

T303 SHALL retain:

- `ICursesTextWidthProvider` as the injectable screen-width abstraction;
- `UnicodeCursesTextWidthProvider.Instance` as the default provider;
- the existing one-leader/one-continuation representation for two-column
  clusters;
- the public `CursesScreen(..., ICursesTextWidthProvider?)` seam.

Selecting wide-Ambiguous behavior is explicit and does not alter existing
screens which use the default provider.

## 7. Generation and auditability

The generator SHALL:

- reject Unicode input other than 17.0.0 for this release line;
- parse Unicode scalar/range syntax strictly;
- merge adjacent ranges with the same retained semantic class;
- emit deterministic UTF-8 source without a BOM;
- include an auto-generated marker and Unicode version constant.

Generated data SHALL be tested at representative boundaries and against a
maintained conformance fixture before T303 is complete.

## 8. Non-goals

T303 does not implement:

- bidirectional layout;
- Arabic/Indic shaping;
- font-dependent glyph measurement;
- locale auto-detection;
- terminal probing for observed glyph width;
- arbitrary widths greater than two columns.

## 9. Gate

T303 is complete when:

1. Unicode 17.0.0 generated `A`, `W`, and `F` data is checked in;
2. the default provider uses generated data instead of the old permanent
   hand-maintained wide-range predicate;
3. narrow and wide Ambiguous policies are public and deterministic;
4. emoji/text-presentation behavior remains cluster-aware;
5. generated range boundaries are covered by tests;
6. Windows, Linux, and macOS Staging validation passes;
7. package/fresh-consumer validation covers the public policy surface.
