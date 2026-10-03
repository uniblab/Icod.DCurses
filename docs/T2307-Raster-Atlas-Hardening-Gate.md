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

T2307 is accepted at exact source head `751890de112be2e8d0be1757e1fdc6fd47b14c8d`
by [pull-request workflow 1296](https://github.com/uniblab/Icod.DCurses/actions/runs/37106805634), run `37106805634`.
The package candidate and all six Windows, Linux and macOS x64/ARM64 runtime jobs
are green. The first macOS x64 attempt passed net8.0 and net10.0 but one net9.0
256-tile RGB24 workload exhausted the harness's 15-second write wait; the unchanged
job rerun passed all three frameworks. A later documentation-head run exhausted the
same wait in net8.0 on a 16-tile case while its net9.0/net10.0 and all other runtime
jobs passed. Because the failing framework and workload moved while product source
did not, this is runner scheduling against an undersized test-harness deadline, not
workload cost or a product defect. The scripted-I/O wait is therefore one minute;
the deterministic operation-count assertions remain unchanged.

The following exact-head run exposed a separate setup race on macOS ARM64: the
fixture delivered the Kitty acknowledgement and Primary-DA barrier as independent
input chunks, so scheduler timing could let the barrier complete verification before
the acknowledgement was recorded. The fixture now follows Terminal 1.24's public
verification test pattern and delivers the ordered Kitty-plus-DA frames in one input
chunk. Product code and workload assertions remain unchanged.

The Staging artifact `icod-dcurses-pr-packages-Staging` has GitHub digest
`sha256:22a72c3be6ca810b285d90d10e473c0d89d3fc65ac0d9e7145c20e94e2804d81`.
Its package hashes are:

- `Icod.DCurses.2.3.0-alpha.1.nupkg`:
  `b2f864305446e02f13d760bbf6ab2d22f9e1f7b6bead133a7237af49769e827c`;
- `Icod.DCurses.2.3.0-alpha.1.snupkg`:
  `96d48e775f0f05c51188441fd4730d020a7c846bdd3d49624c052044adbe4c62`.

Live raster rendering is deliberately not claimed by this scripted gate. It remains
part of the T2308/T2310 application and release-candidate checklists.
