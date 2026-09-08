# Icod.DCurses 0.3 Public API Baseline

**Project:** `Icod.DCurses`  
**Release line:** `0.3.x`  
**Prepared during:** T307  
**Current development version:** `0.3.0-alpha.6`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Unicode width-data baseline:** `17.0.0`  
**Status:** Release-line source contract baseline

---

## 1. Purpose

This document records the public contract intentionally added by `Icod.DCurses 0.3` over the stable `0.2.0` baseline.

The 0.1 screen/window/session contract and 0.2 semantic-input contract remain in force. The 0.3 release does not intentionally remove or rename an existing public member.

## 2. New public enum

`0.3` adds:

```text
CursesAmbiguousWidthPolicy.Narrow = 0
CursesAmbiguousWidthPolicy.Wide   = 1
```

`Narrow` is the default. DCurses does not infer this policy from locale or environment variables.

## 3. Built-in Unicode width provider additions

`UnicodeCursesTextWidthProvider` retains its existing `Instance` singleton and `ICursesTextWidthProvider` implementation.

`0.3` adds:

```text
static UnicodeCursesTextWidthProvider WideAmbiguousInstance { get; }
static string UnicodeDataVersion { get; }
CursesAmbiguousWidthPolicy AmbiguousWidthPolicy { get; }
```

The built-in provider is pinned to Unicode 17.0.0 terminal-width data for the 0.3 release line.

The default singleton uses `Narrow`; `WideAmbiguousInstance` is the explicit opt-in provider.

## 4. New public `CursesText` helper surface

`0.3` adds the static `CursesText` type with exactly these column-oriented operations:

```text
int MeasureColumns(
    string text,
    ICursesTextWidthProvider? textWidthProvider = null
)

string TruncateToColumns(
    string text,
    int maxColumns,
    ICursesTextWidthProvider? textWidthProvider = null
)

string SliceByColumns(
    string text,
    int startColumn,
    int columnCount,
    ICursesTextWidthProvider? textWidthProvider = null
)
```

These helpers share the same normalization, text-element segmentation, and width-provider contract as `CursesWindow` string writes.

## 5. Column-helper semantics

The following behavior is part of the 0.3 contract:

- malformed UTF-16 is replaced with U+FFFD before segmentation;
- terminal control characters are rejected by the printable column helpers;
- helper boundaries are terminal-column boundaries, not UTF-16 offsets;
- no helper splits a Unicode text element;
- no helper returns half of a two-column element;
- truncation omits a complete element that would exceed the limit;
- slicing uses the half-open interval `[startColumn, startColumn + columnCount)`;
- a slice omits an element when either requested boundary cuts through that element;
- unattached leading zero-width elements are omitted;
- zero-width elements following an included visible element are retained;
- caller-supplied `ICursesTextWidthProvider` implementations remain supported.

## 6. Unicode data and presentation contract

The built-in provider uses checked-in, versioned Unicode 17.0.0 data generated from:

- `EastAsianWidth.txt` for `A`, `W`, and `F` classification plus Unicode's normative default-wide ranges;
- `emoji-data.txt` `Emoji` property for VS16/emoji-ZWJ candidate classification.

Normal builds do not download Unicode data.

VS15 requests base/text width semantics. VS16 promotes a scalar to emoji-width semantics only when the base scalar has the Unicode `Emoji` property. Keycap and regional-indicator handling remain explicit sequence rules.

## 7. Cell-footprint result

T305 changes no public type or member.

`CursesVirtualScreen` retains its existing exact low-level cell-storage API. Stronger leader/continuation repair is applied to `CursesScreen`-owned structural/window operations and through internal invariant validation/repair machinery.

This distinction is intentional compatibility behavior.

## 8. Dependency boundary

The 0.3 release introduces no new package dependency and no newly approved Terminal/TermInfo type in a public DCurses signature.

The intentional upstream public-type allow-list remains:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

`PublicDependencyBoundaryTests` remains authoritative for this boundary.

## 9. Regret review result

The 0.3 additions are intentionally small:

- one two-value semantic enum;
- three read-only built-in-provider members;
- one static helper type with three column-oriented operations.

No separate public grapheme object, Unicode database object, generated-range type, invariant validator, or emoji classification type is exposed. Those remain implementation details so later Unicode-data maintenance does not expand the compatibility surface unnecessarily.

## 10. Deferred

The 0.3 baseline does not add:

- window insert/delete/copy/overlay operations (`0.4.0`);
- pads and large virtual surfaces (`0.5.0`);
- text shaping or bidi layout;
- locale-driven automatic ambiguous-width selection;
- arbitrary display widths greater than two columns.
