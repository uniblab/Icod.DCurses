# T1610 — Public API, Package, Documentation, Dependency, and Licensing Regret Gate

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1610  
**Published compatibility floor:** `1.5.0`  
**Current source/package identity:** `1.6.0-alpha.2`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Starting accepted implementation/test head:** `fd9d28352ff64b47c6e9dfd9e81ed425125f9b86`  
**Starting workflow:** #989 / `35145912917`  
**Explicit 1.6 machine-contract head:** `cd46c0e7ee61bbca09552f34e269177498502710`  
**Machine-contract workflow:** #995 / `35150729133`  
**Status:** audit complete; final release-facing exact-head qualification follows this record before T1611

---

## Objective

T1610 is the final pre-RC regret gate for the complete 1.6 retained mixed-media surface. It adds no feature family. Its purpose is to decide whether the accepted API, package graph, documentation, licensing, samples, and package-only consumer evidence are suitable to promote unchanged into T1611 RC/stable-source closure.

Any production/API defect found here would return to a RED -> correction -> exact-head requalification cycle. Release-document and metadata corrections may remain inside T1610 provided they do not change the frozen exported contract.

## Public API qualification

Published 1.5 floor:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Frozen 1.6 candidate:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The delta is exactly **+6 exported types / +34 canonical lines**. The compiler-derived fingerprint is identical across `net8.0`, `net9.0`, and `net10.0`.

New exported types are exactly:

```text
CursesRasterCell
CursesRasterOwnershipLossReason
CursesRasterOwnershipState
CursesRasterOwnershipStatus
CursesRasterPlaceholder
CursesRasterResource
```

`PublicApiFingerprintTests` guards the full declared surface including type kind/base/interfaces, constructors, fields/constants, properties, events, methods, generic constraints, ref kinds, optional/default parameter metadata, and nullability.

T1610 adds `PublicOneSixApiBaselineTests` so the explicit `docs/Public-API-Fingerprint-1.6.json` artifact is itself pinned by counts/hash/type inventory rather than being reachable only through the historical current-development alias used by the long-lived fingerprint fixture.

Exact machine-contract head `cd46c0e7ee61bbca09552f34e269177498502710` passed workflow #995 / `35150729133`, all seven jobs green. Linux x64 built with zero warnings/errors and passed **899/899 tests** on each of `net8.0`, `net9.0`, and `net10.0`.

## Public naming, constructors, nullability, and enum numerics

The raster facade remains mechanism-oriented and does not expose protocol vocabulary.

- `CursesRasterResource` and `CursesRasterPlaceholder` are sealed, asynchronously disposable, and have no public constructors.
- `CursesRasterCell` is an immutable value token with no public constructor and no independent cleanup ownership.
- `CursesRasterOwnershipState` is an immutable DCurses semantic projection rather than a Terminal lifecycle type leak.
- the compiler fingerprint freezes exact nullability/default/ref metadata on all public members.

Numeric identities remain:

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

No existing 1.5 enum numeric identity changes.

## Ownership, disposal, and cross-session regret review

The accepted ownership split remains coherent:

```text
CursesSession
    -> CursesRasterResource
        -> CursesRasterPlaceholder
            -> CursesRasterCell values retained in logical surfaces
```

Terminal remains authoritative for live raster identity, acknowledgement, encoding, and lifecycle certainty. DCurses owns logical placement/composition/damage/refresh.

The final behavior keeps these guarantees:

- same-session logical copies may duplicate retained token references without duplicating Terminal ownership;
- known foreign-session transfers into a live destination are rejected before partial destination mutation;
- stale/released/disposed tokens may remain as logical intent but are rejected before DCurses cursor/rendition/raster output;
- explicit resource/placeholder disposal is idempotent and invalidates physical raster knowledge;
- session invalidation and output uncertainty invalidate trusted physical state without hidden re-upload or backend switching;
- no reverse ownership index or unbounded scene registry was introduced.

T1607 and T1609 provide disposal/concurrency, lifecycle-loss, cancellation, output-failure, deterministic caller-retry, and allocation/churn coverage.

## Dependency boundary audit

The published 1.5 public contract already exposed these five lower-layer definitions:

