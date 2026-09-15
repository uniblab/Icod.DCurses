# T1605 — Panel Raster Composition, Transparency, and Resize Coherence

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1605  
**Development identity:** `1.6.0-alpha.2`  
**AssemblyVersion:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** accepted

## Objective

T1605 makes retained raster state a first-class third axis of panel composition beside ordinary cells and semantic metadata while preserving the existing panel ownership, clipping, z-order, incremental-damage, and resize models.

The tranche establishes that:

- base-screen raster state survives full panel composition;
- opaque panel coordinates replace covered base raster state;
- `BlankCellsTransparent` remains transparent only when the ordinary panel cell is blank **and** the coordinate carries no retained raster cell;
- semantic metadata alone does not make a blank coordinate visually opaque;
- a blank+raster coordinate is visually present;
- topmost visible panel raster wins deterministically;
- move, hide, show, dispose, reorder, and resize invalidate and recompose the correct retained coordinates;
- panel raster mutations participate in incremental change tracking;
- surviving upper-left raster state is preserved across panel resize while out-of-bounds state is discarded; and
- destination clipping never destroys raster state retained by the panel itself.

## RED evidence

The test-only RED head was:

```text
f8a19117a7767499858320527324c17512f3a75b
```

Workflow **#941 / 35017402849** proved the intended missing behavior. The package candidate passed and the complete solution built with zero warnings and zero errors. On Linux x64, each of `net8.0`, `net9.0`, and `net10.0` reported:

```text
10 failed
842 passed
852 total
```

All failures were the new `CursesPanelRasterCompositionTests`; existing behavior remained green.

A later user package-tag-only commit changed `Icod.DCurses.csproj` without changing the T1605 product behavior under test. Those package tags were preserved unchanged through the GREEN implementation.

## GREEN implementation

The production changes were intentionally confined to the existing panel-composition machinery:

- `src/Internal/CursesPanelCompositor.cs` now copies and resolves retained raster state with cells and metadata, and its blank-cell transparency decision includes raster presence;
- `src/Internal/CursesPanelCompositionState.cs` now compares and applies the raster axis during incremental recomposition and uses the same raster-aware transparency rule when identifying the topmost contributing producer; and
- existing T1604 `CursesScreen.Resize(..., preserveContents: true)` propagation supplies panel resize preservation through `CursesPanelSurface` without a second raster-specific resize engine.

The principal GREEN commits were:

```text
0705475068348fe9fda005d3c61b20bf7b578187
8af83db9dde3f256063031bd462b5352f9b651e2
```

An intermediate compile check exposed a narrow internal-type mismatch between `CursesRasterCellReference` and the public nullable `CursesRasterCell` value expected by `CursesLogicalCellState`. The correction was limited to using `.Cell` at the two wrapper boundaries:

```text
23f0aeeffea1e5451b65beac4cfeb35963a36d9a
aee6ecb51a77b5d7d81b92bc733c3cc9152d703c
```

No public API was added or changed by T1605, so the accepted T1603/T1604 `1.6.0-alpha.2` public fingerprint remains the current public baseline.

## Acceptance evidence

Exact accepted code head:

```text
aee6ecb51a77b5d7d81b92bc733c3cc9152d703c
```

Workflow **#946 / 35029247284** passed all seven jobs:

- Package candidate — success
- Runtime Windows x64 — success
- Runtime Windows ARM64 — success
- Runtime Linux x64 — success
- Runtime Linux ARM64 — success
- Runtime macOS x64 — success
- Runtime macOS ARM64 — success

Linux x64 recorded:

```text
Build succeeded.
0 Warning(s)
0 Error(s)

net8.0:  852 passed / 852 total
net9.0:  852 passed / 852 total
net10.0: 852 passed / 852 total
```

T1605 is therefore accepted. T1606 may proceed to Terminal 1.15 placeholder refresh integration and retained physical/rendition tracking.