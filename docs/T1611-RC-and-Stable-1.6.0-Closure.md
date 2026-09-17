# T1611 — RC and Stable-Source 1.6.0 Closure

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1611  
**Published predecessor:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Frozen API:** 75 exported types / 559 canonical lines  
**API SHA-256:** `266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458`  
**Accepted T1610 head:** `dbb663406cfeb62ccdd4181fe34b3264a6e0462a`  
**T1610 workflow:** #999 / `35155346200`  
**Accepted RC head:** `47728ad870205c89bc1d7c0014667774ea9960d9`  
**RC workflow:** #1002 / `35158260589`  
**Accepted stable-source implementation/release-facing head:** `cb791454d79ade67d13988625d2f76d15b9cfe82`  
**Stable-source workflow:** #1015 / `35171774174`  
**Stable-source identity:** `1.6.0`  
**Status:** publication-ready after the documentation-only closure-record head containing this file passes the same exact-head PR matrix

---

## Objective

T1611 promotes the implementation/API accepted by T1610 unchanged through `1.6.0-rc.1` and then to stable-source `1.6.0`. No feature/API change is accepted in this tranche. Any implementation/API correction returns to T1610 for renewed regret review.

The only intended changes after T1610 are release identity and release-facing documentation/evidence. The final release audit also allowed sample/documentation completeness work that changes no library source or exported contract.

## T1610 qualification

The final pre-RC release-regret head was:

```text
dbb663406cfeb62ccdd4181fe34b3264a6e0462a
```

Workflow #999 / `35155346200` passed all seven PR Staging jobs:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```

Linux x64 built with zero warnings/errors and passed 899/899 tests on each of `net8.0`, `net9.0`, and `net10.0`.

## RC qualification

The same implementation/API was promoted to:

```text
Version         1.6.0-rc.1
PackageVersion  1.6.0-rc.1
AssemblyVersion 1.0.0.0
```

Exact RC head:

```text
47728ad870205c89bc1d7c0014667774ea9960d9
```

Workflow #1002 / `35158260589` passed all seven jobs. Linux x64 again built with zero warnings/errors and passed 899/899 tests on each target framework.

No `src/` or public-API change was made between the T1610 qualification and RC qualification.

## Stable-source promotion

After the green RC qualification, release identity was promoted to:

```text
Version         1.6.0
PackageVersion  1.6.0
AssemblyVersion 1.0.0.0
```

Publication-facing README, CHANGELOG, API fingerprint metadata, and the human-readable 1.6 API baseline were aligned to stable-source state. The exported API fingerprint remained unchanged.

One development-era machine guard initially required `docs/Public-API-Fingerprint-1.6.json` to retain the historical `1.6.0-alpha.2` / `development` label. Stable-source correctly changes that artifact to `1.6.0` / `stable-source`. The guard was narrowed to what it is intended to freeze: schema, API hash, exported type count, contract line count, and exact exported-type inventory, while separately asserting the stable-source release/status metadata. No production/API change was involved.

## Final sample/readme audit

The pre-merge release audit compared `Icod.DCurses.MixedMedia.Sample` to the approved T1608 application goal. The accepted sample already demonstrated retained text, raster placeholders, pads/viewports, panels, clipping, interaction geometry, and serialized refresh, but did not yet demonstrate semantic hyperlink metadata in the mixed-media composition.

A test-first release-audit expansion added a retained `CursesHyperlink` through `WriteWithMetadata` in the same pannable pad, plus a visible fallback message when raster ownership is unavailable. The final sample therefore demonstrates all three retained presentation axes together:

```text
ordinary text/cell state
terminal-independent semantic hyperlink metadata
session-bound raster placeholder state
```

The sample continues presenting text/metadata/panel content when raster ownership is unavailable and never infers or silently switches a graphics backend. This sample/documentation work changes no production source or public API.

The root/package `README.md` and `samples/README.md` were aligned to the stable `1.6.0` release identity and final sample behavior before the stable-source qualification.

## Stable-source qualification

Exact stable-source implementation/release-facing head:

```text
cb791454d79ade67d13988625d2f76d15b9cfe82
```

Workflow #1015 / `35171774174` passed all seven jobs:

```text
Package candidate       success
Runtime Windows x64     success
Runtime Windows ARM64   success
Runtime Linux x64       success
Runtime Linux ARM64     success
Runtime macOS x64       success
Runtime macOS ARM64     success
```

Linux x64 built with:

```text
0 Warning(s)
0 Error(s)
```

and passed:

```text
net8.0   899 / 899
net9.0   899 / 899
net10.0  899 / 899
```

The package candidate validated the stable `1.6.0` package and isolated package-only consumers on all target frameworks. No production/API change occurred after the T1610 freeze.

## Frozen stable contract

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The release remains additive over the published 1.5 contract:

```text
1.5: 69 exported types / 525 lines
1.6: 75 exported types / 559 lines
Delta: +6 exported types / +34 lines
```

## Stable package graph

```text
Icod.DCurses 1.6.0
├── Icod.Terminal 1.15.0
└── Icod.TermInfo 1.14.0
```

Target frameworks remain:

```text
net8.0
net9.0
net10.0
```

The package remains LGPL-3.0-or-later and requires license acceptance. Symbols, source-link/repository metadata, README, icon/toolchain image, XML documentation, and license are part of the established package validation contract.

## Publication boundary

T1611 branch work deliberately does **not** perform these maintainer actions:

1. merge PR #31 into `main`;
2. post-merge Release workflow acceptance on `main`;
3. create/push tag `v1.6.0`;
4. create the GitHub Release;
5. publish `Icod.DCurses 1.6.0` to NuGet.

Those actions are intentionally separate maintainer actions. The intended publication sequence is:

```text
merge PR #31 into main
-> require post-merge main/Release workflow success
-> create/push v1.6.0 tag from the accepted main commit
-> require tag/release validation if configured
-> create GitHub Release using the 1.6 changelog/release notes
-> publish Icod.DCurses 1.6.0 and symbols to NuGet
-> verify the NuGet package page and a fresh public-feed consumer
```

## Final acceptance record

The substantive stable-source release head `cb791454d79ade67d13988625d2f76d15b9cfe82` is accepted by workflow #1015 / `35171774174`, seven of seven jobs green. The commit containing this final closure record is documentation-only; its exact-head PR workflow is the final repository verification before maintainer merge. Any source, package, sample, or release-facing document change after that verification requires requalification.
