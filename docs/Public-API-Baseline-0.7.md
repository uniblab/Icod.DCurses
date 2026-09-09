# Icod.DCurses 0.7 Public API Baseline

**Release line:** `0.7.0`  
**Baseline checkpoint:** stable `0.7.0` source  
**Stable predecessor:** `0.6.0`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Frozen stable-source contract; T708 complete; T709 merge gate active

## Purpose

This document records the deliberate public API delta introduced by the 0.7 refresh/output-optimization release line.

Unless listed here, the frozen 0.6 public contract remains unchanged.

## Accepted public delta

`0.7.0` adds exactly one public configuration property:

```csharp
public sealed class CursesSessionOptions {
    public bool UseSynchronizedOutput { get; init; }
}
```

The default value is `false`.

When enabled, each `CursesSession.RefreshAsync()` transaction composes with `Icod.Terminal` synchronized-output ownership. DCurses does not construct DEC private mode 2026 control sequences itself and does not claim that a terminal supports the mode merely because framing was requested.

The option remains opt-in because one unnested refresh adds 16 protocol bytes for the begin/end frames, plus Terminal's final-release flush semantics. Small or high-frequency refresh workloads may therefore prefer the ordinary unframed path.

The public option is justified because it exposes a curses-shaped refresh policy at the DCurses transaction boundary without requiring application code to own or coordinate a Terminal synchronized-output lease.

## Intentionally internal 0.7 implementation types

The following concepts remain internal implementation details:

- `CursesOutputCostModel`;
- cursor-motion candidate resolution;
- erase candidate resolution;
- insert/delete-character candidate resolution;
- insert/delete-line and scrolling-region candidate resolution;
- retained physical-screen state;
- refresh-engine operation selection;
- emitted-byte/write measurement helpers;
- optimization plan/operation records.

No public refresh-statistics or diagnostics type is added. T701-T708 measurements remain test/maintainer infrastructure because no consumer requirement justifies freezing a diagnostics API.

## Existing public semantics remain unchanged

The optimization release does not change:

- `CursesScreen`, `CursesWindow`, pad, viewport, or logical-cell semantics;
- Unicode width policy;
- `CursesStyle` or `CursesPresentationCapabilities` semantics;
- semantic line-glyph identity;
- input/event/protocol public contracts;
- requested final cursor position;
- session suspension/disposal restoration behavior.

## Dependency boundary

0.7 adds no new `Icod.Terminal` or `Icod.TermInfo` type to public signatures.

The approved dependency-boundary allow-list remains:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Existing `PublicDependencyBoundaryTests` continue to machine-enforce this boundary.

## Machine guards

`PublicRefreshOptimizationApiContractTests` freezes:

- presence and Boolean type of `CursesSessionOptions.UseSynchronizedOutput`;
- the `false` default;
- internal visibility of all 0.7 cost/selector/refresh implementation types.

The package-only fresh consumer also constructs both default and enabled `CursesSessionOptions` instances so the packed API is compiled and executed outside the source project.

## Regret decision

No 0.7 selector, cost model, operation-plan type, or diagnostics surface becomes public in stable `0.7.0`.

The one accepted public option is small, orthogonal, default-safe, integration-tested for framing order and nested Terminal lease composition, and does not expand the Terminal/TermInfo signature boundary.

The exact `0.7.0-rc.1` head passed the Windows/Linux/macOS Staging matrix and canonical package/fresh-consumer gate. The contract has therefore been promoted unchanged to stable `0.7.0` source. No feature/API change may enter during the remaining T709 merge/publication gates.
