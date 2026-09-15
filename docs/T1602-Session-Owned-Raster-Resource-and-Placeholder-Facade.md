# T1602 — Session-Owned Raster Resource and Placeholder Facade

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1602  
**Theme:** Session-owned raster resource and placeholder facade  
**Accepted implementation head:** `5a6b2de0052212cbd6cd8c4c4d2be5c8b59a7183`  
**Workflow:** #930 / `35013512211`  
**Source/package identity:** `1.6.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** complete and qualified

---

## Purpose

T1602 implements the ownership facade frozen by T1601 without yet adding retained logical raster storage, panel composition, or physical refresh integration.

The tranche establishes the public ownership chain:

```text
CursesSession
    -> CursesRasterResource
        -> CursesRasterPlaceholder
            -> CursesRasterCell
```

`Icod.Terminal` remains the live ownership, acknowledgement, protocol-identity, encoding, and cleanup authority. DCurses adds only its retained-presentation facade and semantic lifecycle projection.

## RED evidence

The first test-only RED head was `374e2229fb2a6349b0788ea75486b95d300ba566`. Its first run exposed a test-harness defect (`xUnit1030`) and an accidental T1603 logical-API assertion; neither was accepted as a valid feature RED.

The corrected test-only RED head was:

```text
f521726e50fd986a3e259128d3bfda82a46527a8
```

The corrected suite built successfully and failed during test execution for the intended missing T1602 contract:

- `CursesRasterResource` and the other frozen facade types were absent;
- `CursesRasterOwnershipMapper` was absent;
- the public dependency boundary did not yet expose `Icod.Terminal.TerminalRasterImage`.

Only after that expected missing-feature failure was observed was production implementation committed.

## Implemented public facade

T1602 adds the six exported types frozen by T1601:

```text
CursesRasterCell
CursesRasterOwnershipLossReason
CursesRasterOwnershipState
CursesRasterOwnershipStatus
CursesRasterPlaceholder
CursesRasterResource
```

`CursesSession` adds:

```csharp
public ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceAsync(
    TerminalRasterImage image,
    CancellationToken cancellationToken = default
);
```

`CursesRasterResource` adds the session-bound resource ownership facade and validated placeholder creation; `CursesRasterPlaceholder` exposes dimensions, ownership observation, cell-token creation, and asynchronous disposal; `CursesRasterCell` remains an opaque readonly token with public descriptive row/column coordinates and private placeholder/session association.

No logical `GetRasterCell`, `SetRasterCell`, or `WriteRasterCell` API is implemented in T1602; those remain T1603 work.

## Lifecycle projection

DCurses exposes its own stable semantic vocabulary rather than publishing Terminal lifecycle implementation types.

The mapping from Terminal is exhaustive by enum member meaning, not a numeric cast. Unknown future Terminal status or loss-reason values fail closed with `ArgumentOutOfRangeException` rather than silently acquiring a DCurses meaning.

A disposed DCurses resource/placeholder reports:

```text
Status      Disposed
LossReason  ExplicitDisposal
```

Reading ownership remains synchronous and side-effect free.

## Validation and ownership behavior

T1602 verifies that:

- placeholder dimensions are locally bounded to `1..256` in each axis before Terminal state/output is accessed;
- wrapper disposal is idempotent;
- resource creation delegates to the canonical `TerminalSession` through the existing DCurses terminal-activity serialization gate;
- placeholder creation delegates to the owned `TerminalRasterResource`;
- controlled Terminal `Available`, `Unavailable`, `Unsupported`, and `Failed` results preserve their status and diagnostics through the DCurses wrapper;
- no source image is cached by DCurses;
- no raw Kitty/Sixel identity or command data is exposed.

## Public dependency boundary

The only new lower-layer type admitted into the public DCurses contract is:

```text
Icod.Terminal.TerminalRasterImage
```

The dependency guard continues to reject public leakage of Terminal raster resources, placeholders, cell tokens, lifecycle state/enums, placement types, protocol ids, or backend selectors.

## Compiler-derived API candidate

The T1602 implementation initially produced the expected fingerprint-only failure against the published 1.5 baseline while every other test passed.

The measured `1.6.0-alpha.1` contract is:

```text
75 exported types
554 canonical declared contract lines
sha256 c2ccbe1ced4155d595fbe8ed93c5efb0ceb250e484b184fc83c108cb6adac84d
```

The candidate is recorded in `docs/Public-API-Fingerprint-1.6.json` and is identical on `net8.0`, `net9.0`, and `net10.0`.

## Package identity and tags

The accepted head promotes source/package identity to:

```text
Version         1.6.0-alpha.1
PackageVersion  1.6.0-alpha.1
AssemblyVersion 1.0.0.0
```

The package tags added by the maintainer during T1601 are retained:

```text
console-ui
text-ui
```

No runtime dependency version changed.

## Qualification

Exact head:

```text
5a6b2de0052212cbd6cd8c4c4d2be5c8b59a7183
```

passed workflow #930 / `35013512211` across all seven Staging jobs:

```text
Package candidate       green
Windows x64             green
Windows ARM64           green
Linux x64               green
Linux ARM64             green
macOS x64               green
macOS ARM64             green
```

Linux x64 recorded a warning-free build and:

```text
net8.0   818 passed / 0 failed
net9.0   818 passed / 0 failed
net10.0  818 passed / 0 failed
```

## Exit decision

T1602 is complete. T1603 may now add the separate sparse retained-raster logical plane and the `CursesVirtualScreen` / `CursesWindow` logical raster operations frozen by T1601.
