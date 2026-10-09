# Icod.DCurses 2.3 Raster-Atlas Measurement

## Method

The T2307 gate measures deterministic work rather than runner-dependent elapsed time.
Each scripted presentation runs through the public `CursesRasterAtlas.PresentAsync`
boundary and the real Icod.Terminal 1.24 transaction implementation. The transport
acknowledges every operation and records the focused output sequence.

The matrix covers 1, 4, 16, 64, 121 and 256 changed cells for both RGB24 and RGBA32
replacement images. For `N` changes, the required operation count is exactly:

```text
1 Replace composition + N region updates + 1 frame selection = N + 2
```

The tests also inspect every region operation so RGB24 (`f=24`) and RGBA32 (`f=32`)
remain separate Terminal inputs. A presentation never walks an application world or
map: its validation, sorting and protocol work are proportional to the caller's
bounded changed-tile list.

Allocation measurements warm the validation path and take the minimum of eight
same-thread samples. The same 64 updates are measured against an 8x8 atlas and a
256x256 atlas. Both must remain below 32 KiB and within 1 KiB of one another. This is
a regression ceiling, not an allocation promise for every runtime implementation.

## Deterministic workload table

| Changed tiles | Compose | Region updates | Select | Total operations |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 1 | 1 | 3 |
| 4 | 1 | 4 | 1 | 6 |
| 16 | 1 | 16 | 1 | 18 |
| 64 | 1 | 64 | 1 | 66 |
| 121 | 1 | 121 | 1 | 123 |
| 256 | 1 | 256 | 1 | 258 |

Every row runs once with RGB24 updates and once with RGBA32 updates on .NET 8, 9
and 10 in each runtime lane. The PR package job separately packs and executes the
fresh NuGet-only consumer.

Exact source head `751890de112be2e8d0be1757e1fdc6fd47b14c8d` passed
[pull-request workflow 1296](https://github.com/uniblab/Icod.DCurses/actions/runs/37106805634), run `37106805634`. The Staging artifact
digest is `sha256:22a72c3be6ca810b285d90d10e473c0d89d3fc65ac0d9e7145c20e94e2804d81`.
Its package hashes are:

- `Icod.DCurses.2.3.0-alpha.1.nupkg`:
  `b2f864305446e02f13d760bbf6ab2d22f9e1f7b6bead133a7237af49769e827c`;
- `Icod.DCurses.2.3.0-alpha.1.snupkg`:
  `96d48e775f0f05c51188441fd4730d020a7c846bdd3d49624c052044adbe4c62`.

## Decision

The evidence does not justify adding Terminal batching, Indexed8 region replacement,
a DCurses source-image cache or map-sized retained storage to 2.3. The fixed two-
operation transaction envelope is visible, but the changed-tile work is linear,
bounded and format-correct. A future Terminal proposal requires application evidence
showing that those individual acknowledged region operations are the limiting cost.

No elapsed-time threshold is portable enough to be a release condition. Live terminal
latency and display behavior remain part of the T2308/T2310 manual acceptance gate.
