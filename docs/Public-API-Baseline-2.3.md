# Icod.DCurses 2.3 Development Public API Baseline

**Candidate identity:** `2.3.0-alpha.1`  
**Status:** Reopened for the approved complete-frame extension; fingerprint observed in CI run 37133853737; final qualification pending. Earlier atlas-only qualification remains recorded in T2309.
**AssemblyVersion:** `2.0.0.0`  
**Direct production dependency:** `Icod.Terminal 1.25.0-alpha.2`

## Compiled candidate fingerprint

```text
105 exported types
839 canonical declared contract lines
sha256 b86ba658fd90d041eeccf5345ecc71a978bea785f0b2bbb9b8274091ccc4afc1
```

`Public-API-Fingerprint-2.3.json` is the candidate fingerprint. CI compiles and
compares the assembly on .NET 8, 9 and 10. The published 2.2 fingerprint remains
immutable at 100 exported types, 783 contract lines and SHA-256
`7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097`.

## Additive 2.3 surface

| Kind | Member |
| --- | --- |
| New type | `CursesRasterAtlas` |
| New type | `CursesRasterAtlasGeometry` |
| New type | `CursesRasterAtlasTileUpdate` |
| New type | `CursesRasterAtlasPresentationResult` |
| New type | `CursesRasterAtlasPresentationStatus` |
| Session method | `QueryRasterAtlasGeometryAsync(int, int, TimeSpan, CancellationToken)` |
| Session method | `CreateRasterAtlasAsync(TerminalRasterImage, int, int, CancellationToken)` |
| Session method | `RefreshRasterAsync(TerminalRasterImage, int, int, CursesRasterAtlasGeometry, CancellationToken)` |
| Window method | `WriteRasterAtlas(int, int, CursesRasterAtlas, CursesRectangle)` |

No published 2.2 type or member is removed or changed. The only new external public
type in a signature is the already-approved `TerminalRasterImage`; no Icod.TermInfo
type enters the DCurses public or direct dependency boundary.

## Contract summary

- Atlas axes are independently bounded to 1..256 cells. Pixel dimensions are positive
  and every derived product is checked.
- Creation requires exact image/grid divisibility and advisory room for one resource,
  one placement and two frames. It retains no initial source image.
- Projection validates the complete source/destination rectangle, owning session and
  current lifecycle before mutating retained cells. The window cursor is unchanged.
- One presentation accepts zero to 4,096 unique coordinates. Every replacement is an
  exact tile-sized RGB24 or RGBA32 image. Validation completes before Terminal output,
  and updates are applied in deterministic row-major order.
- Presentation copies front to back using Replace, applies every acknowledged region,
  selects the back frame last and swaps identities only after success.
- A controlled Terminal result preserves the known front identity. An exception after
  possible output marks `RequiresRecreation`, invalidates physical-screen certainty and
  rejects later cell/presentation use.
- Disposal is task-idempotent, participates in the session activity gate and releases
  the placeholder before the resource. The application still owns durable pixels and
  explicit recreation/fallback policy.

The [measurement report](Raster-Atlas-Measurement-2.3.md),
[sample README](../samples/Icod.DCurses.RasterAtlas.Sample/README.md) and
[development roadmap](../Icod.DCurses-2.3.0-Development-Roadmap.md) define the
acceptance evidence and non-goals.

## Complete-frame extension

The user-approved Terminal 1.25 fallback work reopens the fingerprint gate with one additive session method and no new public type. The compiled fingerprint above was observed on .NET 8/9/10 in CI run 37133853737. See the [implementation contract](superpowers/plans/2026-10-03-complete-frame-raster-fallback.md). Retained atlas identity and ownership remain unchanged.
