# T304 — Public Column-Oriented Text Helpers

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Checkpoint:** `0.3.0-alpha.4`  
**Status:** Complete

## Purpose

T304 exposes the same normalized Unicode text and terminal-column semantics used by `CursesWindow` to applications that need to measure or trim text before drawing it.

## Public surface

```text
CursesText.MeasureColumns(...)
CursesText.TruncateToColumns(...)
CursesText.SliceByColumns(...)
```

Each helper accepts an optional `ICursesTextWidthProvider`; omitting it uses `UnicodeCursesTextWidthProvider.Instance`.

## Contract

The helpers:

- normalize malformed UTF-16 before segmentation;
- use the same runtime text-element segmentation seam as window writes;
- use the supplied curses width provider for every complete text element;
- reject terminal control characters;
- never split a Unicode text element;
- never return half of a two-column element;
- omit a two-column element if a truncation or slice boundary cuts through it;
- omit unattached leading zero-width elements;
- retain zero-width elements which follow an included visible element;
- use half-open column intervals for slicing.

`SliceByColumns(text, startColumn, columnCount)` includes only text elements whose complete terminal-column spans lie inside the requested interval.

## Acceptance

Automated tests compare helper results against actual `CursesWindow` placement for ordinary text, combining sequences, emoji, wide CJK text, malformed UTF-16, and explicit wide-Ambiguous policy.

The isolated package-only consumer compiles and executes all three helpers from the generated `.nupkg`.
