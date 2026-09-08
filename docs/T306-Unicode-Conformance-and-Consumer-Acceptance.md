# T306 — Unicode Conformance and Consumer Acceptance

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Checkpoint:** `0.3.0-alpha.6`  
**Status:** Complete

## Purpose

T306 expands the 0.3 Unicode contract beyond isolated examples and proves that the same semantics are usable through windows, public column helpers, the packed NuGet artifact, and a live diagnostics showcase.

## Automated corpus

The conformance suite covers representative terminal-cell cases for:

- ASCII and ordinary narrow text;
- combining-mark stacks;
- supplementary-plane scalar text;
- VS15 text presentation and VS16 emoji presentation;
- emoji modifier sequences;
- emoji ZWJ professions;
- regional-indicator flags;
- keycap sequences;
- CJK wide text;
- fullwidth text;
- East Asian Ambiguous text under narrow and explicit wide policy;
- malformed UTF-16 replacement before segmentation;
- two-column clipping and wrapping as complete elements.

The corpus verifies that `CursesText` helpers and `CursesWindow` agree on column width and placement for the same width provider.

## Package-only consumer

The isolated package consumer restores only the generated `Icod.DCurses` package and exercises:

- Unicode data-version visibility;
- narrow and wide Ambiguous providers;
- a Unicode 17 width sample;
- `CursesText.MeasureColumns`;
- `CursesText.TruncateToColumns`;
- `CursesText.SliceByColumns`.

This proves the public Unicode/column surface from the packed artifact rather than through repository project references.

## Interactive diagnostics

`Icod.DCurses.Showcase` now displays:

- the pinned Unicode width-data version;
- the default Ambiguous policy;
- ordinary Unicode and combining text;
- CJK/fullwidth samples;
- emoji presentation and modifier samples;
- flag, keycap, and ZWJ samples;
- measured narrow/wide column counts for an Ambiguous character.

The showcase is intended for visual diagnostics only. Automated correctness remains test-driven.

## Validation result

The completed T305/T306 head passed:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package and fresh-consumer validation.

T307 is the next gate: public API/documentation/package regret review before release-candidate promotion.
