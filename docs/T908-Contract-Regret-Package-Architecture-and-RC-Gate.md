# T908 — Contract Regret, Package, Architecture, and Release-Candidate Gate

**Release line:** `0.9.0`  
**Candidate version:** `0.9.0-rc.1`  
**Assembly version:** `0.9.0.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Status:** complete; exact RC head accepted

## Objective

T908 closes contract-development work for `0.9.0` and proves that the API and behavioral contract selected during T901–T907 is suitable for direct promotion to stable `0.9.0` and, subsequently, to `1.0.0` release closure without another breaking-cleanup tranche.

No feature or public API addition is introduced by T908.

## Accepted public contract

The canonical compiled public API fingerprint remains:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The fingerprint is produced identically for `net8.0`, `net9.0`, and `net10.0` and covers declared public types/members, enum values, generic constraints, parameter order/ref-kind/default values, accessor visibility, fields/constants, and compiled nullability metadata.

Any change to this contract requires an explicit baseline and migration review. T908 accepts no such change.

## Semantic and lifetime freeze

T908 carries forward the dedicated semantic and hardening gates for behavior that reflection alone cannot freeze:

- zero-based, row/column, parent-local window geometry;
- rows/columns dimension semantics and range validation;
- half-open column slicing and no wide-glyph splitting;
- Unicode 17.0.0 width semantics and explicit East Asian Ambiguous-width policy;
- semantic line-glyph identity versus ordinary Unicode text;
- wide-cell footprint repair across overwrite and resize boundaries;
- caller cancellation token preservation;
- disposal-induced wait cancellation as `ObjectDisposedException`;
- repeated disposal sharing one restoration operation;
- Terminal-session ownership transfer only after successful DCurses initialization;
- partial-output uncertainty invalidating retained physical knowledge;
- safe repaint on retry;
- preservation of independent primary/restoration failures;
- authoritative Terminal restoration after DCurses presentation failures.

## Dependency-boundary regret decision

The final approved lower-layer public type set remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Each exposure is intentional and already justified in the 0.9 dependency-regret record. No additional `Icod.Terminal` or `Icod.TermInfo` type enters the public surface.

## Package and release workflow review

The package continues to require:

- package id/title/version/author/project/license/repository metadata;
- `net8.0`, `net9.0`, and `net10.0` assemblies and XML documentation;
- portable symbols;
- exact dependency groups for `Icod.Terminal 1.4.0` and `Icod.TermInfo 1.10.0`;
- package README/license/icon payloads;
- fresh package-only consumer validation.

During T906 the GitHub Release workflow was corrected so release-note dependency versions are read from the project `PackageReference` values rather than from a stale hard-coded literal. This prevents release-page dependency wording from drifting from the generated package.

## Representative acceptance

The accepted T907 gate composes existing deterministic suites rather than duplicating them. Coverage includes editor-like and pager-like operation selection, Unicode/wide-cell boundaries, rich key/focus/paste/mouse input, resize/suspend/resume, cancellation/disposal, output failure/retry, repeated full-screen/rich-input ownership, large-pad panning, large-screen refresh, sparse/high-frequency/no-op refresh, and the `top`, `slabtop`, and `watch` acceptance sample builds.

## Pre-RC checkpoint

Exact pre-RC head `1011c7db06632137c4ca268d496e2f561040addb` passed:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

This establishes that T901–T907 were complete before candidate version promotion.

## Accepted RC result

Exact `0.9.0-rc.1` head:

```text
67269d0346e31c356414007dc807c82eeebe97aa
```

passed the complete seven-job Staging matrix:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

The API fingerprint and dependency boundary remained unchanged.

T908 is therefore complete and T909 may promote the unchanged API/behavior contract to stable `0.9.0` while performing release/documentation closure only.
