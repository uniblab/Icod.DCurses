# T2307 Raster-Atlas Hardening Gate

## Acceptance inventory

| Requirement | Evidence |
| --- | --- |
| inclusive 1..256 geometry axes and checked pixel products | `CursesRasterAtlasCapacityTests` |
| maximum 4,096 unique updates and over-capacity fail-before-read | `CursesRasterAtlasCapacityTests` |
| row/column bounds, duplicate coordinates and exact tile geometry | `CursesRasterAtlasCapacityTests` plus validation/transaction suites |
| work and allocation independent of atlas/world extent | `CursesRasterAtlasAllocationTests` |
| 1/4/16/64/121/256 RGB24 and RGBA32 operation evidence | `CursesRasterAtlasWorkloadTests` |
| no retained source image or map-sized cache | transaction reflection guard and allocation tests |
| package construction and fresh NuGet-only consumption | PR package candidate job |

The complete method and expected operation counts are in
[`Raster-Atlas-Measurement-2.3.md`](Raster-Atlas-Measurement-2.3.md).

## Gate state

Implementation is complete in source. Acceptance requires an unchanged exact branch
head to pass the package candidate plus all Windows, Linux and macOS x64/ARM64 runtime
jobs. Until that run is recorded here, T2307 is implemented but not accepted.

Live raster rendering is deliberately not claimed by this scripted gate. It is tested
separately by the application and release-candidate checklists.