```text
Icod.Terminal.TerminalControlResult<T>
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalSession
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Version 1.6 intentionally adds exactly one:

```text
Icod.Terminal.TerminalRasterImage
```

`PublicDependencyBoundaryTests` rejects every other Terminal/TermInfo type definition appearing through constructors, properties, events, methods, bases, interfaces, generic arguments, arrays, pointers, or by-ref shapes.

No public `TerminalRasterResource`, `TerminalRasterPlaceholder`, `TerminalRasterPlaceholderCell`, Terminal raster lifecycle type, placement type, protocol id, session-generation id, backend selector, or graphics payload leaks from DCurses.

The production package graph remains intentionally unchanged for 1.6:

```text
Icod.DCurses
├── Icod.Terminal 1.15.0
└── Icod.TermInfo 1.14.0
```

The broader question of whether DCurses should remove its direct `Icod.TermInfo` dependency and funnel live-terminal concerns entirely through Terminal is explicitly deferred to the 1.7 development track. T1610 does not reopen that architecture during release closure.

## Package audit

The package candidate from documentation/package head `4abec8a3b177c56c413a73831169f3ef0a25b609` passed workflow #993 / `35149289553`, all seven jobs green.

The workflow artifact `icod-dcurses-pr-packages` was inspected directly. The `.nupkg` contains the expected:

- package nuspec;
- `README.md`;
- LGPL license file;
- package icon and toolchain image;
- `Icod.DCurses.dll` and XML documentation for `net8.0`, `net9.0`, and `net10.0`;
- repository/source metadata required by the established build;
- matching symbol package output.

The nuspec dependency groups remain exactly `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0` for every target framework.

The package-only consumer compiles and executes the full established smoke surface plus `RasterSmoke.cs`, which constructs `TerminalRasterImage`, freezes the DCurses ownership enums/state, and reflects the new resource/placeholder/window/virtual-screen raster members against only the packed package.

No sample/test source is unintentionally packed into the library package.

## Package metadata review

T1610 updates release-facing package metadata without changing package identity:

```text
Version         1.6.0-alpha.2
PackageVersion  1.6.0-alpha.2
AssemblyVersion 1.0.0.0
```

The description and release notes now describe the complete retained mixed-media release rather than the early T1603 alpha state.

The existing user-added package tags are retained. The pre-existing spelling error `interfacce` was corrected to `interface`; no intentional tag was removed.

Repository URL/type, package project URL, icon, README, symbol package settings, deterministic/source-link settings, and license metadata remain consistent with prior stable releases.

## Licensing audit

Repository source remains LGPL-3.0-or-later for the library, with GPL-3.0-or-later headers for the test suite where already established.

The repository `LICENSE` contains the GNU Lesser General Public License version 3 text. The project declares:

```text
PackageLicenseExpression          LGPL-3.0-or-later
PackageRequireLicenseAcceptance   true
```

The license file is packed with the package. No third-party raster codec, image decoder, graphics command library, or additional license-bearing runtime dependency was introduced by 1.6.

## Documentation and sample audit

The root README has been rewritten as the current product/package entry point. It now describes:

- the published 1.5 stable baseline and current 1.6 pre-RC development line;
- retained mixed-media presentation as a first-class capability;
- the three-layer ownership boundary among TermInfo, Terminal, and DCurses;
- `CursesRasterResource` -> `CursesRasterPlaceholder` -> `CursesRasterCell` ownership;
- application-owned durable source image data;
- no private protocol ids, raw Kitty/Sixel commands, hidden replay, or automatic backend fallback;
- the frozen 75-type / 559-line / SHA-256 candidate contract;
- the mixed-media sample and current 1.6 engineering authorities.

A root `CHANGELOG.md` now exists and summarizes the cumulative stable 1.0-1.5 history plus the 1.6 retained mixed-media changes. Historical tranche documents remain engineering evidence rather than being duplicated into the README.

`Icod.DCurses.MixedMedia.Sample` is in the solution and demonstrates public-only retained raster usage with pads/viewports, panel overlay/composition, clipping, interaction geometry, and normal serialized refresh.

The optional TermInfo planning demonstration was deliberately not made part of the 1.6 sample after the dependency/layering question was deferred to 1.7. Production behavior and the accepted 1.6 dependency graph remain unchanged.

## Regret conclusions

No production/API correction is required before RC promotion.

The final 1.6 surface remains:

- additive over 1.5;
- bounded and session-owned;
- protocol-neutral at the DCurses public layer;
- sparse for applications that do not use raster media;
- explicit about lifecycle loss and caller-owned recovery policy;
- free of a hidden graphics scene graph, source-image cache, backend selector, reader, worker, event loop, or widget framework.

T1609 found one internal allocation issue—boxing-heavy value-type equality during raster replacement—and corrected it with an internal typed comparison. That correction changed no exported signature and the 1.6 fingerprint remained unchanged.

## T1610 exit gate

The release-facing branch head containing this document, the public baseline, README/CHANGELOG/package metadata, and roadmap-status corrections must pass the complete PR Staging matrix:

```text
package candidate
Windows x64
Windows ARM64
Linux x64
Linux ARM64
macOS x64
macOS ARM64
```

Only after that exact head is green may T1611 promote the **unchanged implementation/API** to `1.6.0-rc.1`.

No merge, `v1.6.0` tag, GitHub Release, or NuGet publication belongs to T1610/T1611 automatic branch work; those remain explicit maintainer actions after stable-source qualification.
