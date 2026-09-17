# Icod.DCurses 1.6 Public API Baseline

**Release:** `1.6.0`  
**Stable-source package identity:** `1.6.0`  
**Published compatibility floor:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Declared runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Machine-contract qualification head:** `cd46c0e7ee61bbca09552f34e269177498502710`  
**Machine-contract workflow:** #995 / `35150729133`  
**Accepted T1610 head:** `dbb663406cfeb62ccdd4181fe34b3264a6e0462a`  
**T1610 workflow:** #999 / `35155346200`  
**Accepted RC head:** `47728ad870205c89bc1d7c0014667774ea9960d9`  
**RC workflow:** #1002 / `35158260589`  
**Status:** frozen stable-source 1.6 contract; final stable-source branch qualification is owned by T1611

## Published 1.5 floor

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

## Frozen 1.6 contract

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The compiler-derived fingerprint is stored in `docs/Public-API-Fingerprint-1.6.json` and is identical on `net8.0`, `net9.0`, and `net10.0`.

The additive delta over 1.5 is exactly:

```text
+6 exported types
+34 canonical declared contract lines
```

No published 1.5 exported type is removed.

## New exported types

```text
Icod.DCurses.CursesRasterCell
Icod.DCurses.CursesRasterOwnershipLossReason
Icod.DCurses.CursesRasterOwnershipState
Icod.DCurses.CursesRasterOwnershipStatus
Icod.DCurses.CursesRasterPlaceholder
Icod.DCurses.CursesRasterResource
```

`CursesRasterResource` and `CursesRasterPlaceholder` are sealed asynchronous ownership facades with no public constructors. `CursesRasterCell` is an immutable value token with no public constructor and no independent cleanup ownership.

## New public members on existing types

`CursesSession` adds:

```csharp
ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceAsync(
    TerminalRasterImage image,
    CancellationToken cancellationToken = default
);
```

`CursesVirtualScreen` adds:

```csharp
CursesRasterCell? GetRasterCell(int row, int column);
void SetRasterCell(int row, int column, CursesRasterCell? rasterCell);
```

`CursesWindow` adds the same local-coordinate get/set operations plus:

```csharp
void WriteRasterCell(CursesRasterCell rasterCell);
```

The ordinary visual/text cell, semantic metadata, and retained raster cell remain independent retained axes.

## Raster ownership vocabulary

```text
CursesRasterOwnershipStatus
Current  = 0
Stale    = 1
Released = 2
Disposed = 3

CursesRasterOwnershipLossReason
None                = 0
SessionStateLost    = 1
ResourceMissing     = 2
ParentPlacementLost = 3
AncestorReleased    = 4
ResourceReleased    = 5
ExplicitDisposal    = 6
```

`CursesRasterOwnershipState` is the immutable pair of status and loss reason. DCurses maps Terminal ownership observations by semantic member rather than numeric cast.

## Public dependency boundary

The published 1.5 public contract intentionally exposed:

```text
Icod.Terminal.TerminalControlResult<T>
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalSession
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Version 1.6 adds exactly one lower-layer public type:

```text
Icod.Terminal.TerminalRasterImage
```

No Terminal raster resource, placeholder, placeholder-cell, lifecycle, placement, generation-id, protocol-id, backend-selector, or graphics-payload type leaks from the DCurses public surface.

The direct production dependency graph remains `Icod.Terminal 1.15.0` plus `Icod.TermInfo 1.14.0`. The broader dependency-layering question is deferred to the 1.7 development track.

## Retained mixed-media contract

The frozen model is a third retained axis beside the dense visual/text plane and sparse semantic-metadata plane. Raster state is stored in a lazy row-sparse side plane so ordinary no-media surfaces do not pay a permanent per-cell raster-reference cost.

Retained raster cells participate in ordinary replacement/clear, insert/delete/scroll, rectangle copy/overlay, subwindows, pads/viewports, panels, clipping, resize, sparse damage, full repaint, synchronized output, lifecycle invalidation, and caller-driven retry after uncertain output.

A blank panel coordinate carrying raster content is visually present under `BlankCellsTransparent`. Semantic metadata alone does not make a blank coordinate opaque.

Terminal remains the sole owner of live protocol identity, acknowledgement, encoding, and raster lifecycle certainty. DCurses owns logical placement/composition/damage/refresh. Applications own durable source-image data and recovery policy.

DCurses does not retain a hidden source-image cache, recreate stale ownership, blindly replay committed output, or switch automatically to another graphics backend.

## Machine guards and qualification

The contract is guarded by:

- `PublicApiFingerprintTests` for complete declared signatures, nullability, defaults, ref-kinds, enum values, constraints, fields, properties, events, and methods;
- `PublicOneSixApiBaselineTests` for the explicit 1.6 fingerprint artifact;
- `PublicDependencyBoundaryTests` for Terminal/TermInfo leakage;
- raster public-API candidate tests for constructors, enum numerics, lifecycle shape, and exact facade signatures;
- package-only raster smoke tests compiled and executed against the packed NuGet artifact.

T1610 accepted exact head `dbb663406cfeb62ccdd4181fe34b3264a6e0462a` on workflow #999 / `35155346200`, all seven jobs green. The unchanged API/implementation was then qualified as `1.6.0-rc.1` at `47728ad870205c89bc1d7c0014667774ea9960d9`, workflow #1002 / `35158260589`, all seven jobs green; Linux x64 built with zero warnings/errors and passed 899/899 tests on each target framework.

No feature/API change is accepted after this baseline without returning to the T1610 regret gate.
