# T2208 Stable-Source Release Gate

**Date:** 2026-09-25\
**Status:** accepted, seven of seven Staging PR jobs successful\
**Executable stable-source head:** `42d08b73e4382a9aacf26ec94875ff572ded5872`\
**PR workflow:** [36100907303](https://github.com/uniblab/Icod.DCurses/actions/runs/36100907303)

## Candidate identity and boundary

`Version` and `PackageVersion` are `2.2.0` on the open draft PR. This is an
unpublished stable-source candidate, not a published package.
`AssemblyVersion` remains `2.0.0.0`; the targets remain .NET 8, 9 and 10; and
the sole direct production dependency remains `Icod.Terminal 1.18.0`. The
frozen 2.2 public API remains 100 exported types, 783 canonical contract lines
and SHA-256
`7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097`.
No production behavior or public API changed between the qualified RC and this
stable-source candidate.

The [RC gate](T2208-RC-Qualification.md) records the preceding exact-head
seven-job Staging matrix and RC package provenance. The PR workflow runs
Staging only. Release validation remains pending a separately authorized merge
and push to `main`.

## Candidate verification

All six Windows, Linux and macOS x64/ARM64 runtime jobs and the Staging package
job passed. Every runtime job passed 1,286 tests on each of .NET 8, 9 and 10
with zero failures and zero skips. Every build reported zero warnings and zero
errors. The macOS x64 target-framework tests remained sequential as agreed.
The package job ran fresh package-only consumers on every target framework and
a live Linux pseudo-terminal refresh.

Staging artifact `10848338924` has ZIP SHA-256
`5a11de0944f6c997ecc616a2079a5d063492311dff022ac41f809c1d152b412e`.
Direct inspection found exactly these candidates:

| Package | SHA-256 |
| --- | --- |
| `Icod.DCurses.2.2.0.nupkg` | `534832401526625a029142f4775773af059696345d8dc513986b840f362c0175` |
| `Icod.DCurses.2.2.0.snupkg` | `42e6144be487d4d534f6c48b1b5da1028180dd630a339ef7c6548efaa76b9fdf` |

The nuspec records version `2.2.0`, repository commit
`42d08b73e4382a9aacf26ec94875ff572ded5872`, all three target frameworks and
only `Icod.Terminal 1.18.0` in each dependency group. The maintainer previously
confirmed both live editor and roguelike samples at the accepted T2206 gate.

## Maintainer handoff

T2208 and planned 2.2 development are complete. Keep PR #34 open, draft and
unmerged until separately reviewed and authorized. A future push to `main`
must pass its Release-only workflow before any tag, GitHub Release or NuGet
publication is chosen. None of those external release actions has occurred.
