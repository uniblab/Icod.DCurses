# T2208 Release Candidate Qualification

**Date:** 2026-09-25\
**Status:** accepted, seven of seven Staging PR jobs successful\
**RC validation head:** `74f10d0427ac3e4add237a7802fdd73d7a31495a`\
**PR workflow:** [36100234859](https://github.com/uniblab/Icod.DCurses/actions/runs/36100234859)

## Candidate identity and boundary

`Version` and `PackageVersion` were `2.2.0-rc.1` at the exact validation head.
`AssemblyVersion` remained `2.0.0.0`; the targets remained .NET 8, 9 and 10;
and the sole direct production dependency remained `Icod.Terminal 1.18.0`.
The frozen 2.2 public API remained 100 exported types, 783 canonical contract
lines and SHA-256
`7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097`.
No production behavior or public API changed while promoting the accepted
T2207 source to RC identity.

PR validation ran Staging only: six Windows, Linux and macOS x64/ARM64 runtime
jobs plus one package job. Every runtime job passed 1,286 tests on each of .NET
8, 9 and 10 with zero failures and zero skips. Every build reported zero
warnings and zero errors. The macOS x64 target-framework tests remained
sequential as agreed. The package job also ran fresh package-only consumers on
all three target frameworks and a live Linux pseudo-terminal refresh.

## Package provenance

Staging artifact `10848363012` has ZIP SHA-256
`b49280d50cc7f08f6bba12e309f6a5ecb864175432d62bde5146369803d0d986`.
Direct inspection found exactly these candidates:

| Package | SHA-256 |
| --- | --- |
| `Icod.DCurses.2.2.0-rc.1.nupkg` | `a763ad45482087959176b804e97457ba8e775d5fe0bca39b4f0bf0e445b65430` |
| `Icod.DCurses.2.2.0-rc.1.snupkg` | `fbe83cd44421b79f097436cdecf5d2afe17cdb9f4e4336f088ac4d8e735c5546` |

The nuspec records version `2.2.0-rc.1`, repository commit
`74f10d0427ac3e4add237a7802fdd73d7a31495a`, all three target frameworks and
only `Icod.Terminal 1.18.0` in each dependency group. The RC was not published.

## Disposition

The RC gate is accepted. T2208 may advance the unchanged frozen source to an
unpublished `2.2.0` stable-source identity. That identity requires its own
exact-head seven-job Staging qualification and package-provenance inspection.
PR #34 remains draft and unmerged. Release validation remains reserved for a
separately authorized push to `main`; no tag, GitHub Release or NuGet
publication has occurred.
