# Icod.DCurses 1.6 Public API Baseline

**Release:** `1.6.0`  
**Current source/package identity at API freeze:** `1.6.0-alpha.2`  
**Published compatibility floor:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Declared runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Machine-contract qualification head:** `cd46c0e7ee61bbca09552f34e269177498502710`  
**Machine-contract workflow:** #995 / `35150729133`  
**Status:** frozen pre-RC 1.6 candidate contract; T1610 release-regret gate

## Published 1.5 floor

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

## Frozen 1.6 candidate contract

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The compiler-derived fingerprint is stored in `docs/Public-API-Fingerprint-1.6.json` and is identical on `net8.0`, `net9.0`, and `net10.0`.

The additive delta over 1.5 is:

```text
+6 exported types
+34 canonical declared contract lines
```

No published 1.5 exported type is removed.

## New exported types

Version 1.6 adds exactly these six exported DCurses types:

```text
Icod.DCurses.CursesRasterCell
Icod.DCurses.CursesRasterOwnershipLossReason
Icod.DCurses.CursesRasterOwnershipState
Icod.DCurses.CursesRasterOwnershipStatus
Icod.DCurses.CursesRasterPlaceholder
Icod.DCurses.CursesRasterResource
```

The public raster surface is deliberately DCurses-shaped. `CursesRasterResource` and `CursesRasterPlaceholder` are sealed asynchronous ownership facades with no public constructors. `CursesRasterCell` is an immutable value token with no public constructor and no independent cleanup ownership.

## New public members on existing types

`CursesSession` adds:

```csharp
ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceAsync(
    TerminalRasterImage image,
    CancellationToken cancellationToken = default
);
```

`CursesVirtualScreen` adds retained raster inspection/mutation by logical coordinate:

```csharp
CursesRasterCell? GetRasterCell(int row, int column);
void SetRasterCell(int row, int column, CursesRasterCell? rasterCell);
```

`CursesWindow` adds the same local-coordinate get/set operations plus current-cursor retained raster writing:

```csharp
void WriteRasterCell(CursesRasterCell rasterCell);
```

The ordinary visual/text cell, semantic metadata, and retained raster cell remain independent retained axes.

## Raster ownership vocabulary

`CursesRasterOwnershipStatus` freezes these numeric identities:

```text
Current  = 0
Stale    = 1
Released = 2
Disposed = 3
```

`CursesRasterOwnershipLossReason` freezes:

```text
None                = 0
SessionStateLost    = 1
ResourceMissing     = 2
ParentPlacementLost = 3
AncestorReleased    = 4
ResourceReleased    = 5
ExplicitDisposal    = 6
```

`CursesRasterOwnershipState` is the immutable pair of status and loss reason. DCurses maps Terminal ownership observations by semantic member rather than by numeric cast.

## Dependency boundary

The published 1.5 public contract intentionally exposed five lower-layer type definitions:

```text
Icod.Terminal.TerminalControlResult<T>
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalSession
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Version 1.6 permits exactly one additional lower-layer type:

```text
Icod.Terminal.TerminalRasterImage
```

The final approved public dependency set is therefore six definitions. The reuse of `TerminalRasterImage` avoids introducing a duplicate DCurses pixel/image-format hierarchy.

The 1.6 public contract does **not** expose Terminal raster resources, placeholders, placeholder-cell tokens, lifecycle types, placement types, generation ids, Kitty/Sixel ids, APC/DCS payloads, or backend command dictionaries.

The broader question of whether DCurses should retain a direct production dependency on `Icod.TermInfo` is explicitly deferred to the 1.7 development track and does not alter this 1.6 baseline.

## Retained mixed-media semantics

The frozen model is a third retained axis beside the dense visual/text plane and sparse semantic-metadata plane. Raster state is stored in a lazy row-sparse side plane so ordinary no-media surfaces do not pay a permanent per-cell raster-reference cost.

Retained raster cells participate in:

- ordinary replacement and clear semantics;
- insert/delete/scroll editing;
- rectangle copy and overlay;
- subwindows;
- pads and independent viewports;
- panel z-order, blank-cell transparency, hide/show/move/resize/disposal;
- screen resize and clipping;
- sparse damage and full repaint;
- synchronized output;
- lifecycle invalidation and caller-driven retry after uncertain output.

A blank panel coordinate carrying raster content is visually present under `BlankCellsTransparent` composition. Semantic metadata alone does not make a blank coordinate opaque.

## Ownership and lifecycle guarantees

Terminal remains the sole owner of live protocol identity, acknowledgement, encoding, and persistent-raster lifecycle certainty. DCurses owns logical placement/composition/damage/refresh.

A retained token is associated privately with one live `CursesSession`. Same-session logical copies may duplicate the reference; foreign-session transfer into a live destination is rejected before partial mutation.

`Stale`, `Released`, and `Disposed` tokens may remain as logical intent but are rejected before DCurses cursor/rendition/raster output. Physical raster knowledge is invalidated on session-state loss, explicit facade disposal, cancellation, or committed output uncertainty as appropriate.

DCurses does not retain a hidden source-image cache, recreate stale ownership, blindly replay committed output, or switch automatically to Sixel or another graphics backend.

## Compatibility and regret conclusions

The T1610 review found no public naming, constructor, nullability, enum-number, ownership, mutability, session-association, or dependency leak that justifies changing the accepted surface before RC.

- the six new types are additive over the published 1.5 contract;
- the only new lower-layer public type is the explicitly approved `TerminalRasterImage`;
- `AssemblyVersion` remains `1.0.0.0`;
- target frameworks remain `net8.0`, `net9.0`, and `net10.0`;
- direct package dependencies remain `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`;
- no widget/application framework or generic raster scene graph enters the contract.

## Machine guards and qualification

The contract is guarded by:

- `PublicApiFingerprintTests`, which regenerates canonical declared signatures including constructors, enum values, generic constraints, parameter/ref/default metadata, and nullability;
- `PublicOneSixApiBaselineTests`, which pins the explicit `docs/Public-API-Fingerprint-1.6.json` counts/hash/type inventory;
- `PublicDependencyBoundaryTests`, which rejects unapproved Terminal/TermInfo type leakage;
- raster public-API candidate tests that freeze constructors, enum numerics, lifecycle shape, and exact facade signatures;
- package-only raster smoke tests compiled and executed against the packed NuGet artifact.

Exact machine-contract head `cd46c0e7ee61bbca09552f34e269177498502710` passed workflow #995 / `35150729133` across package candidate plus Windows/Linux/macOS x64/ARM64. Linux x64 built with zero warnings/errors and passed 899/899 tests on each of `net8.0`, `net9.0`, and `net10.0`.

T1611 may change package identity from alpha to RC/stable, but no feature/API change is accepted after this baseline without returning to the T1610 regret gate.
