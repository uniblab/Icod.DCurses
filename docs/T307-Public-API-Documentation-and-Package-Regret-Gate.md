# T307 — Public API, Documentation, and Package Regret Gate

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Current development version:** `0.3.0-rc.1`  
**Status:** Complete; release-candidate promotion approved

## 1. Purpose

T307 reviews the complete 0.3 Unicode/terminal-cell contract before release-candidate promotion. The goal is to make API regret cheap now rather than discover an unnecessary public commitment after 0.3 ships.

## 2. Public additions accepted

The 0.3 public delta is intentionally limited to:

```text
CursesAmbiguousWidthPolicy
    Narrow = 0
    Wide   = 1

UnicodeCursesTextWidthProvider.WideAmbiguousInstance
UnicodeCursesTextWidthProvider.UnicodeDataVersion
UnicodeCursesTextWidthProvider.AmbiguousWidthPolicy

CursesText.MeasureColumns(...)
CursesText.TruncateToColumns(...)
CursesText.SliceByColumns(...)
```

These additions solve durable application needs without exposing the generated Unicode tables or internal segmentation/invariant machinery.

## 3. Public additions deliberately rejected

The following remain internal or absent:

- generated East Asian Width range types/tables;
- generated Emoji-property range types/tables;
- a public Unicode database object;
- a public grapheme/text-element object model;
- public cell-footprint validation/repair APIs;
- locale/environment based automatic Ambiguous-width selection;
- terminal glyph-width probing;
- shaping/bidi APIs.

This keeps the public surface independent of future data-generation implementation details.

## 4. Compatibility review

The existing stable contracts are retained:

- `ICursesTextWidthProvider` remains the width-policy seam;
- `UnicodeCursesTextWidthProvider.Instance` remains the default singleton;
- default East Asian Ambiguous width remains narrow;
- `CursesScreen(..., ICursesTextWidthProvider?)` remains the explicit screen policy seam;
- `CursesVirtualScreen` remains an exact low-level logical-cell store when used directly;
- existing 0.1 and 0.2 input/session/window API shapes are not intentionally removed or renamed.

The stronger T305 footprint rules are applied to screen-owned structural/window operations without turning low-level exact cell storage into a different public abstraction.

## 5. Unicode-data review

The 0.3 built-in width policy is pinned to Unicode 17.0.0.

Checked-in generated data is derived from:

```text
EastAsianWidth.txt
    East_Asian_Width = A, W, F
    plus normative default-wide ranges

emoji/emoji-data.txt
    Emoji binary property
```

The Emoji-property table replaces the earlier broad hand-maintained candidate ranges. This corrects VS16 behavior for Emoji-property scalars outside those ranges (for example copyright, registered, trademark, and arrow symbols) and avoids treating every scalar inside the old broad ranges as an emoji candidate.

Normal builds remain offline. Unicode-data updates are intentional source changes rather than host-runtime drift.

## 6. Column-helper review

The three `CursesText` methods are retained because they expose foundational operations applications otherwise have to reproduce incorrectly.

No additional convenience overloads are added in 0.3. The optional `ICursesTextWidthProvider` parameter gives callers policy control without multiplying overloads.

The slicing contract remains conservative: only complete text elements whose occupied column spans lie inside the requested half-open interval are returned.

## 7. Dependency review

The package dependency graph remains:

```text
Icod.DCurses
    -> Icod.Terminal 1.0.0
    -> Icod.TermInfo 1.10.0
```

No new package dependency is introduced.

The public upstream type allow-list remains unchanged and is guarded by `PublicDependencyBoundaryTests`.

## 8. Machine guards

T307 adds or retains machine checks for:

- frozen `CursesAmbiguousWidthPolicy` numeric values;
- exactly the approved `CursesText` public helper methods/signatures;
- built-in provider policy/version members;
- Unicode generated-range integrity and version agreement;
- positive/negative Unicode Emoji-property VS16 cases;
- package-only use of Unicode policy and column helpers;
- unchanged Terminal/TermInfo public dependency boundary.

## 9. Documentation review

The repository now documents:

- Unicode 17.0.0 data ownership;
- malformed UTF-16 normalization;
- grapheme/text-element segmentation ownership;
- narrow-default/wide-opt-in Ambiguous semantics;
- column truncation/slicing boundaries;
- screen-owned wide-cell footprint guarantees and the low-level storage distinction;
- package-only consumer behavior;
- live Unicode diagnostics.

The detailed accepted public surface is recorded in `docs/Public-API-Baseline-0.3.md`.

## 10. Gate result

The completed `0.3.0-alpha.6` source passed:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation;
- fresh package-only consumers for all supported target frameworks.

The public regret review is therefore closed and the same feature-frozen contract is promoted to `0.3.0-rc.1` for T308 release validation.

No additional feature family enters after this point.
