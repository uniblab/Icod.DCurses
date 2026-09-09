# Icod.DCurses 1.0 Public API Baseline

**Stable source version:** `1.0.0`  
**Merged main commit:** `686814fe3036484265513d06fe48aec0315912aa`  
**Accepted release candidate:** `1.0.0-rc.1`  
**Frozen predecessor contract:** `0.9.0`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.5.0`; `Icod.TermInfo 1.10.0`

## Purpose

This document is the human-readable stable-contract companion to `docs/Public-API-Fingerprint-1.0.json`.

`Icod.DCurses 1.0` promotes the exact public contract frozen during 0.9. The major-version release is a stability commitment, not a feature-family or breaking-cleanup tranche.

## Canonical compiled surface

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The machine fingerprint is canonicalized from compiled assemblies and covers declared public type/member signatures, enum names/values, generic constraints, parameter order/ref-kind/default values, accessor visibility, public fields/constants, and compiled nullable metadata.

The stable 1.0 fingerprint is machine-guarded to remain identical to the historical 0.9 fingerprint. Exact `1.0.0-rc.1` head `1968bae18610e69e56dc8f720bffb099cb58eb24` reproduced and passed this contract on the complete six-architecture plus package/fresh-consumer gate. Documentation-complete stable source head `5c17607194b546c6d831be5811d1865a195b970e` passed the same complete gate before merge into `main`.

## Public type families

The stable contract consists of the established type families for:

- session creation, options, lifecycle, and resize events;
- logical screens, virtual screens, windows, pads, and viewports;
- cells, colors, styles, text attributes, text helpers, line glyphs, and presentation capabilities;
- Unicode width policy and pluggable text-width providers;
- input events, semantic keys/modifiers/phases, focus, mouse, paste, lifecycle events, and input-protocol leases/options;
- cursor visibility and terminal alerts.

There is no public refresh-engine, physical-screen-state, operation-cost, transport, synchronization, hardening, diagnostics, scheduler, or test-seam type.

## Geometry/text semantic baseline

The stable 1.x contract retains:

- zero-based row/column coordinates;
- parent-local subwindow origins;
- rows/columns meaning height/width;
- half-open column-oriented text intervals;
- no splitting of two-column Unicode elements;
- Unicode 17.0.0 width data;
- narrow East Asian Ambiguous characters by default with explicit wide policy;
- wide-cell leader/continuation repair;
- semantic line cells distinct from ordinary Unicode box-drawing text.

## Lifetime/concurrency/failure baseline

The stable 1.x contract retains:

- single-writer logical screen/window/pad/viewport mutation unless explicitly documented otherwise;
- one Terminal-owned event consumer allowed concurrently with serialized refresh/output work;
- Terminal remains the byte-stream decoder and live terminal-state owner;
- caller cancellation remains cancellation;
- disposal-unblocked public waits surface `ObjectDisposedException`;
- repeated disposal shares one restoration operation;
- output uncertainty invalidates retained physical knowledge;
- later refresh may safely repaint;
- independently meaningful primary/restoration failures remain observable;
- Terminal restoration remains authoritative.

## Intentional lower-layer public types

The stable public dependency boundary permits exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

No additional Terminal/TermInfo type is accepted into the public DCurses signature surface.

## Compatibility statement

No public breaking cleanup or feature-family addition is accepted between the frozen 0.9 contract and stable 1.0.

The 1.0 promotion changes package/assembly major-version identity and establishes the stable compatibility commitment. Existing 0.9 source consumers should not require API migration solely because they move to 1.0.

The post-closure dependency refresh from `Icod.Terminal 1.4.0` to `1.5.0` does not alter the DCurses public API fingerprint or DCurses version identity.

## Machine guards

- `PublicApiFingerprintTests` regenerates and verifies the canonical compiled API against the 1.0 baseline.
- `PublicStableApiBaselineTests` requires the 1.0 baseline to match the 0.9 freeze.
- semantic freeze tests protect geometry, Unicode/cell, and related behavioral contracts.
- lifetime/hardening tests protect cancellation/disposal/restoration/failure behavior.
- `PublicDependencyBoundaryTests` rejects additional upstream public-type leakage.
- package validation protects exact dependencies, assembly identity, XML documentation, symbols, metadata, and package-only consumption.
