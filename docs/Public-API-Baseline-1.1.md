# Icod.DCurses 1.1 Public API Baseline

**Release line:** `1.1.0`  
**Pre-RC accepted checkpoint:** `1.1.0-alpha.8`  
**Stable compatibility floor:** `1.0.0`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`

## Purpose

This document is the human-readable companion to `docs/Public-API-Fingerprint-1.1.json`.

Version 1.1 is an additive semantic-content release over the published 1.0 contract. It introduces retained hyperlink meaning without transferring raw OSC 8 ownership from `Icod.Terminal` into DCurses.

## Canonical compiled surface

```text
sha256:            21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
exported types:    45
contract lines:   337
```

The compiler-derived fingerprint is identical across `net8.0`, `net9.0`, and `net10.0` and covers public type/member signatures, enum values, generic constraints, parameter order/ref-kind/default values, accessor visibility, public fields/constants, and compiled nullable metadata.

The stable 1.0 floor remains independently frozen at 43 exported types / 309 lines. T1108 does not rewrite or weaken that baseline.

## Intentional new types

Exactly two exported types are added by 1.1:

```text
Icod.DCurses.CursesHyperlink
Icod.DCurses.CursesCellMetadata
```

No additional `Icod.Terminal` or `Icod.TermInfo` type enters the public DCurses signature surface.

## Semantic API

The accepted logical semantic surface consists of:

```text
CursesVirtualScreen.GetMetadata(...)
CursesVirtualScreen.SetMetadata(...)

CursesWindow.GetMetadata(...)
CursesWindow.SetMetadata(...)
CursesWindow.WriteWithMetadata(string, CursesCellMetadata)
CursesWindow.Write(string, CursesStyle, CursesCellMetadata)
CursesWindow.WriteCell(CursesCell, CursesCellMetadata)
```

The two-argument convenience method is deliberately named `WriteWithMetadata(...)` rather than overloading `Write(string, ...)` with `CursesCellMetadata`.

Stable 1.0 already exposes `Write(string, CursesStyle)`. Adding `Write(string, CursesCellMetadata)` would make previously valid source such as:

```csharp
window.Write( "text", default );
```

ambiguous when recompiled against 1.1. The pre-RC regret gate therefore uses a distinct convenience name and preserves the stable source form.

The three-argument `Write(string, CursesStyle, CursesCellMetadata)` remains unambiguous and provides explicit style plus semantic metadata in one operation.

## Metadata extensibility and nullability

`CursesCellMetadata` is an immutable semantic container. Its 1.1 constructor requires a non-null `CursesHyperlink`, because hyperlink is the only semantic kind constructible in this release.

The `Hyperlink` property is nevertheless declared `CursesHyperlink?`. This is intentional forward compatibility: a later additive semantic kind can be represented without forcing every metadata object to contain a hyperlink and without weakening a previously published non-null property contract.

Every `CursesCellMetadata` instance constructible through the 1.1 constructor still has a hyperlink at runtime.

## Hyperlink value contract

`CursesHyperlink` remains a DCurses-owned immutable record. It stores one absolute encoded ASCII target plus an optional identifier. Validation mirrors the reviewed Terminal hyperlink constraints without exposing Terminal hyperlink lease or protocol types.

DCurses does not dereference, activate, fetch, or navigate to the URI. Terminal remains authoritative for physical OSC 8 framing and live protocol state.

## Retained semantic behavior

The accepted public surface is supported by the 1.1 retained behavior established in T1102–T1107:

- row-sparse semantic storage rather than a permanent metadata field in every cell;
- coherent leader/continuation metadata for two-column text elements;
- ordinary replacement clears overwritten semantics;
- semantic-only changes participate in damage/change tracking;
- semantic metadata moves through editing, scrolling, copy, pads, viewports, and preserved resize;
- physical retained state compares semantic metadata independently from glyph/style state;
- equivalent adjacent links coalesce into bounded Terminal hyperlink writes;
- uncertain output invalidates retained physical knowledge;
- non-cancellation bounded-hyperlink failure fails later application text closed until disposal;
- synchronized-output cleanup remains retryable through the retained Terminal lease.

## Compatibility decision

The 1.1 contract is additive over stable 1.0 in exported type/member availability while also preserving source compatibility for the reviewed `default`-style write case.

`AssemblyVersion` remains `1.0.0.0` under the compatible additive 1.x policy.

No public raw OSC/CSI/DCS/APC writer, refresh-engine implementation type, sparse-plane type, physical-screen state, Terminal hyperlink lease, output shim, diagnostics record, or test seam is exported.

## Machine guards

- `PublicApiFingerprintTests` regenerates and verifies the complete compiled 1.1 surface.
- `PublicStableApiBaselineTests` continues to protect the published 1.0/0.9 freeze.
- `PublicDependencyBoundaryTests` permits only the previously approved five Terminal/TermInfo type definitions.
- `PublicSemanticMetadataRegretTests` freezes the nullable-property/non-null-constructor decision and the source-compatible semantic convenience name.
- package-only validation compiles and executes the 1.1 semantic value/write/inspection/mutation surface from the generated NuGet package.
- package verification requires XML documentation, portable symbols, exact dependencies, README/license/icon/repository metadata, and clean framework groups.

The accepted fingerprint may change after this point only for a documented release-blocking correction before RC; otherwise T1109 promotes this contract unchanged.
