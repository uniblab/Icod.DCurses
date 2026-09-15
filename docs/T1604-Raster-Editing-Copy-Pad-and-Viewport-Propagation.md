# T1604 — Raster Editing, Copy, Pad, and Viewport Propagation

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1604  
**Development identity:** `1.6.0-alpha.2`  
**AssemblyVersion:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** accepted

## Objective

T1604 carries retained raster state through the existing logical editing and rectangular-transfer machinery rather than creating raster-specific editing algorithms.

The tranche covers insert/delete cells and lines, explicit scrolling, subwindow projection, destructive and overlapping copy, raster-aware blank overlay, pad/viewports, clipping, same-session transfer, screen-resize preservation, and pre-mutation rejection of a known foreign-session raster reference when the destination is bound to a live `CursesSession`.

## RED evidence

The first test-only head exposed a harness compile mistake and was not accepted as RED. The corrected test-only RED head is:

```text
79152c0d6108a31ba38484f09bf25eadb6d94dd6
```

Workflow **#937 / 35016106603** built successfully with zero warnings/errors on Linux x64, then failed at test execution on the intended T1604 gaps. Linux x64 reported:

```text
11 failed
830 passed
841 total
```

The failures were retained-raster loss across editing/copy/pad paths plus the intentionally absent destination-session ownership seam. The package candidate passed.

## GREEN implementation

The minimal production candidate is:

```text
27cd4bd5f1be4ed1285e4524af0f8cf4fe8e5504
```

The exact qualified documentation/source head is:

```text
281b58e51041dc55793635a0aa1f164be0334873
```

It includes raster in `CursesLogicalCellState` snapshots, restores raster after ordinary-cell/metadata commit, preserves coordinate raster during valid wide-cell normalization, treats blank+raster as opaque during overlay, validates a complete transfer before destination mutation, binds session-created screens to their canonical raster owner, and preserves raster in the overlapping region of a preserving logical-screen resize. Pads and viewports inherit the corrected rectangular transfer path.

No public API was added; the T1603 `1.6.0-alpha.2` fingerprint remains the public baseline.

## Qualification evidence

Workflow **#939 / 35016800082** passed all seven jobs on the exact qualified head:

```text
Package candidate       success
Runtime Windows x64     success
Runtime Windows ARM64   success
Runtime Linux x64       success
Runtime Linux ARM64     success
Runtime macOS x64       success
Runtime macOS ARM64     success
```

Linux x64 reported a Staging build with **0 warnings / 0 errors** and **841/841 tests passing on each of net8.0, net9.0, and net10.0**.

## Handoff to T1605

T1605 now owns retained-raster panel composition: base-frame copying, panel overlay, `BlankCellsTransparent` behavior, topmost z-order resolution, visibility/move/dispose damage behavior, clipping, and panel resize preservation/discard. The compositor must use the same three-axis logical state model rather than treating raster content as metadata or text.
