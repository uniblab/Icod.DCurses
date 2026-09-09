# T907 — Representative Application-Shaped Acceptance Gate

**Release line:** `0.9.0`  
**Purpose:** prove the frozen contract under production-shaped managed TUI workloads without importing application policy into DCurses  
**Status:** acceptance set defined; full matrix required before T908

## Acceptance philosophy

T907 does not create a second benchmark or integration framework. The repository already contains deterministic tests and package-only consumers which exercise the application shapes required by the 1.0 roadmap. T907 freezes that combined set as a release gate.

Wall-clock timing is not a correctness threshold. The gates are final logical/physical semantics, deterministic byte/write behavior where already pinned, ownership/restoration, absence of leaked state, and successful bounded completion.

## Editor-like workload

The editor shape is covered by:

- public `CursesWindow` editing tests for insert/delete cells and lines, copy/overlay, drawing, touch/damage, nested views, clipping, and wide-cell repair;
- `PublicSemanticContractFreezeTests` for zero-based parent-local geometry, half-open text slicing, semantic line identity, and wide-footprint repair;
- `CursesOptimizationRegretTests` for the deterministic editor-style physical character-insertion workload and fallback comparison;
- package-only smoke execution of window inspection/editing/composition through the generated package.

The editor contract includes Unicode text, styled content, local cursor movement, and safe retained refresh behavior.

## Pager-like workload

The pager shape is covered by:

- deterministic line insertion/deletion and scrolling tests;
- `CursesOptimizationRegretTests` for the pager line-deletion/scroll workload and fallback comparison;
- large-screen and sparse-refresh hardening tests;
- resize/lifecycle hardening which invalidates/repaints safely after geometry changes.

## Unicode-heavy workload

Unicode acceptance includes:

- Unicode 17.0.0 width data;
- combining/variation-selector/emoji ZWJ/keycap/flag coverage from the existing Unicode suites;
- East Asian Wide/Fullwidth and explicit Ambiguous-width policy;
- malformed UTF-16 normalization;
- two-column leader/continuation invariants;
- no half-wide clipping/slicing;
- semantic line glyphs remaining distinct from ordinary Unicode box-drawing text.

The package-only consumer also exercises Unicode widths and column helpers from the packed artifact.

## Rich-input workload

The input contract is covered by the existing semantic keyboard and rich-input acceptance suites:

- ordinary text and semantic keys;
- key press/repeat/release phases;
- modifier vocabulary;
- focus events;
- bracketed paste;
- mouse semantics;
- controlled protocol acquisition;
- end-of-input mapping;
- fragmented UTF-8 preservation across cancellation.

All decoding remains Terminal-owned. T907 does not introduce a second parser or input loop.

## Lifecycle and ownership workload

`CursesLifecycleHardeningTests`, `CursesSessionLifetimeHardeningTests`, and `CursesOwnershipSoakHardeningTests` collectively cover:

- resize storms;
- suspend/resume coordination;
- disposal while suspended;
- caller cancellation versus disposal-induced wait cancellation;
- repeated complete session ownership cycles;
- rich-input/full-screen lease handoff and restoration;
- repeated disposal without duplicate cleanup.

## Pad and large-surface workload

Pad tests and `CursesScaleHardeningTests` cover:

- large off-screen surfaces;
- derived shared views;
- independent viewport observations;
- repeated horizontal/vertical panning;
- geometry revalidation;
- large screen refresh;
- repeated sparse updates;
- repeated no-op refreshes.

## Failure and recovery workload

`CursesOutputFailureHardeningTests` and refresh failure tests cover:

- partial-progress output failure;
- conservative retained-state invalidation;
- subsequent safe complete repaint;
- synchronized-output primary/restoration dual failures;
- rendition cleanup failure while Terminal restoration remains authoritative;
- aggregate preservation of independently meaningful failures.

## High-frequency refresh workload

The 0.7 regret fixtures and 0.8 scale tests retain deterministic high-frequency coverage including 1,000 sparse/one-cell updates and repeated no-op refreshes. Output byte/write fixtures remain maintainer comparison points, not universal terminal performance claims.

## Application-shaped samples

The repository samples remain compatibility indicators:

- `Icod.DCurses.Watch.Acceptance` — periodic update/pause/resize behavior;
- `Icod.DCurses.Slabtop.Acceptance` — table/sort/summary refresh behavior;
- `Icod.DCurses.Top.Acceptance` — multi-window navigation, help/prompt, styled rapid refresh, and resize relayout;
- `Icod.DCurses.Input.Showcase` — live rich-input facade demonstration.

They keep process-observation/application policy outside DCurses.

## Package-only acceptance

`tools/package-smoke` is run from the generated package artifact and exercises:

- approved Terminal/TermInfo dependency types;
- input semantic surface;
- Unicode width policy;
- column text helpers;
- window editing/composition/damage;
- pads/viewports;
- presentation and interactive-session paths in the optional live mode.

The ordinary package gate runs the deterministic non-interactive path.

## T907 gate

Before T908 promotion, the complete repository test suite and package-only consumer must pass for `net8.0`, `net9.0`, and `net10.0` on the PR matrix. T908 then requires the same accepted workload set on Windows x64/ARM64, Linux x64/ARM64, and macOS x64/ARM64 plus package validation.

No T907 result justifies a new public feature family. A discovered compatibility defect is fixed within the existing contract where possible; any unavoidable public correction must return through the T901-T905 fingerprint/regret/migration process before RC promotion.
